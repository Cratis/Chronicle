// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;

public class a_command_result : Specification
{
    protected DbConnection _connection;
    protected DbCommand _command;

    void Establish()
    {
        _connection = Substitute.For<DbConnection>();
        _command = Substitute.For<DbCommand>();
        _connection.Database.Returns("database");
        _connection.CreateCommand().Returns(_command);
        _command.Parameters.Returns(Substitute.For<DbParameterCollection>());
        _command.CreateParameter().Returns(_ => new SqlParameter());
    }

    protected void Return(object result) => _command.ExecuteScalarAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<object?>(result));
}
