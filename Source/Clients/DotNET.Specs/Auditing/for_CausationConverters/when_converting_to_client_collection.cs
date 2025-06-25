// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_to_client_collection : Specification
{
    IEnumerable<Contracts.Auditing.Causation> causations;
    IEnumerable<Causation> result;
    DateTimeOffset occurred;
    string type;
    IDictionary<string, string> properties;

    void Establish()
    {
        occurred = DateTimeOffset.UtcNow;
        type = "Root";
        properties = new Dictionary<string, string> { { "key", "value" } };
        causations = [new Contracts.Auditing.Causation { Occurred = occurred, Type = type, Properties = properties }];
    }

    void Because() => result = causations.ToClient();

    [Fact] void should_return_a_collection_with_same_count() => result.Count().ShouldEqual(1);
    [Fact] void should_map_occurred() => result.First().Occurred.ShouldEqual(occurred);
    [Fact] void should_map_type() => result.First().Type.Value.ShouldEqual(type);
    [Fact] void should_map_properties() => result.First().Properties.ShouldEqual(properties);
}
