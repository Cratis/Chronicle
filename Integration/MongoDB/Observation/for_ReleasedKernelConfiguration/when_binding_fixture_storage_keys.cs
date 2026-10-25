// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelConfiguration;

[Collection(ReleasedConfigurationCollection.Name)]
public class when_binding_fixture_storage_keys : Cratis.Specifications.Specification
{
    (bool TypeMatches, bool ConnectionMatches, bool OptionsAreReleased, bool StorageIsReleased) _binding;

    void Because() => _binding = ReleasedConfiguration.Bind(ReleasedKernelFixture.StorageTypeKey, ReleasedKernelFixture.StorageConnectionKey);

    [Fact] void should_bind_the_fixture_provider() => _binding.TypeMatches.ShouldBeTrue();
    [Fact] void should_bind_the_fixture_connection() => _binding.ConnectionMatches.ShouldBeTrue();
    [Fact] void should_use_released_options_not_head() => _binding.OptionsAreReleased.ShouldBeTrue();
    [Fact] void should_use_released_storage_not_head() => _binding.StorageIsReleased.ShouldBeTrue();
}
