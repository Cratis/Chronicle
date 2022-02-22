// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Connections;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Delegate for when a client is disconnected.
/// </summary>
/// <param name="context">The client <see cref="ConnectionContext"/>.</param>
public delegate Task ClientDisconnected(ConnectionContext context);
