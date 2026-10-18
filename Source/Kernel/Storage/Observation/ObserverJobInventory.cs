// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A job registered before Start. The containing row's state moves intended to started to terminal;
/// a terminal payload is written atomically with the terminal state and retained until consumption.
/// </summary>
/// <param name="JobId">The observer-chosen job id.</param>
/// <param name="JobType">The addressable job grain type.</param>
/// <param name="Kind">The reader kind.</param>
/// <param name="Origin">The continuation or restart-plan identity, if any.</param>
/// <param name="Outcome">The terminal outcome, absent until terminal.</param>
public record ObserverJobInventory(JobId JobId, string JobType, ObserverDefinitionChangeJobKind Kind, ObserverJobOrigin? Origin = null, ObserverJobOutcome? Outcome = null);
