// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

/// <summary>
/// Defines a kernel observer subscriber that will receive all events it subscribes to.
/// </summary>
/// <typeparam name="TReactor">The type of reactor that will be used.</typeparam>
public interface IReactorObserverSubscriber<TReactor> : IObserverSubscriber, IAmOwnedByKernel
    where TReactor : IReactor;
