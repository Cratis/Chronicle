// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;

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

    /// <summary>
    /// Get the <see cref="ProjectionDefinition"/> from a business rule.
    /// </summary>
    /// <param name="businessRule"><see cref="IBusinessRule"/> to get from.</param>
    /// <returns>The <see cref="ProjectionDefinition"/>.</returns>
    ProjectionDefinition GetProjectionDefinitionFor(IBusinessRule businessRule);

    /// <summary>
    /// Perform projection defined by <see cref="IBusinessRule"/> into the rule itself.
    /// </summary>
    /// <param name="businessRule"><see cref="IBusinessRule"/> that defines the projection and gets projected into.</param>
    /// <param name="modelIdentifier">Optional model identifier.</param>
    void ProjectTo(IBusinessRule businessRule, object? modelIdentifier = default);
}
