// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_recovering_from_failed_activation : given.schema_sharing
{
    readonly Exception _failure = new("Metadata provider dependency is unavailable");
    Exception _error;

    void Because()
    {
        _artifacts.ComplianceForPropertiesProviders = [typeof(given.IgnoredComplianceMetadataProvider)];
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(given.IgnoredComplianceMetadataProvider)).Returns(_ => throw _failure);
        _error = Catch.Exception(() => _ = new EventStoreForTesting(services, _artifacts));
        _other = new(null, _artifacts);
    }

    [Fact]
    void should_allow_a_later_store_with_the_same_provider_types()
    {
        _error.ShouldBeOfExactType<ClientArtifactActivationFailed>();
        _error.GetBaseException().ShouldBeSame(_failure);
        SchemaFor(_other).ToJson().ShouldEqual(SchemaFor(_store).ToJson());
    }
}
