// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for client credentials secret that has been changed.
/// </summary>
/// <param name="Id">The unique identifier for the client.</param>
/// <param name="ClientSecret">The new hashed client secret.</param>
[EventType]
public record ClientCredentialsSecretChanged(string Id, string ClientSecret);
