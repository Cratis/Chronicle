// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition;

public class when_materializing_a_legacy_definition : Specification
{
    static readonly JsonSerializerOptions _options = new() { Converters = { new ConceptAsJsonConverterFactory() } };
    UniqueConstraintDefinition _definition;

    void Because() => _definition = JsonSerializer.Deserialize<UniqueConstraintDefinition>(
        """
        { "Name": "versions", "EventDefinitions": [] }
        """,
        _options)!;

    [Fact] void should_default_to_one_value_per_source() => _definition.Mode.ShouldEqual(UniqueConstraintMode.PerEventSource);
    [Fact] void should_default_to_no_removal_properties() => _definition.RemovalEventDefinitions.ShouldBeEmpty();
}
