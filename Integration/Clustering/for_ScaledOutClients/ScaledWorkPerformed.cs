// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients;

/// <summary>
/// Emitted when a unit of scaled-out work was performed against a partition.
/// </summary>
/// <param name="Round">The append round the work belongs to.</param>
[EventType]
public record ScaledWorkPerformed(int Round);
