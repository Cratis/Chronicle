// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A client-owned observer subscriber interface used for testing watchdog behavior.
/// </summary>
public interface IClientOwnedObserverSubscriber : IObserverSubscriber, IAmOwnedByClient;
