// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for client credentials that have been removed.
/// </summary>
/// <param name="Id">The unique identifier for the client.</param>
[EventType]
public record ClientCredentialsRemoved(string Id);
