// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_a_plain_reducer;

public class and_only_the_classic_marker_interface_is_implemented : an_explicit_store
{
    IReducerHandler _handler;
    ReduceResult _result;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        var activated = new ActivatedArtifact(new Classic(), typeof(Classic), NullLogger<ActivatedArtifact>.Instance);
        _activator.Activate(Arg.Any<IServiceProvider>(), typeof(Classic)).Returns(activated);
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<ReduceOperationMessage>());
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _handler = await _store.Reducers.Register(typeof(Classic));
        _result = await _handler.Invoker.Invoke(_provider, [new EventAndContext(new PlainEvent("reduced"), EventContext.Empty)], null);
    }

    void Destroy()
    {
        _handler?.Disconnect();
        (_handler as IDisposable)?.Dispose();
    }

    [Fact] void should_infer_the_instance_model_without_an_explicit_artifact_nomination() => _handler.ReadModelType.ShouldEqual(typeof(Model));
    [Fact] void should_keep_classic_invocation() => _handler.Invoker.ShouldBeOfExactType<ReducerInvoker>();
    [Fact] void should_reduce_the_registered_event() => ((Model)_result.ReadModelState!).Name.ShouldEqual("reduced");

    public record Model(string Name);
    public class Classic : IReducer
    {
        public Model Apply(PlainEvent @event, Model? current) => new(@event.Name);
    }
}
