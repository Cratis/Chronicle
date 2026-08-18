// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Describes how the data a query or handler produces becomes the data the contract declares.
/// </summary>
/// <param name="ContractTypeName">The fully qualified contract type the data travels as.</param>
/// <param name="Apply">Rewrites an expression producing the domain value into one producing the contract value.</param>
/// <param name="IsIdentity">Whether the domain value already is the contract value.</param>
/// <param name="MethodGroup">The name of a generated method that performs the conversion, when there is one.</param>
public record ImplementationDataMapping(string ContractTypeName, Func<string, string> Apply, bool IsIdentity, string? MethodGroup = null)
{
    /// <summary>
    /// Resolves the mapping for a type a query or handler produces.
    /// </summary>
    /// <param name="type">The type the artifact produces.</param>
    /// <param name="readModelType">The read model the query is declared on, when the caller is a query.</param>
    /// <param name="context">The generation context.</param>
    /// <param name="depth">How deeply nested in a sequence the type is, so element lambdas do not shadow each other.</param>
    /// <returns>The mapping.</returns>
    /// <exception cref="UnsupportedServiceShape">Thrown when the shape cannot be dispatched to.</exception>
    public static ImplementationDataMapping For(Type type, Type? readModelType, ImplementationContext context, int depth = 0)
    {
        if (TypeHelper.IsOneOfType(type))
        {
            throw new UnsupportedServiceShape(
                type.FullName ?? type.Name,
                "a query returning OneOf has no single success value the generator can unwrap - return the value, or throw for the failure.");
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return ForNullable(underlying, readModelType, context, depth);
        }

        if (SequenceElement(type) is { } element)
        {
            var inner = For(element, readModelType, context, depth + 1);
            var typeName = $"IEnumerable<{inner.ContractTypeName}>";

            if (inner.IsIdentity)
            {
                return new(typeName, expression => expression, true);
            }

            // Mapping introduces a lazy Select, and what leaves here is serialized from its runtime type.
            // Materialize so the sequence that reaches the serializer is the one this code describes.
            var name = $"element{depth}";
            var projection = inner.MethodGroup ?? $"{name} => {inner.Apply(name)}";
            return new(typeName, expression => $"{expression}.Select({projection}).ToList()", false);
        }

        if (TypeHelper.IsReadModelType(type))
        {
            if (readModelType is not null && type != readModelType)
            {
                throw new UnsupportedServiceShape(
                    type.FullName ?? type.Name,
                    $"a query on '{readModelType.Name}' returning a different read model would be described by the wrong message - move the query onto '{type.Name}'.");
            }

            var mapping = context.MappingForReadModel(type);
            return new(mapping.ContractTypeName, expression => $"{mapping.MethodName}({expression})", false, mapping.MethodName);
        }

        if (TypeHelper.IsConceptType(type))
        {
            var unwrapped = TypeHelper.UnwrapConceptType(type);
            return For(unwrapped, readModelType, context, depth) with
            {
                Apply = expression => ImplementationValues.ToContract(expression, type),
                IsIdentity = false
            };
        }

        // A type protobuf cannot represent travels as its stand-in, so the contract declares the stand-in and the
        // conversion is written out here. Identity is wrong even though the conversion is implicit: what the
        // artifact returns is Task<DateTimeOffset>, which no amount of variance makes a
        // Task<SerializableDateTimeOffset>. See TransportTypes.
        if (TransportTypes.NameFor(type) is { } transport)
        {
            var transportTypeName = $"global::{TransportTypes.PrimitivesNamespace}.{transport}";
            return new(transportTypeName, expression => $"({transportTypeName}){expression}", false);
        }

        return new(QualifiedTypeName.For(type), expression => expression, true);
    }

    /// <summary>
    /// Resolves the mapping for a nullable value type.
    /// </summary>
    /// <param name="underlying">The type the nullable wraps.</param>
    /// <param name="readModelType">The read model the query is declared on, when the caller is a query.</param>
    /// <param name="context">The generation context.</param>
    /// <param name="depth">How deeply nested in a sequence the type is.</param>
    /// <returns>The mapping.</returns>
    /// <exception cref="UnsupportedServiceShape">Thrown when the nullable wraps a type that needs converting.</exception>
    static ImplementationDataMapping ForNullable(Type underlying, Type? readModelType, ImplementationContext context, int depth)
    {
        // A stand-in declares conversions for the nullable form too, so it absorbs the null rather than needing
        // the generator to branch around it.
        if (TransportTypes.NameFor(underlying) is { } transport)
        {
            var transportTypeName = $"global::{TransportTypes.PrimitivesNamespace}.{transport}?";
            return new(transportTypeName, expression => $"({transportTypeName}){expression}", false);
        }

        var inner = For(underlying, readModelType, context, depth);
        return inner.IsIdentity
            ? new($"{inner.ContractTypeName}?", expression => expression, true)
            : throw new UnsupportedServiceShape(
                underlying.FullName ?? underlying.Name,
                "a nullable of a type that needs converting has no defined null behavior on the wire - use the non-nullable form.");
    }

    /// <summary>
    /// Gets the element type when a type is a sequence the wire carries as a repeated field.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The element type, or null when the type is not a sequence.</returns>
    static Type? SequenceElement(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition().FullName?.StartsWith("System.Collections.Generic.IEnumerable`", StringComparison.Ordinal) == true)
        {
            return type.GetGenericArguments()[0];
        }

        return null;
    }
}
