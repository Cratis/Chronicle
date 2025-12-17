// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_checking_has_for_by_type_and_it_does_not_exist : given.all_dependencies
{
    class MyReadModel;

    bool _result;

#pragma warning disable CA2263 // Prefer generic overload when type is known
    void Because() => _result = _reducers.HasFor(typeof(MyReadModel));
#pragma warning restore CA2263 // Prefer generic overload when type is known

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
