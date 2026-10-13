// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Mirrors structural self-reducer recognition without assuming any client's event registry.
/// </summary>
internal static class SelfReducerPolicy
{
    /// <summary>
    /// Excludes abstract, generic and classic reducer declarations.
    /// </summary>
    /// <param name="type">The candidate model.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>Whether structural recognition applies.</returns>
    internal static bool CanBeSelf(INamedTypeSymbol type, Compilation compilation) =>
        type is { TypeKind: TypeKind.Class, IsAbstract: false, IsGenericType: false } &&
        !WellKnownTypes.ImplementsIReducer(type, compilation);

    /// <summary>
    /// Enumerates ordinary declarations including inherited and hidden methods.
    /// </summary>
    /// <param name="type">The model.</param>
    /// <returns>The declared hierarchy's methods.</returns>
    internal static IEnumerable<IMethodSymbol> Methods(INamedTypeSymbol type)
    {
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>().Where(method => method.MethodKind == MethodKind.Ordinary))
            {
                yield return method;
            }
        }
    }

    /// <summary>
    /// Recognizes at least one valid static transition.
    /// </summary>
    /// <param name="type">The candidate model.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>Whether the type is structurally a self reducer.</returns>
    internal static bool IsSelf(INamedTypeSymbol type, Compilation compilation) =>
        CanBeSelf(type, compilation) && Methods(type).Any(method => IsValid(method, type, compilation));

    /// <summary>
    /// Recognizes invalid static methods that refer to their model as state or result.
    /// </summary>
    /// <param name="method">The declaration.</param>
    /// <param name="model">The candidate model.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>Whether the method warrants a signature warning.</returns>
    internal static bool IsNearMiss(IMethodSymbol method, INamedTypeSymbol model, Compilation compilation) =>
        method.Name == "Reduce" && method.IsStatic &&
        (ReturnsModel(method, model, compilation) || Equal(method.Parameters.ElementAtOrDefault(1)?.Type, model));

    /// <summary>
    /// Checks the complete runtime static transition shape, accepting oblivious current state.
    /// </summary>
    /// <param name="method">The declaration.</param>
    /// <param name="model">The model.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>Whether the method satisfies the structural signal.</returns>
    internal static bool IsValid(IMethodSymbol method, INamedTypeSymbol model, Compilation compilation) =>
        method.Name == "Reduce" && method is { IsStatic: true, DeclaredAccessibility: Accessibility.Public, IsGenericMethod: false } &&
        method.Parameters.Length is 2 or 3 && method.Parameters.All(IsPlainParameter) &&
        method.Parameters[0].Type is { TypeKind: TypeKind.Class or TypeKind.Interface, SpecialType: not SpecialType.System_Object } &&
        Equal(method.Parameters[1].Type, model) && method.Parameters[1].NullableAnnotation != NullableAnnotation.NotAnnotated &&
        (method.Parameters.Length == 2 || Equal(method.Parameters[2].Type, compilation.GetTypeByMetadataName(WellKnownTypes.EventContextName))) &&
        ReturnsModel(method, model, compilation);

    /// <summary>
    /// Checks synchronous and asynchronous model results.
    /// </summary>
    /// <param name="method">The declaration.</param>
    /// <param name="model">The model.</param>
    /// <param name="compilation">The owning compilation.</param>
    /// <returns>Whether the result is the exact model or Task of that model.</returns>
    internal static bool ReturnsModel(IMethodSymbol method, INamedTypeSymbol model, Compilation compilation) =>
        Equal(method.ReturnType, model) ||
        (method.ReturnType is INamedTypeSymbol { IsGenericType: true } task &&
         Equal(task.OriginalDefinition, compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")) && Equal(task.TypeArguments[0], model));

    /// <summary>
    /// Rejects by-ref, optional and params arguments.
    /// </summary>
    /// <param name="parameter">The argument declaration.</param>
    /// <returns>Whether the argument is ordinary.</returns>
    internal static bool IsPlainParameter(IParameterSymbol parameter) =>
        parameter.RefKind == RefKind.None && !parameter.IsOptional && !parameter.IsParams;

    /// <summary>
    /// Compares CLR symbol identities independently of nullable annotations.
    /// </summary>
    /// <param name="first">The first symbol.</param>
    /// <param name="second">The second symbol.</param>
    /// <returns>Whether the symbols are equal.</returns>
    internal static bool Equal(ISymbol? first, ISymbol? second) => SymbolEqualityComparer.Default.Equals(first, second);

    /// <summary>
    /// Reads type attributes while respecting inherited metadata.
    /// </summary>
    /// <param name="type">The model.</param>
    /// <returns>Its own and inherited attributes.</returns>
    internal static IEnumerable<AttributeData> Attributes(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var attribute in current.GetAttributes())
            {
                if (Equal(current, type) || IsInherited(attribute.AttributeClass)) yield return attribute;
            }
        }
    }

    /// <summary>
    /// Enumerates declarations beneath a namespace or type.
    /// </summary>
    /// <param name="owner">The declaration owner.</param>
    /// <returns>All nested types.</returns>
    internal static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol owner)
    {
        foreach (var member in owner.GetMembers().OfType<INamespaceOrTypeSymbol>())
        {
            if (member is INamedTypeSymbol type) yield return type;
            foreach (var nested in Types(member)) yield return nested;
        }
    }

    static bool IsInherited(INamedTypeSymbol? attribute)
    {
        for (var current = attribute; current is not null; current = current.BaseType)
        {
            var usage = current.GetAttributes().FirstOrDefault(item => item.AttributeClass?.ToDisplayString() == "System.AttributeUsageAttribute");
            if (usage is not null) return !usage.NamedArguments.Any(argument => argument.Key == "Inherited" && argument.Value.Value is false);
        }
        return true;
    }
}
