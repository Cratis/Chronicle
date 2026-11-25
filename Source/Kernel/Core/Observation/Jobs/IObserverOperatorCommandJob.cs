// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Defines the job carrying out a command an operator gave an observer.
/// </summary>
public interface IObserverOperatorCommandJob : IJob<ObserverOperatorCommandRequest>;
