// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_StoreEntitlementsStartupNotice;

public class when_notifying_and_the_grants_cannot_be_read : Specification
{
    StoreEntitlementsStartupNotice _notice;
    RecordingLogger _logger;
    Exception _error;

    void Establish()
    {
        var entitlements = Substitute.For<IStoreEntitlements>();
        entitlements.GetSnapshot().Returns(Task.FromException<StoreEntitlementsState>(new TimeoutException("Response did not arrive on time")));
        var grains = Substitute.For<IGrainFactory>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(entitlements);
        _logger = new();
        _notice = new(Options.Create(new ChronicleOptions { Authentication = new() { Enabled = true, EnforceStoreEntitlements = true } }), grains, _logger);
    }

    async Task Because() => _error = await Catch.Exception(() => _notice.Notify(CancellationToken.None));

    [Fact] void should_not_fail_the_silo_start() => _error.ShouldBeNull();
    [Fact] void should_warn() => _logger.Levels.ShouldContainOnly([LogLevel.Warning]);
}
