// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_metadata_provider_catalogs_differ : given.schema_sharing
{
    EventStoreForTesting[] _variants;

    void Because()
    {
        InMemoryClientArtifactsProvider[] catalogs =
        [
            new() { ComplianceForTypesProviders = [typeof(PIIMetadataProvider)] },
            new() { ComplianceForPropertiesProviders = [typeof(PIIMetadataProvider)] },
            new() { SecurityForTypesProviders = [typeof(EncryptedMetadataProvider)] },
            new() { SecurityForPropertiesProviders = [typeof(EncryptedMetadataProvider)] }
        ];
        _variants = catalogs.Select(catalog =>
        {
            catalog.EventTypes = _artifacts.EventTypes;
            return new EventStoreForTesting(null, catalog);
        }).ToArray();
    }

    [Fact] void should_not_reuse_the_empty_provider_catalog() => _variants.All(store => !ReferenceEquals(GeneratorFor(store), GeneratorFor(_store))).ShouldBeTrue();
    [Fact] void should_keep_the_four_provider_categories_distinct() => _variants.Select(GeneratorFor).Distinct().Count().ShouldEqual(4);

    void Destroy()
    {
        foreach (var store in _variants ?? [])
        {
            store.Dispose();
        }
    }
}
