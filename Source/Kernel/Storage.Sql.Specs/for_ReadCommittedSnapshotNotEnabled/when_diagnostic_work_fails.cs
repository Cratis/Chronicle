// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_ReadCommittedSnapshotNotEnabled;

public class when_diagnostic_work_fails : Specification
{
    ReadCommittedSnapshotDiagnostics _diagnostics;

    async Task Because() => _diagnostics = await SqlServerReadCommittedSnapshot.SnapshotWithin(
        _ => throw new InvalidOperationException("Password=synthetic-secret\nprivate diagnostic"),
        TimeSpan.FromSeconds(5));

    [Fact] void should_return_an_unavailable_diagnostic() => _diagnostics.Unavailable.ShouldEqual("diagnostic failure");
    [Fact] void should_not_expose_the_failure_message() => _diagnostics.Describe().ShouldNotContain("synthetic-secret");
}
