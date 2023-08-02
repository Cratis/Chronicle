// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Rules;

/// <summary>
/// Defines a system that knows about projections related to rules.
/// </summary>
public interface IRulesProjections
{
    /// <summary>
    /// Gets all the projection definitions.
    /// </summary>
    /// <value>Collection of <see cref="ProjectionDefinition"/>.</value>
    IEnumerable<ProjectionDefinition> All { get; }

    /// <summary>
    /// Check if there are any projections for a specific <see cref="RuleId"/>.
    /// </summary>
    /// <param name="ruleId"><see cref="RuleId"/> to check for.</param>
    /// <returns>True if there is, false if not.</returns>
    /// <remarks>
    /// Not all rules are stateful, and thus does not have a projection.
    /// </remarks>
    bool HasFor(RuleId ruleId);

    /// <summary>
    /// Get the projection definition for a specific <see cref="RuleId"/>.
    /// </summary>
    /// <param name="ruleId"><see cref="RuleId"/> to get for.</param>
    /// <returns><see cref="ProjectionDefinition"/> for the rule.</returns>
    ProjectionDefinition GetFor(RuleId ruleId);
}
