// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_TrustedProxyAuthenticationValidation;

public class when_validating : Specification
{
    static readonly string[] _proxies = ["10.0.0.5"];

    [Fact] void should_accept_a_trusted_proxy() => Validate(new() { KnownProxies = _proxies }).Succeeded.ShouldBeTrue();
    [Fact] void should_accept_a_trusted_sidecar() => Validate(new() { TrustLoopback = true }).Succeeded.ShouldBeTrue();
    [Fact] void should_refuse_trusting_no_proxy() => Validate(new()).Failed.ShouldBeTrue();
    [Fact] void should_reject_a_malformed_proxy_address() => Validate(new() { KnownProxies = ["proxy.local"] }).Failed.ShouldBeTrue();
    [Fact] void should_reject_a_network_without_a_prefix() => Validate(new() { KnownNetworks = ["10.0.0.0"] }).Failed.ShouldBeTrue();
    [Fact] void should_reject_the_framework_default_issuer() => Validate(new() { KnownProxies = _proxies, Issuer = System.Security.Claims.ClaimsIdentity.DefaultIssuer }).Failed.ShouldBeTrue();

    [Fact]
    void should_reject_the_issuer_of_the_external_authority() =>
        new TrustedProxyAuthenticationValidation().Validate(null, new ChronicleOptions
        {
            Authentication = new Authentication
            {
                Authority = "https://idp.example/",
                Http = new HttpAuthentication { Mode = HttpAuthenticationMode.TrustedProxy, TrustedProxy = new() { KnownProxies = _proxies, Issuer = "https://idp.example" } }
            }
        }).Failed.ShouldBeTrue();

    [Fact]
    void should_ignore_the_settings_when_the_mode_is_off() =>
        Validate(new() { Issuer = PrincipalKey.LocalIssuer }, HttpAuthenticationMode.Chronicle).Succeeded.ShouldBeTrue();

    [Fact]
    void should_ignore_the_settings_when_authentication_is_off() =>
        Validate(new() { Issuer = PrincipalKey.LocalIssuer }, enabled: false).Succeeded.ShouldBeTrue();

    [Fact] void should_reject_the_local_issuer() => Validate(new() { KnownProxies = _proxies, Issuer = PrincipalKey.LocalIssuer }).Failed.ShouldBeTrue();
    [Fact] void should_reject_a_missing_user_header() => Validate(new() { KnownProxies = _proxies, UserHeader = " " }).Failed.ShouldBeTrue();
    [Fact] void should_reject_a_malformed_header_name() => Validate(new() { KnownProxies = _proxies, GroupsHeader = "X-Groups:" }).Failed.ShouldBeTrue();
    [Fact] void should_reject_a_blank_administrator_group() => Validate(new() { KnownProxies = _proxies, AdministratorGroup = " admins" }).Failed.ShouldBeTrue();
    [Fact] void should_reject_a_shared_secret_without_a_header() => Validate(new() { KnownProxies = _proxies, SharedSecret = "secret", SharedSecretHeader = string.Empty }).Failed.ShouldBeTrue();

    [Theory]
    [InlineData("/oauth2/sign_out", true)]
    [InlineData("https://proxy.example/oauth2/sign_out", true)]
    [InlineData("//evil.example/sign_out", false)]
    [InlineData("/\\evil.example/sign_out", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("sign_out", false)]
    void should_only_accept_a_path_or_http_sign_out_url(string url, bool expected) =>
        Validate(new() { KnownProxies = _proxies, SignOutUrl = url }).Succeeded.ShouldEqual(expected);

    static Microsoft.Extensions.Options.ValidateOptionsResult Validate(TrustedProxyAuthentication proxy, HttpAuthenticationMode mode = HttpAuthenticationMode.TrustedProxy, bool enabled = true) =>
        new TrustedProxyAuthenticationValidation().Validate(null, new ChronicleOptions
        {
            Authentication = new Authentication { Enabled = enabled, Http = new HttpAuthentication { Mode = mode, TrustedProxy = proxy } }
        });
}
