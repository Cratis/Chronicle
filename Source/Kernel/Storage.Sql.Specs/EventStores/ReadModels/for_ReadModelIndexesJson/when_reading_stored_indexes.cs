// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.for_ReadModelIndexesJson;

public class when_reading_stored_indexes : Specification
{
    string _stored;
    IndexDefinition[] _roundTripped;
    IndexDefinition[] _legacy;
    IndexDefinition[] _missing;

    void Because()
    {
        _stored = ReadModelIndexesJson.Serialize([new IndexDefinition(new PropertyPath("name")), new IndexDefinition(new PropertyPath("lines.productId"))]);
        _roundTripped = ReadModelIndexesJson.Deserialize(_stored);
        _legacy = ReadModelIndexesJson.Deserialize("""["name","total"]""");
        _missing = ReadModelIndexesJson.Deserialize(null);
    }

    [Fact] void should_store_each_index_as_an_object_with_properties() => _stored.ShouldEqual("""[{"properties":[{"path":"name"}]},{"properties":[{"path":"lines.productId"}]}]""");
    [Fact] void should_round_trip_the_paths() => _roundTripped.Select(index => index.PropertyPath.Path).ShouldEqual(["name", "lines.productId"]);
    [Fact] void should_read_plain_path_strings() => _legacy.Select(index => index.PropertyPath.Path).ShouldEqual(["name", "total"]);
    [Fact] void should_treat_missing_json_as_no_indexes() => _missing.ShouldBeEmpty();
}
