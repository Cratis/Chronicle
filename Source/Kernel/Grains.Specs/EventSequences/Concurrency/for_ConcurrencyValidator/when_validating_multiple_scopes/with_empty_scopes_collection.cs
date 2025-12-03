// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_multiple_scopes;

public class with_empty_scopes_collection : given.a_concurrency_validator
{
    ConcurrencyScopes _scopes;
    IEnumerable<ConcurrencyViolation> _result;

    void Establish()
    {
        _scopes = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>());
    }

    async Task Because() => _result = await _validator.Validate(_scopes);

    [Fact] void should_return_no_violations() => _result.ShouldBeEmpty();
    [Fact] void should_not_call_storage() => _eventSequenceStorage.DidNotReceive().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
}
