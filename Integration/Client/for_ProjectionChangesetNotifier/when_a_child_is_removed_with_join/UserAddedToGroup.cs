// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// A user was added to the group the event is appended for.
/// </summary>
/// <param name="UserId">The user that was added.</param>
[EventType("4672782c-7a83-4fe9-8d5a-5623c7c96e19")]
public record UserAddedToGroup(EventSourceId UserId);
