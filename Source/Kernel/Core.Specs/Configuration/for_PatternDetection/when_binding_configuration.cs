// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_PatternDetection;

public class when_binding_configuration : Specification
{
    PatternDetection _bound;

    void Because()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PatternDetection:Enabled"] = "false",
                ["PatternDetection:Overrides:Store"] = "true",
                ["PatternDetection:Overrides:Store/Tenant"] = "false"
            })
            .Build();
        var options = new ChronicleOptions();
        configuration.Bind(options);
        _bound = options.PatternDetection;
    }

    [Fact] void should_bind_the_default() => _bound.Enabled.ShouldBeFalse();
    [Fact] void should_turn_an_event_store_on() => _bound.IsEnabledFor("store", "any").ShouldBeTrue();
    [Fact] void should_turn_a_namespace_off() => _bound.IsEnabledFor("store", "tenant").ShouldBeFalse();
}
