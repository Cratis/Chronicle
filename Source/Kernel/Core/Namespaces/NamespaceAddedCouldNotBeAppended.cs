// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// The exception that is thrown when the durable namespace creation notification could not be appended.
/// </summary>
/// <param name="eventStore">The event store the namespace belongs to.</param>
/// <param name="namespace">The namespace being created.</param>
public class NamespaceAddedCouldNotBeAppended(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    : Exception($"Could not append NamespaceAdded for namespace '{@namespace}' in event store '{eventStore}'.");
