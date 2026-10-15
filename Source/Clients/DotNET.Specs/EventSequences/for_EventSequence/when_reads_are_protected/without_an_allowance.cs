// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reads_are_protected;

public class without_an_allowance : given.public_entries
{
    Task Because() => ExerciseReads();

    [Fact] void should_refuse_every_read_family() => _errors.Values.ShouldEachConformTo(error => error is ReadRefusedWhileReadsAreProtected);
    [Fact] void should_refuse_before_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_cover_all_overloads() => _errors.Count.ShouldEqual(12);
}
