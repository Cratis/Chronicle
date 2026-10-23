// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Identifies a stable event-source bucket within a replay.
/// </summary>
/// <param name="Index">The zero-based bucket index.</param>
/// <param name="Count">The number of buckets in the replay.</param>
/// <param name="HashVersion">The version of the bucket hash algorithm.</param>
public record ReplayBucket(int Index, int Count, int HashVersion);
