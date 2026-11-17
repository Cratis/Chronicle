// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_ChangesetStateNavigator;

public class when_getting_a_property_from_a_poco_ignoring_case : Specification
{
    sealed class State
    {
        public string Name { get; set; } = "value";
    }

    object? _value;
    bool _result;

    void Because() => _result = ChangesetStateNavigator.TryGetValueAtPath(new State(), new PropertyPath("name"), ArrayIndexers.NoIndexers, out _value);

    [Fact] void should_find_the_value() => _result.ShouldBeTrue();
    [Fact] void should_return_the_property_value() => _value.ShouldEqual("value");
}
