// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_reading_after_connecting : given.a_decision_reader
{
    async Task Establish() => await _reader.GetDetached<Model>("source");

    async Task Because()
    {
        _lifecycle.OnConnected += Raise.Event<Connections.Connected>();
        await _reader.GetDetached<Model>("source");
    }

    [Fact] void should_check_the_agreement_again() => _projectionService.ReceivedCalls().Count().ShouldEqual(2);
    [Fact] void should_return_both_reads() => _folds.ShouldEqual(2);
}
