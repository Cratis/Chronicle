// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding;

public class when_encoding_parts : Specification
{
    static readonly Guid _guid = Guid.Parse("6f1c2c43-6c8c-4b3c-9d1c-3a7b4f0e2a11");
    static readonly DateTimeOffset _moment = new(2026, 3, 4, 5, 6, 7, TimeSpan.FromHours(2));

    CultureInfo _previous;
    string _inGerman;
    string _inInvariant;
    string _reordered;
    string _collidingFirst;
    string _collidingSecond;
    string _nullPart;
    string _emptyPart;

    void Establish() => _previous = CultureInfo.CurrentCulture;

    void Because()
    {
        var parts = new Dictionary<string, object?> { ["id"] = _guid, ["amount"] = 1.50m, ["at"] = _moment, ["count"] = 1234567, ["ratio"] = 0.5d };
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        _inInvariant = CompositeKeyEncoding.Encode(parts);
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        _inGerman = CompositeKeyEncoding.Encode(parts);
        _reordered = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["b"] = "2", ["a"] = "1" });
        _collidingFirst = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = "a_b", ["b"] = "c" });
        _collidingSecond = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = "a", ["b"] = "b_c" });
        _nullPart = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = null });
        _emptyPart = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = string.Empty });
    }

    void Destroy() => CultureInfo.CurrentCulture = _previous;

    [Fact] void should_not_depend_on_the_culture() => _inGerman.ShouldEqual(_inInvariant);
    [Fact] void should_format_typed_parts_invariantly() => _inInvariant.ShouldEqual("{\"amount\":1.5,\"at\":\"2026-03-04T05:06:07.0000000+02:00\",\"count\":1234567,\"id\":\"6f1c2c43-6c8c-4b3c-9d1c-3a7b4f0e2a11\",\"ratio\":0.5}");
    [Fact] void should_order_parts_by_name() => _reordered.ShouldEqual("{\"a\":\"1\",\"b\":\"2\"}");
    [Fact] void should_keep_parts_containing_a_delimiter_apart() => _collidingFirst.ShouldNotEqual(_collidingSecond);
    [Fact] void should_keep_null_apart_from_empty() => _nullPart.ShouldNotEqual(_emptyPart);
    [Fact] void should_decode_back_to_the_parts() => CompositeKeyEncoding.TryDecode(_reordered, out _).ShouldBeTrue();
}
