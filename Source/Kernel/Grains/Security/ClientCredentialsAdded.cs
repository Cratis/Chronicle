// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for client credentials that have been added.
/// </summary>
/// <param name="Id">The unique identifier for the client.</param>
/// <param name="ClientId">The client identifier.</param>
/// <param name="ClientSecret">The hashed client secret.</param>
[EventType]
public record ClientCredentialsAdded(string Id, string ClientId, string ClientSecret);
