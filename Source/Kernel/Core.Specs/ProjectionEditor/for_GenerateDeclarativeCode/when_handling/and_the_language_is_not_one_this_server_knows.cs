// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ProjectionEditor.for_GenerateDeclarativeCode.when_handling;

/// <summary>
/// A name this server does not know means C#, which is what a caller that says nothing gets - the point of
/// carrying the language as a name rather than an enum is that an unknown value is not a wire error.
/// </summary>
public class and_the_language_is_not_one_this_server_knows : given.a_declaration_to_generate_from
{
    void Establish() => Compiles(ReadModel);

    async Task Because() => await new GenerateDeclarativeCode(EventStore, "Default", Declaration, Language: "cobol").Handle(_storage, _languageService);

    [Fact] void should_fall_back_to_csharp() =>
        _languageService.Received(1).GenerateDeclarativeCode(
            Arg.Any<ProjectionDefinition>(),
            Arg.Any<ReadModelDefinition>(),
            ProjectionCodeLanguage.CSharp);
}
