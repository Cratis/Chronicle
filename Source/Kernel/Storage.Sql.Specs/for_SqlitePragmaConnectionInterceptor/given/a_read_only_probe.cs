// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.for_SqlitePragmaConnectionInterceptor.given;

public class a_read_only_probe : Specification
{
    protected DbContext _context;
    protected Exception _error;
    string _path;

    void Establish()
    {
        _path = Path.GetFullPath(Path.Combine(".ai-work", "4911", $"sqlite-probe-{Guid.NewGuid():N}.db"));
        using (var connection = new SqliteConnection($"Data Source={_path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Probe (Id INTEGER PRIMARY KEY);";
            command.ExecuteNonQuery();
        }

        _context = new DbContext(new DbContextOptionsBuilder()
            .UseSqlite($"Data Source={_path};Mode=ReadOnly;Pooling=False")
            .AddConceptAsSupport()
            .AddInterceptors(SqlitePragmaConnectionInterceptor.Instance)
            .Options);
    }

    protected bool SharesNamespaceProvider()
    {
        using var context = new NamespaceDbContext(new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddConceptAsSupport()
            .Options);
        return ReferenceEquals(_context.GetService<IModelSource>(), context.GetService<IModelSource>());
    }

    protected string JournalMode()
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        return (string)command.ExecuteScalar()!;
    }

    void Destroy()
    {
        _context.Dispose();
        File.Delete(_path);
    }
}
