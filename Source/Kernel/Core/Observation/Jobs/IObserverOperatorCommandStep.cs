// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Defines the step that gives the observer the command of an <see cref="IObserverOperatorCommandJob"/>.
/// </summary>
public interface IObserverOperatorCommandStep : IJobStep<ObserverOperatorCommandRequest, object, ObserverOperatorCommandStepState>;
