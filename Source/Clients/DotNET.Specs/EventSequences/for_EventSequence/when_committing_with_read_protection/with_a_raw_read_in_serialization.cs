// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_raw_read_in_serialization : given.a_trusted_commit
{
    void Establish() => _eventSerializer.Serialize(Arg.Any<object>()).Returns(async _ =>
    {
        await _sequence.GetTailSequenceNumber();
        return new System.Text.Json.Nodes.JsonObject();
    });

    Task Because() => Execute(async () =>
    {
        Read();
        _unit.AddEvents(EventSequenceId.Log, [new("source", "event")], []);
        await Commit();
    });

    [Fact] void should_not_grant_serialization_a_read_allowance() => _error.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
    [Fact] void should_not_append() => _request.ShouldBeNull();
}
