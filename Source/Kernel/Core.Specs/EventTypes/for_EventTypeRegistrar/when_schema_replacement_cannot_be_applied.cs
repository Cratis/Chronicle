// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_schema_replacement_cannot_be_applied : given.additive_evolution_enabled
{
    Exception _error;

    void Establish() => _eventTypesStorage.TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>()).Returns(false);

    async Task Because() => _error = await Catch.Exception(() => Register());

    [Fact] void should_reject_without_claiming_success() => _error.ShouldBeOfExactType<EventTypeSchemaChanged>();
    [Fact] void should_bound_replacement_attempts() => _eventTypesStorage.Received(3).TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
    [Fact] void should_not_audit_an_unapplied_change() => _systemEventSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_invalidate_when_nothing_changed() => _eventTypesCacheClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_call_unconditional_registration() => _eventTypesStorage.DidNotReceive().Register(Arg.Any<IEnumerable<EventTypeToRegister>>());
}
