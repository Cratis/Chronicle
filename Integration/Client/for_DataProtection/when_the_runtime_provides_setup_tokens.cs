// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Validation;
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
    CommandResult _remoteInvalid;
    CommandResult _remoteMissing;

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

        // Bootstrap/reset state can differ; create a known remote user to satisfy user-existence validation.
        // A non-admin is sufficient: the handler rejects malformed tokens before checking administrator credentials.
        var remoteUser = Guid.NewGuid();
        var username = $"setup-token-{remoteUser:N}";
        var password = Guid.NewGuid().ToString("N");
        var added = await users.AddUser(new Contracts.Security.AddUserRequest
        {
            UserId = remoteUser,
            Username = username,
            Email = $"{username}@example.com",
            Password = password
        });
        Assert.True(added.IsSuccess, Describe(added));
        _remoteInvalid = await users.SetInitialAdminPassword(new Contracts.Security.SetInitialAdminPasswordRequest
        {
            UserId = remoteUser,
            Password = password,
            ConfirmedPassword = password,
            SetupToken = "invalid"
        });
        _remoteMissing = await users.SetInitialAdminPassword(new Contracts.Security.SetInitialAdminPasswordRequest
        {
            UserId = remoteUser,
            Password = password,
            ConfirmedPassword = password,
            SetupToken = string.Empty
        });
    }

    [Fact] void should_validate_tokens_on_the_in_process_host() => _companionValid.ShouldBeTrue();
    [Fact] void should_route_the_in_process_host_to_its_own_persisted_keys() => _companionPersisted.ShouldBeTrue();
    [Fact] void should_use_the_grain_repository_on_the_in_process_host() => _companionGrainBacked.ShouldBeTrue();

    [Fact]
    void should_reject_invalid_setup_requests_through_the_out_of_process_command_pipeline()
    {
        Assert.SkipWhen(ChronicleFixture.Options.Mode != ChronicleRuntimeMode.OutOfProcess, "Requires the out-of-process CI lane.");
        var diagnostic = Describe(_remoteInvalid);
        Assert.False(_remoteInvalid.IsSuccess, diagnostic);
        Assert.True(_remoteInvalid.IsAuthorized, diagnostic);
        Assert.True(string.IsNullOrEmpty(_remoteInvalid.AuthorizationFailureReason), diagnostic);
        Assert.True(_remoteInvalid.IsValid, diagnostic);
        Assert.True(_remoteInvalid.HasExceptions, diagnostic);
        Assert.True(_remoteInvalid.ExceptionMessages.Contains(new InvalidInitialAdminSetupToken().Message), diagnostic);
    }

    [Fact]
    void should_reject_missing_tokens_through_the_out_of_process_command_pipeline()
    {
        Assert.SkipWhen(ChronicleFixture.Options.Mode != ChronicleRuntimeMode.OutOfProcess, "Requires the out-of-process CI lane.");
        var diagnostic = Describe(_remoteMissing);
        Assert.False(_remoteMissing.IsSuccess, diagnostic);
        Assert.True(_remoteMissing.IsAuthorized, diagnostic);
        Assert.True(string.IsNullOrEmpty(_remoteMissing.AuthorizationFailureReason), diagnostic);
        Assert.False(_remoteMissing.IsValid, diagnostic);
        Assert.False(_remoteMissing.HasExceptions, diagnostic);
        Assert.True(_remoteMissing.ValidationResults.Count == 1, diagnostic);
        Assert.True(_remoteMissing.ValidationResults.All(result =>
            result.Severity == ValidationResultSeverity.Error &&
            result.Members.SequenceEqual(["setupToken"]) &&
            result.Message == "Setup token is required."),
            diagnostic);
    }

    static string Describe(CommandResult result) =>
        $"CorrelationId={result.CorrelationId}; IsSuccess={result.IsSuccess}; IsAuthorized={result.IsAuthorized}; AuthorizationFailureReason={result.AuthorizationFailureReason}; " +
        $"IsValid={result.IsValid}; ValidationResults=[{string.Join("; ", result.ValidationResults.Select(validation => $"{validation.Severity}: {string.Join(',', validation.Members)}: {validation.Message}"))}]; " +
        $"HasExceptions={result.HasExceptions}; ExceptionMessages=[{string.Join("; ", result.ExceptionMessages)}]; ExceptionStackTrace={result.ExceptionStackTrace}";
}
