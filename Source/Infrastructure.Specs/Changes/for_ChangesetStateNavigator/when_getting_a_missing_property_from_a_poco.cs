// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_ChangesetStateNavigator;

public class when_getting_a_missing_property_from_a_poco : Specification
{
    bool _result;

    void Because() => _result = ChangesetStateNavigator.TryGetValueAtPath(new object(), new PropertyPath("missing"), ArrayIndexers.NoIndexers, out _);

    [Fact] void should_not_find_the_value() => _result.ShouldBeFalse();
}
