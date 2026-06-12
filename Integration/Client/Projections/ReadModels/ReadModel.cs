// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Projections.Concepts;
using Cratis.Geospatial;

namespace Cratis.Chronicle.Integration.Projections.ReadModels;

public record ReadModel(
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
    GuidConcept GuidConceptValue,
    Point PointValue,
    LineString LineStringValue,
    Polygon PolygonValue,
    DateTimeOffset LastUpdated,
    EventSequenceNumber? __lastHandledEventSequenceNumber = default)
{
    public virtual bool Equals(ReadModel? other) =>
        other is not null &&
        StringValue == other.StringValue &&
        BoolValue == other.BoolValue &&
        IntValue == other.IntValue &&
        FloatValue == other.FloatValue &&
        AreDoublesEqual(DoubleValue, other.DoubleValue) &&
        EnumValue == other.EnumValue &&
        GuidValue == other.GuidValue &&
        DateTimeValue == other.DateTimeValue &&
        DateOnlyValue == other.DateOnlyValue &&
        TimeOnlyValue == other.TimeOnlyValue &&
        DateTimeOffsetValue == other.DateTimeOffsetValue &&
        StringConceptValue == other.StringConceptValue &&
        BoolConceptValue == other.BoolConceptValue &&
        IntConceptValue == other.IntConceptValue &&
        FloatConceptValue == other.FloatConceptValue &&
        DoubleConceptValue == other.DoubleConceptValue &&
        GuidConceptValue == other.GuidConceptValue &&
        PointValue == other.PointValue &&
        LineStringValue.Coordinates.SequenceEqual(other.LineStringValue.Coordinates) &&
        PolygonValue.Shell.Coordinates.SequenceEqual(other.PolygonValue.Shell.Coordinates) &&
        PolygonValue.Holes.Length == other.PolygonValue.Holes.Length &&
        Enumerable.Range(0, PolygonValue.Holes.Length).All(i =>
            PolygonValue.Holes[i].Coordinates.SequenceEqual(other.PolygonValue.Holes[i].Coordinates)) &&
        LastUpdated == other.LastUpdated &&
        __lastHandledEventSequenceNumber == other.__lastHandledEventSequenceNumber;

    static bool AreDoublesEqual(double left, double right)
    {
        if (left == right) return true;
        if (double.IsNaN(left) || double.IsNaN(right)) return false;

        const double epsilon = 1e-9;
        return Math.Abs(left - right) <= epsilon;
    }

    public override int GetHashCode() =>
        HashCode.Combine(StringValue, BoolValue, IntValue, FloatValue, DoubleValue, EnumValue, GuidValue, DateTimeValue);

    static Random _random = new();

    public static ReadModel CreateWithKnownValues() => new(
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
            KnownValues.GuidConceptValue,
            KnownValues.PointValue,
            KnownValues.LineStringValue,
            KnownValues.PolygonValue,
            DateTimeOffset.UtcNow);

    public static ReadModel CreateWithRandomValues() => new(
        _random.NextDouble().ToString(),
        (_random.Next() % 1) == 0,
        _random.Next(5000),
        _random.NextSingle(),
        _random.NextDouble(),
        (EnumWithValues)(_random.Next((int)EnumWithValues.ThirdValue) + 1),
        Guid.NewGuid(),
        DateTime.UtcNow.AddDays(_random.Next(60)).RoundDownTicks(),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(_random.Next(60))),
        TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(_random.Next(48))).RoundDownTicks(),
        DateTimeOffset.UtcNow.AddDays(_random.Next(60)).RoundDownTicks(),
        _random.NextDouble().ToString(),
        (_random.Next() % 1) == 0,
        _random.Next(5000),
        _random.NextSingle(),
        _random.NextDouble(),
        Guid.NewGuid(),
        new Point(Math.Round((_random.NextDouble() * 180) - 90, 6), Math.Round((_random.NextDouble() * 360) - 180, 6)),
        new LineString([new Point(42.123, 10.456), new Point(43.456, 11.789)]),
        new Polygon(new LinearRing([new Point(0, 0), new Point(10, 0), new Point(10, 10), new Point(0, 10), new Point(0, 0)]), []),
        DateTimeOffset.UtcNow.AddDays(_random.Next(60)).RoundDownTicks());
}
