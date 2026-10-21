// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_retrying_a_failed_deferred_binding : Specification
{
    IUnitOfWork _unit;
    IEventStore _store;
    Exception _firstError;
    int _resolutions;

    void Establish()
    {
        _store = Substitute.For<IEventStore>();
        IEventStore Resolve()
        {
            if (++_resolutions == 1) throw new InvalidOperationException("Namespace is not available yet.");
            return _store;
        }

        var manager = new UnitOfWorkManager(Resolve, null, UnitOfWorkLifecyclePolicy.Compatibility, null);
        _unit = manager.Begin(CorrelationId.New());
    }

    async Task Because()
    {
        _firstError = Catch.Exception(() => _unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown()));
        _unit.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
        await _unit.Rollback();
    }

    [Fact] void should_report_the_original_failure() => _firstError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_retry_instead_of_caching_the_failure() => _resolutions.ShouldEqual(2);
    [Fact] void should_use_the_successfully_resolved_store() => _store.Received().GetEventSequence(EventSequenceId.Log);
}
