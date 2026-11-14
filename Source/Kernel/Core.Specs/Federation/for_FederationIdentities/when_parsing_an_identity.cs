// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationIdentities;

public class when_parsing_an_identity : Specification
{
    string _trustDomain;
    string _subject;
    string _instance;
    bool _parsed;

    void Because() => _parsed = FederationIdentities.TryParse(FederationIdentities.Create("example", "acme", "prod-1"), out _trustDomain, out _subject, out _instance);

    [Fact] void should_parse_what_was_created() => _parsed.ShouldBeTrue();
    [Fact] void should_give_the_trust_domain() => _trustDomain.ShouldEqual("example");
    [Fact] void should_give_the_opaque_subject() => _subject.ShouldEqual("acme");
    [Fact] void should_give_the_instance() => _instance.ShouldEqual("prod-1");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("spiffe://example/customer/acme")]
    [InlineData("spiffe://example/customer/acme/kernel/prod/extra")]
    [InlineData("spiffe://example/tenant/acme/kernel/prod")]
    [InlineData("spiffe://example/customer/acme/node/prod")]
    [InlineData("spiffe://Example/customer/acme/kernel/prod")]
    [InlineData("spiffe://example/customer/../kernel/prod")]
    [InlineData("spiffe://example/customer/acme?x=1/kernel/prod")]
    [InlineData("http://example/customer/acme/kernel/prod")]
    public void should_refuse_a_form_that_is_not_the_identity_format(string? identity) =>
        FederationIdentities.TryParse(identity, out _, out _, out _).ShouldBeFalse();
}
