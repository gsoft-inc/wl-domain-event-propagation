using System.Net;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Workleap.DomainEventPropagation.Extensions;
using Secret = Duende.IdentityServer.Models.Secret;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class IntegrationTests
{
    private const string Audience = "eventPropagation";

    private readonly ITestOutputHelper _testOutputHelper;

    public IntegrationTests(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Real_Client_Server_Communication()
    {
        // Define some OAuth 2.0 scopes for fictional invoices access management
        var identityApiScopes = new ApiScope[] { };

        // Define the protected resources, here an invoice API (represents something we want to communicate with)
        var identityApiResources = new[]
        {
            new ApiResource(Audience, "Event Propagation Endpoint") { Scopes = { $"{Audience}:read", $"{Audience}:pay" } },
        };

        // Define the OAuth 2.0 clients and the scopes that can be granted
        var identityOAuthClients = new[]
        {
            // This client only allows to read invoices
            new Client
            {
                ClientId = "invoices_read_client",
                ClientSecrets = new[] { new Secret("invoices_read_client_secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = { $"{Audience}:read" },
            },
        };

        // Build a real but in-memory ASP.NET Core test server that will both act as identity provider (using IdentityServer) and as the protected API that we'll try to access using a authenticated HttpClient
        var webAppBuilder = WebApplication.CreateBuilder();
        webAppBuilder.WebHost.UseTestServer(x => x.BaseAddress = new Uri("https://identity.local", UriKind.Absolute));

        webAppBuilder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string>()
            {
                ["AzureAd:Instance"] = "https://identity.local",
                ["AzureAd:TenantId"] = "50103d8b-4c52-4c40-9f78-1461000290d7",
                ["AzureAd:ClientId"] = "cfde6263-aa07-4ae0-a054-66f536cfdc7b",
                ["AzureAd:Audience"] = "cfde6263-aa07-4ae0-a054-66f536cfdc7b",
            });

        // Here begins services registrations in the dependency injection container
        webAppBuilder.Services.AddLogging(x =>
            x.SetMinimumLevel(LogLevel.Debug)
                .ClearProviders());
                // .AddProvider(new XunitLoggerProvider(this._testOutputHelper)));
        webAppBuilder.Services.AddSingleton<TestServer>(x => (TestServer)x.GetRequiredService<IServer>());
        webAppBuilder.Services.AddSingleton<TestServerHandler>();
        webAppBuilder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

        webAppBuilder.Services.AddIdentityServer()
            .AddInMemoryClients(identityOAuthClients)
            .AddInMemoryApiResources(identityApiResources)
            .AddInMemoryApiScopes(identityApiScopes);
        // Create the authorization policy that will be used to protect our invoices endpoints
        webAppBuilder.Services.AddAuthentication();
        // webAppBuilder.Services.AddOptions<JwtBearerOptions>(ClientCredentialsDefaults.AuthenticationScheme).Configure<TestServerHandler>((options, testServerClient) =>
        // {
        //     options.Audience = Audience;
        //     options.Authority = "https://identity.local";
        //     options.BackchannelHttpHandler = testServerClient;
        // });

        // Change the primary HTTP message handler of this library to communicate with this in-memory test server without accessing the network
        webAppBuilder.Services.AddHttpClient("TestHttpClient")
            .ConfigurePrimaryHttpMessageHandler(x => x.GetRequiredService<TestServer>().CreateHandler());

        // Here begins ASP.NET Core middleware pipelines registration
        var webApp = webAppBuilder.Build();

        webApp.UseIdentityServer();
        webApp.UseAuthentication();
        webApp.UseAuthorization();

        webApp.UseEndpoints(builder =>
        {
            builder.AddEventPropagationEndpoint()
                .WithAuthorization();
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            // Start the web app without blocking, the cancellation token source will make sure it will be shutdown if something wrong happens
            _ = webApp.RunAsync(cts.Token);

            var invoicesReadHttpClient = webApp.Services.GetRequiredService<IHttpClientFactory>().CreateClient("invoices_read_http_client");

            // Consuming an anonymous/public endpoint should work
            var publicEndpointResponse = await invoicesReadHttpClient.GetStringAsync("https://invoice-app.local/public", cts.Token);
            Assert.Equal("This endpoint is public", publicEndpointResponse);

            // Reading invoices should be successful because we're authenticated with a JWT that has the "invoices" audience and "invoices.read" scope
            var readInvoicesResponse = await invoicesReadHttpClient.GetStringAsync("https://invoice-app.local/read-invoices", cts.Token);
            Assert.Equal("This protected endpoint is for reading invoices", readInvoicesResponse);

            // Paying invoices should throw a forbidden HTTP exception because the JWT doesn't have the "invoices.pay" scope
            var forbiddenException = await Assert.ThrowsAsync<HttpRequestException>(() => invoicesReadHttpClient.GetStringAsync("https://invoice-app.local/pay-invoices", cts.Token));
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenException.StatusCode);

            // We require JWT-authenticated requests to be sent over HTTPS
            // var unsecuredException = await Assert.ThrowsAsync<ClientCredentialsException>(() => invoicesReadHttpClient.GetStringAsync("http://invoice-app.local/public", cts.Token));
            // Assert.Equal("Due to security concerns, authenticated requests must be sent over HTTPS", unsecuredException.Message);
        }
        finally
        {
            // Shut down the web app
            cts.Cancel();
        }
    }

    private sealed class TestServerHandler : DelegatingHandler
    {
        private readonly TestServer _testServer;
        private HttpClient? _testServerClient;

        public TestServerHandler(TestServer testServer)
        {
            this._testServer = testServer;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this._testServerClient ??= this._testServer.CreateClient();

            // Request has to be cloned since it has already gone through the httpclient wrapping this handler even though the request hasn't been sent yet.
            var cloneRequest = await CloneHttpRequest(request, cancellationToken);

            return await this._testServerClient.SendAsync(cloneRequest, cancellationToken);
        }

        private static async Task<HttpRequestMessage> CloneHttpRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cloneRequest = new HttpRequestMessage(request.Method, request.RequestUri);
            cloneRequest.Version = request.Version;

            foreach (var (key, value) in request.Headers)
            {
                cloneRequest.Headers.TryAddWithoutValidation(key, value);
            }

            foreach (var (key, value) in request.Options)
            {
                cloneRequest.Options.TryAdd(key, value);
            }

            if (request.Content != null)
            {
                var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                cloneRequest.Content = new ByteArrayContent(contentBytes);

                foreach (var (key, value) in request.Content.Headers)
                {
                    cloneRequest.Content.Headers.TryAddWithoutValidation(key, value);
                }
            }

            return cloneRequest;
        }
    }
}