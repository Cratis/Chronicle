// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Testing;

public static class CodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    /// <summary>
    /// Verify the code fix output for the provided source.
    /// </summary>
    /// <param name="source">The C# source to analyze.</param>
    /// <param name="fixedSource">The expected fixed source.</param>
    /// <param name="expected">The expected diagnostic.</param>
    /// <returns>A task representing the verification.</returns>
    public static async Task VerifyCodeFix(string source, string fixedSource, params ExpectedDiagnostic[] expected)
    {
        await AnalyzerVerifier<TAnalyzer>.VerifyAnalyzer(source, expected).ConfigureAwait(false);

        var markedSource = SourceMarker.Parse(source);
        var project = TestProject.CreateProject(markedSource.Source);
        var document = project.Documents.First();
        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer());
        var compilationWithAnalyzers = compilation!.WithAnalyzers(analyzers);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
        var orderedDiagnostics = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

        orderedDiagnostics.Length.ShouldEqual(expected.Length);

        var diagnostic = orderedDiagnostics.FirstOrDefault();
        diagnostic.ShouldNotBeNull();

        var actions = new List<CodeAction>();
        var codeFixProvider = new TCodeFix();
        var context = new CodeFixContext(document, diagnostic!, (action, _) => actions.Add(action), CancellationToken.None);
        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

        actions.ShouldNotBeEmpty();

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        var applyChanges = operations.OfType<ApplyChangesOperation>().FirstOrDefault();
        applyChanges.ShouldNotBeNull();

        var newSolution = applyChanges!.ChangedSolution;
        var newDocument = newSolution.GetDocument(document.Id);
        newDocument.ShouldNotBeNull();

        var newText = await newDocument!.GetTextAsync().ConfigureAwait(false);
        NormalizeWhitespace(newText.ToString()).ShouldEqual(NormalizeWhitespace(fixedSource));
    }

    /// <summary>
    /// Normalize line whitespace for comparisons.
    /// </summary>
    /// <param name="source">The source string to normalize.</param>
    /// <returns>The normalized string.</returns>
    static string NormalizeWhitespace(string source)
    {
        return string.Join('\n', source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(line => line.Trim()));
    }
}
