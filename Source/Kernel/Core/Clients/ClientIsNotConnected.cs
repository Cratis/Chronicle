// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Exception thrown when a client is not found.
/// </summary>
/// <param name="connectionId">The connection ID of the unknown client.</param>
public class ClientIsNotConnected(ConnectionId connectionId)
    : Exception($"Client '{connectionId}' is not connected.");
