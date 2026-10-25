// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_verifying_a_fresh_database(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    MongoDBIndexReport _report;
    string[] _databases;

    async Task Because()
    {
        _commands.Clear();
        _report = await _provisioner.Verify([Store], [Namespace]);
        using var cursor = await _client.ListDatabaseNamesAsync();
        _databases = (await cursor.ToListAsync()).Where(name => name.StartsWith(_prefix, StringComparison.Ordinal)).ToArray();
    }

    [Fact] void should_report_missing_indexes() => _report.IsCurrent.ShouldBeFalse();
    [Fact] void should_include_global_encryption() => _report.Scopes.Any(scope => scope.Scope == "global-encryption" && scope.Missing.Contains("encryption-keys/id_identifier_id_revision")).ShouldBeTrue();
    [Fact] void should_include_jobs() => _report.Scopes.Any(scope => scope.Scope == $"jobs:{Store}/{Namespace}" && scope.Missing.Count == 6).ShouldBeTrue();
    [Fact] void should_not_issue_ddl() => DdlCommands.ShouldEqual(0);
    [Fact] void should_not_insert_registration_rows() => _commands.Any(command => IsAnyOf(command, "insert", "update", "delete")).ShouldBeFalse();
    [Fact] void should_not_materialize_any_database() => _databases.Length.ShouldEqual(0);
}
