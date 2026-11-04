// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter.given;

public class a_query_filter : Security.given.a_system_store_caller
{
    protected SystemStoreQueryFilter _filter;
    protected QueryResult? _result;

    protected QueryContext ContextFor(string name, params (string Key, object Value)[] arguments)
    {
        var queryArguments = new QueryArguments();
        foreach (var (key, value) in arguments) queryArguments[key] = value;
        return new QueryContext(name, CorrelationId.New(), Paging.NotPaged, Sorting.None, queryArguments, ServiceProvider: CreateServices());
    }

    void Establish() => _filter = new SystemStoreQueryFilter();
}
