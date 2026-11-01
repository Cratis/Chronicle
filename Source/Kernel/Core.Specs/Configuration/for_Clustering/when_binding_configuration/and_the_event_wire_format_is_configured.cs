// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Clustering.when_binding_configuration;

public class and_the_event_wire_format_is_configured : Specification
{
    ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions();

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["clustering:eventWireFormat"] = "Compact" })
        .Build()
        .Bind(_options);

    [Fact] void should_bind_compact() => _options.Clustering.EventWireFormat.ShouldEqual(EventWireFormat.Compact);
    [Fact] void should_default_to_legacy_json() => new Clustering().EventWireFormat.ShouldEqual(EventWireFormat.Json);
}
