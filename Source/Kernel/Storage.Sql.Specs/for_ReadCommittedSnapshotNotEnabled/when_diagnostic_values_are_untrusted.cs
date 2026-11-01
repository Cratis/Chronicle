// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_ReadCommittedSnapshotNotEnabled;

public class when_diagnostic_values_are_untrusted : Specification
{
    string _description;

    void Because() => _description = new ReadCommittedSnapshotDiagnostics(
        "ONLINE\nPassword=synthetic-secret",
        new string('A', 1000),
        false,
        "LCK_M_X\r\nforged",
        57,
        [new ReadCommittedSnapshotBlocker(57, "private status", "private program", "private host", "private mode", "private request", "private wait", 58)])
        .Describe();

    [Fact] void should_omit_sensitive_values() => _description.ShouldNotContain("synthetic-secret");
    [Fact] void should_omit_client_metadata() => _description.ShouldNotContain("private");
    [Fact] void should_not_accept_multiline_values() => _description.ShouldNotContain("\n");
    [Fact] void should_bound_state_values() => _description.ShouldNotContain(new string('A', 61));
    [Fact] void should_preserve_numeric_blocker_ids() => _description.ShouldContain("blocking session 58");
}
