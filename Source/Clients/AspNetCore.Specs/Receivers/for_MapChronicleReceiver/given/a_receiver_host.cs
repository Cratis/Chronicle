// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Receivers;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.AspNetCore.Receivers.for_MapChronicleReceiver.given;

public class a_receiver_host : Specification
{
    WebApplication _application;
    protected HttpClient _http;
    protected HttpResponseMessage _response;
    private protected IChronicleReceiver _receiver;
    private protected DeliveryTarget? _target;
    protected byte[] _body;
    protected string _responseBody;
    protected CancellationToken _cancellationToken;
    protected readonly TaskCompletionSource _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _aborted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected virtual string? CustomPath => null;
    protected virtual string Path => "/chronicle/deliveries/store/tenant-a/reactor/my-reactor";
    protected virtual bool HoldDelivery => false;
    private protected virtual DeliveryStatus OutcomeStatus => DeliveryStatus.Succeeded;
    protected static byte[] CreateRequestBody() => Encoding.UTF8.GetBytes("{\"partition\":\"order-42\",\"events\":[]}\n");

    async Task Establish()
    {
        var client = Substitute.For<IChronicleClient>();
        client.Options.Returns(new ChronicleClientOptions());
        client.CausationManager.Returns(Substitute.For<ICausationManager>());
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"tenant-a");
        store.UnitOfWorkManager.Returns(new UnitOfWorkManager(store));
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(store);
        _receiver = Substitute.For<IChronicleReceiver>();
        _receiver.Receive(Arg.Any<DeliveryRequest>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var request = call.Arg<DeliveryRequest>();
            _target = request.Target;
            _cancellationToken = call.Arg<CancellationToken>();
            await using var body = new MemoryStream();
            await request.Body.CopyToAsync(body, _cancellationToken);
            _body = body.ToArray();
            _received.TrySetResult();
            if (HoldDelivery)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, _cancellationToken);
                }
                catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
                {
                    _aborted.TrySetResult();
                }
            }

            return DeliveryOutcome.Rejected(OutcomeStatus, "safe reason");
        });
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddCratisChronicle(options => options.EventStore = "store");
        builder.Services.AddSingleton(client);
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.OccurrenceHandlers.Returns<IEnumerable<Type>>([]);
        builder.Services.AddSingleton(artifacts);
        builder.Services.AddSingleton(_receiver);
        _application = builder.Build();
        _application.MapChronicleReceiver(CustomPath);
        await _application.StartAsync();
        _http = _application.GetTestClient();
    }

    protected virtual async Task Send()
    {
        using var content = new ByteArrayContent(CreateRequestBody());
        _response = await _http.PostAsync(Path, content);
        _responseBody = await _response.Content.ReadAsStringAsync();
    }

    protected async Task InvokeMappedReceiver(HttpContext context)
    {
        await using var scope = _application.Services.CreateAsyncScope();
        context.RequestServices = scope.ServiceProvider;
        var endpoint = ((IEndpointRouteBuilder)_application).DataSources.SelectMany(_ => _.Endpoints).Single();
        var receive = endpoint.RequestDelegate ?? throw new InvalidOperationException("Receiver endpoint has no request delegate");
        await receive(context);
    }

    async Task Destroy()
    {
        _response?.Dispose();
        _http?.Dispose();
        if (_application is not null) await _application.DisposeAsync();
    }
}
