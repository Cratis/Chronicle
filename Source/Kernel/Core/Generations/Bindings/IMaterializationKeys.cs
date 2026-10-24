// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Generations.Bindings;

/// <summary>Resolves the durable identity of a projection materialization.</summary>
public interface IMaterializationKeys
{
    /// <summary>Resolves the store incarnation and configured sink.</summary>
    /// <param name="store">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="projection">The projection.</param>
    /// <returns>The materialization key.</returns>
    Task<MaterializationKey> For(EventStoreName store, EventStoreNamespaceName ns, ProjectionId projection);
}
