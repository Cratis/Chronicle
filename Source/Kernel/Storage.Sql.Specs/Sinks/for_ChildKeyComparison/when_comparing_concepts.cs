// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildKeyComparison;

public class when_comparing_concepts : Specification
{
    bool[] _matches;

    void Because() => _matches =
    [
        ChildKeyComparison.AreEqual(new { id = new Number(7) }, new { ID = 7L }),
        ChildKeyComparison.AreEqual(new { ID = 7L }, new { id = new Number(7) }),
        ChildKeyComparison.AreEqual(new Composite(new Identity(new Number(7))), new { id = 7L }),
        ChildKeyComparison.AreEqual(new { id = 7L }, new Composite(new Identity(new Number(7)))),
        ChildKeyComparison.AreEqual(new Number(7), 7L),
        ChildKeyComparison.AreEqual(7L, new Number(7))
    ];

    [Fact] void should_unwrap_root_and_nested_concepts_symmetrically() => _matches.ShouldEachConformTo(match => match);

    public record Number(int Value) : ConceptAs<int>(Value);
    public record Identity(Number Id) : IComparable
    {
        public static bool operator <(Identity left, Identity right) => left.CompareTo(right) < 0;
        public static bool operator <=(Identity left, Identity right) => left.CompareTo(right) <= 0;
        public static bool operator >(Identity left, Identity right) => left.CompareTo(right) > 0;
        public static bool operator >=(Identity left, Identity right) => left.CompareTo(right) >= 0;
        public int CompareTo(object? obj) => obj is Identity other ? Id.Value.CompareTo(other.Id.Value) : 1;
    }
    public record Composite(Identity Value) : ConceptAs<Identity>(Value);
}
