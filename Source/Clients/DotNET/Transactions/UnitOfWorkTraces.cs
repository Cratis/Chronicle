// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Chronicle.Transactions;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class UnitOfWorkTraces
{
    [Span("client.unit_of_work.commit", ActivityKind.Client)]
    internal static partial IActivityScope<UnitOfWork> Commit(
        this IActivitySource<UnitOfWork> source,
        string correlationId);

    [Span("client.unit_of_work.rollback", ActivityKind.Client)]
    internal static partial IActivityScope<UnitOfWork> Rollback(
        this IActivitySource<UnitOfWork> source,
        string correlationId);
}
