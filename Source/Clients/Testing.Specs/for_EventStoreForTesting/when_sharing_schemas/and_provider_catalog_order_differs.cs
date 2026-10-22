// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_provider_catalog_order_differs : given.schema_sharing
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
            ComplianceForTypesProviders = [typeof(given.IgnoredComplianceMetadataProvider), typeof(PIIMetadataProvider)]
        });
    }

    [Fact] void should_not_share_generators_for_different_provider_precedence() => ReferenceEquals(GeneratorFor(_first), GeneratorFor(_other)).ShouldBeFalse();
    [Fact] void should_generate_identical_schemas() => SchemaFor(_first).ToJson().ShouldEqual(SchemaFor(_other).ToJson());

    void Destroy() => _first?.Dispose();
}
