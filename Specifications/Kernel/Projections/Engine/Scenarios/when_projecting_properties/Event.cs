// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Scenarios.when_projecting_properties;

[EventType("990e339a-eb22-4945-905e-1c8d948d517b")]
public record Event(
    string StringValue,
    bool BoolValue,
    int IntValue,
    double DoubleValue,
    StringConcept StringConceptValue,
    BoolConcept BoolConceptValue,
    IntConcept IntConceptValue,
    DoubleConcept DoubleConceptValue);
