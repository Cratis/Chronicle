// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Projections.given;

namespace Cratis.Chronicle.Projections.for_ChildrenJoinBuilder;

public class when_specifying_on : events_with_derivatives
{
    ObsoleteAttribute? _interfaceAttribute;
    ObsoleteAttribute? _implementationAttribute;

    void Because()
    {
        _interfaceAttribute = typeof(IChildrenJoinBuilder<Item, ItemChanged>).GetMethod("On")!.GetCustomAttribute<ObsoleteAttribute>();
        _implementationAttribute = typeof(ChildrenJoinBuilder<Container, Item, ItemChanged>).GetMethod("On")!.GetCustomAttribute<ObsoleteAttribute>();
    }

    [Fact] void should_deprecate_the_interface_method() => _interfaceAttribute.ShouldNotBeNull();
    [Fact] void should_deprecate_the_implementation_method() => _implementationAttribute.ShouldNotBeNull();
    [Fact] void should_keep_the_interface_method_callable() => _interfaceAttribute!.IsError.ShouldBeFalse();
    [Fact] void should_keep_the_implementation_method_callable() => _implementationAttribute!.IsError.ShouldBeFalse();
}
