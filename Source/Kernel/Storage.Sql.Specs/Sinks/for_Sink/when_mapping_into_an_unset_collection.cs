// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_mapping_into_an_unset_collection : given.an_initialized_row_with_unset_properties
{
    async Task Because()
    {
        var indexers = new ArrayIndexers([new ArrayIndexer("[children]", "id", "child-1")]);
        var mapper = PropertyMappers.FromEventValueProvider("[children].name", _ => "First child");
        await Apply(mapper(_event, _initial, indexers));
    }

    [Fact] void should_create_the_child() => Children().Count().ShouldEqual(1);
    [Fact] void should_set_the_child_property() => ((IDictionary<string, object?>)Children().Single())["name"].ShouldEqual("First child");

    IEnumerable<ExpandoObject> Children() => ((IEnumerable<object>)_stored["children"]!).Cast<ExpandoObject>();
}
