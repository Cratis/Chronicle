// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_a_reserved_destination_is_cached : given.a_sink_factory
{
    readonly List<Exception?> _errors = [];

    async Task Because()
    {
        await _sinks.GetFor(_model);
        _factory.ClearReceivedCalls();
        _sink.ClearReceivedCalls();
        var reserved = _model with { ContainerName = SinkProvenanceNames.Metadata.ToUpperInvariant() };
        _errors.Add(await Catch.Exception(() => _sinks.GetFor(reserved)));
        _errors.Add(await Catch.Exception(() => _sinks.GetWithoutProvisioning(reserved)));
        _errors.Add(await Catch.Exception(() => _sinks.GetFor(_model, SinkProvenanceNames.Metadata)));
    }

    [Fact] void should_refuse_reserved_names_even_after_the_cache_is_populated() => _errors.TrueForAll(error => error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_not_ask_the_provider_to_resolve_or_create_a_reserved_destination() => _factory.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_use_the_cached_sink() => _sink.ReceivedCalls().Any().ShouldBeFalse();
}
