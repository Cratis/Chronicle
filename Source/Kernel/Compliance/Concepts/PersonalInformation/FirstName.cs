// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance.GDPR;

namespace Cratis.Compliance.Concepts.PersonalInformation
{
    public record FirstName(string Value) : PIIConceptAs<string>(Value);
}
