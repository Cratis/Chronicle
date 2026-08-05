// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A read model whose child collection is declared <em>nullable</em>, which is what makes it able to tell the
/// harness's answer apart from the runtime's.
/// </summary>
/// <remarks>
/// The read-model sink never writes an empty child collection - the path is owned by <c>ChildAdded</c> and
/// <c>ChildRemoved</c>, and writing <c>[]</c> from a root event would race a sibling partition's already-added
/// child away. So a collection with no children is an absent field, and a reader that leaves a nullable
/// declaration alone answers <see langword="null"/>. A non-nullable declaration cannot distinguish the two,
/// because both the harness pre-seeding <c>[]</c> and the reader resolving an absent field to <c>[]</c> look
/// identical from a spec.
/// </remarks>
/// <param name="Id">Roster identifier.</param>
/// <param name="GroupName">The group name.</param>
/// <param name="Members">Members, declared nullable so an absent collection stays distinguishable.</param>
[FromEvent<RosterOpened>]
public record RosterWithOptionalMembers(
    Guid Id,
    string GroupName,

    [ChildrenFrom<MemberEnrolled>(key: nameof(MemberEnrolled.MemberId), identifiedBy: nameof(RosterMember.MemberId))]
    IEnumerable<RosterMember>? Members);
