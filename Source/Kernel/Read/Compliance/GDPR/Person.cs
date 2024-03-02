// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Concepts.Compliance.PersonalInformation;

namespace Aksio.Cratis.Kernel.Read.Compliance.GDPR;

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
