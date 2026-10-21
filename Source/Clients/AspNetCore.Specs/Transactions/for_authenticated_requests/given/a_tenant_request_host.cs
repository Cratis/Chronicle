// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_authenticated_requests.given;

public class a_tenant_request_host : Specification
{
    protected readonly ConcurrentQueue<string> _resolutions = new();
    protected readonly ConcurrentQueue<string> _commits = new();
    protected readonly ConcurrentQueue<string> _endpointNamespaces = new();
    protected readonly ConcurrentQueue<IUnitOfWork> _units = new();
    protected readonly ConcurrentQueue<bool> _featuresAfterRequest = new();
    protected ICausationManager _causation;
    protected WebApplication _app;
    HttpClient _http;

    protected async Task StartHost(bool automatic = true)
    {
        var accessor = new HttpContextAccessor();
        var resolver = Substitute.For<IEventStoreNamespaceResolver>();
        resolver.Resolve().Returns(_ =>
        {
            var user = accessor.HttpContext!.User;
            if (user.Identity?.IsAuthenticated != true) throw new InvalidOperationException("Namespace resolution requires an authenticated user.");
            var tenant = user.FindFirst("tenant")!.Value;
            _resolutions.Enqueue(tenant);
            return (EventStoreNamespaceName)tenant;
        });
        var client = Substitute.For<IChronicleClient>();
        _causation = Substitute.For<ICausationManager>();
        client.CausationManager.Returns(_causation);
        var defaultStore = CreateStore(EventStoreNamespaceName.Default);
        client.GetEventStore(Arg.Any<EventStoreName>(), null).Returns(defaultStore);
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call => CreateStore(call.Arg<EventStoreNamespaceName>()));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCratisChronicle(options =>
        {
            options.EventStore = "store";
            options.UseAutomaticMiddleware = automatic;
        });
        builder.Services.AddSingleton<IHttpContextAccessor>(accessor);
        builder.Services.AddSingleton(resolver);
        builder.Services.AddSingleton(client);
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.OccurrenceHandlers.Returns([]);
        builder.Services.AddSingleton(artifacts);
        builder.Services.AddAuthentication("tenant").AddScheme<AuthenticationSchemeOptions, tenant_authentication_handler>("tenant", _ => { });
        builder.Services.AddAuthorization();
        _app = builder.Build();
        _app.UseRouting();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.Use(async (context, next) =>
        {
            await next(context);
            _featuresAfterRequest.Enqueue(context.Features.Get<IUnitOfWorkCompletionFeature>() is not null);
        });
        _app.UseCratisChronicle();
        _app.UseCratisChronicle();
        _app.MapGet("/write", (HttpContext context) =>
        {
            var store = context.RequestServices.GetRequiredService<IEventStore>();
            _endpointNamespaces.Enqueue(store.Namespace.Value);
            var unit = context.RequestServices.GetRequiredService<IUnitOfWorkManager>().Current;
            _units.Enqueue(unit);
            unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
            return store.Namespace.Value;
        }).RequireAuthorization();
        _app.MapGet("/empty", () => "empty");
        _app.MapGet("/failure", (HttpContext context) =>
        {
            var unit = context.RequestServices.GetRequiredService<IUnitOfWorkManager>().Current;
            _units.Enqueue(unit);
            unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }).RequireAuthorization();
        await _app.StartAsync();
        _http = _app.GetTestClient();
    }

    protected async Task<HttpStatusCode> Send(string path, string? tenant = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (tenant is not null) request.Headers.Add("X-Tenant", tenant);
        using var response = await _http.SendAsync(request);
        return response.StatusCode;
    }

    async Task Destroy()
    {
        _http?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
    }

    IEventStore CreateStore(EventStoreNamespaceName tenant)
    {
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns(tenant);
        store.UnitOfWorkManager.Returns(new UnitOfWorkManager(store));
        var sequence = Substitute.For<IEventSequence>();
        store.GetEventSequence(EventSequenceId.Log).Returns(sequence);
        sequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ =>
            {
                _commits.Enqueue(tenant.Value);
                return new AppendManyResult();
            });
        return store;
    }
}
