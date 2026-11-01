// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelConfiguration;

[Collection(ReleasedConfigurationCollection.Name)]
public class when_binding_original_cluster_keys : Cratis.Specifications.Specification
{
    (bool TypeMatches, bool ConnectionMatches, bool OptionsAreReleased, bool StorageIsReleased) _binding;

    void Because() => _binding = ReleasedConfiguration.Bind(ReleasedConfiguration.OriginalStorageTypeKey, ReleasedConfiguration.OriginalStorageConnectionKey);

    [Fact] void should_not_bind_the_original_provider() => _binding.TypeMatches.ShouldBeFalse();
    [Fact] void should_not_bind_the_original_connection() => _binding.ConnectionMatches.ShouldBeFalse();
    [Fact] void should_use_released_options_not_head() => _binding.OptionsAreReleased.ShouldBeTrue();
    [Fact] void should_use_released_storage_not_head() => _binding.StorageIsReleased.ShouldBeTrue();
}
