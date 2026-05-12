// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents the unique identifier of an instance of an event source.
/// </summary>
/// <param name="Value">Actual value.</param>
public record EventSourceId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="EventSourceId"/>.
    /// </summary>
    public static readonly EventSourceId Unspecified = new(string.Empty);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourceId"/> class.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value.</param>
    public EventSourceId(Guid value) : this(value.ToString())
    {
    }

    /// <summary>
    /// Check whether or not the <see cref="EventSourceId"/> is specified.
    /// </summary>
    public bool IsSpecified => this != Unspecified;

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    /// <returns>A converted <see cref="EventSourceId"/>.</returns>;
    public static implicit operator EventSourceId(Guid id) => new(id.ToString());

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="id"><see cref="string"/> to convert from.</param>
    /// <returns>A converted <see cref="EventSourceId"/>.</returns>;
    public static implicit operator EventSourceId(string id) => new(id);

    /// <summary>
    /// Create a new <see cref="EventSourceId"/>.
    /// </summary>
    /// <returns>A new <see cref="EventSourceId"/>.</returns>
    public static EventSourceId New() => Guid.NewGuid();
}

/// <summary>
/// Represents a type-safe event source identifier wrapping a strongly-typed value.
/// The generic type parameter is passed directly to <see cref="ConceptAs{T}"/>, giving value-equality
/// semantics over <typeparamref name="T"/> while also supporting implicit conversion to and from
/// the untyped <see cref="EventSourceId"/> for wire compatibility.
/// </summary>
/// <typeparam name="T">
/// The type of the underlying value. Supported types are <see cref="string"/>, <see cref="Guid"/>,
/// <see cref="ConceptAs{T}"/> wrappers over those primitives, and any type whose
/// <see cref="object.ToString"/> produces a stable string representation.
/// </typeparam>
/// <param name="TypedValue">The typed value that this identifier wraps.</param>
public record EventSourceId<T>(T TypedValue) : ConceptAs<T>(TypedValue)
    where T : IComparable
{
    /// <summary>
    /// Implicitly convert from <typeparamref name="T"/> to <see cref="EventSourceId{T}"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    /// <returns>A converted <see cref="EventSourceId{T}"/>.</returns>
    public static implicit operator EventSourceId<T>(T value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="EventSourceId{T}"/> to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id"><see cref="EventSourceId{T}"/> to convert from.</param>
    /// <returns>The underlying typed value.</returns>
    public static implicit operator T(EventSourceId<T> id) => id.TypedValue;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventSourceId{T}"/>,
    /// parsing the string value into <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    /// <returns>A converted <see cref="EventSourceId{T}"/>.</returns>
    public static implicit operator EventSourceId<T>(string value) => new(ParseValue(value));

    /// <summary>
    /// Implicitly convert from <see cref="EventSourceId{T}"/> to an untyped <see cref="EventSourceId"/>
    /// by serializing the typed value to its string representation.
    /// </summary>
    /// <param name="id"><see cref="EventSourceId{T}"/> to convert from.</param>
    /// <returns>An untyped <see cref="EventSourceId"/> carrying the string representation.</returns>
    public static implicit operator EventSourceId(EventSourceId<T> id) => new(ConvertToString(id.TypedValue));

    /// <summary>
    /// Implicitly convert from an untyped <see cref="EventSourceId"/> to <see cref="EventSourceId{T}"/>
    /// by parsing the string value into <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id"><see cref="EventSourceId"/> to convert from.</param>
    /// <returns>A converted <see cref="EventSourceId{T}"/>.</returns>
    public static implicit operator EventSourceId<T>(EventSourceId id) => new(ParseValue(id.Value));

    /// <summary>
    /// Create an <see cref="EventSourceId{T}"/> from an <see cref="EventSourceId"/>,
    /// parsing its string value back into <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id"><see cref="EventSourceId"/> to convert from.</param>
    /// <returns>A converted <see cref="EventSourceId{T}"/>.</returns>
    public static EventSourceId<T> From(EventSourceId id) => new(ParseValue(id.Value));

    /// <summary>
    /// Convert a <typeparamref name="T"/> value to its string representation for use as an event source identifier.
    /// </summary>
    /// <param name="value">The typed value to convert.</param>
    /// <returns>The string representation of <paramref name="value"/>.</returns>
    internal static string ConvertToString(T value) =>
        value switch
        {
            string s => s,
            Guid g => g.ToString(),
            ConceptAs<string> c => c.Value,
            ConceptAs<Guid> c => c.Value.ToString(),
            _ => value!.ToString()!
        };

    /// <summary>
    /// Parse a string representation back into <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed <typeparamref name="T"/> value.</returns>
    internal static T ParseValue(string value)
    {
        var targetType = typeof(T);

        if (targetType == typeof(string)) return (T)(object)value;
        if (targetType == typeof(Guid)) return (T)(object)Guid.Parse(value);

        if (typeof(ConceptAs<string>).IsAssignableFrom(targetType))
            return (T)Activator.CreateInstance(targetType, value)!;

        if (typeof(ConceptAs<Guid>).IsAssignableFrom(targetType))
            return (T)Activator.CreateInstance(targetType, Guid.Parse(value))!;

        return (T)Convert.ChangeType(value, targetType);
    }
}
