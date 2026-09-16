// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_requested_receipts : given.an_acknowledged_append
{
    async Task Because() => await _eventSequence.Append(_source, "single");

    [Fact] void should_opt_in_for_single_appends() => _request.IncludeReceipt.ShouldBeTrue();
}
