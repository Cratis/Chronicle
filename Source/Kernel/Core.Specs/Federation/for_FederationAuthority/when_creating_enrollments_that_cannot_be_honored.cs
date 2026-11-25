// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_creating_enrollments_that_cannot_be_honored : given.a_federation_authority
{
    Exception _withoutEndpoint;
    Exception _withInvalidSubject;
    Exception _withInvalidInstance;
    Exception _whenNotHosted;

    async Task Because()
    {
        var withoutEndpoint = Create(new() { Federation = new() { Enabled = true, Hosted = new() { Enabled = true } } });
        var notHosted = Create(new() { Federation = new() { Enabled = true, Hosted = new() { PublicEndpoint = "https://federation.example.com" } } });

        _withoutEndpoint = await Catch.Exception(() => withoutEndpoint.CreateEnrollment("acme", "prod"));
        _withInvalidSubject = await Catch.Exception(() => _authority.CreateEnrollment("acme/../other", "prod"));
        _withInvalidInstance = await Catch.Exception(() => _authority.CreateEnrollment("acme", "prod instance"));
        _whenNotHosted = await Catch.Exception(() => notHosted.CreateEnrollment("acme", "prod"));
    }

    [Fact] void should_refuse_when_there_is_no_public_endpoint() => _withoutEndpoint.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_refuse_a_subject_that_is_not_a_safe_segment() => _withInvalidSubject.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_refuse_an_instance_that_is_not_a_safe_segment() => _withInvalidInstance.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_refuse_when_hosted_federation_is_not_enabled() => _whenNotHosted.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_not_record_anything() => _registry.Appended.ShouldBeEmpty();
}
