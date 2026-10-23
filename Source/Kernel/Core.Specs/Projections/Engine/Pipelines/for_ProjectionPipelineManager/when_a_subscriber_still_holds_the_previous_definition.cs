// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

/// <summary>
/// Reproduces https://github.com/Cratis/Chronicle/issues/4806: the replay caches a pipeline for the re-registered
/// definition, which no longer has the child collection, while a subscriber still builds its pipeline from the
/// previous definition and therefore accepts the removed child's event. The subscriber must get a pipeline built
/// from its own definition, not the replay's, or the event reaches a projection with no key resolver for it.
/// </summary>
public class when_a_subscriber_still_holds_the_previous_definition : given.a_pipeline_manager
{
    readonly EventType _removedChildEvent = new("ReactionGiven", 1);
    IProjection _previous;
    AppendedEvent _event;
    Exception _error;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(_removedChildEvent);
        _projection.GetKeyResolverFor(_removedChildEvent).Returns(_ => throw new MissingKeyResolverForEventType(_removedChildEvent));

        var identifier = _projection.Identifier;
        _previous = Substitute.For<IProjection>();
        _previous.Identifier.Returns(identifier);
        _previous.EventSequenceId.Returns(EventSequenceId.Log);
        _previous.DefinitionFingerprint.Returns("previous");
        _previous.GetKeyResolverFor(_removedChildEvent).Returns((_, _, _) => Task.FromException<KeyResolverResult>(new KeyResolvedByThePreviousDefinition()));
    }

    async Task Because()
    {
        await _manager.GetFor(_store, _namespace, _projection);
        var pipeline = await _manager.GetFor(_store, _namespace, _previous);
        _error = await Catch.Exception(() => pipeline.Handle(_event));
    }

    [Fact] void should_resolve_the_key_with_the_subscribers_definition() => _error.ShouldBeOfExactType<KeyResolvedByThePreviousDefinition>();
    [Fact] void should_not_ask_the_replays_definition_for_a_key_resolver() => _projection.DidNotReceive().GetKeyResolverFor(_removedChildEvent);

    sealed class KeyResolvedByThePreviousDefinition : Exception;
}
