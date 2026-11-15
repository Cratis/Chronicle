// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_ordering;

/// <summary>
/// Appending is the default and is left out of the definition, so definitions built before ordering existed keep
/// serializing - and fingerprinting - exactly as they did.
/// </summary>
public class and_no_ordering_is_defined : given.a_children_builder
{
    void Because() => _result = _builder.Build();

    [Fact] void should_leave_the_ordering_out() => _result.Ordering.ShouldBeNull();
}
