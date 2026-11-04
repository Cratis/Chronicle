// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Shares immutable discovery results between registries whose artifact, model and event type set are equal.
/// </summary>
/// <typeparam name="TValue">The immutable discovery result.</typeparam>
/// <remarks>
/// <para>
/// The cache is a trie of ephemerons. Every link is a <see cref="ConditionalWeakTable{TKey, TValue}"/> keyed on one of
/// the CLR types the result was discovered for - the artifact, the model and each event type - and the result hangs off
/// the last link, so no wrapper object ever serves as a key. A link keeps its subtree alive only while its key type is.
/// </para>
/// <para>
/// A result references the types it was discovered for, and a dependent handle keeps its value alive whenever its key
/// is alive, even after the table owning it became unreachable. A link keyed on a type that outlives an earlier link
/// would therefore pin the result and, through it, the earlier link's type. Links are ordered so that never happens:
/// non-collectible types first, then the collectible ones, which must all come from one assembly and so share one
/// lifetime. A set whose collectible types span several assemblies has no such order and is built without sharing.
/// </para>
/// <para>
/// Sets of non-collectible types are shared for the life of the process; a set touching a collectible assembly is shared
/// while that assembly is loaded and becomes collectable with it. Lookups happen only while a registry builds its
/// discovery results, never per event. A hit takes no lock; a miss builds under the leaf's lock so equal sets build
/// once, and a failed build stores nothing.
/// </para>
/// </remarks>
internal sealed class EventTypeSetCache<TValue>
    where TValue : class
{
    readonly Node _root = new();

    enum Role
    {
        Artifact = 0,
        Model = 1,
        Event = 2
    }

    /// <summary>
    /// Gets the result for an artifact, model and unordered event type set.
    /// </summary>
    /// <param name="types">The registered CLR types.</param>
    /// <param name="artifact">The artifact being discovered.</param>
    /// <param name="model">The artifact's model.</param>
    /// <param name="build">The discovery operation.</param>
    /// <returns>The shared result, or an unshared one when the types have no safe link order.</returns>
    internal TValue Get(IEnumerable<Type> types, Type artifact, Type model, Func<IEnumerable<Type>, TValue> build)
    {
        var links = Links(types, artifact, model);
        if (!ShareOneCollectibleLifetime(links))
        {
            return build(EventTypes(links));
        }

        Array.Sort(links);
        var node = _root;
        var previous = default(Link);
        foreach (var link in links)
        {
            if (link != previous)
            {
                node = node.Next(link);
                previous = link;
            }
        }

        return node.GetOrBuild(links, build);
    }

    static Type[] EventTypes(Link[] links) => [.. links.Where(link => link.Role == Role.Event).Select(link => link.Type).Distinct()];

    static Link[] Links(IEnumerable<Type> types, Type artifact, Type model)
    {
        var eventTypes = types.TryGetNonEnumeratedCount(out _) ? types : types.ToArray();
        var links = new Link[eventTypes.Count() + 2];
        links[0] = new(Role.Artifact, artifact);
        links[1] = new(Role.Model, model);
        var index = 2;
        foreach (var type in eventTypes)
        {
            links[index++] = new(Role.Event, type);
        }

        return links;
    }

    static bool ShareOneCollectibleLifetime(Link[] links)
    {
        Assembly? collectible = null;
        foreach (var link in links)
        {
            if (!link.Type.IsCollectible)
            {
                continue;
            }

            collectible ??= link.Type.Assembly;
            if (IsComposed(link.Type) || collectible != link.Type.Assembly)
            {
                return false;
            }
        }

        return true;
    }

    static bool IsComposed(Type type) => type.IsConstructedGenericType || type.HasElementType;

    /// <summary>
    /// One step of the path, ordered non-collectible first, then by role and identity. Being comparable itself lets a
    /// sort run without allocating a comparer. Ties between distinct types only cost a second build; they never share
    /// between sets.
    /// </summary>
    /// <param name="Role">What the type is to the discovery.</param>
    /// <param name="Type">The type.</param>
    readonly record struct Link(Role Role, Type Type) : IComparable<Link>
    {
        public static bool operator <(Link left, Link right) => left.CompareTo(right) < 0;
        public static bool operator >(Link left, Link right) => left.CompareTo(right) > 0;
        public static bool operator <=(Link left, Link right) => left.CompareTo(right) <= 0;
        public static bool operator >=(Link left, Link right) => left.CompareTo(right) >= 0;

        public int CompareTo(Link other)
        {
            var order = Type.IsCollectible.CompareTo(other.Type.IsCollectible);
            order = order != 0 ? order : Role.CompareTo(other.Role);
            order = order != 0 ? order : RuntimeHelpers.GetHashCode(Type).CompareTo(RuntimeHelpers.GetHashCode(other.Type));
            return order != 0 ? order : Type.MetadataToken.CompareTo(other.Type.MetadataToken);
        }
    }

    sealed class Node
    {
        readonly object _lock = new();
        readonly ConditionalWeakTable<Type, Node>?[] _children = new ConditionalWeakTable<Type, Node>?[3];
        TValue? _value;

        public Node Next(Link link) => Children(link.Role).GetValue(link.Type, static _ => new());

        public TValue GetOrBuild(Link[] links, Func<IEnumerable<Type>, TValue> build)
        {
            var value = Volatile.Read(ref _value);
            if (value is not null)
            {
                return value;
            }

            lock (_lock)
            {
                value = _value ?? build(EventTypes(links));
                Volatile.Write(ref _value, value);
                return value;
            }
        }

        ConditionalWeakTable<Type, Node> Children(Role role)
        {
            ref var slot = ref _children[(int)role];
            var children = Volatile.Read(ref slot);
            if (children is not null)
            {
                return children;
            }

            children = new();
            return Interlocked.CompareExchange(ref slot, children, null) ?? children;
        }
    }
}
