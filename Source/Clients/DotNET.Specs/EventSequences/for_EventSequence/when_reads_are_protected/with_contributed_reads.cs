// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reads_are_protected;

public class with_contributed_reads : given.public_entries
{
    Task Because() => ExerciseAppends(ReadAllowance.Contributed);

    [Fact] void should_refuse_every_append_family() => _errors.Values.ShouldEachConformTo(error => error is ImmediateAppendWhileReadsAreProtected);
    [Fact] void should_refuse_before_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_refuse_before_serialization() => _eventSerializer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_refuse_before_strategy_selection() => _concurrencyScopeStrategies.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_cover_all_overloads() => _errors.Count.ShouldEqual(15);
}
