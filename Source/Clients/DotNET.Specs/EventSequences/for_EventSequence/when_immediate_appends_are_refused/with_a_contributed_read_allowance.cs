// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_a_contributed_read_allowance : given.public_entries
{
    Task Because() => ExerciseAppends(ReadAllowance.Contributed);

    [Fact] void should_keep_every_append_refused() => _errors.Values.ShouldEachConformTo(error => error is ImmediateAppendRefused);
    [Fact] void should_cover_all_ten_entries() => _errors.Count.ShouldEqual(10);
    [Fact] void should_not_reach_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_serialize() => _eventSerializer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_providers() => _eventSources.ReceivedCalls().ShouldBeEmpty();
}
