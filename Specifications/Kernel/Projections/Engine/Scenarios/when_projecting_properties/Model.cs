// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Scenarios.when_projecting_properties;

public record Model(
    string StringValue,
    bool BoolValue,
    int IntValue,
    double DoubleValue,
    StringConcept StringConceptValue,
    BoolConcept BoolConceptValue,
    IntConcept IntConceptValue,
    DoubleConcept DoubleConceptValue);
