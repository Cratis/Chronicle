// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts
{
    /// <summary>
    /// Expresses a Concept as a another type, usually a primitive such as Guid, Int or String.
    /// </summary>
    /// <typeparam name="T">Type of the concept.</typeparam>
    public record ConceptAs<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ConceptAs{T}"/>.
        /// </summary>
        /// <param name="value">Value to initialize the concept with.</param>
        /// <exception cref="ArgumentNullException">Thrown when incoming value is null.</exception>
        public ConceptAs(T value)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            Value = value;
        }

        /// <summary>
        /// Gets or inits the underlying value for the instance.
        /// </summary>
        public T Value { get; init; }

        /// <summary>
        /// Implicitly convert from the <see cref="ConceptAs{T}"/> to the value of the type given.
        /// </summary>
        /// <param name="value"><see cref="ConceptAs{T}"/> to convert from.</param>
        public static implicit operator T(ConceptAs<T> value) => value.Value;

        /// <inheritdoc/>
        public sealed override string ToString()
        {
            return Value?.ToString() ?? "[n/a]";
        }
    }
}
