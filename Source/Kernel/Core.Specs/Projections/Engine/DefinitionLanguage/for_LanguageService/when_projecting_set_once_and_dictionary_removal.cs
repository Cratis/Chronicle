// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_projecting_set_once_and_dictionary_removal : given.a_runtime_language_service
{
    IDictionary<string, object?> _result;
    IDictionary<string, object?> _entries;

    async Task Because()
    {
        await Compile("""
            from Changed
              set once name = name
              set once age = age
              set once active = active
              set once entries.$eventContent.entry = name
            """);
        var first = Project(new { name = "first", age = 0, active = false, entry = "one" }, new ExpandoObject());
        var second = Project(new { name = "second", age = 99, active = true, entry = "two" }, first.CurrentState);
        var third = Project(new { name = "third", age = 99, active = true, entry = "one" }, second.CurrentState);
        await Compile("""
            from Changed
              remove entries.$eventContent.entry
            """);
        var missing = Project(new { entry = "absent" }, third.CurrentState);
        _result = Project(new { entry = "two" }, missing.CurrentState).CurrentState;
        _entries = (IDictionary<string, object?>)_result["entries"]!;
        Project(new { entry = "one" }, new ExpandoObject());
    }

    [Fact] void should_keep_the_first_value() => _result["name"].ShouldEqual("first");
    [Fact] void should_preserve_zero() => _result["age"].ShouldEqual(0);
    [Fact] void should_preserve_false() => _result["active"].ShouldEqual(false);
    [Fact] void should_keep_the_sibling_entry() => _entries["one"].ShouldEqual("first");
    [Fact] void should_delete_the_entry_instead_of_setting_it_to_null() => _entries.ContainsKey("two").ShouldBeFalse();
    [Fact] void should_not_create_a_missing_entry() => _entries.ContainsKey("absent").ShouldBeFalse();
}
