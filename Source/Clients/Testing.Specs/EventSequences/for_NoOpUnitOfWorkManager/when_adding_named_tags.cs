// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.EventSequences.for_NoOpUnitOfWorkManager;

public class when_adding_named_tags : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new NoOpUnitOfWorkManager().Begin(CorrelationId.New()).AddEvent(EventSequenceId.Log, EventSourceId.New(), "event", [new("name", "value")], Causation.Unknown()));

    [Fact] void should_fail_loudly_instead_of_discarding_named_tags() => _error.ShouldBeOfExactType<UnitOfWorkNamedTagsNotSupported>();
}
