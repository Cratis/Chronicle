// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Monads;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Represents an implementation of <see cref="ILanguageService"/>.
/// </summary>
public class LanguageService : ILanguageService
{
    /// <inheritdoc/>
    public Result<CaptureDefinition, CompilerErrors> Compile(string definition)
    {
        var tokenizer = new Tokenizer(definition);
        var tokenizeResult = tokenizer.Tokenize();

        return tokenizeResult.Match(
            tokens =>
            {
                var parser = new Parser(tokens);
                var parseResult = parser.Parse();

                return parseResult.Match(
                    document =>
                    {
                        var compiler = new Compiler();
                        return compiler.Compile(document);
                    },
                    parsingErrors => CompilerErrors.FromParsingErrors(parsingErrors));
            },
            parsingErrors => CompilerErrors.FromParsingErrors(parsingErrors));
    }
}
