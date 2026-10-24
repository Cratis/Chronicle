// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

/// <summary>
/// A definition read failure is reported rather than falling back to partition replay.
/// </summary>
public class and_projection_definition_cannot_be_read : given.a_content_mutation_with_observers
{
    readonly Exception _failure = new InvalidOperationException("Definition storage unavailable");
    Exception _error;

    void Establish() => _projectionDefinitions.Get(Arg.Any<ProjectionId>()).Returns(Task.FromException<ProjectionDefinition>(_failure));

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.Revise(42UL, _eventType, new JsonObject(), CorrelationId.New(), [], Identity.System));

    [Fact] void should_propagate_the_definition_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_not_replay_any_observer() => _observers.Values.ShouldEachConformTo(observer => !observer.ReceivedCalls().Any());
}
