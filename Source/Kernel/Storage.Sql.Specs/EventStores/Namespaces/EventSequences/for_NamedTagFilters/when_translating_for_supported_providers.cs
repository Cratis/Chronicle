// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_NamedTagFilters;

public partial class when_translating_for_supported_providers : Specification
{
    string[] _queries;

    [GeneratedRegex(@"@(?<name>SequenceId|NameHash|Name|ValueHash|Value)(?:_[0-9]+)?\b", RegexOptions.None, 1000)]
    private static partial Regex ParameterPlaceholder { get; }

    void Because()
    {
        _queries =
        [
            Translate(options => options.UseSqlite("Data Source=:memory:")),
            Translate(options => options.UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")),
            Translate(options => options.UseSqlServer("Server=localhost;Database=test;User Id=test;Password=test;TrustServerCertificate=true"))
        ];
    }

    static string Translate(Func<DbContextOptionsBuilder<EventSequenceDbContext>, DbContextOptionsBuilder<EventSequenceDbContext>> configure)
    {
        var builder = configure(new DbContextOptionsBuilder<EventSequenceDbContext>());
        builder.AddConceptAsSupport();
        using var context = new EventSequenceDbContext(builder.Options, "events", Substitute.For<IEventSequenceMigrator>());
        return NamedTagFilters.Apply(context.Events, context.NamedTags, "events", [new(new TagName("account"), ["one", "two"])]).ToQueryString();
    }

    [Fact] void should_generate_correlated_exists_with_hash_and_binary_column_equalities()
    {
        foreach (var query in _queries)
        {
            query.ShouldContain("EXISTS");
            var quote = query.Contains("[Name]", StringComparison.Ordinal) ? ("[", "]") : ("\"", "\"");
            foreach (var column in new[] { "EventSequenceId", "SequenceNumber", "NameHash", "ValueHash", "Name", "Value" })
            {
                query.ShouldContain($"{quote.Item1}{column}{quote.Item2} = ");
            }
        }
    }

    [Fact] void should_parameterize_the_sequence_and_all_tag_components()
    {
        foreach (var query in _queries)
        {
            // ToQueryString includes parameter-value declarations; check only the SQL statement itself.
            var sql = query[query.IndexOf("SELECT ", StringComparison.Ordinal)..];
            foreach (var value in new[] { "account", "one", "two" })
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                foreach (var hex in new[] { Convert.ToHexString(bytes), Convert.ToHexString(SHA256.HashData(bytes)) })
                {
                    // All three providers' binary literals (X'..', '\x..'::bytea, 0x..) contain these bytes.
                    sql.Contains(hex, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
                }
            }

            var placeholders = ParameterPlaceholder.Matches(sql).Select(match => match.Groups["name"].Value).Distinct().ToArray();
            foreach (var component in new[] { "SequenceId", "NameHash", "Name", "ValueHash", "Value" })
            {
                placeholders.ShouldContain(component);
            }
        }
    }
}
