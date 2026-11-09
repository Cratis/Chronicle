// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_binding_with_an_invalid_pin_or_missing_values : given.a_federation_registry
{
    Exception _malformedPin;
    Exception _noId;
    Exception _noCertificateName;
    Exception _noTargetNamespace;

    async Task Because()
    {
        _malformedPin = await Catch.Exception(() => _registry.Bind(Binding() with { ServerPin = "sha256:not-a-hash" }));
        _noId = await Catch.Exception(() => _registry.Bind(Binding(string.Empty)));
        _noCertificateName = await Catch.Exception(() => _registry.Bind(Binding() with { ClientCertificate = " " }));
        _noTargetNamespace = await Catch.Exception(() => _registry.Bind(Binding() with { TargetNamespace = string.Empty }));
    }

    [Fact] void should_refuse_a_malformed_pin() => _malformedPin.ShouldBeOfExactType<InvalidFederationBinding>();
    [Fact] void should_refuse_a_missing_id() => _noId.ShouldBeOfExactType<InvalidFederationBinding>();
    [Fact] void should_refuse_a_missing_certificate_name() => _noCertificateName.ShouldBeOfExactType<InvalidFederationBinding>();
    [Fact] void should_refuse_a_missing_target_namespace() => _noTargetNamespace.ShouldBeOfExactType<InvalidFederationBinding>();
    [Fact] void should_not_append_anything() => _appended.ShouldBeEmpty();
}
