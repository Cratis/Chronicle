// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IServices"/>.
/// </summary>
/// <param name="EventSequences"><see cref="IEventSequences"/> instance.</param>
/// <param name="EventTypes"><see cref="IEventTypes"/> instance.</param>
/// <param name="Observers"><see cref="IObservers"/> instance.</param>
/// <param name="ClientObservers"><see cref="IClientObservers"/> instance.</param>
/// <param name="Projections"><see cref="IProjections"/> instance.</param>
public record Services(
    IEventSequences EventSequences,
    IEventTypes EventTypes,
    IObservers Observers,
    IClientObservers ClientObservers,
    IProjections Projections) : IServices;
