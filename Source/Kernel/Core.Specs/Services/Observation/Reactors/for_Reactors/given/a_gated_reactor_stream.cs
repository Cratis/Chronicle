// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage;
using Cratis.Traces;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.given;

public class a_gated_reactor_stream : Specification
{
    protected readonly Chronicle.given.ObservedConnectionOwnership _ownership = new();
    protected readonly IReactor _reactor = Substitute.For<IReactor>();
    protected readonly IReactorMediator _mediator = Substitute.For<IReactorMediator>();
    protected readonly SemaphoreSlim _registered = new(0);
    readonly List<CancellationTokenSource> _cancellations = [];
    readonly List<IDisposable> _subscriptions = [];
    readonly List<Task<Exception?>> _completions = [];
    readonly List<ReplaySubject<ReactorMessage>> _messages = [];
    Reactors _service;

    async Task Establish()
    {
        using var gate = await _ownership.Enter("connection");
        _ownership.Claim("connection", ConnectionOwner.InProcess);
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IReactor>(Arg.Any<string>(), Arg.Any<string>()).Returns(_reactor);
        _reactor.SetDefinitionAndSubscribe(Arg.Any<Concepts.Observation.Reactors.ReactorDefinition>()).Returns(_ =>
        {
            _registered.Release();
            return Task.CompletedTask;
        });
        var options = Options.Create(new ChronicleOptions());
        _service = new Reactors(grains, _mediator, Substitute.For<IStorage>(), new JsonSerializerOptions(), new ActivitySource<Reactors>(),
            options, NullLogger<Reactors>.Instance, _ownership, new ConnectionOwnerResolver(options));
    }

    protected Task<Exception?> Start(CancellationTokenSource cancellation)
    {
        _cancellations.Add(cancellation);
        var messages = new ReplaySubject<ReactorMessage>(1);
        _messages.Add(messages);
        messages.OnNext(new(new(new RegisterReactor
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new() { ReactorId = "reactor", EventSequenceId = "event-log" }
        })));
        var completed = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscriptions.Add(_service.Observe(messages, new CallContext(new CallOptions(cancellationToken: cancellation.Token)))
            .Subscribe(_ => { }, error => completed.TrySetResult(error), () => completed.TrySetResult(null)));
        _completions.Add(completed.Task);
        return completed.Task;
    }

    async Task Destroy()
    {
        _ownership.Entering = null;
        foreach (var cancellation in _cancellations) cancellation.Cancel();
        await Task.WhenAll(_completions).WaitAsync(TimeSpan.FromSeconds(5));
        foreach (var subscription in _subscriptions) subscription.Dispose();
        foreach (var messages in _messages) messages.Dispose();
        foreach (var cancellation in _cancellations) cancellation.Dispose();
        _registered.Dispose();
    }
}
