// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintsStorage"/>.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/> to use.</param>
public class ConstraintsStorage(IEventStoreDatabase eventStoreDatabase) : IConstraintsStorage
{
    /// <inheritdoc/>
    public Task<IEnumerable<IConstraintDefinition>> GetDefinitions() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task SaveDefinition(IConstraintDefinition definition) => throw new NotImplementedException();
}
