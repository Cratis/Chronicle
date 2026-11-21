// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public static class transaction_conflicts
{
    public static IEnumerable<Exception> All()
    {
        Exception[] native = [new SqliteException("busy", 5), new SqliteException("locked", 6),
            new PostgresException("serialization", "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure),
            new PostgresException("deadlock", "ERROR", "ERROR", PostgresErrorCodes.DeadlockDetected), SqlServer(1205), SqlServer(1222)];
        return native.SelectMany(error => new[] { error, new DbUpdateException("transaction conflict", error) });
    }

    static SqlException SqlServer(int number)
    {
        // SqlClient exposes no public constructor for a provider error. Keep the test-only construction here.
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        var constructor = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).MinBy(ctor => ctor.GetParameters().Length)!;
        var arguments = constructor.GetParameters().Select((parameter, index) => Argument(parameter.ParameterType, index == 0 ? number : 0)).ToArray();
        var error = constructor.Invoke(arguments);
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(errors, [error]);
        var create = typeof(SqlException).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(method => method.Name == "CreateException" && method.GetParameters().Length == 2);
        return (SqlException)create.Invoke(null, [errors, "16.0"])!;
    }

    static object? Argument(Type type, int number)
    {
        if (type == typeof(string)) return "transaction conflict";
        if (type == typeof(int)) return number;
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
