using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Workleap.DomainEventPropagation.Tests.Helpers;

internal static class ResultExtensions
{
    /// <remarks>
    /// <para>
    /// This utility method is convenient for .NET 6 applications that would unit test
    /// minimal APIs and dealing with IResult. 
    /// </para>
    /// <para>
    /// All classes inheriting from IResult are unfortunately internal (e.g. OkObjectResult) and 
    /// we can't get the underlying inherited type. <br/>
    /// From .NET 7 and forward, we could instead leverage TypedResults and public types and conditionally use this method when .NET 6 is used <br/>
    /// More over on https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.typedresults?view=aspnetcore-7.0 <br/>
    /// </para>
    /// <para>
    /// This was heavily inspired from Shawn Wildermuth's video:
    /// https://www.youtube.com/watch?v=BmwJkoPnF24&t=267s
    /// </para>
    /// </remarks>
    internal static async Task<(T? Value, HttpStatusCode StatusCode)> GetResponseAsync<T>(this IResult result)
    {
        var tempContext = new DefaultHttpContext
        {
            // RequestServices needs to be set so the IResult implementation can log
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response =
            {
                // The default response body is Stream.Null which would throw otherwise
                Body = new MemoryStream(),
            }
        };

        await result.ExecuteAsync(tempContext).ConfigureAwait(false);

        var status = (HttpStatusCode)tempContext.Response.StatusCode;

        tempContext.Response.Body.Position = 0;

        using var streamReader = new StreamReader(tempContext.Response.Body);

        var body = await streamReader.ReadToEndAsync().ConfigureAwait(false);

        if (body is not { Length: > 0 })
        {
            return (default, status);
        }

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        return (JsonSerializer.Deserialize<T>(body, jsonOptions), status);
    }
}