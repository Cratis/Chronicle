// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;
using Cratis.Reflection;

namespace Cratis.Chronicle.Orleans.Transactions;

/// <summary>
/// Extension methods for <see cref="IGrainCallContext"/>. 
/// </summary>
internal static class GrainCallContextExtensions
{
    /// <summary>
    /// Checks if the <see cref="IGrainCallContext"/> is for an aggregate root.
    /// </summary>
    /// <param name="context">The <see cref="IGrainCallContext"/> to check.</param>
    /// <returns>True if message is for aggregate root, false if not.</returns>
    public static bool IsMessageToAggregateRoot(this IGrainCallContext context) =>
        context.InterfaceMethod.DeclaringType?.HasInterface<IAggregateRoot>() ?? false;
}