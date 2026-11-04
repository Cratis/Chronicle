// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

public class and_the_namespace_database_file_is_absent : given.a_database_without_read_model_storage
{
    DbContextScope<NamespaceDbContext>? _scope;

    async Task Because() => _scope = await _database.ExistingNamespace("store", "fresh-namespace");

    [Fact] void should_report_the_namespace_as_absent() => _scope.ShouldBeNull();
    [Fact] void should_not_create_a_database_file() => string.Join(',', FilesOnDisk).ShouldEqual("cluster.db");
}
