// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Represents an implementation of the <see cref="ILanguageService"/>.
/// </summary>
/// <param name="generator">The generator used to generate projection language definition strings.</param>
public class LanguageService(IGenerator generator) : ILanguageService
{
    /// <inheritdoc/>
    public Result<ProjectionDefinition, CompilerErrors> Compile(
        string definition,
        ProjectionOwner owner,
        IEnumerable<ReadModelDefinition> readModelDefinitions,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        try
        {
            var tokenizer = new Tokenizer(definition);
            var tokens = tokenizer.Tokenize();
            var parser = new Parser(tokens);
            var parseResult = parser.Parse();

            return parseResult.Match(
                document =>
                {
                    var compiler = new Compiler();
                    return compiler.Compile(document, owner, readModelDefinitions, eventTypeSchemas);
                },
                parsingErrors => CompilerErrors.FromParsingErrors(parsingErrors));
        }
        catch (InvalidOperationException ex)
        {
            // Handle tokenizer errors
            var errors = new CompilerErrors();
            errors.Add(ex.Message, 0, 0);
            return errors;
        }
    }

    /// <inheritdoc/>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        generator.Generate(definition, readModelDefinition);

    /// <inheritdoc/>
    public Result<ReadModelIdentifier, CompilerErrors> GetReadModelIdentifier(string definition)
    {
        try
        {
            var tokenizer = new Tokenizer(definition);
            var tokens = tokenizer.Tokenize();
            var parser = new Parser(tokens);
            var parseResult = parser.Parse();

            return parseResult.Match(
                document =>
                {
                    try
                    {
                        var compiler = new Compiler();
                        var identifier = compiler.GetReadModelIdentifier(document);
                        return Result<ReadModelIdentifier, CompilerErrors>.Success(identifier);
                    }
                    catch (InvalidOperationException ex)
                    {
                        var errors = new CompilerErrors();
                        errors.Add(ex.Message, 0, 0);
                        return errors;
                    }
                },
                parsingErrors => CompilerErrors.FromParsingErrors(parsingErrors));
        }
        catch (InvalidOperationException ex)
        {
            var errors = new CompilerErrors();
            errors.Add(ex.Message, 0, 0);
            return errors;
        }
    }
}
