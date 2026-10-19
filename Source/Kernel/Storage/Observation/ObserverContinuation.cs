// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A partition's catch-up obligation. Its row carries the resume position and revision; replacement jobs get fresh ids.
/// </summary>
/// <param name="Partition">The partition to continue.</param>
/// <param name="JobId">The fresh job id reserved for this obligation.</param>
public record ObserverContinuation(Key Partition, JobId JobId);
