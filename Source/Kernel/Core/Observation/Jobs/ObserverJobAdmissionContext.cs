// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents what an <see cref="IObserverJobAdmissionRule"/> is asked about.
/// </summary>
/// <param name="Admission">The <see cref="JobAdmissionRequest"/> Orleans asked with.</param>
/// <param name="Request">The <see cref="IObserverJobRequest"/> the job runs.</param>
/// <param name="Storage">The <see cref="IStorage"/> a rule reads from.</param>
public record ObserverJobAdmissionContext(JobAdmissionRequest Admission, IObserverJobRequest Request, IStorage Storage);
