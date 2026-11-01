// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlDatabaseCatalog;

public class when_establishing_visibility : Specification
{
    const string SqlServer = SqlDatabaseCatalog.SqlServerVisibilitySql;

    [Fact] void should_require_the_sysadmin_role_on_sql_server() => SqlServer.ShouldContain("IS_SRVROLEMEMBER('sysadmin')");
    [Fact] void should_not_accept_view_any_database_on_sql_server() => SqlServer.ShouldNotContain("VIEW ANY DATABASE");
    [Fact] void should_not_accept_alter_any_database_on_sql_server() => SqlServer.ShouldNotContain("ALTER ANY DATABASE");
}
