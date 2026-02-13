// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for when invalid application credentials are provided.
/// </summary>
/// <param name="ClientId">The client identifier.</param>
[EventType]
public record InvalidApplicationCredentialsProvided(ClientId ClientId);
