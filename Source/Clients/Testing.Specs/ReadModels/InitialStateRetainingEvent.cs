// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Subscribed event that resolves the primary key without changing any read model property.
/// </summary>
[EventType]
public record InitialStateRetainingEvent;
