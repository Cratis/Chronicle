// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;
using Cratis.Monads;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Types;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Represents an implementation of the <see cref="ILanguageService"/>.
/// </summary>
/// <param name="generator">The generator used to generate projection language definition strings.</param>
/// <param name="codeGenerators">The generators that render a projection as client code, one per language.</param>
public class LanguageService(
    IGenerator generator,
    IInstancesOf<IProjectionCodeGenerator> codeGenerators) : ILanguageService
{
    /// <summary>
    /// Screenplay requires a projection to declare at least one block. Chronicle's projection declaration
    /// language also counts `automap`, `no automap` and `sequence` as directives, so this specific
    /// diagnostic is relaxed when any of those are present on the projection.
    /// </summary>
    const string MissingDirectivesDiagnosticSuffix = "must contain at least one directive";

    readonly ScreenplayCompiler _compiler = new();

    /// <inheritdoc/>
    public Result<ProjectionDefinition, CompilerErrors> Compile(
        string definition,
        ProjectionOwner owner,
        IEnumerable<ReadModelDefinition> readModelDefinitions,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        var result = _compiler.CompileProjection(definition);
        var syntax = result.Value;
        var hasDirectiveEquivalents = syntax is not null && (syntax.AutoMap != AutoMapMode.Inherit || syntax.Sequence is not null);
        var errors = GetErrors(result.Diagnostics, ignoreMissingDirectives: hasDirectiveEquivalents);

        if (syntax is null || errors.HasErrors)
        {
            return errors;
        }

        // Validate the projection if schemas are provided
        if (readModelDefinitions.Any() || eventTypeSchemas.Any())
        {
            var validator = new ProjectionValidator(readModelDefinitions, eventTypeSchemas);
            validator.Validate(syntax, errors);

            if (errors.HasErrors)
            {
                return errors;
            }
        }

        var visitor = new ProjectionDefinitionSyntaxVisitor(owner);
        return visitor.Visit(syntax);
    }

    /// <inheritdoc/>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        generator.Generate(definition, readModelDefinition);

    /// <inheritdoc/>
    public Result<ReadModelIdentifier, CompilerErrors> GetReadModelIdentifier(string definition)
    {
        var result = _compiler.CompileProjection(definition);
        var syntax = result.Value;

        // Resolving the read model identifier only needs the projection declaration itself,
        // so an otherwise empty projection body is not an error here.
        var errors = GetErrors(result.Diagnostics, ignoreMissingDirectives: true);

        if (syntax is null || errors.HasErrors)
        {
            return errors;
        }

        return syntax.ReadModel is null ? ReadModelIdentifier.Inferred : new ReadModelIdentifier(syntax.ReadModel);
    }

    /// <inheritdoc/>
    public string GenerateDeclarativeCode(ProjectionDefinition definition, ReadModelDefinition readModelDefinition, ProjectionCodeLanguage language = ProjectionCodeLanguage.CSharp) =>
        GeneratorFor(language, ProjectionCodeStyle.Declarative).GenerateDeclarative(definition, readModelDefinition);

    /// <inheritdoc/>
    public string GenerateModelBoundCode(ProjectionDefinition definition, ReadModelDefinition readModelDefinition, ProjectionCodeLanguage language = ProjectionCodeLanguage.CSharp) =>
        GeneratorFor(language, ProjectionCodeStyle.ModelBound).GenerateModelBound(definition, readModelDefinition);

    /// <inheritdoc/>
    public IEnumerable<ProjectionCodeLanguage> GetLanguagesSupporting(ProjectionCodeStyle style) =>
        codeGenerators.Where(generator => generator.Supports(style)).Select(generator => generator.Language).ToArray();

    static CompilerErrors GetErrors(IEnumerable<Diagnostic> diagnostics, bool ignoreMissingDirectives)
    {
        var errors = diagnostics
            .Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error &&
                !(ignoreMissingDirectives && diagnostic.Message.EndsWith(MissingDirectivesDiagnosticSuffix, StringComparison.Ordinal)))
            .Select(diagnostic => new CompilerError(diagnostic.Message, diagnostic.Location.Line, diagnostic.Location.Column));

        return new CompilerErrors(errors);
    }

    IProjectionCodeGenerator GeneratorFor(ProjectionCodeLanguage language, ProjectionCodeStyle style)
    {
        var codeGenerator = codeGenerators.FirstOrDefault(candidate => candidate.Language == language);

        // A language with no generator at all, and one whose client has no API for the style asked
        // for, are the same answer to the caller: there is nothing to show for that combination.
        return codeGenerator?.Supports(style) == true
            ? codeGenerator
            : throw new ProjectionCodeGenerationNotSupported(language, style);
    }
}
