// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class counter_operations : given.a_language_service_with_schemas<given.Model>
{
    const string Definition = """
        projection Test => Model
          from UserLoggedIn
            key userId
            increment loginCount
            count eventCount
            decrement retryCount
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserLoggedIn)];

    given.CompilerResult _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_from_user_logged_in() => _result.Definition.From.ContainsKey((EventType)"UserLoggedIn").ShouldBeTrue();
    [Fact] void should_have_three_properties() => _result.Definition.From[(EventType)"UserLoggedIn"].Properties.Count.ShouldEqual(3);
    [Fact] void should_have_increment_operation() => _result.Definition.From[(EventType)"UserLoggedIn"].Properties[new PropertyPath("loginCount")].ShouldEqual("increment");
    [Fact] void should_have_count_operation() => _result.Definition.From[(EventType)"UserLoggedIn"].Properties[new PropertyPath("eventCount")].ShouldEqual("count");
    [Fact] void should_have_decrement_operation() => _result.Definition.From[(EventType)"UserLoggedIn"].Properties[new PropertyPath("retryCount")].ShouldEqual("decrement");
    [Fact] void should_generate_count_without_escaping() => _result.GeneratedDsl.ShouldContain("count eventCount");
    [Fact] void should_not_have_at_symbol_for_count() => _result.GeneratedDsl.ShouldNotContain("@Count");
    [Fact] void should_generate_increment_without_escaping() => _result.GeneratedDsl.ShouldContain("increment loginCount");
    [Fact] void should_not_have_at_symbol_for_increment() => _result.GeneratedDsl.ShouldNotContain("@increment");
    [Fact] void should_generate_decrement_without_escaping() => _result.GeneratedDsl.ShouldContain("decrement retryCount");
    [Fact] void should_not_have_at_symbol_for_decrement() => _result.GeneratedDsl.ShouldNotContain("@decrement");
}

