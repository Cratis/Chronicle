// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts;

/// <summary>
/// Expresses a Concept as a another type, usually a primitive such as Guid, Int or String.
/// </summary>
/// <typeparam name="T">Type of the concept.</typeparam>
public record ConceptAs<T> : IComparable<ConceptAs<T>>, IComparable<T>, IComparable
    where T : IComparable
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConceptAs{T}"/>.
    /// </summary>
    /// <param name="value">Value to initialize the concept with.</param>
    /// <exception cref="ArgumentNullException">Thrown when incoming value is null.</exception>
    public ConceptAs(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>
    /// Gets or inits the underlying value for the instance.
    /// </summary>
    public T Value { get; init; }

    /// <summary>
    /// Implicitly convert from the <see cref="ConceptAs{T}"/> to the value of the type given.
    /// </summary>
    /// <param name="value"><see cref="ConceptAs{T}"/> to convert from.</param>
    public static implicit operator T(ConceptAs<T> value) => value.Value;

    /// <summary>
    /// Checks if the first instance of <see cref="ConceptAs{T}"/> is greater than the second.
    /// </summary>
    /// <param name="first">First instance to compare.</param>
    /// <param name="second">Second instane to compare.</param>
    /// <returns>true if the first instance is greater than the second, otherwise false.</returns>
    public static bool operator >(ConceptAs<T> first, ConceptAs<T> second) => first?.CompareTo(second) > 0;

    /// <summary>
    /// Checks if the first instance of <see cref="ConceptAs{T}"/> is greater than or equal to the second.
    /// </summary>
    /// <param name="first">First instance to compare.</param>
    /// <param name="second">Second instance to compare.</param>
    /// <returns>true if the first instance is greater than or equal to the second, otherwise false.</returns>
    public static bool operator >=(ConceptAs<T> first, ConceptAs<T> second) => first.Equals(second) || first > second;

    /// <summary>
    /// Checks if the first instance of <see cref="ConceptAs{T}"/> is less than the second.
    /// </summary>
    /// <param name="first">First instance to compare.</param>
    /// <param name="second">Second instane to compare.</param>
    /// <returns>true if the first instance is less than the second, otherwise false.</returns>
    public static bool operator <(ConceptAs<T> first, ConceptAs<T> second)
    {
        if (first is null)
        {
            return second is not null;
        }

        return first.CompareTo(second) < 0;
    }

    /// <summary>
    /// Checks if the first instance of <see cref="ConceptAs{T}"/> is less than or equal to the second.
    /// </summary>
    /// <param name="first">First instance to compare.</param>
    /// <param name="second">Second instane to compare.</param>
    /// <returns>true if the first instance is less than or equal to the second, otherwise false.</returns>
    public static bool operator <=(ConceptAs<T> first, ConceptAs<T> second) => first.Equals(second) || first < second;

    /// <summary>
    ///  Compares two instances of ConceptAs'T to determine sort order.
    /// </summary>
    /// <param name="other">Instance to compare to.</param>
    /// <returns>Greater than 0 if greater, 0 if the same and less than 0 if lesser.</returns>
    public int CompareTo(ConceptAs<T>? other) => other == null ? 1 : Comparer<T>.Default.Compare(Value, other.Value);

    /// <summary>
    ///  Compares instance to an instance of T to determine sort order.
    /// </summary>
    /// <param name="other">Instance to compare to.</param>
    /// <returns>Greater than 0 if greater, 0 if the same and less than 0 if lesser.</returns>
    public int CompareTo(T? other) => other is null ? 1 : Comparer<T>.Default.Compare(Value, other);

    /// <summary>
    ///  Compares instance to an object to determine sort order.
    /// </summary>
    /// <param name="obj">Instance to compare to.</param>
    /// <returns>Greater than 0 if greater, 0 if the same and less than 0 if lesser.</returns>
    public int CompareTo(object? obj) => obj == null ? 1 : Comparer<T>.Default.Compare(Value, (ConceptAs<T>)obj);

    /// <inheritdoc/>
    public sealed override string ToString() => Value!.ToString() ?? "[n/a]";
}
