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
            new("smoke", "abc123", DateTimeOffset.UnixEpoch),
            new("postgres", new("postgres:16-alpine", "n/a", "16.4"), "single", new(new Dictionary<string, string> { ["fsync"] = "on" }, DurabilityClassifier.SyncOnCommit)),
            new(new([10], [1, 8], [1000], [1000]), 256, new("max 100", 100, true)),
            new(new("macOS", "Arm64", 12, 1024, null, null), new(0.5, 0.4, 0.3), null, 8.4));
        _json = ledger.ToJson();
    }

    [Fact] void should_carry_the_provider() => _json.ShouldContain("\"provider\": \"postgres\"");
    [Fact] void should_carry_the_durability_class() => _json.ShouldContain("\"class\": \"sync-on-commit\"");
    [Fact] void should_carry_the_load() => _json.ShouldContain("\"oneMinute\": 0.5");
    [Fact] void should_carry_the_commit() => _json.ShouldContain("\"chronicleCommit\": \"abc123\"");
    [Fact] void should_carry_the_effective_pool() => _json.ShouldContain("\"connectionPool\": \"max 100\"");
}
