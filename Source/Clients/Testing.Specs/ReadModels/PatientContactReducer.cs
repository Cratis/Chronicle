// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Reduces patient contact details without substituting any layer other than compliance.
/// </summary>
public class PatientContactReducer : IReducerFor<ReducedPatientContact>
{
    /// <summary>
    /// Gets the reducer identifier.
    /// </summary>
    public ReducerId Id => nameof(PatientContactReducer);

    /// <summary>
    /// Records the admitted patient's contact details.
    /// </summary>
    /// <param name="event">The admission event.</param>
    /// <param name="current">The previous contact, if present.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The updated contact.</returns>
    public ReducedPatientContact Admit(PatientAdmitted @event, ReducedPatientContact? current, EventContext context) =>
        new(Guid.Parse(context.EventSourceId.Value), @event.EmailAddress);
}
