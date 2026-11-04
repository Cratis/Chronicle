// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is reported when a batch method fails a partition without giving a reason.
/// </summary>
/// <param name="partition">The <see cref="EventSourceId"/> of the partition.</param>
public class PartitionFailedInBatch(EventSourceId partition)
    : Exception($"Partition '{partition}' was failed by a batch method.");
