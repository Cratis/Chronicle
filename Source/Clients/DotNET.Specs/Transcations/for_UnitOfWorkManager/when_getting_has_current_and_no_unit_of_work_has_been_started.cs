// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_getting_has_current_and_no_unit_of_work_has_been_started : given.a_unit_of_work_manager
{
    bool _result;

    void Because() => _result = _manager.HasCurrent;

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
