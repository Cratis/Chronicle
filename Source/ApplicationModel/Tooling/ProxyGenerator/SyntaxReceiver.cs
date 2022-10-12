// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.ProxyGenerator.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aksio.Cratis.Applications.ProxyGenerator;

/// <summary>
/// Represents a <see cref="ISyntaxReceiver"/> that understands ASP.NET controllers and captures the types we want to generate proxies for.
/// </summary>
public class SyntaxReceiver : ISyntaxReceiver
{
    readonly List<ClassDeclarationSyntax> _candidates = new();
    readonly List<TypeDeclarationSyntax> _derivedTypes = new();

    /// <summary>
    /// Gets the candidates for code generation.
    /// </summary>
    public IEnumerable<ClassDeclarationSyntax> Candidates => _candidates;

    /// <summary>
    /// Gets the derived types for code generation.
    /// </summary>
    public IEnumerable<TypeDeclarationSyntax> DerivedTypes => _derivedTypes;

    /// <inheritdoc/>
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is TypeDeclarationSyntax typeSyntax && typeSyntax.IsDerivedType())
        {
            _derivedTypes.Add(typeSyntax);
            return;
        }
        if (syntaxNode is not ClassDeclarationSyntax classSyntax) return;
        if (!classSyntax.IsController()) return;

        _candidates.Add(classSyntax);
    }
}
