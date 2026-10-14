// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_scopes_use_equivalent_sentinels : given.a_contributed_unit
{
    ConcurrencyScope _supplied;

    void Establish() => _supplied = _required with
    {
        EventStreamType = EventStreamType.All,
        EventStreamId = EventStreamId.Default,
        EventSourceType = EventSourceType.Unspecified,
        EventTypes = []
    };

    async Task Because()
    {
        Stage(_supplied);
        await _unitOfWork.Commit();
    }

    [Fact] void should_keep_the_equivalent_supplied_scope() => _concurrencyScopesAppended[_source].ShouldEqual(_supplied);
}
