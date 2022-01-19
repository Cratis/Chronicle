// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.Concepts.PersonalInformation
{
    public record PersonalInformationId(Guid Value) : ConceptAs<Guid>(Value);
}
