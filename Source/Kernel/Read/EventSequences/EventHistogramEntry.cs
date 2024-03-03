// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Read.EventSequences;

/// <summary>
/// Represents the entry in an event histogram.
/// </summary>
/// <param name="Date">Date of the sampling.</param>
/// <param name="Count">Number of events.</param>
public record EventHistogramEntry(DateTimeOffset Date, uint Count);
