// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Generations.Bindings.for_MaterializationKeys;

public class when_resolving : given.a_materialization
{
    MaterializationKey _result;
    async Task Because() => _result = await _keys.For(_name, _namespace, _projection);
    [Fact] void should_include_the_current_incarnation() => _result.Incarnation.ShouldEqual(_floor.Incarnation);
    [Fact] void should_include_the_namespace() => _result.Namespace.ShouldEqual(_namespace);
    [Fact] void should_include_the_projection() => _result.Projection.ShouldEqual(_projection);
    [Fact] void should_include_the_sink_type() => _result.SinkType.ShouldEqual(_sink.Type);
    [Fact] void should_include_the_sink_configuration() => _result.SinkConfiguration.ShouldEqual(_sink.Configuration);
    [Fact] void should_keep_the_implicit_default_sink() => _result.SinkName.IsDefault.ShouldBeTrue();
    [Fact] void should_keep_the_identity_from_before_named_sinks() => _result.Value.ShouldEqual(string.Join('/', new[] { _floor.Incarnation.Value.ToString("N"), _namespace.Value, _projection.Value, _sink.Type.Value, _sink.Configuration.Value.ToString("N") }.Select(Uri.EscapeDataString)));
}
