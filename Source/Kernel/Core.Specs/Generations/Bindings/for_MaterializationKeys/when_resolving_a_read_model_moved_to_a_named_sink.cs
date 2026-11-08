// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Sinks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Generations.Bindings.for_MaterializationKeys;

public class when_resolving_a_read_model_moved_to_a_named_sink : given.a_materialization
{
    MaterializationKey _before;
    MaterializationKey _after;

    async Task Because()
    {
        var sinks = new SinkConfigurations(Options.Create(new ChronicleOptions
        {
            Sinks = new()
            {
                Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Reporting"] = new() { Type = "SQL", ConnectionDetails = "Host=pg;Database=reporting" }
                }
            }
        }));
        _before = await new MaterializationKeys(_storage, sinks).For(_name, _namespace, _projection);
        _sink = _sink with { Name = "reporting" };
        UseSink(_sink);
        _after = await new MaterializationKeys(_storage, sinks).For(_name, _namespace, _projection);
    }

    [Fact] void should_be_another_materialization() => _after.Value.ShouldNotEqual(_before.Value);
    [Fact] void should_use_the_resolved_sink_type() => _after.SinkType.ShouldEqual(WellKnownSinkTypes.SQL);
    [Fact] void should_use_the_resolved_sink_name() => _after.SinkName.ShouldEqual(new SinkName("Reporting"));
    [Fact] void should_keep_the_key_of_the_default_sink_unchanged() => _before.Value.ShouldEqual(new MaterializationKey(_floor.Incarnation, _namespace, _projection, "mongodb", _before.SinkConfiguration).Value);
}
