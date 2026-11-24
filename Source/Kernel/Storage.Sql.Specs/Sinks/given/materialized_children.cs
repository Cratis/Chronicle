// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.given;

public class materialized_children : Specification
{
    protected IList<object?> _children;
    protected ExpandoObject _target;
    protected ExpandoObject _subset;
    protected ExpandoObject _different;
    protected ExpandoObject _ambiguous;
    protected CompositeKey _key;
    protected Dictionary<string, object?> _document;
    protected ArrayIndexer _indexer;

    void Establish()
    {
        var guid = Guid.Parse("ad037c15-1354-4cd7-a069-f24aa1706109");
        _key = new(new Identity("tenant", new Number(7), guid));
        _target = Child(new { tenant = "tenant", number = 7L, id = guid.ToString().ToUpperInvariant() });
        _subset = Child(new { tenant = "tenant" });
        _different = Child(new { tenant = "other", number = 7L, id = guid.ToString() });
        _ambiguous = Child(new Dictionary<string, object> { ["tenant"] = "tenant", ["Tenant"] = "tenant", ["number"] = 7, ["id"] = guid });
        _children = new List<object?> { _subset, _ambiguous, _target, _different };
        _document = new() { ["children"] = _children };
        _indexer = new("[children]", "id", _key);
    }

    static ExpandoObject Child(object key)
    {
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)["id"] = key;
        ((IDictionary<string, object?>)child)["name"] = "original";
        return child;
    }

    public record Identity(string Tenant, Number Number, Guid Id) : IComparable
    {
        public static bool operator <(Identity left, Identity right) => left.CompareTo(right) < 0;
        public static bool operator <=(Identity left, Identity right) => left.CompareTo(right) <= 0;
        public static bool operator >(Identity left, Identity right) => left.CompareTo(right) > 0;
        public static bool operator >=(Identity left, Identity right) => left.CompareTo(right) >= 0;
        public int CompareTo(object? obj) => obj is Identity other ? (Tenant, Number.Value, Id).CompareTo((other.Tenant, other.Number.Value, other.Id)) : 1;
    }
    public record Number(int Value) : ConceptAs<int>(Value);
    public record CompositeKey(Identity Value) : ConceptAs<Identity>(Value);
}
