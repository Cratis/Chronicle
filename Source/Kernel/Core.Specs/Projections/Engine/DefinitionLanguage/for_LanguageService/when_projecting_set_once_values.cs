// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_projecting_set_once_values : given.a_runtime_language_service
{
    [Theory]
    [InlineData("from Changed")]
    [InlineData("every")]
    public async Task should_allow_a_value_after_a_null_source_and_then_keep_an_empty_string(string block)
    {
        var declaration = $"{block}\n  set once name = name";
        if (block == "every") declaration += "\nfrom Changed";
        await Compile(declaration);
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["name"] = null;
        state = Project(new { name = (string?)null }, state).CurrentState;
        state = Project(new { name = "" }, state).CurrentState;
        state = Project(new { name = "later" }, state).CurrentState;
        ((IDictionary<string, object?>)state)["name"].ShouldEqual("");
    }

    [Theory]
    [InlineData("$eventContext.sequenceNumber")]
    [InlineData("$causedBy.subject")]
    public async Task should_keep_dynamic_keys_through_generation(string suffix)
    {
        await Compile($"from Changed\n  set once entries.{suffix} = name");
        var state = Project(new { name = "first" }, new ExpandoObject()).CurrentState;
        state = Project(new { name = "later" }, state).CurrentState;
        var entries = (IDictionary<string, object?>)((IDictionary<string, object?>)state)["entries"]!;
        entries.Values.ShouldContainOnly("first");
        await Compile($"from Changed\n  remove entries.{suffix}");
        state = Project(new { name = "ignored" }, state).CurrentState;
        ((IDictionary<string, object?>)((IDictionary<string, object?>)state)["entries"]!).ShouldBeEmpty();
    }
}
