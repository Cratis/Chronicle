// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_projecting_nested_set_once : given.a_runtime_language_service
{
    IDictionary<string, object?> _profile;

    async Task Because()
    {
        await Compile("""
            from Changed
            nested profile
              from Changed
                set once name = name
            """);
        var first = Project(new { name = "first" }, new ExpandoObject());
        var second = Project(new { name = "second" }, first.CurrentState);
        _profile = (IDictionary<string, object?>)((IDictionary<string, object?>)second.CurrentState)["profile"]!;
    }

    [Fact] void should_initialize_a_nested_object_once() => _profile["name"].ShouldEqual("first");
    [Fact] void should_preserve_the_nested_expression() =>
        _definition.Nested[(PropertyPath)"profile"].From[(EventType)"Changed"].Properties[(PropertyPath)"name"].ShouldEqual("$setOnce(name)");
}
