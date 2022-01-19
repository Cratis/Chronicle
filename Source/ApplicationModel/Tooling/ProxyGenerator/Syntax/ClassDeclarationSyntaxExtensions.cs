// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aksio.Cratis.Applications.ProxyGenerator.Syntax
{
    /// <summary>
    /// Extension methods for working with <see cref="ClassDeclarationSyntax"/>.
    /// </summary>
    public static class ClassDeclarationSyntaxExtensions
    {
        const char NestedClassDelimiter = '+';
        const char NamespaceClassDelimiter = '.';

        /// <summary>
        /// Get the fully qualified name of a <see cref="ClassDeclarationSyntax"/>.
        /// </summary>
        /// <param name="source"><see cref="ClassDeclarationSyntax"/> to get from.</param>
        /// <returns>Fully qualified name.</returns>
        public static string GetFullName(this ClassDeclarationSyntax source)
        {
            Contract.Requires(source != null);

            var items = new List<string>();
            var parent = source!.Parent;
            while (parent.IsKind(SyntaxKind.ClassDeclaration))
            {
                var parentClass = parent as ClassDeclarationSyntax;
                Contract.Assert(parentClass != null);
                items.Add(parentClass!.Identifier.Text);

                parent = parent.Parent;
            }

            var nameSpace = parent as NamespaceDeclarationSyntax;
            Contract.Assert(nameSpace != null);
            var stringBuilder = new StringBuilder().Append(nameSpace!.Name).Append(NamespaceClassDelimiter);
            items.Reverse();
            items.ForEach(i => stringBuilder.Append(i).Append(NestedClassDelimiter));
            stringBuilder.Append(source.Identifier.Text);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Check whether or not a class is an ASP.NET Core Controller.
        /// </summary>
        /// <param name="syntax"><see cref="ClassDeclarationSyntax"/> to check.</param>
        /// <returns>True if it is a controller, false if not.</returns>
        public static bool IsController(this ClassDeclarationSyntax syntax) => syntax.BaseList?.Types.Any(_ => _.Type.GetName() == "Controller") ?? false;
    }
}
