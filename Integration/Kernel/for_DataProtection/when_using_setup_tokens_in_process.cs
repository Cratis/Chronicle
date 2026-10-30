// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Kernel.Integration.for_DataProtection;

[Collection(ChronicleCollection.Name)]
public class when_using_setup_tokens_in_process(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
{
    bool _valid;
    bool _persisted;
    bool _grainBacked;

    async Task Because()
    {
        var tokens = Services.GetRequiredService<IInitialAdminSetupTokens>();
        var user = new UserId(Guid.NewGuid());
        _valid = tokens.IsValid(user, tokens.Issue(user));
        var keys = await Services.GetRequiredService<IGrainFactory>().GetGrain<IDataProtectionKeys>("chronicle-data-protection").GetAllKeys();
        _persisted = keys.Any();
        _grainBacked = Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository is GrainBasedXmlRepository;
    }

    [Fact] void should_resolve_the_product_provider() => Services.GetRequiredService<IDataProtectionProvider>().ShouldNotBeNull();
    [Fact] void should_validate_a_setup_token() => _valid.ShouldBeTrue();
    [Fact] void should_route_to_the_persistent_key_grain() => _persisted.ShouldBeTrue();
    [Fact] void should_not_use_the_framework_filesystem_repository() => _grainBacked.ShouldBeTrue();
}
