// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_navigating_materialized_identities.given;

public class children_with_a_subset_sibling : Specification
{
    protected ExpandoObject _state;
    protected ExpandoObject _target;
    protected List<ExpandoObject> _children;
    protected readonly PropertyPath _path = "[children].name";
    protected ArrayIndexers _indexers;

    void Establish()
    {
        var guid = Guid.Parse("ad037c15-1354-4cd7-a069-f24aa1706109");
        _target = new { id = new { tenant = "tenant", number = 7L, id = guid.ToString().ToUpperInvariant() } }.AsExpandoObject();
        _children = [new { id = new { tenant = "tenant" } }.AsExpandoObject(), _target];
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["children"] = _children;
        _indexers = new([new ArrayIndexer("[children]", "id", new { Tenant = "tenant", Number = 7, Id = guid }.AsExpandoObject())]);
    }
}
