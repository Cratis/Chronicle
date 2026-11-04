// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is reported when a batch method defers a partition but the kernel does not accept deferred partitions,
/// so the partition is failed instead.
/// </summary>
/// <param name="partition">The <see cref="EventSourceId"/> of the partition.</param>
public class DeferralNotSupportedByKernel(EventSourceId partition)
    : Exception($"Partition '{partition}' was deferred, but the kernel does not accept deferred partitions. The partition is failed instead.");
