// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_to_client : Specification
{
    IEnumerable<Contracts.Auditing.Causation> causations;
    Causation result;
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

    void Because() => result = causations.First().ToClient();

    [Fact] void should_map_occurred() => result.Occurred.ShouldEqual(occurred);
    [Fact] void should_map_type() => result.Type.Value.ShouldEqual(type);
    [Fact] void should_map_properties() => result.Properties.ShouldEqual(properties);
}
