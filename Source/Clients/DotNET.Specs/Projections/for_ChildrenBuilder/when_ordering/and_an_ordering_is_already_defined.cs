// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_ordering;

public class and_an_ordering_is_already_defined : given.a_children_builder
{
    Exception? _error;

    void Because()
    {
        _builder.Prepend();
        _error = Catch.Exception(() => _builder.OrderBy(_ => _.Name));
    }

    [Fact] void should_fail_because_the_ordering_is_already_defined() => _error.ShouldBeOfExactType<ChildrenOrderingAlreadyDefined>();
}
