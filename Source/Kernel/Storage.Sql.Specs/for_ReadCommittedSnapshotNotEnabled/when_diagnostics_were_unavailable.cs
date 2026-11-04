// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_ReadCommittedSnapshotNotEnabled;

public class when_diagnostics_were_unavailable : Specification
{
    ReadCommittedSnapshotNotEnabled _exception;

    void Establish()
    {
        var diagnostics = new ReadCommittedSnapshotDiagnostics(null, null, null, null, null, [], "SQL error 297");
        _exception = new ReadCommittedSnapshotNotEnabled("chronicle+es+System", diagnostics, new InvalidOperationException("timeout"));
    }

    [Fact] void should_say_the_diagnostics_were_unavailable() => _exception.Message.ShouldContain("diagnostics unavailable");
    [Fact] void should_give_the_reason() => _exception.Message.ShouldContain("SQL error 297");
}
