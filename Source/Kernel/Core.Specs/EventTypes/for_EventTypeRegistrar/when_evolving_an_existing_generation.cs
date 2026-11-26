// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_evolving_an_existing_generation : given.additive_evolution_enabled
{
    IReadOnlyList<EventTypeCheckResult> _check;

    async Task Because()
    {
        _check = await _subject.Check("test-store", [Registration()], _storage);
        await Register();
        await Register();
        await Register(Original);
    }

    [Fact] void should_accept_the_read_only_check() => _check.Single().Accepted.ShouldBeTrue();
    [Fact] void should_report_additive() => _check.Single().Classification.ShouldEqual("Additive");
    [Fact] void should_retain_the_added_property() => _definition.Generations.Single().Schema.Properties.ContainsKey("age").ShouldBeTrue();
    [Fact] void should_write_the_schema_once() => _eventTypesStorage.Received(1).TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
    [Fact] void should_evict_every_silo_once() => _eventTypesCacheClient.Received(1).Invalidate(Arg.Any<EventStoreName>(), Arg.Is<EventTypeId>(_ => _.Value == "some-event"));
    [Fact] void should_audit_once() => _systemEventSequence.Received(1).Append(Arg.Any<EventSourceId>(), Arg.Is<EventTypeSchemaEvolved>(_ => _.PreviousSchema.Contains("name") && _.Schema.Contains("age")));
    [Fact] void should_not_start_a_generation_migration() => _systemEventSequence.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<EventTypeGenerationAdded>());
    [Fact] void should_not_send_existing_schemas_to_unconditional_registration() => _eventTypesStorage.Received(3).Register(Arg.Is<IEnumerable<EventTypeToRegister>>(_ => _.All(item => !item.Definition.Generations.Any())));
}
