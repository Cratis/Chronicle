// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters;

public class when_converting_to_client_single_with_null_properties : Specification
{
    Contracts.Auditing.Causation causation;
    Causation result;
    DateTimeOffset occurred;
    string type;

    void Establish()
    {
        occurred = DateTimeOffset.UtcNow;
        type = "Root";
        causation = new Contracts.Auditing.Causation { Occurred = occurred, Type = type, Properties = null! };
    }

    void Because() => result = causation.ToClient();

    [Fact] void should_map_occurred() => result.Occurred.ShouldEqual(occurred);
    [Fact] void should_map_type() => result.Type.Value.ShouldEqual(type);
    [Fact] void should_initialize_properties_as_empty() => result.Properties.ShouldBeEmpty();
}
