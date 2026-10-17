// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Selects the durable lifecycle delivery boundary.
/// </summary>
public enum LifecycleScope
{
    /// <summary>
    /// One occurrence for the event store, independently of every namespace.
    /// </summary>
    Application = 0,

    /// <summary>
    /// One occurrence per namespace existing when the kernel records the durable intent.
    /// </summary>
    AllNamespaces = 1
}
