// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents shadow ownership of an observer, never used for delivery decisions.
/// </summary>
/// <param name="Holder">The kind of work holding the claim.</param>
/// <param name="Job">The job holding the claim, when known.</param>
/// <param name="Epoch">The epoch allocated at grant.</param>
/// <param name="ResumePosition">The position from which work resumes.</param>
/// <param name="PendingWork">The pending partition work.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.ObserverWideClaim")]
public record ObserverWideClaim([Id(0)] ClaimHolderKind Holder, [Id(1)] JobId? Job, [Id(2)] ClaimEpoch Epoch, [Id(3)] EventSequenceNumber ResumePosition, [Id(4)] IReadOnlyList<PendingPartitionWork> PendingWork);
