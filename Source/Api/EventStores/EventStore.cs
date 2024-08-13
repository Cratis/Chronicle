// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Api.EventStores;

/// <summary>
/// Represents the definition of an event store.
/// </summary>
/// <param name="Name">Name of the event store.</param>
/// <param name="Description">Description for the event store.</param>
public record EventStore(EventStoreName Name, string Description);
