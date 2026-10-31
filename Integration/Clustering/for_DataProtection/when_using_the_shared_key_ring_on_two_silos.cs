// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using KernelSecurity = KernelCore::Cratis.Chronicle.Security;
using UserId = KernelConcepts::Cratis.Chronicle.Concepts.Security.UserId;

namespace Cratis.Chronicle.Integration.Clustering.for_DataProtection;

[Collection(ChronicleCollection.Name)]
public class when_using_the_shared_key_ring_on_two_silos(ClusteringFixture fixture) : Specification
{
    bool _forward;
    bool _reverse;
    bool _sameKeys;
    bool _distinctProviders;
    int _hostingSilos;

    async Task Because()
    {
        var first = fixture.SiloServices;
        var second = fixture.ObserversSiloServices;
        _distinctProviders = !ReferenceEquals(first, second);
        var user = new UserId(Guid.NewGuid());
        var issuer = first.GetRequiredService<KernelSecurity.IInitialAdminSetupTokens>();
        var validator = second.GetRequiredService<KernelSecurity.IInitialAdminSetupTokens>();
        _forward = validator.IsValid(user, issuer.Issue(user));
        _reverse = issuer.IsValid(user, validator.Issue(user));
        var firstGrains = first.GetRequiredService<IGrainFactory>();
        var secondGrains = second.GetRequiredService<IGrainFactory>();
        var firstKeys = (await firstGrains.GetGrain<KernelSecurity.IDataProtectionKeys>("chronicle-data-protection").GetAllKeys()).ToArray();
        var secondKeys = (await secondGrains.GetGrain<KernelSecurity.IDataProtectionKeys>("chronicle-data-protection").GetAllKeys()).ToArray();
        _sameKeys = firstKeys.Length > 0 && firstKeys.SequenceEqual(secondKeys);
        var statistics = await firstGrains.GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics();
        _hostingSilos = statistics.Where(_ => _.GrainType.Contains("dataprotectionkeys", StringComparison.OrdinalIgnoreCase))
            .Select(_ => _.SiloAddress).Distinct().Count();
    }

    [Fact] void should_use_two_distinct_silo_service_providers() => _distinctProviders.ShouldBeTrue();
    [Fact] void should_validate_a_token_issued_on_the_other_silo() => _forward.ShouldBeTrue();
    [Fact] void should_validate_in_the_reverse_direction() => _reverse.ShouldBeTrue();
    [Fact] void should_read_the_same_persisted_keys_from_both_silos() => _sameKeys.ShouldBeTrue();
    [Fact] void should_route_both_factories_to_one_hosted_key_grain() => _hostingSilos.ShouldEqual(1);
}
