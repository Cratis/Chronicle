// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_an_expando_object : Specification
{
    ExpandoObject _original;
    ExpandoObject _result;

    void Establish()
    {
        var nested = new ExpandoObject();
        ((IDictionary<string, object?>)nested)["inner"] = "nested-value";
        _original = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)_original;
        dictionary["name"] = "value";
        dictionary["nested"] = nested;
    }

    void Because() => _result = (ExpandoObject)StateValues.Copy(_original);

    [Fact] void should_return_another_instance() => _result.ShouldNotBeSame(_original);
    [Fact] void should_copy_the_values() => ((IDictionary<string, object?>)_result)["name"].ShouldEqual("value");
    [Fact] void should_return_another_nested_instance() => ((IDictionary<string, object?>)_result)["nested"].ShouldNotBeSame(((IDictionary<string, object?>)_original)["nested"]);
    [Fact] void should_copy_the_nested_values() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_result)["nested"]!)["inner"].ShouldEqual("nested-value");
}
