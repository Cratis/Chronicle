// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a reduced contact whose PII is not protected by the scenario's read-model sink.
/// </summary>
/// <param name="Id">The patient identifier.</param>
/// <param name="EmailAddress">The private email address.</param>
[Passive]
public record ReducedPatientContact(Guid Id, PatientEmailAddress EmailAddress);
