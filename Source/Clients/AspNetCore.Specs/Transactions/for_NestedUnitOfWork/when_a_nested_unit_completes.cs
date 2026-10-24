// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_NestedUnitOfWork;

public class when_a_nested_unit_completes : Specification
{
    UnitOfWorkManager _manager;
    IUnitOfWork _request;
    IUnitOfWork _nested;
    bool _currentAfterBegin;
    bool _requestIsCurrentAfterCompletion;
    bool _hasCurrentAfterCompletion;

    void Establish() => _manager = new UnitOfWorkManager(Substitute.For<IEventStore>());

    async Task Because()
    {
        _request = _manager.Begin(CorrelationId.New());
        _nested = _manager.Begin(CorrelationId.New());
        _currentAfterBegin = ReferenceEquals(_manager.Current, _nested);
        await _nested.Commit();
        _hasCurrentAfterCompletion = _manager.HasCurrent;
        _requestIsCurrentAfterCompletion = _hasCurrentAfterCompletion && ReferenceEquals(_manager.Current, _request);
    }

    [Fact] void should_make_the_nested_unit_current_while_it_runs() => _currentAfterBegin.ShouldBeTrue();

    [Fact] void should_restore_the_request_unit_as_current() => _requestIsCurrentAfterCompletion.ShouldBeTrue();
    [Fact] void should_still_have_a_current_unit() => _hasCurrentAfterCompletion.ShouldBeTrue();
}
