// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using KernelSecurity = KernelCore::Cratis.Chronicle.Security;
using UserId = KernelConcepts::Cratis.Chronicle.Concepts.Security.UserId;

namespace Cratis.Chronicle.Integration.Clustering.for_DataProtection;

[Collection(ChronicleCollection.Name)]
public class when_restarting_with_persisted_keys(ClusteringFixture fixture) : Specification
{
    bool _validOnFirst;
    bool _validOnSecond;

    async Task Because()
    {
        var user = new UserId(Guid.NewGuid());
        var token = fixture.SiloServices.GetRequiredService<KernelSecurity.IInitialAdminSetupTokens>().Issue(user);
        await fixture.CrashAndRestart();
        _validOnFirst = fixture.SiloServices.GetRequiredService<KernelSecurity.IInitialAdminSetupTokens>().IsValid(user, token);
        _validOnSecond = fixture.ObserversSiloServices.GetRequiredService<KernelSecurity.IInitialAdminSetupTokens>().IsValid(user, token);
    }

    [Fact] void should_read_the_pre_restart_token_on_the_first_silo() => _validOnFirst.ShouldBeTrue();
    [Fact] void should_read_the_pre_restart_token_on_the_second_silo() => _validOnSecond.ShouldBeTrue();
}
