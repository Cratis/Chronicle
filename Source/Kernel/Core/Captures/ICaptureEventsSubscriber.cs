// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the kernel owned subscriber that runs an events capture, observing the inbox the capture reads from.
/// </summary>
/// <remarks>
/// Events are delivered one at a time in sequence order, which is what keeps the remembered state per key consistent.
/// </remarks>
public interface ICaptureEventsSubscriber : IObserverSubscriber, IAmOwnedByKernel, IUnpartitionedObserverSubscriber;
