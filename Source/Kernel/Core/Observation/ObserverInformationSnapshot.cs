// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a snapshot of an observer's definition, state and subscription, all read in the same grain turn.
/// </summary>
/// <param name="Definition">The <see cref="ObserverDefinition"/>.</param>
/// <param name="State">The <see cref="ObserverState"/>.</param>
/// <param name="IsSubscribed">Whether a subscription is installed, including while its setup is in progress.</param>
/// <param name="RunningStateChangedAt">When the running state last changed, or null if unknown.</param>
public record ObserverInformationSnapshot(
    ObserverDefinition Definition,
    ObserverState State,
    bool IsSubscribed,
    DateTimeOffset? RunningStateChangedAt);
