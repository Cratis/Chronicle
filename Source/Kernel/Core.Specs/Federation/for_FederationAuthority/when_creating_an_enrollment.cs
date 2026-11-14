// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_creating_an_enrollment : given.a_federation_authority
{
    FederationEnrollmentCreatedResult _created;
    FederationEnrollmentCode _code;
    FederationEnrollmentCreated _event;
    string _fingerprint;

    async Task Because()
    {
        _created = await _authority.CreateEnrollment("acme", "prod");
        _code = CodeOf(_created);
        _event = _registry.Appended.Select(_ => _.Event).OfType<FederationEnrollmentCreated>().Single();
        _fingerprint = FederationCertificateAuthority.Fingerprint(_keys.Authority);
    }

    [Fact] void should_carry_the_public_endpoint_in_the_code() => _code.Endpoint.ShouldEqual("https://federation.example.com:35100/");
    [Fact] void should_carry_the_fingerprint_of_the_authority_root_in_the_code() => _code.AuthorityFingerprint.ShouldEqual(_fingerprint);
    [Fact] void should_carry_the_enrollment_id_in_the_code() => _code.EnrollmentId.ShouldEqual(_created.Id);
    [Fact] void should_expire_after_the_enrollment_lifetime() => _created.ExpiresAt.ShouldEqual(_time.GetUtcNow().AddDays(7));
    [Fact] void should_store_only_a_hash_of_the_secret() => _event.TokenHash.ShouldEqual(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(_code.Secret))));
    [Fact] void should_not_record_the_secret_in_any_event() => JsonSerializer.Serialize(_registry.Appended.Select(_ => _.Event).ToArray()).ShouldNotContain(_code.Secret);
    [Fact] void should_not_record_the_code_in_any_event() => JsonSerializer.Serialize(_registry.Appended.Select(_ => _.Event).ToArray()).ShouldNotContain(_created.Code);
    [Fact] void should_record_that_the_authority_was_created() => _registry.Appended.Select(_ => _.Event).OfType<FederationAuthorityCreated>().Single().Fingerprint.ShouldEqual(_fingerprint);
    [Fact] void should_not_reveal_the_code_when_printed() => _created.ToString().ShouldNotContain(_created.Code);
    [Fact] void should_not_reveal_the_secret_when_the_code_is_printed() => _code.ToString().ShouldNotContain(_code.Secret);
}
