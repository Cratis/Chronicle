// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.given;

public class a_registration_stream : Specification
{
    protected RegisterReactor _registration;
    protected Concepts.Observation.Reactors.ReactorDefinition _definition;
    protected Exception _error;
    protected IReactor _reactor;
    readonly Subject<ReactorMessage> _messages = new();
    readonly TaskCompletionSource _registered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IDisposable _subscription;

    void Establish()
    {
        _registration = new RegisterReactor
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new Contracts.Observation.Reactors.ReactorDefinition { ReactorId = "reactor", EventSequenceId = "event-log" }
        };
        _reactor = Substitute.For<IReactor>();
        _reactor.SetDefinitionAndSubscribe(Arg.Any<Concepts.Observation.Reactors.ReactorDefinition>()).Returns(call =>
        {
            _definition = call.Arg<Concepts.Observation.Reactors.ReactorDefinition>();
            _registered.TrySetResult();
            return Task.CompletedTask;
        });
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IReactor>(Arg.Any<string>()).Returns(_reactor);
        var reactors = new Reactors(
            grainFactory,
            Substitute.For<IReactorMediator>(),
            Substitute.For<IStorage>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reactors>(),
            Options.Create(new ChronicleOptions()),
            NullLogger<Reactors>.Instance);
        _subscription = reactors.Observe(_messages).Subscribe(_ => { }, error =>
        {
            _error = error;
            _registered.TrySetResult();
        });
    }

    protected async Task Register()
    {
        _messages.OnNext(new(new(_registration)));
        await _registered.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy()
    {
        _subscription.Dispose();
        _messages.Dispose();
    }
}
