// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_setting_a_property_below_a_null_parent : Specification
{
    ExpandoObject _state;

    void Establish()
    {
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["info"] = null;
    }

    void Because() => new PropertyPath("info.name").SetValue(_state, "Again", ArrayIndexers.NoIndexers);

    [Fact] void should_recreate_the_parent() => ((IDictionary<string, object?>)_state)["info"].ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_set_the_nested_name() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_state)["info"]!)["name"].ShouldEqual("Again");
}
