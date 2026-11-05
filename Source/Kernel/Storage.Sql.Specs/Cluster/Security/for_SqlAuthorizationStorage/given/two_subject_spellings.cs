// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA2100 // Fixture-owned values only.

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_SqlAuthorizationStorage.given;

/// <summary>
/// Two spellings of an authorization subject that the default collation of SQL Server treats as one.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
/// <param name="first">The first spelling.</param>
/// <param name="second">The second spelling.</param>
public abstract class two_subject_spellings(SqlServerFixture fixture, string first, string second) : Specification
{
    static readonly Guid _application = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    protected string _first = first;
    protected string _second = second;
    protected long _collapsedByCollation;
    protected IEnumerable<Authorization> _foundForFirst = [];
    protected IEnumerable<Authorization> _foundForFirstInApplication = [];
    protected IEnumerable<Authorization> _foundForFirstWithStatus = [];
    string _connectionString = string.Empty;

    protected async Task SaveBoth()
    {
        _connectionString = await fixture.CreateDatabase();
        var options = new DbContextOptionsBuilder<ClusterDbContext>().UseSqlServer(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using (var context = new ClusterDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT CASE WHEN N'{_first}' = N'{_second}' THEN 1 ELSE 0 END";
            await context.Database.OpenConnectionAsync();
            _collapsedByCollation = Convert.ToInt64(await command.ExecuteScalarAsync());
        }

        var database = Substitute.For<IDatabase>();
        database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(new(options), () => { })));
        var storage = new SqlAuthorizationStorage(database, new JsonSerializerOptions());
        await storage.Create(Authorization("00000000-0000-0000-0000-000000000001", _first));
        await storage.Create(Authorization("00000000-0000-0000-0000-000000000002", _second));

        _foundForFirst = await storage.FindBySubject(_first);
        _foundForFirstInApplication = await storage.FindByApplicationIdAndSubject(_application, _first);
        _foundForFirstWithStatus = await storage.FindByApplicationIdSubjectAndStatus(_application, _first, "valid");
    }

    protected void Destroy() => fixture.DropDatabase(_connectionString).GetAwaiter().GetResult();

    protected bool OnlyTheFirst(IEnumerable<Authorization> authorizations) =>
        authorizations.Select(_ => _.Id.Value).SequenceEqual([Guid.Parse("00000000-0000-0000-0000-000000000001")]);

    static Authorization Authorization(string id, string subject) =>
        new(id, _application, subject, null, "valid", [], null, []);
}
