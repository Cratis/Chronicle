// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the bounds of the pages a client observer declared it can receive, spanning partitions.
/// </summary>
/// <param name="MaxEventsPerPage">The most events a page may hold. Zero means the kernel decides.</param>
/// <param name="MaxDelay">How long the kernel may wait to fill a page. Zero means it does not wait.</param>
public record PageDeliveryDefinition(int MaxEventsPerPage = 0, TimeSpan MaxDelay = default);
