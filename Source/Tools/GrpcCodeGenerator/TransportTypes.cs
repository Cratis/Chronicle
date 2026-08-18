// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Maps CLR types that protobuf cannot represent onto the contract primitives that stand in for them on the wire.
/// </summary>
/// <remarks>
/// How a value travels is a transport concern, not a domain one, so the artifacts in Core declare the type the
/// domain means - <see cref="DateTimeOffset"/>, not some serializable stand-in - and the substitution happens here,
/// at the edge, on the way into the generated contracts.
/// <para>
/// This exists because the alternative failed silently. protobuf-net emits an opaque, <em>empty</em> message for a
/// type it cannot represent: a schema that parses, generates, and compiles, and transmits nothing. Ten fields
/// across four packages shipped that way - job creation times, user timestamps, event query date ranges - and only
/// non-.NET clients were affected, because protobuf-net has a runtime surrogate that papers over it for .NET.
/// </para>
/// <para>
/// So types are not merely mapped here, they are <em>classified</em>: anything protobuf cannot represent is either
/// given a stand-in or refused outright by <see cref="NameFor"/>. Emitting an empty message is no longer possible.
/// </para>
/// </remarks>
public static class TransportTypes
{
    /// <summary>
    /// The namespace the contract primitives live in, which generated files import.
    /// </summary>
    public const string PrimitivesNamespace = "Cratis.Chronicle.Contracts.Primitives";

    /// <summary>
    /// The stand-in for each CLR type protobuf cannot represent but Chronicle has a contract primitive for.
    /// </summary>
    /// <remarks>
    /// A stand-in has to convert implicitly both ways, so the hand-written service implementations that map between
    /// a Core artifact and its generated contract keep compiling without a cast at every assignment.
    /// </remarks>
    static readonly Dictionary<Type, string> _standIns = new()
    {
        // protobuf-net has no wire representation for DateTimeOffset. bcl.proto declares one, but the runtime
        // model never applies it, so what actually reaches the schema is an empty message. The ISO 8601 string
        // form is also what the pre-16.34.0 contracts used, so every already-published client understands it.
        [typeof(DateTimeOffset)] = $"global::{PrimitivesNamespace}.SerializableDateTimeOffset"
    };

    /// <summary>
    /// The CLR types protobuf cannot represent and Chronicle has no contract primitive for yet.
    /// </summary>
    static readonly HashSet<Type> _unrepresentable =
    [
        typeof(DateOnly),
        typeof(TimeOnly)
    ];

    /// <summary>
    /// Gets the contract type name to use on the wire for a CLR type.
    /// </summary>
    /// <param name="type">The CLR type as the Core artifact declares it.</param>
    /// <returns>The stand-in's type name, or null when the type needs no substitution.</returns>
    /// <exception cref="UnrepresentableTransportType">
    /// Thrown when protobuf cannot represent the type and no stand-in is defined for it. Refusing is the point: the
    /// alternative is a generated schema that looks complete and carries nothing.
    /// </exception>
    public static string? NameFor(Type type)
    {
        if (_standIns.TryGetValue(type, out var standIn))
        {
            return standIn;
        }

        return _unrepresentable.Contains(type) ? throw new UnrepresentableTransportType(type) : null;
    }
}
