// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Grains.Observation;

namespace Cratis.Kernel.Grains.Projections;

/// <summary>
/// Defines a specialized <see cref="IObserverSubscriber"/> for projections.
/// </summary>
public interface IProjectionObserverSubscriber : IObserverSubscriber;
