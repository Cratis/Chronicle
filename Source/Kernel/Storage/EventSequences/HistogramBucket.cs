// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents the number of events that occurred within one time bucket of an event sequence histogram.
/// </summary>
/// <param name="Occurred">The inclusive start of the time bucket, truncated to the requested <see cref="HistogramResolution"/>.</param>
/// <param name="Count">Number of events that occurred within the bucket.</param>
public record HistogramBucket(DateTimeOffset Occurred, long Count);
