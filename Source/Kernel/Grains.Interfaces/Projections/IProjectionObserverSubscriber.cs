// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Observation;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a specialized <see cref="IObserverSubscriber"/> for projections.
/// </summary>
public interface IProjectionObserverSubscriber : IObserverSubscriber, IAmOwnedByKernel;
