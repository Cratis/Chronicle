// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Represents subscription arguments for a reactor observer.
/// </summary>
/// <param name="ConnectedClient">The connected client information.</param>
/// <param name="IsReplayable">Whether the reactor supports replay scenarios.</param>
public record ReactorObserverSubscriptionArgs(object? ConnectedClient, bool IsReplayable = true);