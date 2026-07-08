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
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Code fix provider that removes a redundant <c>.AutoMap()</c> call from a projection builder chain.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveRedundantAutoMapCallCodeFixProvider)), Shared]
public class RemoveRedundantAutoMapCallCodeFixProvider : CodeFixProvider
{
    const string Title = "Remove redundant .AutoMap() call";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        DiagnosticIds.RedundantAutoMapCall);

    /// <inheritdoc/>
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var invocation = root.FindNode(diagnostic.Location.SourceSpan)?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => RemoveAutoMapCallAsync(context.Document, invocation, c),
                equivalenceKey: Title),
            diagnostic);
    }

    static async Task<Document> RemoveAutoMapCallAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return document;
        }

        // Replace 'receiver.AutoMap()' with 'receiver', keeping the surrounding trivia of the invocation.
        var replacement = memberAccess.Expression.WithTriviaFrom(invocation);
        var newRoot = root.ReplaceNode(invocation, replacement);

        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }
}
