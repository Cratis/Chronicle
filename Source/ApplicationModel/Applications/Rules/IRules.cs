// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;

namespace Aksio.Cratis.Applications.Rules;

/// <summary>
/// Represents a system for working with <see cref="RulesFor{TSelf, TCommand}"/>.
/// </summary>
public interface IRules
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
    /// <param name="rule"><see cref="IRule"/> to get from.</param>
    /// <returns>The <see cref="ProjectionDefinition"/>.</returns>
    ProjectionDefinition GetProjectionDefinitionFor(IRule rule);

    /// <summary>
    /// Perform projection defined by <see cref="IRule"/> into the rule itself.
    /// </summary>
    /// <param name="rule"><see cref="IRule"/> that defines the projection and gets projected into.</param>
    /// <param name="modelIdentifier">Optional model identifier.</param>
    void ProjectTo(IRule rule, object? modelIdentifier = default);
}
