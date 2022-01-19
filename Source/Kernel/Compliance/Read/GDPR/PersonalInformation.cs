// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.Concepts.PersonalInformation;

namespace Aksio.Cratis.Compliance.Read.GDPR
{
    public record PersonalInformation(PersonalInformationId Identifier, PersonalInformationType Type, PersonalInformationValue Value);
}
