// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.GrainDirectory;

namespace Cratis.Chronicle.Setup.for_GrainDirectoryConfiguration.when_configuring;

public class and_it_is_not_configured : given.a_silo_builder
{
    void Establish() => Configure(new Dictionary<string, string?>());

    void Because() => _builder.AddConfiguredGrainDirectory();

    [Fact] void should_keep_the_default_grain_directory() => _services.Any(_ => _.ServiceType == typeof(IGrainDirectory)).ShouldBeFalse();
}
