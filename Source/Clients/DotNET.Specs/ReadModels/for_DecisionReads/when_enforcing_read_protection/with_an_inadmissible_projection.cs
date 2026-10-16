// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_an_inadmissible_projection : given.a_protected_reader
{
    [Theory]
    [InlineData(DecisionReadRefusalReason.Join)]
    [InlineData(DecisionReadRefusalReason.NotEventLog)]
    [InlineData(DecisionReadRefusalReason.Reducer)]
    [InlineData(DecisionReadRefusalReason.StreamKeyRequiresStreamScope)]
    public async Task should_refuse_before_kernel_acquisition(DecisionReadRefusalReason reason)
    {
        switch (reason)
        {
            case DecisionReadRefusalReason.Join:
                _definition.Join[new() { Id = "book-source-event" }] = new();
                break;
            case DecisionReadRefusalReason.NotEventLog:
                _definition.EventSequenceId = "other-sequence";
                break;
            case DecisionReadRefusalReason.Reducer:
                _reducers.HasFor(Arg.Any<Type>()).Returns(true);
                break;
            case DecisionReadRefusalReason.StreamKeyRequiresStreamScope:
                _definition.From.First().Value.Key = "$eventContext(eventStreamId)";
                break;
        }
        using var frame = _unit.EnforceReadProtection();
        var error = await Catch.Exception(() => _reader.GetFor<Model>(_unit, "January"));
        error.ShouldBeOfExactType<DecisionReadRefused>();
        ((DecisionReadRefused)error).Reason.ShouldEqual(reason);
        _folds.ShouldEqual(0);
        _sequences.ReceivedCalls().ShouldBeEmpty();
    }
}
