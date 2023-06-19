// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_ensuring_path_for_three_defined_levels : Specification
{
    ExpandoObject result;
    ExpandoObject initial;

    void Establish() => initial = new { first_level = new { second_level = new { third_level = new { property = 42 } } } }.AsExpandoObject();

    void Because() => result = initial.EnsurePath("first_level.second_level.third_level.property", ArrayIndexers.NoIndexers);

    [Fact] void should_return_existing_object() => ((int)((dynamic)result).property).ShouldEqual(42);
}
