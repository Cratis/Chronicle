// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_provider_catalogs_are_equivalent : given.schema_sharing
{
    EventStoreForTesting _first;

    void Because()
    {
        _first = new(null, new InMemoryClientArtifactsProvider
        {
            EventTypes = _artifacts.EventTypes,
            ComplianceForTypesProviders = [typeof(PIIMetadataProvider), typeof(given.IgnoredComplianceMetadataProvider)]
        });
        _other = new(null, new InMemoryClientArtifactsProvider
        {
            EventTypes = _artifacts.EventTypes,
            ComplianceForTypesProviders = [typeof(given.IgnoredComplianceMetadataProvider), typeof(PIIMetadataProvider), typeof(PIIMetadataProvider)]
        });
    }

    [Fact] void should_ignore_order_and_duplicates_in_provider_type_sets() => GeneratorFor(_first).ShouldBeSame(GeneratorFor(_other));
    [Fact] void should_generate_identical_schemas() => SchemaFor(_first).ToJson().ShouldEqual(SchemaFor(_other).ToJson());

    void Destroy() => _first?.Dispose();
}
