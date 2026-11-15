// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Represents the in-memory implementation of <see cref="IReservedEventSequenceIds"/>.
/// </summary>
/// <remarks>
/// The in-memory provider has no physical collection or table names, so no event sequence identifier is reserved.
/// </remarks>
public class InMemoryReservedEventSequenceIds : IReservedEventSequenceIds
{
    /// <inheritdoc/>
    public bool IsReserved(EventSequenceId eventSequenceId) => false;
}
