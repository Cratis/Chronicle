// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// A group was removed.
/// </summary>
[EventType("167aad8a-90cf-4bf6-81ea-4ef6139c69fd")]
public record GroupRemoved();
