using Microsoft.AspNetCore.Builder;
using Workleap.DomainEventPropagation.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services
    .AddEventPropagationSubscriber()
    .AddDomainEventHandler<DummyDomainEventHandler>();

var app = builder.Build();

app.UseRouting();

app.AddEventPropagationEndpoints();

app.Run();

// For context: https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0#basic-tests-with-the-default-webapplicationfactory
#pragma warning disable CA1050 // Declare types in namespaces
public partial class Program
#pragma warning restore CA1050 // Declare types in namespaces
{
}