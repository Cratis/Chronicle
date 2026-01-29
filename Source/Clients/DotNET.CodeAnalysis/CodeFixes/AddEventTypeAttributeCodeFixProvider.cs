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
/// Code fix provider that adds the [EventType] attribute to types.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddEventTypeAttributeCodeFixProvider)), Shared]
public class AddEventTypeAttributeCodeFixProvider : CodeFixProvider
{
    const string Title = "Add [EventType] attribute";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        DiagnosticIds.EventTypeMustHaveAttributeWhenAppended,
        DiagnosticIds.ModelBoundProjectionEventTypeMustHaveAttribute,
        DiagnosticIds.ReactorEventParameterMustHaveAttribute,
        DiagnosticIds.ReducerEventParameterMustHaveAttribute);

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
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration or parameter
        var node = root.FindToken(diagnosticSpan.Start).Parent;
        if (node == null)
        {
            return;
        }

        // Navigate up to find the type symbol
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        ITypeSymbol? typeSymbol = null;

        // Try different node types to find the type symbol
        if (node is IdentifierNameSyntax identifierName)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(identifierName);
            if (symbolInfo.Symbol is ITypeSymbol type)
            {
                typeSymbol = type;
            }
            else if (symbolInfo.Symbol is IParameterSymbol parameter)
            {
                typeSymbol = parameter.Type;
            }
        }
        else if (node is ParameterSyntax parameterSyntax)
        {
            var paramSymbol = semanticModel.GetDeclaredSymbol(parameterSyntax);
            if (paramSymbol != null)
            {
                typeSymbol = paramSymbol.Type;
            }
        }

        if (typeSymbol == null)
        {
            return;
        }

        // Find the type declaration in the current solution
        var typeDeclarationLocation = typeSymbol.Locations.FirstOrDefault(loc => loc.IsInSource);
        if (typeDeclarationLocation == null)
        {
            return;
        }

        var typeDocument = context.Document.Project.Solution.GetDocument(typeDeclarationLocation.SourceTree);
        if (typeDocument == null)
        {
            return;
        }

        // Register the code fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedSolution: c => AddEventTypeAttributeAsync(typeDocument, typeSymbol, c),
                equivalenceKey: Title),
            diagnostic);
    }

    static async Task<Solution> AddEventTypeAttributeAsync(Document document, ITypeSymbol typeSymbol, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document.Project.Solution;
        }

        var typeDeclarationLocation = typeSymbol.Locations.FirstOrDefault(loc => loc.IsInSource);
        if (typeDeclarationLocation == null)
        {
            return document.Project.Solution;
        }

        var typeDeclaration = root.FindNode(typeDeclarationLocation.SourceSpan);
        if (typeDeclaration is not TypeDeclarationSyntax typeDecl)
        {
            return document.Project.Solution;
        }

        // Create the EventType attribute
        var eventTypeAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("EventType"));
        var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(eventTypeAttribute));

        // Add the attribute to the type
        var newTypeDecl = typeDecl.AddAttributeLists(attributeList);

        // Add using directive if not present
        var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

        // Check if we need to add using directive
        if (newRoot is CompilationUnitSyntax compilationUnit)
        {
            var hasUsing = compilationUnit.Usings.Any(u =>
                u.Name?.ToString() == "Cratis.Chronicle.Concepts.Events");

            if (!hasUsing)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName("Cratis.Chronicle.Concepts.Events"));
                newRoot = compilationUnit.AddUsings(usingDirective);
            }
        }

        var newDocument = document.WithSyntaxRoot(newRoot);
        return newDocument.Project.Solution;
    }
}
