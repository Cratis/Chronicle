// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for an application that has been added.
/// </summary>
/// <param name="ClientId">The client identifier.</param>
/// <param name="ClientSecret">The hashed client secret.</param>
[EventType]
[AllEventStores]
public record ApplicationAdded(ClientId ClientId, ClientSecret ClientSecret);
