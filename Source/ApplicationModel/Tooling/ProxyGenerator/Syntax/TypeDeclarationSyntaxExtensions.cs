// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aksio.Cratis.Applications.ProxyGenerator.Syntax;

/// <summary>
/// Extension methods for working with <see cref="TypeDeclarationSyntax"/>.
/// </summary>
public static class TypeDeclarationSyntaxExtensions
{
    /// <summary>
    /// Check whether or not a type (record or class) is adorned with the [DerivedType] attribute.
    /// </summary>
    /// <param name="syntax"><see cref="TypeDeclarationSyntax"/> to check.</param>
    /// <returns>True if it is adorned, false if not.</returns>
    public static bool IsDerivedType(this TypeDeclarationSyntax syntax) => syntax.AttributeLists.Any(_ => _.Attributes.Any(a => (a.Name as IdentifierNameSyntax)?.Identifier.Text == "DerivedType"));
}
