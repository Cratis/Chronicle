// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_StoreEntitlementsStartupNotice;

public class when_notifying : Specification
{
    [Theory]
    [InlineData(true, false, 0, LogLevel.Information)]
    [InlineData(true, false, 1, LogLevel.Information)]
    [InlineData(true, true, 0, LogLevel.Warning)]
    [InlineData(true, true, 1, LogLevel.None)]
    [InlineData(false, false, 0, LogLevel.None)]
    async Task should_emit_only_the_relevant_notice(bool enabled, bool enforce, int grants, LogLevel expected)
    {
        var entitlements = Substitute.For<IStoreEntitlementsCache>();
        entitlements.IsFresh.Returns(true);
        entitlements.Snapshot.Returns(new StoreEntitlementsSnapshot(new StoreEntitlementsState(1, Enumerable.Range(0, grants).Select(_ => new EventStoreAccessGrant(new("issuer", "subject"), null, null, StoreAccess.Read, EventStoreAccessGrantSource.Configuration)).ToArray())));
        var logger = new RecordingLogger();
        var notice = new StoreEntitlementsStartupNotice(Options.Create(new ChronicleOptions { Authentication = new() { Enabled = enabled, EnforceStoreEntitlements = enforce } }), entitlements, logger);
        await notice.Notify(CancellationToken.None);
        logger.Levels.ShouldEqual(expected == LogLevel.None ? [] : new[] { expected });
    }
}
