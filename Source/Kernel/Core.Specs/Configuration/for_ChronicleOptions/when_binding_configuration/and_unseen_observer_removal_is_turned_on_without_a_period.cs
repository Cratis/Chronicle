// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_unseen_observer_removal_is_turned_on_without_a_period : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Cratis:Chronicle:Observers:UnseenRemovalEnabled"] = "true" })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_be_turned_on() => _options.Observers.UnseenRemovalEnabled.ShouldBeTrue();
    [Fact] void should_keep_the_seven_day_period() => _options.Observers.UnseenRemovalAfter.ShouldEqual(TimeSpan.FromDays(7));
}
