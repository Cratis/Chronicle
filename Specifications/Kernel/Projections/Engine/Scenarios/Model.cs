// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Scenarios.Concepts;

namespace Aksio.Cratis.Events.Projections.Scenarios;

public record Model(
    string StringValue,
    bool BoolValue,
    int IntValue,
    float FloatValue,
    double DoubleValue,
    EnumWithValues EnumValue,
    Guid GuidValue,
    DateTime DateTimeValue,
    DateOnly DateOnlyValue,
    TimeOnly TimeOnlyValue,
    DateTimeOffset DateTimeOffsetValue,
    StringConcept StringConceptValue,
    BoolConcept BoolConceptValue,
    IntConcept IntConceptValue,
    FloatConcept FloatConceptValue,
    DoubleConcept DoubleConceptValue,
    EnumWithValuesConcept EnumConceptValue,
    GuidConcept GuidConceptValue,
    DateTimeConcept DateTimeConceptValue,
    DateOnlyConcept DateOnlyConceptValue,
    TimeOnlyConcept TimeOnlyConceptValue,
    DateTimeOffsetConcept DateTimeOffsetConceptValue,
    DateTimeOffset LastUpdated);
