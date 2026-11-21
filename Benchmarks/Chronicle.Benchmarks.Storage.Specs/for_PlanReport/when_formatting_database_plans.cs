// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_PlanReport;

public class when_formatting_database_plans : Specification
{
    string _report = default!;
    void Because() => _report = PlanReport.Format(
        "sqlite",
        1000,
        [
            ("cursor", "SELECT * FROM events", "SEARCH events USING COVERING INDEX ix_events (id>?)"),
            ("postgres", "SELECT * FROM events", "Index Only Scan using ix_tail on events\nBitmap Index Scan on ix_types\nSeq Scan on other_events"),
            ("scan", "SELECT * FROM other", "SCAN other"),
            ("primary-key", "SELECT * FROM events", "SEARCH e USING INTEGER PRIMARY KEY (rowid>?)")
        ]);
    [Fact] void should_report_primary_key_lookups() => _report.ShouldContain("Indexes used: INTEGER PRIMARY KEY");
    [Fact] void should_include_sql() => _report.ShouldContain("```sql\nSELECT * FROM events");
    [Fact] void should_extract_sqlite_indexes() => _report.ShouldContain("Indexes used: ix_events");
    [Fact] void should_extract_postgres_indexes() => _report.ShouldContain("Indexes used: ix_tail, ix_types, full scan: other_events");
    [Fact] void should_report_full_scans() => _report.ShouldContain("Indexes used: full scan: other");
}
