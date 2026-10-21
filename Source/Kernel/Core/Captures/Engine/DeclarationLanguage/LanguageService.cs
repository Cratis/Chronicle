// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Monads;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Represents an implementation of <see cref="ILanguageService"/>.
/// </summary>
/// <param name="logger">The logger.</param>
public class LanguageService(ILogger<LanguageService> logger) : ILanguageService
{
    static readonly ConcurrentDictionary<string, byte> _warnedDeclarations = new();
    readonly ScreenplayCompiler _compiler = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageService"/> class without logging.
    /// </summary>
    public LanguageService() : this(NullLogger<LanguageService>.Instance)
    {
    }

    /// <inheritdoc/>
    public Result<CaptureDefinition, CompilerErrors> Compile(string definition)
    {
        var legacy = LegacyEventsSource.Lift(definition);
        if (legacy.Sequence is not null && _warnedDeclarations.TryAdd(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(definition))), 0))
        {
            logger.DeprecatedSequenceSetting(legacy.Sequence);
        }

        var result = _compiler.CompileCapture(legacy.Source);
        var syntax = result.Value;
        var errors = GetErrors(result.Diagnostics);

        if (syntax is null || errors.HasErrors)
        {
            return errors;
        }

        try
        {
            var compiled = new CaptureDefinitionSyntaxVisitor().Visit(syntax);
            return compiled with
            {
                Source = compiled.Source with
                {
                    Sequence = legacy.Sequence,
                    Events = compiled.Source.Events?.Select(name => legacy.Names.GetValueOrDefault(name, name)).ToList()
                }
            };
        }
        catch (UnsupportedCaptureSyntax unsupported)
        {
            return new CompilerErrors([new CompilerError(unsupported.Message, unsupported.Node.Location.Line, unsupported.Node.Location.Column)]);
        }
    }

    static CompilerErrors GetErrors(IEnumerable<Diagnostic> diagnostics)
    {
        var errors = diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => new CompilerError(diagnostic.Message, diagnostic.Location.Line, diagnostic.Location.Column));

        return new CompilerErrors(errors);
    }
}
