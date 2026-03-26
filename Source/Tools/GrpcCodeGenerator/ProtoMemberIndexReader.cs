// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Reads existing [ProtoMember] index assignments from a generated C# file using Roslyn.
/// </summary>
public static class ProtoMemberIndexReader
{
    /// <summary>
    /// Reads the existing [ProtoMember(N)] indexes for all properties of a given class in the
    /// specified file. Returns an empty dictionary if the file does not exist or the class is
    /// not found.
    /// </summary>
    /// <param name="filePath">Full path to the C# source file.</param>
    /// <param name="className">The name of the class whose property indexes should be read.</param>
    /// <returns>A dictionary mapping property name to its [ProtoMember] index.</returns>
    public static IReadOnlyDictionary<string, int> ReadExistingIndexes(string filePath, string className)
    {
        if (!File.Exists(filePath))
        {
            return new Dictionary<string, int>();
        }

        var source = File.ReadAllText(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var root = syntaxTree.GetCompilationUnitRoot();

        return ExtractIndexesFromClass(root, className);
    }

    /// <summary>
    /// Reads the existing [ProtoMember(N)] indexes for all properties of a given class from the
    /// specified C# source text. Returns an empty dictionary if the class is not found.
    /// </summary>
    /// <param name="source">The C# source text to parse.</param>
    /// <param name="className">The name of the class whose property indexes should be read.</param>
    /// <returns>A dictionary mapping property name to its [ProtoMember] index.</returns>
    public static IReadOnlyDictionary<string, int> ReadExistingIndexesFromSource(string source, string className)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var root = syntaxTree.GetCompilationUnitRoot();
        return ExtractIndexesFromClass(root, className);
    }

    static Dictionary<string, int> ExtractIndexesFromClass(CompilationUnitSyntax root, string className)
    {
        // Property names are always PascalCase in generated files (enforced by ToPascalCase in the
        // generator), so ordinal comparison is correct and sufficient here.
        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        var classDecl = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == className);

        if (classDecl is null)
        {
            return result;
        }

        foreach (var propertyDecl in classDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            var index = GetProtoMemberIndex(propertyDecl);
            if (index.HasValue)
            {
                result[propertyDecl.Identifier.Text] = index.Value;
            }
        }

        return result;
    }

    static int? GetProtoMemberIndex(PropertyDeclarationSyntax propertyDecl)
    {
        foreach (var attributeList in propertyDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (!name.Equals("ProtoMember", StringComparison.Ordinal) &&
                    !name.Equals("ProtoBuf.ProtoMember", StringComparison.Ordinal))
                {
                    continue;
                }

                var arguments = attribute.ArgumentList?.Arguments;
                if (arguments is null || arguments.Value.Count == 0)
                {
                    continue;
                }

                var firstArg = arguments.Value[0];
                if (firstArg.Expression is LiteralExpressionSyntax literal &&
                    literal.Token.Value is int intValue)
                {
                    return intValue;
                }
            }
        }

        return null;
    }
}
