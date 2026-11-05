// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Narrows queries so identifiers are compared exactly: letter case, character width, accents and trailing spaces all count.
/// </summary>
/// <remarks>
/// On SQL Server the plain equality comes first so the existing index is used for the seek. Two residual checks follow
/// on the rows the seek found: a binary collation comparison and a comparison of the length in bytes. No index, column
/// or additional query is needed. Other providers compare exactly with plain equality already.
/// </remarks>
static class ExactIdentifiers
{
    /// <summary>
    /// The binary collation used for the exact comparison.
    /// </summary>
    internal const string BinaryCollation = "Latin1_General_100_BIN2";
    const int BytesPerCharacter = 2;
    static readonly System.Reflection.MethodInfo _byteLengthOfString = typeof(ExactIdentifiers).GetMethod(nameof(ByteLength), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, [typeof(string)])!;

    /// <summary>
    /// Narrow a query to the events of an event source.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<EventEntry> WhereEventSource(this IQueryable<EventEntry> query, DbContext context, EventSourceId eventSourceId)
    {
        var narrowed = query.Where(e => e.EventSourceId == eventSourceId);
        if (!context.Database.IsSqlServer())
        {
            return narrowed;
        }

        var bytes = eventSourceId.Value.Length * BytesPerCharacter;
        return narrowed.Where(e => EF.Functions.Collate(e.EventSourceId, BinaryCollation) == eventSourceId && ByteLength(e.EventSourceId) == bytes);
    }

    /// <summary>
    /// Narrow a query to the events of an event source type.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<EventEntry> WhereSourceType(this IQueryable<EventEntry> query, DbContext context, EventSourceType eventSourceType)
    {
        var narrowed = query.Where(e => e.EventSourceType == eventSourceType);
        if (!context.Database.IsSqlServer())
        {
            return narrowed;
        }

        var bytes = eventSourceType.Value.Length * BytesPerCharacter;
        return narrowed.Where(e => EF.Functions.Collate(e.EventSourceType, BinaryCollation) == eventSourceType && ByteLength(e.EventSourceType) == bytes);
    }

    /// <summary>
    /// Narrow a query to the events of an event stream type.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<EventEntry> WhereStreamType(this IQueryable<EventEntry> query, DbContext context, EventStreamType eventStreamType)
    {
        var narrowed = query.Where(e => e.EventStreamType == eventStreamType);
        if (!context.Database.IsSqlServer())
        {
            return narrowed;
        }

        var bytes = eventStreamType.Value.Length * BytesPerCharacter;
        return narrowed.Where(e => EF.Functions.Collate(e.EventStreamType, BinaryCollation) == eventStreamType && ByteLength(e.EventStreamType) == bytes);
    }

    /// <summary>
    /// Narrow a query to the events of an event stream.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<EventEntry> WhereStreamId(this IQueryable<EventEntry> query, DbContext context, EventStreamId eventStreamId)
    {
        var narrowed = query.Where(e => e.EventStreamId == eventStreamId);
        if (!context.Database.IsSqlServer())
        {
            return narrowed;
        }

        var bytes = eventStreamId.Value.Length * BytesPerCharacter;
        return narrowed.Where(e => EF.Functions.Collate(e.EventStreamId, BinaryCollation) == eventStreamId && ByteLength(e.EventStreamId) == bytes);
    }

    /// <summary>
    /// Narrow a query to the in-flight events of a partition.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="partition">The partition to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<InFlightEventEntry> WherePartition(this IQueryable<InFlightEventEntry> query, DbContext context, string partition)
    {
        var narrowed = query.Where(e => e.Partition == partition);
        if (!context.Database.IsSqlServer())
        {
            return narrowed;
        }

        var bytes = partition.Length * BytesPerCharacter;
        return narrowed.Where(e => EF.Functions.Collate(e.Partition, BinaryCollation) == partition && ByteLength(e.Partition) == bytes);
    }

    /// <summary>
    /// Narrow a query to the claims of exactly one spelling of an owner.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="owner">The owner to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<UniqueConstraintValueEntry> WhereOwner(this IQueryable<UniqueConstraintValueEntry> query, DbContext context, string owner)
    {
        var narrowed = query.Where(e => e.EventSourceId == owner);
        if (!context.Database.IsSqlServer())
        {
            return narrowed;
        }

        var bytes = owner.Length * BytesPerCharacter;
        return narrowed.Where(e => EF.Functions.Collate(e.EventSourceId, BinaryCollation) == owner && ByteLength(e.EventSourceId) == bytes);
    }

    /// <summary>
    /// Narrow a query to the claims of exactly one spelling of an owner.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="owner">The owner to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<UniqueConstraintIndexEntry> WhereOwner(this IQueryable<UniqueConstraintIndexEntry> query, DbContext context, string owner)
    {
        var narrowed = query.Where(e => e.EventSourceId == owner);
        if (!context.Database.IsSqlServer())
        {
            return narrowed;
        }

        var bytes = owner.Length * BytesPerCharacter;
        return narrowed.Where(e => EF.Functions.Collate(e.EventSourceId, BinaryCollation) == owner && ByteLength(e.EventSourceId) == bytes);
    }

