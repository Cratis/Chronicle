// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_computing_definition_digests;

public class and_only_registration_metadata_differs : given.a_definition
{
    void Establish() => _other = _definition with { LastUpdated = DateTimeOffset.FromUnixTimeSeconds(100), Version = "2.0.0", HasLegacyStorageFields = true };
    void Because() => Compute();
    [Fact] void should_keep_the_digest() => _otherDigest.ShouldEqual(_digest);
    [Fact] void should_not_normalize_the_original_definition() => _other.Version.ShouldEqual("2.0.0");
    [Fact] void should_not_clear_the_original_timestamp() => _other.LastUpdated.ShouldEqual(DateTimeOffset.FromUnixTimeSeconds(100));
    [Fact] void should_not_clear_the_original_storage_metadata() => _other.HasLegacyStorageFields.ShouldBeTrue();
}
