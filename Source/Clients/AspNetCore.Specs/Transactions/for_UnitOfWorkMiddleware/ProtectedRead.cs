// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkMiddleware;

internal static class ProtectedRead
{
    /// <summary>Construct a real protected read without making client internals visible to the spec assembly.</summary>
    /// <returns>A protected decision read.</returns>
    internal static IDecisionRead Create() => (IDecisionRead)Activator.CreateInstance(
        typeof(DecisionRead<object>),
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        [(ReadModelKey)"source", null, (EventStoreName)"store", (EventStoreNamespaceName)"namespace", (EventSequenceNumber)4, (EventType[])[new EventType("created", 1)]],
        null)!;
}
