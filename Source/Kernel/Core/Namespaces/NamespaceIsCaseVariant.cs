// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// The requested sequence namespace differs from the namespace's stored casing.
/// </summary>
/// <param name="eventStore">The event store.</param>
/// <param name="requested">The requested namespace.</param>
/// <param name="canonical">The canonical namespace.</param>
public class NamespaceIsCaseVariant(EventStoreName eventStore, EventStoreNamespaceName requested, EventStoreNamespaceName canonical)
    : Exception($"Namespace '{requested.Value}' in event store '{eventStore.Value}' is a case variant of '{canonical.Value}'; use '{canonical.Value}'");
