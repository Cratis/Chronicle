// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.CodeAnalysis.Analyzers;
using Cratis.Chronicle.CodeAnalysis.CodeFixes;
using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_AsyncVoidReactorCodeFixProvider;

public class when_the_handler_has_a_signature_contract : Analyzers.for_ReactorMethodAnalyzer.given.a_reactor_method_analyzer
{
    [Theory]
    [InlineData("public class Parent { public virtual void On(Happened @event) { } }", "Parent", "public override")]
    [InlineData("public interface IHandler { void On(Happened @event); }", "IHandler", "public")]
    [InlineData("public interface IHandler<T> { void On(T @event); }", "IHandler<Happened>", "public")]
    [InlineData("public interface IParent { void On(Happened @event); } public interface IHandler : IParent { }", "IHandler", "public")]
    public async Task should_warn_but_not_offer_a_signature_breaking_fix(string contract, string baseType, string modifiers)
    {
        var source = CreateSource($$"""
            [EventType] public class Happened { }
            {{contract}}
            public class Reactor : {{baseType}}, Cratis.Chronicle.Reactors.IReactor
            {
                {{modifiers}} async void On(Happened @event) { await Task.Yield(); }
            }
            """);
        await AnalyzerVerifier<ReactorMethodAnalyzer>.VerifyAnalyzer(
            source, new ExpectedDiagnostic(DiagnosticIds.ReactorMethodMustNotBeAsyncVoid, DiagnosticSeverity.Warning, "On"));
        var project = TestProject.CreateProject(source);
        var compilation = (await project.GetCompilationAsync())!;
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ReactorMethodAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();
        await VerifyNoFix(project.Documents.Single(), diagnostics.Single());
    }

    [Fact]
    public async Task should_not_offer_a_fix_for_an_explicit_interface_implementation()
    {
        var source = CreateSource("""
            [EventType] public class Happened { }
            public interface IHandler { void On(Happened @event); }
            public class Reactor : IHandler, Cratis.Chronicle.Reactors.IReactor
            {
                async void IHandler.On(Happened @event) { await Task.Yield(); }
            }
            """);
        await AnalyzerVerifier<ReactorMethodAnalyzer>.VerifyAnalyzer(source);
        var document = TestProject.CreateProject(source).Documents.Single();
        var root = (await document.GetSyntaxRootAsync())!;
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(candidate => candidate.ExplicitInterfaceSpecifier is not null);
        var rule = new ReactorMethodAnalyzer().SupportedDiagnostics.Single(candidate => candidate.Id == DiagnosticIds.ReactorMethodMustNotBeAsyncVoid);
        var diagnostic = Diagnostic.Create(rule, method.Identifier.GetLocation(), "On");
        await VerifyNoFix(document, diagnostic);
    }

    static async Task VerifyNoFix(Document document, Diagnostic diagnostic)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await new AsyncVoidReactorCodeFixProvider().RegisterCodeFixesAsync(context);
        actions.ShouldBeEmpty();
    }
}
