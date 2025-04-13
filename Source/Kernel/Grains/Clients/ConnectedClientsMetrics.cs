// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Grains.Clients;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ConnectedClientsMetrics
{
    [Gauge<int>("chronicle-connected-clients", "Number of connected clients")]
    internal static partial void ConnectedClients(this IMeter<ConnectedClients> meter, int count);
}
