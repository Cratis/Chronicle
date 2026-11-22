// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the request for the job carrying out a command an operator gave an observer.
/// </summary>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> of the observer the command is for.</param>
/// <param name="Command">The <see cref="ObserverOperatorCommand"/> to carry out.</param>
/// <remarks>
/// It is deliberately not an observer job request: it owns no part of the observer's event stream and is not subject to
/// the ownership rules of the jobs that do. Whatever the command starts on the observer is.
/// </remarks>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.Jobs.ObserverOperatorCommandRequest")]
public record ObserverOperatorCommandRequest(
    [Id(0)] ObserverKey ObserverKey,
    [Id(1)] ObserverOperatorCommand Command) : IJobRequest;
