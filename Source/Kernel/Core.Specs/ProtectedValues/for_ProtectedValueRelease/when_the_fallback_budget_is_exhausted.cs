// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_the_fallback_budget_is_exhausted : given.a_rotated_key
{
    void Establish()
    {
        _latest = new(_latest.Key, 20);
        for (uint revision = 4; revision < 20; revision++) _revisions[revision] = new EncryptionKeyAtRevision(_latest.Key, revision);
    }

    async Task Because() => _failure = await Catch.Exception(Release);

    [Fact] void should_stop_at_sixteen_older_revisions() => _storage.ReceivedWithAnyArgs(16).TryGetAtRevisionFor(default, default, default, default);
    [Fact] void should_not_probe_beyond_the_cap() => _storage.DidNotReceive().TryGetAtRevisionFor(Address.Item1, Address.Item2, Address.Item3, (EncryptionKeyRevision)3u);
    [Fact] void should_report_seventeen_attempts() => ((EncryptionKeyUnrecoverable)_failure).AttemptedRevisions.ShouldEqual(17);
}
