// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// The exception that is thrown when a replay bucket count is outside the supported range.
/// </summary>
/// <param name="count">The requested bucket count.</param>
public class ReplayBucketCountOutOfRange(int count)
    : Exception($"Replay bucket count must be between 1 and 64, but was {count}.");
