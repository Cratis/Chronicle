// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_another_registrant_has_already_applied_the_same_addition : given.additive_evolution_enabled
{
    void Establish() => _eventTypesStorage.TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>())
        .Returns(call =>
        {
            _definition = _definition with { Generations = [new(1, call.ArgAt<JsonSchema>(3))] };
            return false;
        });

    Task Because() => Register();

    [Fact] void should_keep_the_other_registrants_addition() => _definition.Generations.Single().Schema.Properties.Keys.ShouldContain("age");
    [Fact] void should_not_retry_a_write_after_reading_the_identical_schema() => _eventTypesStorage.Received(1).TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
    [Fact] void should_reread_the_authoritative_definitions() => _eventTypesStorage.Received(2).GetAllDefinitions();
    [Fact] void should_not_duplicate_the_other_registrants_audit() => _systemEventSequence.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<EventTypeSchemaEvolved>());
}
