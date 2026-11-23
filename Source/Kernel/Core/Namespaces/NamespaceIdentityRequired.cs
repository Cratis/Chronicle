// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Exception that gets thrown when a namespace-protected write arrives without the namespace identity its writer captured.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> of the event store.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the write targeted.</param>
public class NamespaceIdentityRequired(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    : Exception($"A write to namespace '{@namespace}' in event store '{eventStore}' requires the namespace identity captured by its writer");
