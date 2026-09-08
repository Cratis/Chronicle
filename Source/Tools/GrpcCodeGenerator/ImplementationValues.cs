// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Renders the expressions that move a value between its domain type and the type it travels as.
/// </summary>
/// <remarks>
/// The contracts carry the unwrapped form of every concept, so the conversion has to happen somewhere. It
/// happens here, in the generated implementation, which is the only place that sees both sides at once.
/// </remarks>
public static class ImplementationValues
{
    /// <summary>
    /// Renders reading a value off the wire and handing it to an artifact that declares a domain type.
    /// </summary>
    /// <param name="expression">The expression reading the wire value.</param>
    /// <param name="declaredType">The type the artifact declares.</param>
    /// <param name="isNullable">
    /// Whether the artifact declares this value as optional - a nullable value type is already discoverable from
    /// <paramref name="declaredType"/> alone, but a nullable reference type is not, so the caller has to say.
    /// </param>
    /// <returns>The expression to pass to the artifact.</returns>
    /// <exception cref="UnsupportedServiceShape">
    /// Thrown when <paramref name="declaredType"/> is a nullable value type wrapping a concept or a shared type -
    /// there is no proven conversion shape for that as a request parameter.
    /// </exception>
    public static string ToDomain(string expression, Type declaredType, bool isNullable = false)
    {
        // A nullable value type wrapping something that needs converting (a struct-backed concept, a nullable
        // enum shared type) has no defined behavior here: unlike the response side's ForNullable, which special
        // cases a transport stand-in and otherwise refuses, a request parameter is rare enough there is no
        // proven cast/null-check shape to fall back to. Refuse rather than silently treating the wire value as
        // already being the domain type, which is what happens when a generic type - Nullable<T> is one - falls
        // through the checks below untouched.
        if (Nullable.GetUnderlyingType(declaredType) is { } underlyingType &&
            (TypeHelper.IsConceptType(underlyingType) || SharedTypeRegistry.QualifiedNameFor(underlyingType) is not null))
        {
            throw new UnsupportedServiceShape(
                declaredType.Name,
                "a nullable value type that needs converting has no defined behavior as a request parameter - use the non-nullable form.");
        }

        if (TypeHelper.IsConceptType(declaredType))
        {
            var cast = $"({QualifiedTypeName.For(declaredType)}){expression}";

            // A concept declared as an optional parameter is a nullable reference type - the check above only
            // refuses nullable *value* types. Its wire field is a nullable string, and a concept's implicit
            // conversion does not accept null, so casting straight through warns at every call site the
            // generator emits. Guard it instead: absent on the wire stays absent in the domain.
            return isNullable && !declaredType.IsValueType
                ? $"({expression} is null ? null : {cast})"
                : cast;
        }

        // The wire value is the generated mirror, a distinct CLR type from the Core type the artifact declares -
        // see SharedTypeRegistry. An enum converts with a cast; anything else is expected to carry a hand-written
        // ToApi() extension, the same convention CausationConverters/IdentityConverters already established. A
        // nullable reference type - a Type alone cannot say so, hence isNullable - guards the call: the artifact
        // declared this optional, so a missing value has to stay missing, not throw inside ToApi().
        if (SharedTypeRegistry.QualifiedNameFor(declaredType) is not null)
        {
            if (declaredType.IsEnum)
            {
                return $"({QualifiedTypeName.For(declaredType)}){expression}";
            }

            return isNullable ? $"{expression}?.ToApi()" : $"{expression}.ToApi()";
        }

        // A collection parameter needs its element converted the same way a scalar one would - a concept or a
        // shared type inside the sequence is still a distinct CLR type from what the artifact declares.
        if (ImplementationDataMapping.SequenceElement(declaredType) is { } elementType)
        {
            var elementConversion = ToDomain("x", elementType);
            if (elementConversion == "x")
            {
                return expression;
            }

            return isNullable
                ? $"{expression}?.Select(x => {elementConversion})"
                : $"{expression}.Select(x => {elementConversion})";
        }

        // A dictionary whose value needs converting has no single-element lambda shape LINQ's ToDictionary can
        // express as cleanly as Select does for a sequence, so - like a non-sequence shared type - it is expected
        // to carry a hand-written ToApi() extension over the whole dictionary rather than an inlined projection.
        if (ImplementationDataMapping.DictionaryValueElement(declaredType) is { } dictionaryValueType &&
            (TypeHelper.IsConceptType(dictionaryValueType) || SharedTypeRegistry.QualifiedNameFor(dictionaryValueType) is not null))
        {
            return isNullable ? $"{expression}?.ToApi()" : $"{expression}.ToApi()";
        }

        return expression;
    }

    /// <summary>
    /// Renders handing a domain value to the wire.
    /// </summary>
    /// <param name="expression">The expression reading the domain value.</param>
    /// <param name="declaredType">The type the artifact declares.</param>
    /// <returns>The expression to assign to the contract property.</returns>
    /// <remarks>
    /// A concept converts down to its primitive implicitly through <c>ConceptAs&lt;T&gt;</c>, but the cast is
    /// written out anyway: it states which primitive the contract expects, so a concept whose underlying type
    /// changes fails here rather than silently changing the wire shape.
    /// </remarks>
    public static string ToContract(string expression, Type declaredType)
    {
        if (TypeHelper.IsConceptType(declaredType))
        {
            return $"({QualifiedTypeName.For(TypeHelper.UnwrapConceptType(declaredType))}){expression}";
        }

        return expression;
    }

    /// <summary>
    /// Gets the property name a constructor parameter surfaces as.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <returns>The property name.</returns>
    public static string PropertyName(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToUpperInvariant(name[0]) + name[1..];
}
