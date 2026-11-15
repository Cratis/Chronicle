// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyFenceRollout;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : when_rolling_out(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase));
