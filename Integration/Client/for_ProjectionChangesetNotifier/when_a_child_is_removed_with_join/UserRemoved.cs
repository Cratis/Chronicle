// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// A user was removed.
/// </summary>
[EventType("8990f4b6-f423-405a-a883-e3ecbf0b3aa3")]
public record UserRemoved();
