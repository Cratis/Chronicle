// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class counter_operations : given.a_language_service
{
    const string definition = """
        projection Test => Model
          from UserLoggedIn
            key userId
            increment LoginCount
            count EventCount
            decrement RetryCount
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(definition, "Model");

    [Fact] void should_have_from_user_logged_in() => _result.From.ContainsKey((EventType)"UserLoggedIn").ShouldBeTrue();
    [Fact] void should_have_three_properties() => _result.From[(EventType)"UserLoggedIn"].Properties.Count.ShouldEqual(3);
    [Fact] void should_have_increment_operation() => _result.From[(EventType)"UserLoggedIn"].Properties[new PropertyPath("LoginCount")].ShouldEqual("increment");
    [Fact] void should_have_count_operation() => _result.From[(EventType)"UserLoggedIn"].Properties[new PropertyPath("EventCount")].ShouldEqual("count");
    [Fact] void should_have_decrement_operation() => _result.From[(EventType)"UserLoggedIn"].Properties[new PropertyPath("RetryCount")].ShouldEqual("decrement");
}
