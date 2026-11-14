// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// The exception that is thrown when a namespace cannot be created because its name is not allowed.
/// </summary>
/// <param name="eventStore">The event store the namespace was meant for.</param>
/// <param name="namespace">The namespace that was refused.</param>
public class InvalidNamespaceName(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    : Exception($"The namespace name '{@namespace}' is not allowed in event store '{eventStore}' because it contains a comma. A name like 'a,a' usually means the tenant header was sent twice.");
