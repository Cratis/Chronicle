// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Sequences.given;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Every command and query that takes an event store selector is listed in <see cref="given.store_scoped_surfaces"/>,
/// so per-application store entitlement (#5410) has a complete surface to enforce and a new surface cannot appear
/// without someone deciding how it is entitled.
/// </summary>
public class when_inventorying_store_scoped_surfaces : Specification
{
    static readonly Type[] _types = typeof(Administrator).Assembly.GetTypes();

    string[] _commands;
    string[] _queries;

    void Because()
    {
        _commands = public_store_write_commands.AllTypes
            .Where(type => StoreProperty(type) is not null)
            .Select(type => type.FullName!)
            .ToArray();
        _queries = _types
            .Where(type => type.IsDefined(typeof(ReadModelAttribute), false))
            .SelectMany(given.store_scoped_query_inventory.For)
            .Distinct()
            .ToArray();
    }

    [Fact] void should_inspect_a_nonempty_command_surface() => _commands.ShouldNotBeEmpty();
    [Fact] void should_inspect_a_nonempty_query_surface() => _queries.ShouldNotBeEmpty();

    [Fact] void should_list_every_store_scoped_command() =>
        string.Join(", ", given.store_scoped_grpc_inventory.Unlisted(_commands, given.store_scoped_surfaces.Commands).Order()).ShouldEqual(string.Empty);

    [Fact] void should_list_every_store_scoped_query() =>
        string.Join(", ", given.store_scoped_grpc_inventory.Unlisted(_queries, given.store_scoped_surfaces.Queries).Order()).ShouldEqual(string.Empty);

    [Fact] void should_not_list_a_command_that_is_no_longer_store_scoped() =>
        string.Join(", ", given.store_scoped_surfaces.Commands.Keys.Where(name => !_commands.Contains(name)).Order()).ShouldEqual(string.Empty);

    [Fact] void should_not_list_a_query_that_is_no_longer_store_scoped() =>
        string.Join(", ", given.store_scoped_surfaces.Queries.Keys.Where(name => !_queries.Contains(name)).Order()).ShouldEqual(string.Empty);

    static PropertyInfo? StoreProperty(Type type) =>
        type == typeof(EventStores.EnsureEventStore) ? type.GetProperty(nameof(EventStores.EnsureEventStore.Name)) : type.GetProperty("EventStore");
}
