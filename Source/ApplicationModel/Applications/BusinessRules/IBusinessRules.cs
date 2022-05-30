// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.BusinessRules;

/// <summary>
/// Represents a system for working with <see cref="BusinessRulesFor{TSelf, TCommand}"/>.
/// </summary>
public interface IBusinessRules
{
    /// <summary>
    /// Check if there are business rules for a specific type.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if there are, false it not.</returns>
    bool HasFor(Type type);

    /// <summary>
    /// Get the types of business rules for a specific type.
    /// </summary>
    /// <param name="type">Type to get for.</param>
    /// <returns>Collection of business rule types.</returns>
    IEnumerable<Type> GetFor(Type type);
}
