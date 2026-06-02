// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Chronicle.Reducers;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ReducerTraces
{
    [Span("client.reducer.handle", ActivityKind.Consumer)]
    internal static partial IActivityScope<Reducers> Handle(
        this IActivitySource<Reducers> source,
        string eventStoreName,
        string namespaceName,
        string eventSequenceId,
        string reducerId);
}
