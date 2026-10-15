// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_reading_a_declared_stream : given.a_declared_stream_reader
{
    DecisionRead<MonthlyState> _read;

    async Task Because() => _read = await _reader.GetDetached<MonthlyState>("source", "Transactions", "2026-10", "BankAccount");

    [Fact] void should_admit_stream_reads() => _reader.AdmitForStream<MonthlyState>().IsAdmitted.ShouldBeTrue();
    [Fact] void should_keep_the_source_key() => _read.Instance!.Id.ShouldEqual("source");
    [Fact] void should_carry_the_declared_stream_to_the_fold() => _readModels.Received(1).GetInstanceByKey(Arg.Is<GetInstanceByKeyRequest>(request => request.EventSourceId == "source" && request.EventSourceType == "BankAccount" && request.EventStreamType == "Transactions" && request.EventStreamId == "2026-10"), Arg.Any<CallContext>());
    [Fact] void should_carry_the_declared_stream_to_the_guard() => ((IDecisionRead)_read).Scope.EventStreamId!.Value.ShouldEqual("2026-10");
}
