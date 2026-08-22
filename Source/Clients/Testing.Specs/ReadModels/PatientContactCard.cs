// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A Guid-keyed read model holding a patient's contact details. It reaches none of the shape-dependent
/// substitutions except the one its <c>[PII]</c>-marked concept brings, which is what makes it the pin for
/// the compliance report.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="EmailAddress">The email address the patient is reachable at.</param>
[Passive]
[FromEvent<PatientAdmitted>]
public record PatientContactCard(Guid Id, PatientEmailAddress EmailAddress);
