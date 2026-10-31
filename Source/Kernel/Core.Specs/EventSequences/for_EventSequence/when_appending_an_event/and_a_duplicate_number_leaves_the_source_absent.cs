// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_a_duplicate_number_leaves_the_source_absent : given.a_before_first_append_retry
{
    AppendResult _result;

    async Task Because() => _result = await AppendSingleWithScope();

    [Fact] void should_accept_the_retry() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_renumbered_position() => _result.SequenceNumber.ShouldEqual((EventSequenceNumber)6);
    [Fact] void should_check_freshness_again() => _scopeChecks.ShouldEqual(2);
    [Fact] void should_attempt_storage_twice() => _attempts.ShouldEqual(2);
    [Fact] void should_report_the_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
}
