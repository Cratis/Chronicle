// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Creates a removable widget carrying the key for its root join.
/// </summary>
/// <param name="CustomerId">Customer identifier used by the root join.</param>
[EventType]
public record RemovableJoinedWidgetCreated(JoinCustomerId CustomerId);
