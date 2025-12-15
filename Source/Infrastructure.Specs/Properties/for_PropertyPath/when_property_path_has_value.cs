// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_property_path_has_value : Specification
{
    PropertyPath _result;

    void Because() => _result = "some.property.path";

    [Fact] void should_be_considered_set() => _result.IsSet.ShouldBeTrue();
}
