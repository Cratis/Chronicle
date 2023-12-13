// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_ensuring_path_for_three_levels_and_second_level_is_already_set_to_null : Specification
{
    ExpandoObject result;
    ExpandoObject initial;

    void Establish() => initial = new { first_level = new { second_level = (object)null! } }.AsExpandoObject();

    void Because() => result = initial.EnsurePath("first_level.second_level.third_level.property", ArrayIndexers.NoIndexers);

    [Fact] void should_return_an_instance() => result.ShouldNotBeNull();
}
