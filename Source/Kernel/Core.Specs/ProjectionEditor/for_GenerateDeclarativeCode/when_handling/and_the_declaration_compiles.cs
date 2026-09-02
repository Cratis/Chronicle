// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ProjectionEditor.for_GenerateDeclarativeCode.when_handling;

public class and_the_declaration_compiles : given.a_declaration_to_generate_from
{
    GeneratedCodeResult _result;

    void Establish() => Compiles(ReadModel);

    async Task Because() => _result = await new GenerateDeclarativeCode(EventStore, "Default", Declaration).Handle(_storage, _languageService);

    [Fact] void should_return_the_generated_code() => _result.Code.ShouldEqual(GeneratedCode);
    [Fact] void should_not_report_any_errors() => _result.Errors.ShouldBeEmpty();

    [Fact] void should_generate_for_the_default_language() =>
        _languageService.Received(1).GenerateDeclarativeCode(
            Arg.Any<ProjectionDefinition>(),
            Arg.Any<ReadModelDefinition>(),
            ProjectionCodeLanguage.CSharp);
}
