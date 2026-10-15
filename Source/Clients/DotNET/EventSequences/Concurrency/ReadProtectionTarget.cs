// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// The complete identity of the log guarded by a contribution.
/// </summary>
/// <param name="EventStore">The store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequence">The sequence.</param>
internal record ReadProtectionTarget(EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId EventSequence);
