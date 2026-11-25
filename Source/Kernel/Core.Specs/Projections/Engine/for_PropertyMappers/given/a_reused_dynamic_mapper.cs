// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers.given;

public abstract class a_reused_dynamic_mapper : Specification
{
    protected ExpandoObject _target = new();
    protected PropertyDifference _first;
    protected PropertyDifference _second;
    protected PropertyDifference _repeated;
    protected PropertyDifference _removal;

    protected void Map(PropertyPath path, AppendedEvent first, AppendedEvent second)
    {
        var mapper = PropertyMappers.FromEventValueProvider(path, _ => "value");
        _first = mapper(first, _target, ArrayIndexers.NoIndexers);
        _second = mapper(second, _target, ArrayIndexers.NoIndexers);
        _repeated = mapper(first, _target, ArrayIndexers.NoIndexers);
        _removal = PropertyMappers.Remove(path)(second, _target, ArrayIndexers.NoIndexers);
    }
}
