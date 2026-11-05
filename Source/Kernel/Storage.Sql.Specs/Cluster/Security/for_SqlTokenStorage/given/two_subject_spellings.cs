// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA2100 // Fixture-owned values only.

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_SqlTokenStorage.given;

/// <summary>
/// Two spellings of a token subject that the default collation of SQL Server treats as one.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
/// <param name="first">The first spelling.</param>
/// <param name="second">The second spelling.</param>
public abstract class two_subject_spellings(SqlServerFixture fixture, string first, string second) : Specification
{
    protected string _first = first;
    protected string _second = second;
    protected long _collapsedByCollation;
    protected IEnumerable<Token> _foundForFirst = [];
    protected IEnumerable<Token> _foundForFirstInApplication = [];
    protected IEnumerable<Token> _foundForFirstWithStatus = [];
    protected Token? _byFirstReference;
    protected Token? _bySecondReference;
    string _connectionString = string.Empty;

    protected async Task SaveBoth()
    {
        _connectionString = await fixture.CreateDatabase();
        var options = new DbContextOptionsBuilder<ClusterDbContext>().UseSqlServer(_connectionString).AddConceptAsSupport().WithoutServiceProviderCaching().Options;
        await using (var context = new ClusterDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            _collapsedByCollation = await Collapsed(context);
        }

        var database = Substitute.For<IDatabase>();
        database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(new(options), () => { })));
        var storage = new SqlTokenStorage(database, new JsonSerializerOptions());
        await storage.Create(new Token { Id = "token-one", ApplicationId = "app", Subject = _first, Status = "valid", ReferenceId = _first });
        await storage.Create(new Token { Id = "token-two", ApplicationId = "app", Subject = _second, Status = "valid", ReferenceId = _second });

        _foundForFirst = await storage.FindBySubject(_first);
        _foundForFirstInApplication = await storage.FindByApplicationIdAndSubject("app", _first);
        _foundForFirstWithStatus = await storage.FindByApplicationIdSubjectAndStatus("app", _first, "valid");
        _byFirstReference = await storage.GetByReferenceId(_first);
        _bySecondReference = await storage.GetByReferenceId(_second);
    }

    protected void Destroy() => fixture.DropDatabase(_connectionString).GetAwaiter().GetResult();

    protected bool OnlyTheFirst(IEnumerable<Token> tokens) => tokens.Select(_ => _.Id).SequenceEqual(["token-one"]);

    async Task<long> Collapsed(ClusterDbContext context)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT CASE WHEN N'{_first}' = N'{_second}' THEN 1 ELSE 0 END";
        await context.Database.OpenConnectionAsync();
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
