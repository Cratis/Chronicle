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

namespace Cratis.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Code fix provider that removes a redundant [Key] or [Subject] attribute from an EventSourceId&lt;T&gt; member.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveKeyOrSubjectAttributeCodeFixProvider)), Shared]
public class RemoveKeyOrSubjectAttributeCodeFixProvider : CodeFixProvider
{
    const string Title = "Remove redundant attribute";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        DiagnosticIds.KeyOrSubjectOnEventSourceId);

    /// <inheritdoc/>
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var attribute = root.FindNode(diagnostic.Location.SourceSpan)?.FirstAncestorOrSelf<AttributeSyntax>();
        if (attribute == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => RemoveAttributeAsync(context.Document, attribute, c),
                equivalenceKey: Title),
            diagnostic);
    }

    static async Task<Document> RemoveAttributeAsync(Document document, AttributeSyntax attribute, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null || attribute.Parent is not AttributeListSyntax attributeList)
        {
            return document;
        }

        // Remove the whole attribute list when this is its only attribute; otherwise drop just this attribute.
        var newRoot = attributeList.Attributes.Count == 1
            ? root.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia)
            : root.ReplaceNode(attributeList, attributeList.WithAttributes(attributeList.Attributes.Remove(attribute)));

        return newRoot == null ? document : document.WithSyntaxRoot(newRoot);
    }
}
