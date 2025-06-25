// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_to_contract : Specification
{
    IEnumerable<Causation> causations;
    IList<Contracts.Auditing.Causation> result;
    DateTimeOffset occurred;
    CausationType type;
    IDictionary<string, string> properties;

    void Establish()
    {
        occurred = DateTimeOffset.UtcNow;
        type = CausationType.Root;
        properties = new Dictionary<string, string> { { "key", "value" } };
        causations = [new Causation(occurred, type, properties)];
    }

    void Because() => result = causations.ToContract();

    [Fact] void should_return_a_list_with_same_count() => result.Count.ShouldEqual(1);
    [Fact] void should_map_occurred() => ((DateTimeOffset)result[0].Occurred).ShouldEqual(occurred);
    [Fact] void should_map_type() => result[0].Type.ShouldEqual(type.Value);
    [Fact] void should_map_properties() => result[0].Properties.ShouldEqual(properties);
}
