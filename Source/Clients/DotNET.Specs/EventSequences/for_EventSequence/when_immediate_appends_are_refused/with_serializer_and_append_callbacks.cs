// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_serializer_and_append_callbacks : given.a_unit_with_native_sequence
{
    Exception _serializerAppendError;
    Exception _notificationAppendError;

    async Task Because()
    {
        using var frame = Refuse();
        await _sequence.Transactional.Append("source", "changed");
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(call =>
        {
            _serializerAppendError = Catch.Exception(new Action(() => _ = _sequence.Append("source", "from serializer")));
            return new JsonObject();
        });
        using var subscription = _sequence.AppendOperations.Subscribe(events =>
            _notificationAppendError = Catch.Exception(new Action(() => _ = _sequence.Append("source", "from notification"))));
        ClearCalls();
        await frame.Commit();
    }

    [Fact] void should_keep_admission_closed_during_serialization() => _serializerAppendError.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_keep_admission_closed_during_notifications() => _notificationAppendError.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_only_send_the_owner_batch() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_still_commit_the_owner_batch() => _unit.IsSuccess.ShouldBeTrue();
}
