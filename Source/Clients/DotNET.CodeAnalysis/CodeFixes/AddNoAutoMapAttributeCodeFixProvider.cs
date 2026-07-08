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
/// Code fix provider that adds a property-level [NoAutoMap] attribute to an explicitly sourced read model
/// property so AutoMap from another subscribed event cannot overwrite it.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddNoAutoMapAttributeCodeFixProvider)), Shared]
public class AddNoAutoMapAttributeCodeFixProvider : CodeFixProvider
{
    const string Title = "Add [NoAutoMap] attribute";
    const string NoAutoMapNamespace = "Cratis.Chronicle.Projections";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        DiagnosticIds.AutoMapSameNamePropertyCollision);

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
        var node = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;
        var member = node?.FirstAncestorOrSelf<ParameterSyntax>() as SyntaxNode
            ?? node?.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
        if (member == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => AddNoAutoMapAsync(context.Document, member, c),
                equivalenceKey: Title),
            diagnostic);
    }

    static async Task<Document> AddNoAutoMapAsync(Document document, SyntaxNode member, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        var attributeList = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("NoAutoMap"))))
            .WithTrailingTrivia(SyntaxFactory.Space);

        var newMember = member switch
        {
            ParameterSyntax parameter => parameter.AddAttributeLists(attributeList),
            PropertyDeclarationSyntax property => property.AddAttributeLists(attributeList),
            _ => member
        };

        var newRoot = root.ReplaceNode(member, newMember);

        if (newRoot is CompilationUnitSyntax compilationUnit &&
            !compilationUnit.Usings.Any(u => u.Name?.ToString() == NoAutoMapNamespace))
        {
            newRoot = compilationUnit.AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(NoAutoMapNamespace)));
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
