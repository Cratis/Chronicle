// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for an application that has been removed.
/// </summary>
/// <param name="Id">The unique identifier for the application.</param>
[EventType]
public record ApplicationRemoved(string Id);
