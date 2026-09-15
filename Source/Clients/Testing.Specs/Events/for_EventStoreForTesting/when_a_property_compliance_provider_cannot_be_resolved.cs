// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

public class when_a_property_compliance_provider_cannot_be_resolved : Specification
{
    IServiceProvider _services;
    IClientArtifactsProvider _artifacts;
    Exception _failure;
    Exception _error;

    void Establish()
    {
        _failure = new Exception("Property compliance dependency is unavailable");
        _services = Substitute.For<IServiceProvider>();
        _services.GetService(typeof(PIIMetadataProvider)).Returns(_ => throw _failure);
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.ComplianceForPropertiesProviders.Returns([typeof(PIIMetadataProvider)]);
    }

    void Because() => _error = Catch.Exception(() => _ = new EventStoreForTesting(_services, _artifacts));

    [Fact] void should_fail_instead_of_dropping_compliance_metadata() => _error.ShouldBeOfExactType<ClientArtifactActivationFailed>();
    [Fact] void should_identify_the_provider() => _error.Message.ShouldContain(typeof(PIIMetadataProvider).FullName!);
    [Fact] void should_preserve_the_original_cause() => _error.GetBaseException().ShouldEqual(_failure);
}
