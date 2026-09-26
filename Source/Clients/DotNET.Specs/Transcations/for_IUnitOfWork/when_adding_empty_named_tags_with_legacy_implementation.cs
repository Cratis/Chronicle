// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_IUnitOfWork;

public class when_adding_empty_named_tags_with_legacy_implementation : Specification
{
    when_enrolling_a_batch_with_legacy_implementation.LegacyUnitOfWork _unitOfWork;
    Exception _error;

    void Establish() => _unitOfWork = new();
    void Because() => _error = Catch.Exception(() => ((IUnitOfWork)_unitOfWork).AddEvent(EventSequenceId.Log, EventSourceId.New(), "event", [], Causation.Unknown()));

    [Fact] void should_delegate_to_the_legacy_method() => _unitOfWork.AddEventCalls.ShouldEqual(1);
    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
