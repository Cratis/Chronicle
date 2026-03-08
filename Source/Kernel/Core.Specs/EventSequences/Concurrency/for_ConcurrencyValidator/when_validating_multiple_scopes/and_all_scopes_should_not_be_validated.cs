// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_multiple_scopes;

public class and_all_scopes_should_not_be_validated : given.a_concurrency_validator
{
    ConcurrencyScopes _scopes;
    IEnumerable<ConcurrencyViolation> _result;

    void Establish()
    {
        var eventSourceId1 = EventSourceId.New();
        var eventSourceId2 = EventSourceId.New();

        _scopes = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            { eventSourceId1, ConcurrencyScope.None },
            { eventSourceId2, ConcurrencyScope.NotSet }
        });
    }

    async Task Because() => _result = await _validator.Validate(_scopes);

    [Fact] void should_return_no_violations() => _result.ShouldBeEmpty();
    [Fact] void should_not_call_storage() => _eventSequenceStorage.DidNotReceive().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
}
