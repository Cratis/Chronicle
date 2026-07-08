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
/// Code fix provider that removes a redundant <c>.Set(x =&gt; x.P).To(e =&gt; e.P)</c> mapping from a projection builder chain.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveRedundantSetToCodeFixProvider)), Shared]
public class RemoveRedundantSetToCodeFixProvider : CodeFixProvider
{
    const string Title = "Remove redundant .Set(...).To(...) mapping";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        DiagnosticIds.RedundantSetToWithMatchingNames);

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
        var toInvocation = root.FindNode(diagnostic.Location.SourceSpan)?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (toInvocation is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => RemoveSetToMappingAsync(context.Document, toInvocation, c),
                equivalenceKey: Title),
            diagnostic);
    }

    static async Task<Document> RemoveSetToMappingAsync(Document document, InvocationExpressionSyntax toInvocation, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // The '.To(...)' invocation's receiver is 'base.Set(<lambda>)'; replace the whole '.To(...)'
        // invocation with 'base' (the expression the '.Set' was called on), dropping the redundant mapping.
        if (toInvocation.Expression is not MemberAccessExpressionSyntax toMember ||
            toMember.Expression is not InvocationExpressionSyntax setInvocation ||
            setInvocation.Expression is not MemberAccessExpressionSyntax setMember)
        {
            return document;
        }

        var replacement = setMember.Expression.WithTriviaFrom(toInvocation);
        var newRoot = root.ReplaceNode(toInvocation, replacement);

        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }
}
