// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

/// <summary>
/// A missing stored definition requires full replay even when point lookup throws.
/// </summary>
public class and_projection_definition_is_missing : given.a_content_mutation_with_observers
{
    void Establish()
    {
        _definitions.RemoveAll(definition => definition.Identifier.Value != "missing");
        _storedProjectionDefinitions.Clear();
        _projectionDefinitions.Get((ProjectionId)"missing").Returns(Task.FromException<ProjectionDefinition>(new KeyNotFoundException()));
    }

    Task Because() => _eventSequence.Revise(42UL, _eventType, new JsonObject(), CorrelationId.New(), [], Identity.System);

    [Fact] Task should_fully_replay_the_observer_once() => ShouldReplayAll("missing");
}
