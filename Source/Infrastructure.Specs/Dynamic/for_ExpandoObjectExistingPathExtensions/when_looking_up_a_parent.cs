// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExistingPathExtensions;

public class when_looking_up_a_parent : Specification
{
    ExpandoObject _root;
    ExpandoObject _first;
    ExpandoObject _second;
    ExpandoObject _info;
    ExpandoObject? _identified;
    ExpandoObject? _positional;
    ExpandoObject? _absentAncestor;
    ExpandoObject? _nullAncestor;
    ExpandoObject? _missingIdentifier;
    ExpandoObject? _missingIndexer;
    ExpandoObject? _outOfRange;
    ExpandoObject? _invalidIndex;
    ExpandoObject? _negativeIndex;

    void Establish()
    {
        _info = new ExpandoObject();
        _first = new ExpandoObject();
        _second = new ExpandoObject();
        ((IDictionary<string, object?>)_first)["id"] = "first";
        ((IDictionary<string, object?>)_first)["info"] = _info;
        ((IDictionary<string, object?>)_second)["id"] = "second";
        _root = new ExpandoObject();
        ((IDictionary<string, object?>)_root)["items"] = new List<ExpandoObject> { _first, _second };
        ((IDictionary<string, object?>)_root)["nullOuter"] = null;
    }

    void Because()
    {
        _identified = _root.TryGetExistingPath("[items].info.name", new ArrayIndexers([new ArrayIndexer("[items]", "id", "first")]));
        _positional = _root.TryGetExistingPath("[items].info.name", new ArrayIndexers([new ArrayIndexer("[items]", PropertyPath.NotSet, 0)]));
        _absentAncestor = _root.TryGetExistingPath("missing.info.name", ArrayIndexers.NoIndexers);
        _nullAncestor = _root.TryGetExistingPath("nullOuter.info.name", ArrayIndexers.NoIndexers);
        _missingIdentifier = _root.TryGetExistingPath("[items].info.name", new ArrayIndexers([new ArrayIndexer("[items]", "id", "missing")]));
        _missingIndexer = _root.TryGetExistingPath("[items].info.name", ArrayIndexers.NoIndexers);
        _outOfRange = _root.TryGetExistingPath("[items].info.name", new ArrayIndexers([new ArrayIndexer("[items]", PropertyPath.NotSet, 4)]));
        _invalidIndex = _root.TryGetExistingPath("[items].info.name", new ArrayIndexers([new ArrayIndexer("[items]", PropertyPath.NotSet, "bad")]));
        _negativeIndex = _root.TryGetExistingPath("[items].info.name", new ArrayIndexers([new ArrayIndexer("[items]", PropertyPath.NotSet, -1)]));
    }

    [Fact] void should_resolve_the_identified_child() => ReferenceEquals(_identified, _info).ShouldBeTrue();
    [Fact] void should_resolve_the_positional_child() => ReferenceEquals(_positional, _info).ShouldBeTrue();
    [Fact] void should_not_create_an_absent_ancestor() => ((IDictionary<string, object?>)_root).ContainsKey("missing").ShouldBeFalse();
    [Fact] void should_not_resolve_an_absent_ancestor() => _absentAncestor.ShouldBeNull();
    [Fact] void should_not_recreate_a_null_ancestor() => ((IDictionary<string, object?>)_root)["nullOuter"].ShouldBeNull();
    [Fact] void should_not_resolve_a_null_ancestor() => _nullAncestor.ShouldBeNull();
    [Fact] void should_not_resolve_a_missing_identifier() => _missingIdentifier.ShouldBeNull();
    [Fact] void should_not_resolve_a_missing_indexer() => _missingIndexer.ShouldBeNull();
    [Fact] void should_not_resolve_an_out_of_range_index() => _outOfRange.ShouldBeNull();
    [Fact] void should_not_resolve_an_invalid_index() => _invalidIndex.ShouldBeNull();
    [Fact] void should_not_resolve_a_negative_index() => _negativeIndex.ShouldBeNull();
    [Fact] void should_leave_the_collection_unchanged() => ((List<ExpandoObject>)((IDictionary<string, object?>)_root)["items"]!).Count.ShouldEqual(2);
}
