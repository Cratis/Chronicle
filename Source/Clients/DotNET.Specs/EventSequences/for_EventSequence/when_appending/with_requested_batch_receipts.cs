// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_requested_batch_receipts : given.an_acknowledged_append
{
    async Task Because() => await _eventSequence.AppendMany(_source, ["first", "second"]);

    [Fact] void should_opt_in_for_single_source_batches() => _legacyRequest.IncludeReceipts.ShouldBeTrue();
}
