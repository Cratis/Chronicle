// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ConstantValueExpression;

public class when_converting_supported_constants : Specification
{
    readonly List<(string Event, string All, string Expected)> _results = [];
    CultureInfo _originalCulture;

    void Establish()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
    }

    void Destroy() => CultureInfo.CurrentCulture = _originalCulture;

    void Because()
    {
        Add(true, "True");
        Add('x', "x");
        Add((byte)1, "1");
        Add((sbyte)-1, "-1");
        Add((short)-2, "-2");
        Add((ushort)2, "2");
        Add(-3, "-3");
        Add(3U, "3");
        Add(-4L, "-4");
        Add(4UL, "4");
        Add(1.25F, "1.25");
        Add(2.5D, "2.5");
        Add(3.75M, "3.75");
        Add("text", "text");
        Add(Guid.Parse("e3af8350-6750-4ee0-87ed-150c4ed96adf"), "e3af8350-6750-4ee0-87ed-150c4ed96adf");
        Add(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), "2026-01-02T03:04:05.0000000Z");
        Add(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)), "2026-01-02T03:04:05.0000000+02:00");
        Add(new DateOnly(2026, 1, 2), "2026-01-02");
        Add(new TimeOnly(3, 4, 5), "03:04:05.0000000");
        Add(TimeSpan.FromMinutes(5), "00:05:00");
        Add(new Uri("https://example.com/path"), "https://example.com/path");
        Add(Status.Active, "1");
        Add(LongStatus.Active, "Active");
        Add(new TextConcept("concept"), "concept");
        Add<int?>(42, "42");
        Add<decimal?>(1.25M, "1.25");
        Add<Guid?>(Guid.Empty, "00000000-0000-0000-0000-000000000000");
        Add<string?>(null, null);
        Add<int?>(null, null);
        Add<TextConcept?>(null, null);
        Add((Half)1.5, "1.5");
        Add((Int128)12, "12");
        Add((UInt128)12, "12");
        Add(new BigInteger(12), "12");
        Add((nint)12, "12");
        Add((nuint)12, "12");
    }

    [Fact]
    void should_preserve_event_specific_literals()
    {
        foreach (var result in _results) result.Event.ShouldEqual(result.Expected);
    }

    [Fact]
    void should_preserve_every_event_literals()
    {
        foreach (var result in _results) result.All.ShouldEqual(result.Expected);
    }

    void Add<T>(T value, string? literal)
    {
        var eventBuilder = new SetBuilder<object, object, T, object>(new object(), new PropertyPath("Target"), new DefaultNamingPolicy());
        var allBuilder = new AllSetBuilder<object, object>(new object(), new PropertyPath("Target"), new DefaultNamingPolicy());
        eventBuilder.ToValue(value);
        allBuilder.ToValue(value);
        _results.Add((eventBuilder.Build(), allBuilder.Build(), literal is null ? "$null" : $"$value({literal})"));
    }

    enum Status { Active = 1 }
    enum LongStatus : long { Active = 1 }
    record TextConcept(string Value) : ConceptAs<string>(Value);
}
