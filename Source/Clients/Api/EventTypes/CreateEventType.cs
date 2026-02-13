// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.EventTypes;

/// <summary>
/// Represents the command for creating an event type.
/// </summary>
/// <param name="Name">Name of the event type to create.</param>
public record CreateEventType(string Name);
