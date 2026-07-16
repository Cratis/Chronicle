// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public record ThingId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static ThingId New() => new(Guid.NewGuid());
}
