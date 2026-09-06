// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;
using Cratis.Chronicle.Storage;
using ProjectionCodeLanguage = Cratis.Chronicle.Concepts.Projections.ProjectionCodeLanguage;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a request to generate declarative projection client code from projection declaration language.
/// </summary>
/// <param name="EventStore">The event store the projection targets.</param>
/// <param name="Namespace">The namespace the projection targets.</param>
/// <param name="Declaration">The projection declaration language representation of the projection.</param>
/// <param name="DraftReadModel">Optional draft read model definition to use for code generation.</param>
/// <param name="Language">The language to generate for - one of the <see cref="ProjectionCodeLanguage"/> names. Defaults to C#.</param>
[Command]
[BelongsTo(WellKnownServices.ProjectionEditor)]
public record GenerateDeclarativeCode(EventStoreName EventStore, EventStoreNamespaceName Namespace, string Declaration, DraftReadModel? DraftReadModel = null, string Language = nameof(ProjectionCodeLanguage.CSharp))
{
    /// <summary>
    /// Handles the command by compiling the declaration and generating the projection as client code.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the read models and event types.</param>
    /// <param name="languageService">The <see cref="ILanguageService"/> to compile and generate with.</param>
    /// <returns>The generated code, or the syntax errors saying why nothing was generated.</returns>
    public Task<GeneratedCodeResult> Handle(IStorage storage, ILanguageService languageService) =>
        ProjectionCodeGeneration.Generate(
            EventStore,
            Declaration,
            DraftReadModel,
            ProjectionCodeGeneration.ParseLanguage(Language),
            ProjectionCodeStyle.Declarative,
            storage,
            languageService);
}
