// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Scenarios.Concepts;

namespace Aksio.Cratis.Kernel.Projections.Scenarios;

public static class KnownValues
{
    public static string StringValue = "Forty two";
    public static bool BoolValue = true;
    public static int IntValue = 42;
    public static float FloatValue = 42.43f;
    public static double DoubleValue = 42.42;
    public static EnumWithValues EnumValue = EnumWithValues.SecondValue;
    public static Guid GuidValue = Guid.NewGuid();
    public static DateTime DateTimeValue = DateTime.UtcNow;
    public static DateOnly DateOnlyValue = DateOnly.FromDateTime(DateTime.UtcNow);
    public static TimeOnly TimeOnlyValue = TimeOnly.FromDateTime(DateTime.UtcNow);
    public static DateTimeOffset DateTimeOffsetValue = DateTimeOffset.UtcNow;
    public static StringConcept StringConceptValue = "Forty three";
    public static BoolConcept BoolConceptValue = true;
    public static IntConcept IntConceptValue = 43;
    public static FloatConcept FloatConceptValue = 44.45f;
    public static DoubleConcept DoubleConceptValue = 43.43;
    public static EnumWithValuesConcept EnumConceptValue = EnumWithValues.ThirdValue;
    public static GuidConcept GuidConceptValue = Guid.NewGuid();
    public static DateTimeConcept DateTimeConceptValue = DateTime.UtcNow;
    public static DateOnlyConcept DateOnlyConceptValue = DateOnly.FromDateTime(DateTime.UtcNow);
    public static TimeOnlyConcept TimeOnlyConceptValue = TimeOnly.FromDateTime(DateTime.UtcNow);
    public static DateTimeOffsetConcept DateTimeOffsetConceptValue = DateTimeOffset.UtcNow;
}
