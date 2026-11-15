// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.GrainDirectory;

namespace Cratis.Chronicle.Setup.for_GrainDirectoryConfiguration.when_configuring;

public class and_it_is_enabled : given.a_silo_builder
{
    void Establish() => Configure(new Dictionary<string, string?>
    {
        ["Cratis:Chronicle:Clustering:DistributedGrainDirectoryEnabled"] = "true"
    });

    void Because() => _builder.AddConfiguredGrainDirectory();

    [Fact] void should_register_the_distributed_grain_directory_as_the_default() => _services.Any(_ => _.ServiceType == typeof(IGrainDirectory) && _.IsKeyedService && Equals(_.ServiceKey, GrainDirectoryAttribute.DEFAULT_GRAIN_DIRECTORY)).ShouldBeTrue();
}
