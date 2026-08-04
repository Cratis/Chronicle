// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The same roster with its child collection declared non-nullable - the shape almost every read model is
/// written in, because the collection reads like something that is always there.
/// </summary>
/// <param name="Id">Roster identifier.</param>
/// <param name="GroupName">The group name.</param>
/// <param name="Members">Members, declared non-nullable.</param>
[FromEvent<RosterOpened>]
public record RosterWithRequiredMembers(
    Guid Id,
    string GroupName,

    [ChildrenFrom<MemberEnrolled>(key: nameof(MemberEnrolled.MemberId), identifiedBy: nameof(RosterMember.MemberId))]
    IEnumerable<RosterMember> Members);
