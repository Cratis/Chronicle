// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_a_plain_reducer;

public class and_the_model_is_inferred_from_instance_methods : an_explicit_store
{
    IReducerHandler _handler;
    ReduceResult _result;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("event-alias")).RegisterReducer<PlainReducer>("reducer-alias");
        var activated = new ActivatedArtifact(new PlainReducer(), typeof(PlainReducer), NullLogger<ActivatedArtifact>.Instance);
        _activator.Activate(Arg.Any<IServiceProvider>(), typeof(PlainReducer)).Returns(activated);
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _handler = _store.Reducers.GetHandlerFor<PlainReducer>();
        _result = await _handler.Invoker.Invoke(
            _provider,
            [new EventAndContext(new PlainEvent("reduced"), EventContext.Empty with { EventType = _store.EventTypes.GetEventTypeFor(typeof(PlainEvent)) })],
            null);
    }

    [Fact] void should_infer_the_single_read_model() => _handler.ReadModelType.ShouldEqual(typeof(Model));
    [Fact] void should_use_the_explicit_reducer_identifier() => _handler.Id.ShouldEqual((ReducerId)"reducer-alias");
    [Fact] void should_use_registry_event_identities() => _handler.EventTypes.ShouldContainOnly(new EventType("event-alias", 1));
    [Fact] void should_use_the_existing_invocation_engine() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_produce_the_reduced_state() => ((Model)_result.ReadModelState!).Name.ShouldEqual("reduced");

    public record Model(string Name);

    public class PlainReducer
    {
        public Model Handle(PlainEvent @event, Model? current) => new(@event.Name);
    }
}
