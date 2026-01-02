// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.LanguageDefinition;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_AstToProjectionDefinitionCompiler;

public class when_compiling_simple_projection : Specification
{
    const string Dsl = "projection MyProjection => Users\n  from UserRegistered\n    key e.userId\n    name = e.name\n    email = e.email";

    ProjectionDefinition _result;

    void Because()
    {
        var tokenizer = new Tokenizer(Dsl);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var document = parser.Parse();
        var compiler = new Compiler();
        _result = compiler.Compile(document, new ProjectionId("test"), ProjectionOwner.Client, EventSequenceId.Log);
    }

    [Fact] void should_have_users_as_read_model() => _result.ReadModel.Value.ShouldEqual("Users");
    [Fact] void should_have_one_event_type() => _result.From.Count.ShouldEqual(1);
    [Fact] void should_have_user_registered_event() => _result.From.Keys.First().Id.Value.ShouldEqual("UserRegistered");
    [Fact] void should_have_key_set_to_userId() => _result.From.Values.First().Key.Value.ShouldEqual("userId");
    [Fact] void should_have_name_property_mapped() => _result.From.Values.First().Properties[new PropertyPath("name")].ShouldEqual("name");
    [Fact] void should_have_email_property_mapped() => _result.From.Values.First().Properties[new PropertyPath("email")].ShouldEqual("email");
}
