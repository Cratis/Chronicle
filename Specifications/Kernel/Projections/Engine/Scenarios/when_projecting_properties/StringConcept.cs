// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Events.Projections.Scenarios.when_projecting_properties;

public record StringConcept(string Value): ConceptAs<string>(Value)
{
    public static implicit operator StringConcept(string value) => new(value);
}
