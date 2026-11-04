// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_ReadCommittedSnapshotNotEnabled;

public class when_describing_blocked_provisioning : Specification
{
    ReadCommittedSnapshotNotEnabled _exception;

    void Establish()
    {
        var diagnostics = new ReadCommittedSnapshotDiagnostics(
            "ONLINE",
            "MULTI_USER",
            false,
            "LCK_M_X",
            57,
            [new ReadCommittedSnapshotBlocker(57, "sleeping", "Password=synthetic-secret\r\nforged log", "User Id=synthetic-user\nhost", "S", "GRANT", null, null)]);
        _exception = new ReadCommittedSnapshotNotEnabled("chronicle+es+System", diagnostics, new InvalidOperationException("timeout"));
    }

    [Fact] void should_name_the_database() => _exception.Message.ShouldContain("chronicle+es+System");
    [Fact] void should_name_the_blocking_session() => _exception.Message.ShouldContain("session 57");
    [Fact] void should_name_the_wait_type() => _exception.Message.ShouldContain("LCK_M_X");
    [Fact] void should_name_the_database_state() => _exception.Message.ShouldContain("ONLINE");
    [Fact] void should_omit_the_client_program() => _exception.Message.ShouldNotContain("synthetic-secret");
    [Fact] void should_omit_the_client_host() => _exception.Message.ShouldNotContain("synthetic-user");
    [Fact] void should_not_allow_forged_log_lines() => _exception.Message.ShouldNotContain("forged log");
    [Fact] void should_remain_on_one_line() => _exception.Message.ShouldNotContain("\n");
    [Fact] void should_not_contain_a_password() => _exception.Message.ShouldNotContain("Password");
    [Fact] void should_not_contain_a_user_id() => _exception.Message.ShouldNotContain("User Id");
    [Fact] void should_keep_the_original_failure() => _exception.InnerException.ShouldBeOfExactType<InvalidOperationException>();
}
