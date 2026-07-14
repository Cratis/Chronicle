// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model projected by the fluent <see cref="MembershipRosterProjection"/>: a group with a keyed child
/// collection of members, each enriched with the member's name through a child-level <c>[Join]</c> against
/// <see cref="MemberProfileCreated"/> on a separate member stream. Used to verify that a child join updates the
/// existing child in place — rather than appending a duplicate — regardless of seed order.
/// </summary>
/// <param name="Id">The roster (group) identifier.</param>
/// <param name="GroupName">The group name.</param>
/// <param name="Members">The member child rows, keyed by <see cref="RosterMember.MemberId"/>.</param>
public record MembershipRoster(
    EventSourceId Id,
    string GroupName,
    IEnumerable<RosterMember> Members);
