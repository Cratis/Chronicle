// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_beginning_new : given.a_unit_of_work_manager
{
    CorrelationId _correlationId;
    IUnitOfWork _result;
    IUnitOfWork _current;

    void Establish()
    {
        _correlationId = CorrelationId.New();
    }

    void Because()
    {
        _result = _manager.Begin(_correlationId);
        _current = _manager.Current;
    }

    [Fact] void should_return_an_instance() => _result.ShouldNotBeNull();
    [Fact] void should_have_the_same_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_the_new_instance_as_current() => _current.ShouldEqual(_result);
    [Fact] void should_have_the_unit_of_work_based_on_its_id() => _manager.TryGetFor(_correlationId, out var _).ShouldBeTrue();
}
