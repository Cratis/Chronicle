// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Monads;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Represents an implementation of <see cref="ILanguageService"/>.
/// </summary>
public class LanguageService : ILanguageService
{
    readonly ScreenplayCompiler _compiler = new();

    /// <inheritdoc/>
    public Result<CaptureDefinition, CompilerErrors> Compile(string definition)
    {
        var result = _compiler.CompileCapture(definition);
        var syntax = result.Value;
        var errors = GetErrors(result.Diagnostics);

        if (syntax is null || errors.HasErrors)
        {
            return errors;
        }

        var visitor = new CaptureDefinitionSyntaxVisitor();
        return visitor.Visit(syntax);
    }

    static CompilerErrors GetErrors(IEnumerable<Diagnostic> diagnostics)
    {
        var errors = diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => new CompilerError(diagnostic.Message, diagnostic.Location.Line, diagnostic.Location.Column));

        return new CompilerErrors(errors);
    }
}
