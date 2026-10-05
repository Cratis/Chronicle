// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

[CollectionDefinition(Name)]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server sink replay promotion";
}
