// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients;

/// <summary>
/// Represents a single reactor invocation - which client instance handled which partition.
/// </summary>
/// <param name="Instance">The parsable silo address of the silo hosting the client instance that handled the event.</param>
/// <param name="Partition">The event source id of the partition that was handled.</param>
public record HandledPartition(string Instance, string Partition);
