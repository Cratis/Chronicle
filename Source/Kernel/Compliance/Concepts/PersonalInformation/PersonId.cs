// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;

namespace Cratis.Compliance.Concepts.PersonalInformation
{
    public record PersonId(string Value) : ConceptAs<string>(Value)
    {
        public static implicit operator EventSourceId(PersonId PersonId) => new(PersonId.Value);
    }
}
