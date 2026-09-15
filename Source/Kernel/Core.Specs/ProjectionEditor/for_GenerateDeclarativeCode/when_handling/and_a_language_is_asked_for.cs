// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ProjectionEditor.for_GenerateDeclarativeCode.when_handling;

/// <summary>
/// The language crosses the wire by name, so the command has to read it back to the enum the language service
/// takes - case-insensitively, because the name is whatever the caller typed.
/// </summary>
public class and_a_language_is_asked_for : given.a_declaration_to_generate_from
{
    void Establish() => Compiles(ReadModel);

    async Task Because() => await new GenerateDeclarativeCode(EventStore, "Default", Declaration, Language: "typescript").Handle(_storage, _languageService);

    [Fact] void should_generate_for_that_language() =>
        _languageService.Received(1).GenerateDeclarativeCode(
            Arg.Any<ProjectionDefinition>(),
            Arg.Any<ReadModelDefinition>(),
            ProjectionCodeLanguage.TypeScript);
}
