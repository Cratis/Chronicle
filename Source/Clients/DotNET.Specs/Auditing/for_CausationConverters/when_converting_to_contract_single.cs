// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_to_contract_single : Specification
{
    Causation causation;
    Contracts.Auditing.Causation result;
    DateTimeOffset occurred;
    CausationType type;
    IDictionary<string, string> properties;

    void Establish()
    {
        occurred = DateTimeOffset.UtcNow;
        type = CausationType.Root;
        properties = new Dictionary<string, string> { { "key", "value" } };
        causation = new(occurred, type, properties);
    }

    void Because() => result = causation.ToContract();

    [Fact] void should_map_occurred() => ((DateTimeOffset)result.Occurred).ShouldEqual(occurred);
    [Fact] void should_map_type() => result.Type.ShouldEqual(type.Value);
    [Fact] void should_map_properties() => result.Properties.ShouldEqual(properties);
}
