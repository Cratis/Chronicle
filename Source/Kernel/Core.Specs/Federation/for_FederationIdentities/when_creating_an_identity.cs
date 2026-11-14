// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationIdentities;

public class when_creating_an_identity : Specification
{
    string _identity;
    Exception _pathTraversal;
    Exception _slash;
    Exception _empty;
    Exception _newline;
    Exception _tooLong;
    Exception _uppercaseTrustDomain;

    void Because()
    {
        _identity = FederationIdentities.Create("example.org", "acme-1", "prod_2");
        _pathTraversal = Catch.Exception(() => FederationIdentities.Create("example", "..", "prod"));
        _slash = Catch.Exception(() => FederationIdentities.Create("example", "acme/other", "prod"));
        _empty = Catch.Exception(() => FederationIdentities.Create("example", "acme", string.Empty));
        _newline = Catch.Exception(() => FederationIdentities.Create("example", "acme\n", "prod"));
        _tooLong = Catch.Exception(() => FederationIdentities.Create("example", new string('a', 65), "prod"));
        _uppercaseTrustDomain = Catch.Exception(() => FederationIdentities.Create("Example", "acme", "prod"));
    }

    [Fact] void should_follow_the_identity_format() => _identity.ShouldEqual("spiffe://example.org/customer/acme-1/kernel/prod_2");
    [Fact] void should_refuse_a_path_traversal() => _pathTraversal.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_refuse_a_slash() => _slash.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_refuse_an_empty_segment() => _empty.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_refuse_a_trailing_newline() => _newline.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_refuse_a_segment_that_is_too_long() => _tooLong.ShouldBeOfExactType<InvalidFederationEnrollment>();
    [Fact] void should_refuse_an_uppercase_trust_domain() => _uppercaseTrustDomain.ShouldBeOfExactType<InvalidFederationEnrollment>();
}
