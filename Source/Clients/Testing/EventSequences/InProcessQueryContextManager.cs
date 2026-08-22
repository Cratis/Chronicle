// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an <see cref="IQueryContextManager"/> for an in-process scenario, where there is no real HTTP
/// request to derive a <see cref="QueryContext"/> from.
/// </summary>
/// <remarks>
/// Kernel-side artifacts that read <c>IQueryContextManager.Current</c> need this resolvable even outside a real
/// ASP.NET Core pipeline.
/// </remarks>
internal sealed class InProcessQueryContextManager : IQueryContextManager
{
    QueryContext _current = new(
        "InProcess",
        CorrelationId.NotSet,
        Paging.NotPaged,
        Sorting.None,
        QueryArguments.Empty,
        [],
        new DefaultServiceProvider(),
        default);

    /// <inheritdoc/>
    public QueryContext Current => _current;

    /// <inheritdoc/>
    public void Set(QueryContext context) => _current = context;
}
