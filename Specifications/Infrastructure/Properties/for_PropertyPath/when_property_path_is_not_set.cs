// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties.for_PropertyPath;

public class when_property_path_is_not_set : Specification
{
    PropertyPath result;

    void Because() => result = PropertyPath.NotSet;

    [Fact] void should_be_considered_not_set() => result.IsSet.ShouldBeFalse();
}
