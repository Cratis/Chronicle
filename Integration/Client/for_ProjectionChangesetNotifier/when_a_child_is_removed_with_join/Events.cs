// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// The spec's own event types: shared event types can outlive the fixture reset and starve a later spec's projection.
/// </summary>
/// <param name="Name">The group name.</param>
[EventType("59059a43-8f11-4fc6-994f-0ee2d1c65d0b")]
public record GroupCreated(string Name);

/// <summary>
/// A user was created.
/// </summary>
/// <param name="Name">The user name.</param>
[EventType("33dca4bf-cec8-4c8d-9c83-9833c6b8767e")]
public record UserCreated(string Name);

/// <summary>
/// A user was added to the group the event is appended for.
/// </summary>
/// <param name="UserId">The user that was added.</param>
[EventType("4672782c-7a83-4fe9-8d5a-5623c7c96e19")]
public record UserAddedToGroup(EventSourceId UserId);

/// <summary>
/// A group was removed.
/// </summary>
[EventType("167aad8a-90cf-4bf6-81ea-4ef6139c69fd")]
public record GroupRemoved();

/// <summary>
/// A user was removed.
/// </summary>
[EventType("8990f4b6-f423-405a-a883-e3ecbf0b3aa3")]
public record UserRemoved();
