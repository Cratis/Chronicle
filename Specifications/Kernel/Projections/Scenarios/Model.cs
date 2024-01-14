// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Scenarios.Concepts;

namespace Aksio.Cratis.Kernel.Projections.Scenarios;


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
    DateTimeOffset LastUpdated)
{
    static Random random = new();

    public static Model CreateWithKnownValues() => new(
            KnownValues.StringValue,
            KnownValues.BoolValue,
            KnownValues.IntValue,
            KnownValues.FloatValue,
            KnownValues.DoubleValue,
            KnownValues.EnumValue,
            KnownValues.GuidValue,
            KnownValues.DateTimeValue,
            KnownValues.DateOnlyValue,
            KnownValues.TimeOnlyValue,
            KnownValues.DateTimeOffsetValue,
            KnownValues.StringConceptValue,
            KnownValues.BoolConceptValue,
            KnownValues.IntConceptValue,
            KnownValues.FloatConceptValue,
            KnownValues.DoubleConceptValue,
            KnownValues.EnumConceptValue,
            KnownValues.GuidConceptValue,
            KnownValues.DateTimeConceptValue,
            KnownValues.DateOnlyConceptValue,
            KnownValues.TimeOnlyConceptValue,
            KnownValues.DateTimeOffsetConceptValue,
            DateTimeOffset.UtcNow);

    public static Model CreateWithRandomValues() => new(
        random.NextDouble().ToString(),
        (random.Next() % 1) == 0,
        random.Next(5000),
        random.NextSingle(),
        random.NextDouble(),
        (EnumWithValues)random.Next((int)EnumWithValues.ThirdValue),
        Guid.NewGuid(),
        DateTime.UtcNow.AddDays(random.Next(60)),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(random.Next(60))),
        TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(random.Next(48))),
        DateTimeOffset.UtcNow.AddDays(random.Next(60)),
        random.NextDouble().ToString(),
        (random.Next() % 1) == 0,
        random.Next(5000),
        random.NextSingle(),
        random.NextDouble(),
        (EnumWithValues)random.Next((int)EnumWithValues.ThirdValue),
        Guid.NewGuid(),
        DateTime.UtcNow.AddDays(random.Next(60)),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(random.Next(60))),
        TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(random.Next(48))),
        DateTimeOffset.UtcNow.AddDays(random.Next(60)),
        DateTimeOffset.UtcNow.AddDays(random.Next(60)));
}
