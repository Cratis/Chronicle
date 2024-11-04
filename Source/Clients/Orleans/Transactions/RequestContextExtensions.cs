// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Execution;

namespace Cratis.Chronicle.Orleans.Transactions;

public static class RequestContextExtensions
{
    public static bool TryGetCorrelationId([NotNullWhen(true)] CorrelationId? correlationId)
    {
        correlationId = RequestContext.Get(Constants.CorrelationIdKey) as CorrelationId;
        return correlationId != null;
    }
}
