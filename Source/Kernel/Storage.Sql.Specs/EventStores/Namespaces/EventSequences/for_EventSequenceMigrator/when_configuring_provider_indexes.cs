// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_configuring_provider_indexes
{
    [Theory]
    [InlineData("Data Source=:memory:", true, "INTEGER")]
    [InlineData("Host=localhost;Database=model", false, "bigint")]
    [InlineData("Server=localhost;Initial Catalog=model", true, "bigint")]
    void should_preserve_the_provider_specific_access_paths(string connectionString, bool hasSequenceIndex, string sequenceType)
    {
        var options = new DbContextOptionsBuilder<EventSequenceDbContext>(SpecDbContextOptions.ForConnection<EventSequenceDbContext>(connectionString))
            .WithoutServiceProviderCaching().Options;
        using var context = new EventSequenceDbContext(options, "events", Substitute.For<IEventSequenceMigrator>());
        var entity = context.Model.FindEntityType(typeof(EventEntry))!;
        var indexes = entity.GetIndexes().Select(index => string.Join(',', index.Properties.Select(property => property.Name))).ToArray();
        indexes.Contains("SequenceNumber").ShouldEqual(hasSequenceIndex);
        indexes.ShouldContain("EventSourceId");
        indexes.ShouldContain("Type");
        indexes.ShouldContain("EventSourceId,Type,SequenceNumber");
        indexes.ShouldContain("PublicationIdentityHash");
        entity.FindPrimaryKey()!.Properties.Select(property => property.Name).ShouldEqual(["SequenceNumber"]);
        entity.FindProperty(nameof(EventEntry.SequenceNumber))!.GetColumnType().ShouldEqual(sequenceType);
    }
}
