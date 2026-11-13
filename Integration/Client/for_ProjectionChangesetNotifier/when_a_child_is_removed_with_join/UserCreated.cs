// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// A user was created.
/// </summary>
/// <param name="Name">The user name.</param>
[EventType("33dca4bf-cec8-4c8d-9c83-9833c6b8767e")]
public record UserCreated(string Name);
