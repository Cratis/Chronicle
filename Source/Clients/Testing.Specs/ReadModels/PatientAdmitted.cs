// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Emitted when a patient is admitted, recording the email address they are reachable at.
/// </summary>
/// <param name="EmailAddress">The email address the patient is reachable at.</param>
[EventType]
public record PatientAdmitted(PatientEmailAddress EmailAddress);
