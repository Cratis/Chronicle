// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Testing;

public static class AnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    /// Verify analyzer diagnostics for the provided source.
    /// </summary>
    /// <param name="source">The C# source to analyze.</param>
    /// <param name="expected">The expected diagnostics.</param>
    /// <returns>A task representing the verification.</returns>
    public static async Task VerifyAnalyzer(string source, params ExpectedDiagnostic[] expected)
    {
        var markedSource = SourceMarker.Parse(source);
        var project = TestProject.CreateProject(markedSource.Source);
        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer());
        var compilationWithAnalyzers = compilation!.WithAnalyzers(analyzers);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
        var orderedDiagnostics = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

        orderedDiagnostics.Length.ShouldEqual(expected.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            VerifyDiagnostic(orderedDiagnostics[i], expected[i], markedSource.Markers, i);
        }
    }

    static void VerifyDiagnostic(Diagnostic diagnostic, ExpectedDiagnostic expected, IReadOnlyDictionary<int, TextSpan> markers, int index)
    {
        diagnostic.Id.ShouldEqual(expected.Id);
        diagnostic.Severity.ShouldEqual(expected.Severity);

        foreach (var argument in expected.MessageArguments)
        {
            diagnostic.GetMessage().ShouldContain(argument);
        }

        if (TryGetExpectedSpan(markers, index, out var span))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            diagnosticSpan.Start.ShouldBeGreaterThanOrEqual(span.Start);
            diagnosticSpan.End.ShouldBeLessThanOrEqual(span.End);
        }
    }

    static bool TryGetExpectedSpan(IReadOnlyDictionary<int, TextSpan> markers, int index, out TextSpan span)
    {
        if (markers.TryGetValue(index, out span))
        {
            return true;
        }

        if (markers.Count == 1 && markers.TryGetValue(0, out span))
        {
            return true;
        }

        span = default;
        return false;
    }
}
