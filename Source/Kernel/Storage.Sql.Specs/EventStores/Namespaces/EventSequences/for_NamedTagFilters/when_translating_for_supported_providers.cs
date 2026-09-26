// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_NamedTagFilters;

public class when_translating_for_supported_providers : Specification
{
    string[] _queries;

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

    [Fact] void should_generate_correlated_exists_with_hash_and_binary_comparisons() => _queries.All(query =>
        query.Contains("EXISTS", StringComparison.OrdinalIgnoreCase) &&
        query.Contains("NameHash", StringComparison.Ordinal) &&
        query.Contains("ValueHash", StringComparison.Ordinal) &&
        query.Contains("Name", StringComparison.Ordinal) &&
        query.Contains("Value", StringComparison.Ordinal)).ShouldBeTrue();
}
