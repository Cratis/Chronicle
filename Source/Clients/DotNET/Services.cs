// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IServices"/>.
/// </summary>
/// <param name="EventSequences"><see cref="IEventSequences"/> instance.</param>
/// <param name="EventTypes"><see cref="IEventTypes"/> instance.</param>
/// <param name="Constraints"><see cref="IConstraints"/> instance.</param>
/// <param name="Observers"><see cref="IObservers"/> instance.</param>
/// <param name="Reactors"><see cref="IReactors"/> instance.</param>
/// <param name="Reducers"><see cref="IReducers"/> instance.</param>
/// <param name="Projections"><see cref="IProjections"/> instance.</param>
/// <param name="server"><see cref="IServer"/> instance.</param>
public record Services(
    IEventSequences EventSequences,
    IEventTypes EventTypes,
    IConstraints Constraints,
    IObservers Observers,
    IReactors Reactors,
    IReducers Reducers,
    IProjections Projections,
    IServer server) : IServices;
