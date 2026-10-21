// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_committing_an_unused_deferred_unit : Specification
{
    IUnitOfWork _unit;
    int _resolutions;

    void Establish()
    {
        IEventStore Resolve()
        {
            _resolutions++;
            throw new InvalidOperationException("The request has no namespace.");
        }

        var manager = new UnitOfWorkManager(Resolve, null, UnitOfWorkLifecyclePolicy.Compatibility, null);
        _unit = manager.Begin(CorrelationId.New());
    }

    async Task Because() => await _unit.Commit();

    [Fact] void should_not_resolve_the_store() => _resolutions.ShouldEqual(0);
    [Fact] void should_complete_the_unit() => _unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_succeed() => _unit.IsSuccess.ShouldBeTrue();
}
