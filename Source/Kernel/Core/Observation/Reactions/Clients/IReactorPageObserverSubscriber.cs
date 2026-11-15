// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Defines the subscriber of a client reactor that declared page delivery, and so receives pages spanning partitions.
/// </summary>
public interface IReactorPageObserverSubscriber : IReactorObserverSubscriber, IObserverPartitionPageSubscriber;
