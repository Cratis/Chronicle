// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Clustering.when_binding_configuration;

/// <summary>
/// The distributed grain directory is opt-in because changing it requires a full cluster stop and start.
/// </summary>
public class and_the_distributed_grain_directory_is_enabled : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:Clustering:DistributedGrainDirectoryEnabled"] = "true"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_enable_the_distributed_grain_directory() => _options.Clustering.DistributedGrainDirectoryEnabled.ShouldBeTrue();
}
