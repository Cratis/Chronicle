// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_an_unrelated_sibling_flow : given.a_unit_with_native_sequence
{
    Exception _siblingError;

    async Task Because()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sibling = Task.Run(async () =>
        {
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return await Catch.Exception(() => _sequence.Append("sibling", "changed"));
        });
        using var frame = Refuse();
        ClearCalls();
        release.SetResult();
        _siblingError = await sibling.WaitAsync(TimeSpan.FromSeconds(10));
        _error = await Catch.Exception(() => _sequence.Append("owner", "changed"));
    }

    [Fact] void should_not_change_the_unrelated_flow() => _siblingError.ShouldBeNull();
    [Fact] async Task should_send_only_the_sibling_append() => await _sequences.Received(1).Append(Arg.Is<Contracts.Sequences.AppendRequest>(request => request.EventSourceId == "sibling"), Arg.Any<CallContext>());
    [Fact] void should_keep_the_owner_flow_refused() => _error.ShouldBeOfExactType<ImmediateAppendRefused>();
}
