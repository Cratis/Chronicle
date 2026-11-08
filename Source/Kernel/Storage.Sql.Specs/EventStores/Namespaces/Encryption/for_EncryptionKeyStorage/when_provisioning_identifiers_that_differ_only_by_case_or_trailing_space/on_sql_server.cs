// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_provisioning_identifiers_that_differ_only_by_case_or_trailing_space;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : conformance(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    [Fact] async Task should_use_an_exact_collation_for_keys() => (await _harness.CollationOf("EncryptionKeys")).ShouldEqual("Latin1_General_100_BIN2");
    [Fact] async Task should_use_an_exact_collation_for_erasures() => (await _harness.CollationOf("EncryptionKeyErasures")).ShouldEqual("Latin1_General_100_BIN2");
}
