// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.API.Compliance.PersonalInformation;

namespace Cratis.API.Compliance.GDPR.Queries;

/// <summary>
/// Represents a person.
/// </summary>
/// <param name="Id">Unique identifier of the person.</param>
/// <param name="SocialSecurityNumber"><see cref="SocialSecurityNumber"/> for the person.</param>
/// <param name="FirstName"><see cref="FirstName"/> for the person.</param>
/// <param name="LastName"><see cref="LastName"/> for the person.</param>
/// <param name="Address"><see cref="Address"/> for the person.</param>
/// <param name="City"><see cref="City"/> for the person.</param>
/// <param name="PostalCode"><see cref="PostalCode"/> for the person.</param>
/// <param name="Country"><see cref="Country"/> for the person.</param>
/// <param name="PersonalInformation">Collection of any additional <see cref="PersonalInformation"/> for the person.</param>
public record Person(
    PersonId Id,
    SocialSecurityNumber SocialSecurityNumber,
    FirstName FirstName,
    LastName LastName,
    Address Address,
    City City,
    PostalCode PostalCode,
    Country Country,
    IEnumerable<PersonalInformation> PersonalInformation);
