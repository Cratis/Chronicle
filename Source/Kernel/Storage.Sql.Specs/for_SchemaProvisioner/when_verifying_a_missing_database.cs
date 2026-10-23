// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_verifying_a_missing_database : given.a_schema_provisioner
{
    SchemaReport _report;

    void Establish() => File.Delete(Path.Combine(_directory, "cluster.db"));

    async Task Because() => _report = await _provisioner.Verify(Scope);

    [Fact] void should_report_the_missing_database() => _report.IsCurrent.ShouldBeFalse();
    [Fact] void should_not_create_a_database() => Directory.GetFiles(_directory).ShouldBeEmpty();
    [Fact] void should_name_the_missing_object() => _report.Scopes.Single().Pending.ShouldContain("database missing");
}
