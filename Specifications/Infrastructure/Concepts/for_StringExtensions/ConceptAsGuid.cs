// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_StringExtensions;

public record ConceptAsGuid(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator ConceptAsGuid(Guid id) => new(id);
}
