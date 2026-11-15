// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.StructuredLiteralPrototype;

/// <summary>
/// Characterizes the CURRENT legacy collision for the proposed #5525 token: legacy grammar treats it as a plain property name.
/// </summary>
public class when_a_proposed_token_is_a_legacy_property_path : Specification
{
    const string Token = "$structured:v1(e30)";

    PropertyPath _path = null!;
    object? _value;

    void Because()
    {
        _path = new PropertyPath(Token);
        dynamic target = new ExpandoObject();
        ((IDictionary<string, object?>)target)[Token] = "legacy";
        _value = _path.GetValue((ExpandoObject)target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_keep_the_token_as_the_path() => _path.Path.ShouldEqual(Token);

    [Fact] void should_treat_it_as_one_property_name_segment() => _path.Segments.ShouldContainOnly(new PropertyName(Token));

    [Fact] void should_read_the_dictionary_entry_with_that_name_in_current_legacy_behavior() => _value.ShouldEqual("legacy");
}
