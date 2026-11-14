// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationEndpointPolicy;

public class when_resolving_an_endpoint : Specification
{
    FederationEndpointPolicy _production;
    FederationEndpointPolicy _development;
    FederationEndpointPolicy _allowedByConfiguration;
    NamedPolicy _restrictedToHosts;
    NamedPolicy _resolvingToMetadata;
    NamedPolicy _resolvingToAPublicAddress;

    IPAddress[]? _publicAddress;
    IPAddress[]? _privateAddress;
    IPAddress[]? _privateAddressAllowedByConfiguration;
    IPAddress[]? _privateAddressInTheList;
    NamedPolicy _privateAllowedHost;
    IPAddress[]? _plainHttp;
    IPAddress[]? _loopback;
    IPAddress[]? _loopbackV6;
    IPAddress[]? _linkLocalMetadata;
    IPAddress[]? _metadataMappedToV6;
    IPAddress[]? _metadataUniqueLocalV6;
    IPAddress[]? _unspecified;
    IPAddress[]? _withUserInfo;
    IPAddress[]? _nameResolvingToMetadata;
    IPAddress[]? _nameResolvingToAPublicAddress;
    IPAddress[]? _loopbackInDevelopment;
    IPAddress[]? _loopbackAllowedByConfiguration;
    IPAddress[]? _hostNotInTheList;
    IPAddress[]? _hostInTheList;

    static FederationEndpointPolicy Policy(bool development = false, bool allowRestricted = false)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(development ? Environments.Development : Environments.Production);
        return new(Options.Create(Configured(allowRestricted, [])), environment);
    }

    static ChronicleOptions Configured(bool allowRestricted, string[] allowedHosts) =>
        new() { Federation = new() { Enrollment = new() { AllowRestrictedAddresses = allowRestricted, AllowedHosts = allowedHosts } } };

    static NamedPolicy Named(string[] allowedHosts, params IPAddress[] resolvesTo) => Named(allowedHosts, false, resolvesTo);

    static NamedPolicy Named(string[] allowedHosts, bool allowRestricted, params IPAddress[] resolvesTo)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        return new(resolvesTo, Options.Create(Configured(allowRestricted, allowedHosts)), environment);
    }

    static Task<IPAddress[]?> Resolve(FederationEndpointPolicy policy, string endpoint) => policy.Resolve(new Uri(endpoint), CancellationToken.None);

    void Establish()
    {
        _production = Policy();
        _development = Policy(development: true);
        _allowedByConfiguration = Policy(allowRestricted: true);
        _restrictedToHosts = Named(["federation.example.com"], IPAddress.Parse("93.184.216.34"));
        _resolvingToMetadata = Named([], IPAddress.Parse("169.254.169.254"));
        _privateAllowedHost = Named(["peer.internal.example"], allowRestricted: true, IPAddress.Parse("10.1.2.3"));
        _resolvingToAPublicAddress = Named([], IPAddress.Parse("93.184.216.34"));
    }

    async Task Because()
    {
        _publicAddress = await Resolve(_production, "https://93.184.216.34:35100/");
        _privateAddress = await Resolve(_production, "https://10.1.2.3:35100/");
        _privateAddressAllowedByConfiguration = await Resolve(_allowedByConfiguration, "https://10.1.2.3:35100/");
        _privateAddressInTheList = await Resolve(_privateAllowedHost, "https://peer.internal.example/");
        _plainHttp = await Resolve(_production, "http://93.184.216.34:35100/");
        _loopback = await Resolve(_production, "https://127.0.0.1:35100/");
        _loopbackV6 = await Resolve(_production, "https://[::1]:35100/");
        _linkLocalMetadata = await Resolve(_production, "https://169.254.169.254/");
        _metadataMappedToV6 = await Resolve(_production, "https://[::ffff:169.254.169.254]/");
        _metadataUniqueLocalV6 = await Resolve(_production, "https://[fd00:ec2::254]/");
        _unspecified = await Resolve(_production, "https://0.0.0.0:35100/");
        _withUserInfo = await Resolve(_production, "https://user:pass@93.184.216.34:35100/");
        _nameResolvingToMetadata = await Resolve(_resolvingToMetadata, "https://federation.example.com/");
        _nameResolvingToAPublicAddress = await Resolve(_resolvingToAPublicAddress, "https://federation.example.com/");
        _loopbackInDevelopment = await Resolve(_development, "https://127.0.0.1:35100/");
        _loopbackAllowedByConfiguration = await Resolve(_allowedByConfiguration, "https://127.0.0.1:35100/");
        _hostNotInTheList = await Resolve(_restrictedToHosts, "https://other.example.com/");
        _hostInTheList = await Resolve(_restrictedToHosts, "https://federation.example.com/");
    }

    [Fact] void should_allow_a_public_https_address() => _publicAddress.ShouldContainOnly(IPAddress.Parse("93.184.216.34"));
    [Fact] void should_refuse_a_private_network_address_by_default() => _privateAddress.ShouldBeNull();
    [Fact] void should_allow_a_private_network_address_when_the_configuration_says_so() => _privateAddressAllowedByConfiguration.ShouldNotBeNull();
    [Fact] void should_allow_a_private_network_address_for_a_host_in_the_list_when_the_configuration_says_so() => _privateAddressInTheList.ShouldNotBeNull();
    [Fact] void should_refuse_plain_http() => _plainHttp.ShouldBeNull();
    [Fact] void should_refuse_loopback() => _loopback.ShouldBeNull();
    [Fact] void should_refuse_loopback_over_ipv6() => _loopbackV6.ShouldBeNull();
    [Fact] void should_refuse_the_link_local_metadata_address() => _linkLocalMetadata.ShouldBeNull();
    [Fact] void should_refuse_the_metadata_address_mapped_into_ipv6() => _metadataMappedToV6.ShouldBeNull();
    [Fact] void should_refuse_the_unique_local_ipv6_metadata_address() => _metadataUniqueLocalV6.ShouldBeNull();
    [Fact] void should_refuse_the_unspecified_address() => _unspecified.ShouldBeNull();
    [Fact] void should_refuse_an_endpoint_with_credentials() => _withUserInfo.ShouldBeNull();
    [Fact] void should_refuse_a_name_that_resolves_to_the_metadata_address() => _nameResolvingToMetadata.ShouldBeNull();
    [Fact] void should_hand_back_the_address_it_checked_for_a_name() => _nameResolvingToAPublicAddress.ShouldContainOnly(IPAddress.Parse("93.184.216.34"));
    [Fact] void should_allow_loopback_in_development() => _loopbackInDevelopment.ShouldNotBeNull();
    [Fact] void should_allow_loopback_when_the_configuration_says_so() => _loopbackAllowedByConfiguration.ShouldNotBeNull();
    [Fact] void should_refuse_a_host_that_is_not_in_the_allowed_list() => _hostNotInTheList.ShouldBeNull();
    [Fact] void should_allow_a_host_in_the_allowed_list() => _hostInTheList.ShouldNotBeNull();

    sealed class NamedPolicy(IPAddress[] resolvesTo, IOptions<ChronicleOptions> options, IHostEnvironment environment) : FederationEndpointPolicy(options, environment)
    {
        protected override Task<IPAddress[]> ResolveHost(string host, CancellationToken cancellationToken) => Task.FromResult(resolvesTo);
    }
}
