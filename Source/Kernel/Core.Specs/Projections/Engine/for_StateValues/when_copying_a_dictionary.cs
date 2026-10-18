// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_a_dictionary : Specification
{
    Dictionary<string, object> _original;
    object _result;

    void Establish() => _original = new() { ["key"] = "value" };

    void Because() => _result = StateValues.Copy(_original);

    [Fact] void should_return_another_instance() => _result.ShouldNotBeSame(_original);
    [Fact] void should_keep_the_dictionary_shape() => (_result is IDictionary<string, object>).ShouldBeTrue();
    [Fact] void should_keep_the_entries() => ((IDictionary<string, object>)_result)["key"].ShouldEqual("value");
}
