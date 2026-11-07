// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// The read model that is watched.
/// </summary>
/// <param name="Id">The user id.</param>
/// <param name="Name">The user name.</param>
/// <param name="Groups">The groups the user is in.</param>
/// <param name="__lastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record WatchedUser(
    EventSourceId Id,
    string Name,
    IEnumerable<WatchedMembership> Groups,
    EventSequenceNumber __lastHandledEventSequenceNumber);

/// <summary>
/// A group a user is in.
/// </summary>
/// <param name="GroupId">The group id.</param>
public record WatchedMembership(EventSourceId GroupId);
