// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_IUnitOfWork;

public class when_adding_named_tags_with_legacy_implementation : Specification
{
    IUnitOfWork _unitOfWork;
    Exception _error;

    void Establish() => _unitOfWork = new when_enrolling_a_batch_with_legacy_implementation.LegacyUnitOfWork();
    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEventWithNamedTags(EventSequenceId.Log, EventSourceId.New(), "event", [new("key", "value")], Causation.Unknown()));

    [Fact] void should_fail_with_a_dedicated_exception() => _error.ShouldBeOfExactType<UnitOfWorkNamedTagsNotSupported>();
    [Fact] void should_not_call_the_legacy_method() => ((when_enrolling_a_batch_with_legacy_implementation.LegacyUnitOfWork)_unitOfWork).AddEventCalls.ShouldEqual(0);
}
