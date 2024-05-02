// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Reflection.for_TypeExtensions;

public class when_getting_default_constructor_from_type_with_default_constructor : Specification
{
    ConstructorInfo constructor_info;

    void Because() => constructor_info = typeof(TypeWithDefaultConstructor).GetDefaultConstructor();

    [Fact] void should_return_a_constructor() => constructor_info.ShouldNotBeNull();
}
