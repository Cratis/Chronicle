// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>A stale lifecycle revision lost its conditional write and must not issue an effect.</summary>
/// <param name="namespace">The canonical namespace name.</param>
public class NamespaceLifecycleChanged(EventStoreNamespaceName @namespace) : Exception($"Namespace '{@namespace}' lifecycle changed; retry the operation.");
