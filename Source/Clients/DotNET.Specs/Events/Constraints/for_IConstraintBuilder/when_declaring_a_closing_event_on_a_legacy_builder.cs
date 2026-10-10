// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_IConstraintBuilder;

public class when_declaring_a_closing_event_on_a_legacy_builder : Specification
{
    Exception _error;

    void Because()
    {
        IConstraintBuilder builder = new LegacyBuilder();
        _error = Catch.Exception(() => builder.ClosesStreamOn<Closed>());
    }

    [Fact] void should_reject_with_the_named_capability_error() => _error.ShouldBeOfExactType<ClosesStreamConstraintsNotSupported>();

    record Closed();

    sealed class UnrelatedLegacyOperation() : Exception("This specification exercises only the new default interface member.");

    sealed class LegacyBuilder : IConstraintBuilder
    {
        public IConstraintBuilder PerEventSourceType() => throw new UnrelatedLegacyOperation();
        public IConstraintBuilder PerEventStreamType() => throw new UnrelatedLegacyOperation();
        public IConstraintBuilder PerEventStreamId() => throw new UnrelatedLegacyOperation();
        public IConstraintBuilder ForEventSequences(params EventSequenceId[] eventSequenceIds) => throw new UnrelatedLegacyOperation();
        public IConstraintBuilder ForEventLog() => throw new UnrelatedLegacyOperation();
        public IConstraintBuilder Unique(Action<IUniqueConstraintBuilder> callback) => throw new UnrelatedLegacyOperation();
        public IConstraintBuilder Unique<TEventType>(ConstraintViolationMessage? message = default, ConstraintName? name = default) => throw new UnrelatedLegacyOperation();
        public IConstraintBuilder Unique<TEventType>(ConstraintViolationMessageProvider messageCallback, ConstraintName? name = default) => throw new UnrelatedLegacyOperation();
        public IConstraintBuilder RemovedWith<TRemovalEventType>() => throw new UnrelatedLegacyOperation();
        public void AddConstraint(IConstraintDefinition constraint) => throw new UnrelatedLegacyOperation();
        public IImmutableList<IConstraintDefinition> Build() => throw new UnrelatedLegacyOperation();
    }
}
