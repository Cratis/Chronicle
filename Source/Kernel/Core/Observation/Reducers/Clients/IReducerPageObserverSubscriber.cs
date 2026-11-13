// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Defines the subscriber of a client reducer that declared page delivery, and so receives pages spanning partitions.
/// </summary>
public interface IReducerPageObserverSubscriber : IReducerObserverSubscriber, IObserverPartitionPageSubscriber;
