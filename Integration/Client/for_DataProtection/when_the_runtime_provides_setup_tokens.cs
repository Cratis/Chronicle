// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Integration.for_DataProtection;

[Collection(ChronicleCollection.Name)]
public class when_the_runtime_provides_setup_tokens(ChronicleFixture fixture) : Specification(fixture)
{
    bool _companionValid;
    bool _companionPersisted;
    bool _companionGrainBacked;
    bool _remoteInvalidRejected;
    bool _remoteMissingRejected;

    async Task Because()
    {
        var tokens = Services.GetRequiredService<IInitialAdminSetupTokens>();
        var user = new UserId(Guid.NewGuid());
        _companionValid = tokens.IsValid(user, tokens.Issue(user));
        _companionPersisted = (await Services.GetRequiredService<IGrainFactory>()
            .GetGrain<IDataProtectionKeys>("chronicle-data-protection").GetAllKeys()).Any();
        _companionGrainBacked = Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository is GrainBasedXmlRepository;
        if (ChronicleFixture.Options.Mode != ChronicleRuntimeMode.OutOfProcess) return;

        var users = ((Contracts.IChronicleServicesAccessor)EventStore.Connection).Services.Users;
        var status = await users.GetStatus();
        status.IsSuccess.ShouldBeTrue();

        // Authentication is disabled in this fixture, so administrator bootstrap is intentionally absent.
        var administrator = UserId.InitialAdministrator.Value;
        var invalid = await users.SetInitialAdminPassword(new Contracts.Security.SetInitialAdminPasswordRequest
        {
            UserId = administrator,
            Password = "new-password",
            ConfirmedPassword = "new-password",
            SetupToken = "invalid"
        });
        _remoteInvalidRejected = !invalid.IsSuccess && invalid.ExceptionMessages.Contains(new InvalidInitialAdminSetupToken().Message);
        var missing = await users.SetInitialAdminPassword(new Contracts.Security.SetInitialAdminPasswordRequest
        {
            UserId = administrator,
            Password = "new-password",
            ConfirmedPassword = "new-password",
            SetupToken = string.Empty
        });
        _remoteMissingRejected = !missing.IsSuccess && missing.ValidationResults.Any();
    }

    [Fact] void should_validate_tokens_on_the_in_process_host() => _companionValid.ShouldBeTrue();
    [Fact] void should_route_the_in_process_host_to_its_own_persisted_keys() => _companionPersisted.ShouldBeTrue();
    [Fact] void should_use_the_grain_repository_on_the_in_process_host() => _companionGrainBacked.ShouldBeTrue();

    [Fact]
    void should_reject_invalid_setup_requests_through_the_out_of_process_command_pipeline()
    {
        Assert.SkipWhen(ChronicleFixture.Options.Mode != ChronicleRuntimeMode.OutOfProcess, "Requires the out-of-process CI lane.");
        _remoteInvalidRejected.ShouldBeTrue();
    }

    [Fact]
    void should_reject_missing_tokens_through_the_out_of_process_command_pipeline()
    {
        Assert.SkipWhen(ChronicleFixture.Options.Mode != ChronicleRuntimeMode.OutOfProcess, "Requires the out-of-process CI lane.");
        _remoteMissingRejected.ShouldBeTrue();
    }
}
