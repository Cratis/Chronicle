// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Events.Constraints.Operator;

/// <summary>
/// Provides a side-effect-free authorization probe for rebuilding constraint indexes.
/// </summary>
/// <param name="CanReindex">Whether the caller may rebuild constraint indexes.</param>
[ReadModel]
[Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
public record ConstraintReindexCapability(bool CanReindex)
{
    /// <summary>
    /// Checks whether the caller may request a constraint index rebuild.
    /// </summary>
    /// <param name="eventStore">The event store whose constraint indexes would be rebuilt.</param>
    /// <returns>A UI-visibility hint after the query authorization policy succeeds.</returns>
    public static ConstraintReindexCapability CanReindexConstraints(EventStoreName eventStore)
    {
        // This is a UI-visibility hint only; the command enforces authorization server-side.
        return new(true);
    }
}
