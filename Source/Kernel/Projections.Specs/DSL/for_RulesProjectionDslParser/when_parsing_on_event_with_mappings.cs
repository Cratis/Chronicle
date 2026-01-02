// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage;
using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.for_RulesProjectionDslParser;

public class when_parsing_on_event_with_mappings : Specification
{
    const string Dsl = "projection Test => Model\n  from EventType\n    key e.userId\n    name = e.fullName\n    email = e.emailAddress";

    FromEventBlock _onEvent;

    void Because()
    {
        var tokenizer = new Tokenizer(Dsl);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var result = parser.Parse();
        _onEvent = (FromEventBlock)result.Projections[0].Directives[0];
    }

    [Fact] void should_have_event_type() => _onEvent.EventType.Name.ShouldEqual("EventType");
    [Fact] void should_have_key() => _onEvent.Key.ShouldNotBeNull();
    [Fact] void should_have_two_mappings() => _onEvent.Mappings.Count.ShouldEqual(2);
    [Fact] void should_have_assignment_operations() => _onEvent.Mappings.All(m => m is AssignmentOperation).ShouldBeTrue();
}
