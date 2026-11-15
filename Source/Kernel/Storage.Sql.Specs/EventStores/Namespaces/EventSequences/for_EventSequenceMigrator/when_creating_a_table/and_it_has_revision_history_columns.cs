// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.when_creating_a_table;

public class and_it_has_revision_history_columns : given.a_named_tag_migrator
{
    EventEntry _stored;
    IProperty _original;
    IProperty _revisions;

    async Task Because()
    {
        await using var context = CreateContext();
        await context.EnsureTableExists();
        context.Events.Add(new() { SequenceNumber = 1, OriginalContent = "ignored", Revisions = "ignored" });
        await context.SaveChangesAsync();
        _stored = await context.Events.AsNoTracking().SingleAsync();
        _original = context.Model.FindEntityType(typeof(EventEntry))!.FindProperty(nameof(EventEntry.OriginalContent))!;
        _revisions = context.Model.FindEntityType(typeof(EventEntry))!.FindProperty(nameof(EventEntry.Revisions))!;
    }

    [Fact] void should_create_nullable_original_content() => _stored.OriginalContent.ShouldBeNull();
    [Fact] void should_create_nullable_revisions() => _stored.Revisions.ShouldBeNull();
    [Fact] void should_ignore_original_content_on_insert() => _original.GetBeforeSaveBehavior().ShouldEqual(PropertySaveBehavior.Ignore);
    [Fact] void should_ignore_revisions_on_insert() => _revisions.GetBeforeSaveBehavior().ShouldEqual(PropertySaveBehavior.Ignore);
    [Fact] void should_allow_original_content_updates() => _original.GetAfterSaveBehavior().ShouldEqual(PropertySaveBehavior.Save);
    [Fact] void should_allow_revision_updates() => _revisions.GetAfterSaveBehavior().ShouldEqual(PropertySaveBehavior.Save);
}