    /// <summary>
    /// Narrow a query to the claims of exactly one spelling of a subject.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="subject">The subject to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<UniqueConstraintValueEntry> WhereSubject(this IQueryable<UniqueConstraintValueEntry> query, DbContext context, string subject)
    {
        var narrowed = query.Where(e => e.Subject == subject);
        if (!context.Database.IsSqlServer())
        {
            return narrowed;
        }

        var bytes = subject.Length * BytesPerCharacter;
        return narrowed.Where(e => EF.Functions.Collate(e.Subject!, BinaryCollation) == subject && ByteLength(e.Subject!) == bytes);
    }

    /// <summary>
    /// Narrow a query to the claims of exactly one spelling of a subject.
    /// </summary>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="subject">The subject to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<UniqueConstraintIndexEntry> WhereSubject(this IQueryable<UniqueConstraintIndexEntry> query, DbContext context, string subject)
    {
        var narrowed = query.Where(e => e.Subject == subject);
        if (!context.Database.IsSqlServer())
        {
            return narrowed;
        }

        var bytes = subject.Length * BytesPerCharacter;
        return narrowed.Where(e => EF.Functions.Collate(e.Subject!, BinaryCollation) == subject && ByteLength(e.Subject!) == bytes);
    }

    /// <summary>
    /// Narrow a query to the rows whose identifier column holds exactly one spelling of a value.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity queried.</typeparam>
    /// <param name="query">The query to narrow.</param>
    /// <param name="context">The context the query runs on.</param>
    /// <param name="column">Selects the identifier column. It must be a string column of Unicode type.</param>
    /// <param name="value">The value to match.</param>
    /// <returns>The narrowed query.</returns>
    internal static IQueryable<TEntity> WhereExact<TEntity>(this IQueryable<TEntity> query, DbContext context, Expression<Func<TEntity, string?>> column, string value)
    {
        var parameter = column.Parameters[0];
        var holder = Expression.Constant(new ExactValue(value, value.Length * BytesPerCharacter));
        var stored = column.Body;
        var plain = Expression.Equal(stored, Expression.Property(holder, nameof(ExactValue.Value)));
        if (!context.Database.IsSqlServer())
        {
            return query.Where(Expression.Lambda<Func<TEntity, bool>>(plain, parameter));
        }

        var collate = Expression.Call(
            typeof(RelationalDbFunctionsExtensions),
            nameof(RelationalDbFunctionsExtensions.Collate),
            [typeof(string)],
            Expression.Constant(EF.Functions),
            stored,
            Expression.Constant(BinaryCollation));
        var binary = Expression.Equal(collate, Expression.Property(holder, nameof(ExactValue.Value)));
        var length = Expression.Equal(
            Expression.Call(_byteLengthOfString, stored),
            Expression.Property(holder, nameof(ExactValue.Bytes)));
        var exact = Expression.AndAlso(plain, Expression.AndAlso(binary, length));
        return query.Where(Expression.Lambda<Func<TEntity, bool>>(exact, parameter));
    }

    /// <summary>
    /// Server side length in bytes of a stored value. Only valid inside a query on SQL Server.
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <returns>The length in bytes.</returns>
    /// <exception cref="NotSupportedException">Always thrown when called outside a query.</exception>
    internal static int ByteLength(EventSourceId value) => throw new NotSupportedException();

    /// <inheritdoc cref="ByteLength(EventSourceId)"/>
    /// <param name="value">The stored value.</param>
    internal static int ByteLength(EventSourceType value) => throw new NotSupportedException();

    /// <inheritdoc cref="ByteLength(EventSourceId)"/>
    /// <param name="value">The stored value.</param>
    internal static int ByteLength(EventStreamType value) => throw new NotSupportedException();

    /// <inheritdoc cref="ByteLength(EventSourceId)"/>
    /// <param name="value">The stored value.</param>
    internal static int ByteLength(EventStreamId value) => throw new NotSupportedException();

    /// <inheritdoc cref="ByteLength(EventSourceId)"/>
    /// <param name="value">The stored value.</param>
    internal static int ByteLength(string value) => throw new NotSupportedException();

    /// <summary>
    /// Registers the server side byte length function for every overload used by the exact predicates.
    /// </summary>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to register on.</param>
    internal static void RegisterByteLength(ModelBuilder modelBuilder)
    {
        foreach (var type in new[] { typeof(EventSourceId), typeof(EventSourceType), typeof(EventStreamType), typeof(EventStreamId), typeof(string) })
        {
            modelBuilder
                .HasDbFunction(typeof(ExactIdentifiers).GetMethod(nameof(ByteLength), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, [type])!)
                .HasParameter("value", parameter => parameter.HasStoreType("nvarchar(450)"))
                .HasTranslation(args => new SqlFunctionExpression(
                    "DATALENGTH",
                    args,
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    typeof(int),
                    null));
        }
    }
}
