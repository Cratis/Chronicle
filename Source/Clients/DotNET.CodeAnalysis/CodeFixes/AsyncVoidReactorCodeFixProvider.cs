// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Changes an async void reactor handler to async Task without changing its body.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncVoidReactorCodeFixProvider)), Shared]
public class AsyncVoidReactorCodeFixProvider : CodeFixProvider
{
    const string Title = "Return Task from async reactor handler";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.ReactorMethodMustNotBeAsyncVoid];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var method = root?.FindNode(context.Span).FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetDeclaredSymbol(method, context.CancellationToken) is not { IsAsync: true, ReturnsVoid: true })
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(Title, cancellationToken => ReturnTask(context.Document, method, cancellationToken), Title),
            context.Diagnostics);
    }

    static async Task<Document> ReturnTask(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var returnType = SyntaxFactory.ParseTypeName("global::System.Threading.Tasks.Task").WithTriviaFrom(method.ReturnType);
        return document.WithSyntaxRoot(root.ReplaceNode(method, method.WithReturnType(returnType)));
    }
}
