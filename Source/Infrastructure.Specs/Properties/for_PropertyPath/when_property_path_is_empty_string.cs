// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_property_path_is_empty_string : Specification
{
    PropertyPath _result;

    void Because() => _result = string.Empty;

    [Fact] void should_be_considered_not_set() => _result.IsSet.ShouldBeFalse();
}
