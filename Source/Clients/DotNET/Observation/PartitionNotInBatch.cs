// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a batch method reports an outcome for a partition the batch holds no events for.
/// </summary>
/// <param name="partition">The <see cref="EventSourceId"/> of the partition.</param>
public class PartitionNotInBatch(EventSourceId partition)
    : Exception($"The batch holds no events for partition '{partition}', so no outcome can be reported for it.");
