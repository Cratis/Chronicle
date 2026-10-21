// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_the_unique_constraint_store_fails : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    readonly IOException _failure = new("Store unavailable");

    void Establish()
    {
        var unique = new UniqueConstraintDefinition("unique", [new("Registered", ["email"])]);
        _definitions.Add(unique);
        _state.Changes = [new("unique", true, [ConstraintChangeType.EventAdded])];
        _uniqueStorage.Save(Arg.Any<EventSourceId>(), unique, Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>()).Returns(Task.FromException(_failure));
        _events.Add(Event("Registered", 1, Payload("email", "shared@example.com")));
    }

    async Task Because() => await Perform();

    [Fact] void should_fail_with_the_storage_error()
    {
        _result.TryGetException(out var error).ShouldBeTrue();
        error.ShouldBeSame(_failure);
    }
}
