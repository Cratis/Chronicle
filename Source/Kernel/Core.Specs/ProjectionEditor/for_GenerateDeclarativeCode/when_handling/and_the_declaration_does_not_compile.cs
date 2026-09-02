// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

namespace Cratis.Chronicle.ProjectionEditor.for_GenerateDeclarativeCode.when_handling;

public class and_the_declaration_does_not_compile : given.a_declaration_to_generate_from
{
    GeneratedCodeResult _result;

    void Establish() => FailsToCompile(new CompilerError("something is wrong", 4, 2));

    async Task Because() => _result = await new GenerateDeclarativeCode(EventStore, "Default", Declaration).Handle(_storage, _languageService);

    [Fact] void should_not_generate_any_code() => _result.Code.ShouldBeEmpty();
    [Fact] void should_carry_the_error_message() => _result.Errors.Single().Message.ShouldEqual("something is wrong");
    [Fact] void should_carry_where_the_error_is() =>
        (_result.Errors.Single().Line, _result.Errors.Single().Column).ShouldEqual((4, 2));
}
