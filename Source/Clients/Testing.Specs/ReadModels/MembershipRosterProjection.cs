// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Fluent projection for <see cref="MembershipRoster"/>, exercising a child-level <c>[Join]</c>: each member
/// child row is created from <see cref="MemberEnrolled"/> (on the group stream) and enriched with the member's
/// name joined from <see cref="MemberProfileCreated"/> (on the member's own stream). Mirrors the children-join
/// integration projection shape so the in-memory harness is validated against the same behavior.
/// </summary>
public class MembershipRosterProjection : IProjectionFor<MembershipRoster>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<MembershipRoster> builder) => builder
        .From<RosterOpened>(b => b.Set(m => m.GroupName).To(e => e.GroupName))
        .Children(_ => _.Members, _ => _
            .IdentifiedBy(m => m.MemberId)
            .From<MemberEnrolled>(b => b.UsingKey(e => e.MemberId))
            .Join<MemberProfileCreated>(j => j
                .Set(m => m.MemberName).To(e => e.MemberName)));
}
