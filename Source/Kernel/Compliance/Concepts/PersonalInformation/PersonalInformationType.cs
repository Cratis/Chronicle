// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.Concepts.PersonalInformation
{
    public record PersonalInformationType(string Value) : ConceptAs<string>(Value);
}
