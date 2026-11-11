// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_StoreEntitlementsStartupNotice;

/// <summary>
/// The startup task loads the grants into the entitlement cache. When that load did not succeed the cache is not fresh,
/// and its empty snapshot says nothing about the configured grants.
/// </summary>
public class when_notifying_and_the_grants_cannot_be_read : Specification
{
    StoreEntitlementsStartupNotice _notice;
    RecordingLogger _logger;
    Exception _error;

    void Establish()
    {
        var entitlements = Substitute.For<IStoreEntitlementsCache>();
        entitlements.IsFresh.Returns(false);
        entitlements.Snapshot.Returns(StoreEntitlementsSnapshot.Empty);
        _logger = new();
        _notice = new(Options.Create(new ChronicleOptions { Authentication = new() { Enabled = true, EnforceStoreEntitlements = true } }), entitlements, _logger);
    }

    async Task Because() => _error = await Catch.Exception(() => _notice.Notify(CancellationToken.None));

    [Fact] void should_not_fail_the_silo_start() => _error.ShouldBeNull();
    [Fact] void should_warn_that_the_grants_could_not_be_read() => _logger.Events.ShouldContainOnly([nameof(StoreEntitlementsStartupNoticeLogging.GrantsUnavailable)]);
    [Fact] void should_warn() => _logger.Levels.ShouldContainOnly([LogLevel.Warning]);
    [Fact] void should_not_claim_there_are_no_grants() => _logger.Events.ShouldNotContain(nameof(StoreEntitlementsStartupNoticeLogging.EmptyEnforcement));
}
