// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance.Concepts.PersonalInformation;

namespace Cratis.Compliance.Domain.GDPR
{
    public record DeletePIIForPerson(PersonId PersonId);
}
