// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Namespace incarnation coordination is disabled or unsupported by the provider.</summary>
/// <param name="eventStore">The event store.</param>
public class NamespaceIncarnationUnavailable(EventStoreName eventStore) : Exception($"Namespace incarnation coordination is unavailable for event store '{eventStore}'.");
