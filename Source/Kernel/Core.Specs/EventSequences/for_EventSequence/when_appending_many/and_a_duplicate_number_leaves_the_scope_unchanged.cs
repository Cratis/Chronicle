// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_a_duplicate_number_leaves_the_scope_unchanged : given.a_batch_append_retry
{
    async Task Because() => _result = await AppendWithScope();

    [Fact] void should_append_the_batch() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_check_the_scope_again() => _scopeChecks.ShouldEqual(2);
    [Fact] void should_retry_the_storage_write() => _attempts.ShouldEqual(2);
    [Fact] void should_report_the_renumbered_batch() => _result.SequenceNumbers.ShouldContainOnly((EventSequenceNumber)6, (EventSequenceNumber)7, (EventSequenceNumber)8);
    [Fact] void should_report_the_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
}
