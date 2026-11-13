// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// A group a user is in.
/// </summary>
/// <param name="GroupId">The group id.</param>
public record WatchedMembership(EventSourceId GroupId);
