// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_checking_has_for_by_type_and_it_does_not_exist : given.all_dependencies
{
    class MyReadModel
    {
    }

    bool _result;

    void Because() => _result = _reducers.HasFor(typeof(MyReadModel));

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
