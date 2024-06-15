// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Services;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Defines a client for <see cref="IObserverService"/>.
/// </summary>
public interface IObserverServiceClient : IGrainServiceClient<IObserverService>, IObserverService;
