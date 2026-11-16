// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

[PerFactSpecificationLifecycle]
public class when_using_legacy_evolution_configuration : given.additive_evolution_enabled
{
    [Theory]
    [InlineData(false, "test-store")]
    [InlineData(false, "another-store")]
    [InlineData(true, "test-store")]
    public async Task should_bind_but_ignore_the_legacy_opt_in_list(bool strict, string legacyEventStore)
    {
        var entries = new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:AdditiveEventTypeSchemaEvolutionEventStores:0"] = legacyEventStore
        };
        if (strict)
        {
            entries["Cratis:Chronicle:StrictEventTypeSchemaEventStores:0"] = "test-store";
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(entries).Build();
        var options = configuration.GetSection("Cratis:Chronicle").Get<ChronicleOptions>();
#pragma warning disable CS0618 // Verify that the deprecated configuration property remains bindable.
        options.AdditiveEventTypeSchemaEvolutionEventStores.ShouldContain(legacyEventStore);
#pragma warning restore CS0618
        _subject = new EventTypeRegistrar(_grainFactory, NullLogger<EventTypeRegistrar>.Instance, Options.Create(options));
        var results = await _subject.Check("test-store", [Registration()], _storage);
        results.Single().Accepted.ShouldEqual(!strict);
        var error = await Catch.Exception(() => Register());
        (error is EventTypeSchemaChanged).ShouldEqual(strict);
    }
}
