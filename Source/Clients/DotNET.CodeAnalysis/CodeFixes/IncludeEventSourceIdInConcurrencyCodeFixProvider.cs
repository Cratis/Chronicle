// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Cratis.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Offers an explicit per-source narrowing of a declared concurrency condition.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IncludeEventSourceIdInConcurrencyCodeFixProvider)), Shared]
public class IncludeEventSourceIdInConcurrencyCodeFixProvider : CodeFixProvider
{
    const string Title = "Include EventSourceId in concurrency";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.ConcurrencyWithoutEventSourceId);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        if (root?.FindNode(diagnostic.Location.SourceSpan) is not ExpressionSyntax expression) return;
        context.RegisterCodeFix(CodeAction.Create(Title, c => IncludeSource(context.Document, expression, c), Title), diagnostic);
    }

    static async Task<Document> IncludeSource(Document document, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;
        var flag = SyntaxFactory.ParseExpression("global::Cratis.Chronicle.EventSources.ConcurrencyDimensions.EventSourceId")
            .WithAdditionalAnnotations(Simplifier.Annotation);
        var replacement = SyntaxFactory.BinaryExpression(
            SyntaxKind.BitwiseOrExpression,
            SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia()).WithAdditionalAnnotations(Simplifier.Annotation),
            flag)
            .WithTriviaFrom(expression).WithAdditionalAnnotations(Formatter.Annotation);
        return document.WithSyntaxRoot(root.ReplaceNode(expression, replacement));
    }
}
