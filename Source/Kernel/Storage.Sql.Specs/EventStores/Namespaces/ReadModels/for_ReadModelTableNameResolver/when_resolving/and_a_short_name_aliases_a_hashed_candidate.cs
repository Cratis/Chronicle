// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.when_resolving;

public class and_a_short_name_aliases_a_hashed_candidate : given.a_table_name_resolver
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_reject_the_second_owner_without_moving_the_first(bool shortFirst)
    {
        var longName = new string('x', 64);
        var longOwner = _owner with { ContainerName = longName, LogicalName = longName };
        var shortName = ReadModelTableNames.Candidate(_catalog.Provider, longName);
        var shortOwner = _owner with { ReadModelIdentifier = "short-model", ContainerName = shortName, LogicalName = shortName };
        var first = shortFirst ? shortOwner : longOwner;
        var second = shortFirst ? longOwner : shortOwner;
        var original = await CreateResolver().Resolve(first);
        var error = await Catch.Exception(() => CreateResolver().Resolve(second));

        error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
        _bindings.Rows.Count.ShouldEqual(1);
        _bindings.Rows.Single().Owner.ShouldEqual(first);
        _bindings.Rows.Single().PhysicalName.ShouldEqual(original.PhysicalName);
        _metadata.ShapeChecks.ShouldEqual(0);
        _catalog.Names.ShouldBeEmpty();
    }
}
