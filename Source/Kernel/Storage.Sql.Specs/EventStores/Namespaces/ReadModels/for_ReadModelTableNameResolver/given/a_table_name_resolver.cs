// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameResolver.given;

public class a_table_name_resolver : Specification
{
    private protected ReadModelTableOwner _owner;
    private protected name_catalog _catalog;
    private protected name_bindings _bindings;
    private protected ownership_metadata _metadata;
    private protected ReadModelTableNameResolver _resolver;
    private protected ReadModelTableNameBinding _result;
    protected Exception _error;

    void Establish()
    {
        _owner = new("store", "default", "orders-model", "orders", ReadModelTableRole.Live, "orders");
        _catalog = new();
        _bindings = new(_catalog);
        _metadata = new();
        _resolver = CreateResolver();
    }

    private protected ReadModelTableNameResolver CreateResolver() => new(_bindings, _catalog, _metadata);

    private protected async Task Resolve(bool requireExisting = false)
    {
        _error = await Catch.Exception(async () => _result = await _resolver.Resolve(_owner, requireExisting));
    }
}
