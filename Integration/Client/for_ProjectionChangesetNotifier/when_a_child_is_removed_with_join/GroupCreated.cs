// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// A group was created. The spec has its own event types: shared event types can outlive the fixture reset and starve a later
/// spec's projection.
/// </summary>
/// <param name="Name">The group name.</param>
[EventType("59059a43-8f11-4fc6-994f-0ee2d1c65d0b")]
public record GroupCreated(string Name);
