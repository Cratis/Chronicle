// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.EventStores;

/// <summary>
/// Represents a request to add an event store.
/// </summary>
/// <param name="Name">Name of the event store to add.</param>
/// <param name="Description">Optional domain specification describing the purpose and context.</param>
public record AddEventStore(string Name, string? Description = null);
