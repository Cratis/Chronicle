// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Defines a specialized <see cref="IObserverSubscriber"/> for projections.
/// </summary>
public interface IWebhookObserverSubscriber : IObserverSubscriber, IAmOwnedByKernel;
