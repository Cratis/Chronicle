// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// The exception that is thrown when a type is not a valid concurrency strategy.
/// </summary>
/// <param name="type">The type that is not a valid concurrency strategy.</param>
public class TypeIsNotAConcurrencyStrategy(Type type) : Exception($"The type '{type.FullName}' is not a valid concurrency strategy.")
{
    /// <summary>
    /// Throws <see cref="TypeIsNotAConcurrencyStrategy"/> if the specified type is not a concurrency strategy.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="TypeIsNotAConcurrencyStrategy">Thrown if the type is not a concurrency strategy.</exception>
    public static void ThrowIfNotAConcurrencyStrategy(Type type)
    {
        if (!typeof(IConcurrencyScopeStrategy).IsAssignableFrom(type))
        {
            throw new TypeIsNotAConcurrencyStrategy(type);
        }
    }
}
