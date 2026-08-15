// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A fluent <see cref="IConstraint"/> holding an <see cref="InvitedEmailAddress"/> for as long as the invitation is
/// outstanding, released by every one of the three ways an invitation can end.
/// </summary>
public class UniqueInvitedEmailAddress : IConstraint
{
    /// <summary>
    /// The name of the constraint.
    /// </summary>
    public const string Name = "UniqueInvitedEmailAddress";

    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(_ => _
            .On<InvitationSent>(e => e.Address)
            .WithName(Name)
            .RemovedWith<InvitationAccepted>()
            .RemovedWith<InvitationRevoked>()
            .RemovedWith<InvitationExpired>());
}
