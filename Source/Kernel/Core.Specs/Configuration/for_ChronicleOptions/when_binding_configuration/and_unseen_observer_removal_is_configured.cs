// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_unseen_observer_removal_is_configured : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:Observers:UnseenRemovalEnabled"] = "true",
                ["Cratis:Chronicle:Observers:UnseenRemovalAfter"] = "30.00:00:00",
                ["Cratis:Chronicle:Observers:UnseenRemovalSweepInterval"] = "00:10:00",
                ["Cratis:Chronicle:Observers:UnseenRemovalBatchSize"] = "25"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_bind_that_it_is_turned_on() => _options.Observers.UnseenRemovalEnabled.ShouldBeTrue();
    [Fact] void should_bind_the_period() => _options.Observers.UnseenRemovalAfter.ShouldEqual(TimeSpan.FromDays(30));
    [Fact] void should_bind_the_sweep_interval() => _options.Observers.UnseenRemovalSweepInterval.ShouldEqual(TimeSpan.FromMinutes(10));
    [Fact] void should_bind_the_batch_size() => _options.Observers.UnseenRemovalBatchSize.ShouldEqual(25);
}
