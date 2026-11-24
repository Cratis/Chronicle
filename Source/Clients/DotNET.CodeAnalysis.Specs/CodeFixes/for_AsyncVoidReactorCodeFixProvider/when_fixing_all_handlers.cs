// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.CodeAnalysis.Analyzers;
using Cratis.Chronicle.CodeAnalysis.CodeFixes;
using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_AsyncVoidReactorCodeFixProvider;

public class when_fixing_all_handlers : Analyzers.for_ReactorMethodAnalyzer.given.a_reactor_method_analyzer
{
    [Fact]
    public async Task should_fix_only_handlers_without_signature_contracts()
    {
        var source = CreateSource("""
            [EventType] public class Happened { }
            public interface IHandler { void Contract(Happened @event); }
            public class Reactor : IHandler, Cratis.Chronicle.Reactors.IReactor
            {
                public async void First(Happened @event) { await Task.Yield(); }
                public async void Second(Happened @event) => await Task.Delay(1);
                public async void Contract(Happened @event) { await Task.Yield(); }
                public void Helper() { }
            }
            """);
        var project = TestProject.CreateProject(source);
        var document = project.Documents.Single();
        var provider = new AsyncVoidReactorCodeFixProvider();
        var context = new FixAllContext(
            document,
            provider,
            FixAllScope.Document,
            "Return Task from async reactor handler",
            provider.FixableDiagnosticIds,
            new HandlerDiagnostics(),
            CancellationToken.None);

        var action = await provider.GetFixAllProvider().GetFixAsync(context);
        action.ShouldNotBeNull();
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var fixedDocument = solution.GetDocument(document.Id)!;
        var fixedText = (await fixedDocument.GetTextAsync()).ToString();
        var expected = source
            .Replace("async void First", "async global::System.Threading.Tasks.Task First", StringComparison.Ordinal)
            .Replace("async void Second", "async global::System.Threading.Tasks.Task Second", StringComparison.Ordinal);
        fixedText.ShouldEqual(expected);
        await AnalyzerVerifier<ReactorMethodAnalyzer>.VerifyAnalyzer(
            fixedText, new ExpectedDiagnostic(DiagnosticIds.ReactorMethodMustNotBeAsyncVoid, DiagnosticSeverity.Warning, "Contract"));
    }

    sealed class HandlerDiagnostics : FixAllContext.DiagnosticProvider
    {
        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var compilation = (await document.Project.GetCompilationAsync(cancellationToken))!;
            return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ReactorMethodAnalyzer()))
                .GetAnalyzerDiagnosticsAsync(cancellationToken);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Diagnostic>());

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            await GetDocumentDiagnosticsAsync(project.Documents.Single(), cancellationToken);
    }
}
