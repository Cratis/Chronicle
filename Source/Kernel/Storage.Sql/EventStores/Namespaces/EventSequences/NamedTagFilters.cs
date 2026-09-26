// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>Builds correlated EXISTS predicates for exact binary named-tag matches.</summary>
public static class NamedTagFilters
{
    /// <summary>Applies OR across named-tag criteria, combined with all other query dimensions.</summary>
    /// <param name="events">The events to narrow.</param>
    /// <param name="tags">The shared companion table.</param>
    /// <param name="sequenceId">The event sequence table name.</param>
    /// <param name="criteria">The named-tag criteria.</param>
    /// <returns>The narrowed query.</returns>
    public static IQueryable<EventEntry> Apply(IQueryable<EventEntry> events, DbSet<NamedTagEntry> tags, string sequenceId, IEnumerable<NamedTagCriterion>? criteria)
    {
        if (criteria is null)
        {
            return events;
        }

        var eventParameter = Expression.Parameter(typeof(EventEntry), "eventEntry");
        Expression? combined = null;
        foreach (var criterion in criteria)
        {
            var name = Encoding.UTF8.GetBytes(criterion.Name.Value);
            var nameHash = SHA256.HashData(name);
            var values = criterion.Values?.ToArray();
            if (values is null)
            {
                combined = Combine(combined, Match(tags, eventParameter, sequenceId, name, nameHash));
            }
            else
            {
                foreach (var value in values)
                {
                    var bytes = Encoding.UTF8.GetBytes(value);
                    combined = Combine(combined, Match(tags, eventParameter, sequenceId, name, nameHash, bytes, SHA256.HashData(bytes)));
                }
            }
        }

        return combined is null ? events : events.Where(Expression.Lambda<Func<EventEntry, bool>>(combined, eventParameter));
    }

    static Expression Combine(Expression? left, Expression right) => left is null ? right : Expression.OrElse(left, right);

    static MethodCallExpression Match(DbSet<NamedTagEntry> tags, ParameterExpression eventParameter, string sequenceId, byte[] name, byte[] nameHash, byte[]? value = null, byte[]? valueHash = null)
    {
        var tag = Expression.Parameter(typeof(NamedTagEntry), "tag");
        Expression match = Expression.AndAlso(
            Expression.Equal(Expression.Property(tag, nameof(NamedTagEntry.EventSequenceId)), Expression.Constant(sequenceId)),
            Expression.Equal(Expression.Property(tag, nameof(NamedTagEntry.SequenceNumber)), Expression.Property(eventParameter, nameof(EventEntry.SequenceNumber))));
        match = Expression.AndAlso(match, Expression.Equal(Expression.Property(tag, nameof(NamedTagEntry.NameHash)), Expression.Constant(nameHash)));
        match = Expression.AndAlso(match, Expression.Equal(Expression.Property(tag, nameof(NamedTagEntry.Name)), Expression.Constant(name)));
        if (value is not null)
        {
            match = Expression.AndAlso(match, Expression.Equal(Expression.Property(tag, nameof(NamedTagEntry.ValueHash)), Expression.Constant(valueHash)));
            match = Expression.AndAlso(match, Expression.Equal(Expression.Property(tag, nameof(NamedTagEntry.Value)), Expression.Constant(value)));
        }

        var predicate = Expression.Lambda<Func<NamedTagEntry, bool>>(match, tag);
        return Expression.Call(typeof(Queryable), nameof(Queryable.Any), [typeof(NamedTagEntry)], ((IQueryable<NamedTagEntry>)tags).Expression, Expression.Quote(predicate));
    }
}
