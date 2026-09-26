// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels;

/// <summary>A read that can be enrolled as a decision dependency.</summary>
public interface IDecisionRead
{
    /// <summary>Gets the key.</summary>
    ReadModelKey Key { get; }

    /// <summary>Gets the read model type.</summary>
    Type ReadModelType { get; }

    /// <summary>Gets whether the read carries a guard.</summary>
    bool IsProtected { get; }

    /// <summary>Gets the target event store.</summary>
    EventStoreName EventStore { get; }

    /// <summary>Gets the target namespace.</summary>
    EventStoreNamespaceName Namespace { get; }

    /// <summary>Gets the target sequence.</summary>
    EventSequenceId EventSequenceId { get; }

    /// <summary>Gets the private scope without exposing a sequence number in the public API.</summary>
    internal ConcurrencyScope Scope { get; }
}
