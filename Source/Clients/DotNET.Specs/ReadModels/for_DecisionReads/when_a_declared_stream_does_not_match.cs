// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_a_declared_stream_does_not_match : given.a_declared_stream_reader
{
    [Theory]
    [InlineData("Limits", "BankAccount")]
    [InlineData("Transactions", "OtherAccount")]
    [InlineData("Transactions", null)]
    public async Task should_refuse_before_folding(string stream, string? sourceType)
    {
        var eventSourceType = sourceType is null ? null : (EventSourceType?)sourceType;
        var error = await Catch.Exception(() => _reader.GetDetached<MonthlyState>("source", stream, "2026-10", eventSourceType));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.StreamScopeDoesNotMatchReadModel);
        _folds.ShouldEqual(0);
    }
}
