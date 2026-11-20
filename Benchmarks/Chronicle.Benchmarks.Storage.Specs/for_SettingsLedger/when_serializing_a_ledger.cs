// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_SettingsLedger;

public class when_serializing_a_ledger : Specification
{
    string _json = string.Empty;

    void Because()
    {
        var ledger = new SettingsLedger(
            "postgres",
            "smoke",
            "postgres:16-alpine",
            "n/a",
            "16.4",
            "single",
            new Dictionary<string, string> { ["fsync"] = "on" },
            DurabilityClassifier.SyncOnCommit,
            "abc123",
            [10],
            [1, 8],
            256,
            [1000],
            100,
            1000,
            true,
            new("macOS", "Arm64", 12, 1024, null, null),
            new(0.5, 0.4, 0.3),
            null,
            8.4,
            DateTimeOffset.UnixEpoch);
        _json = ledger.ToJson();
    }

    [Fact] void should_carry_the_provider() => _json.ShouldContain("\"provider\": \"postgres\"");
    [Fact] void should_carry_the_durability_class() => _json.ShouldContain("\"durabilityClass\": \"sync-on-commit\"");
    [Fact] void should_carry_the_load() => _json.ShouldContain("\"oneMinute\": 0.5");
    [Fact] void should_carry_the_commit() => _json.ShouldContain("\"chronicleCommit\": \"abc123\"");
}
