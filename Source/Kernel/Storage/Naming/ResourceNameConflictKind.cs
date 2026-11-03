// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// The kind of conflict a new name has with the existing names.
/// </summary>
public enum ResourceNameConflictKind
{
    /// <summary>
    /// An event store name differs only in case from an existing one.
    /// </summary>
    EventStoreCaseVariant = 0,

    /// <summary>
    /// A namespace name differs only in case from an existing one in the same event store.
    /// </summary>
    NamespaceCaseVariant = 1,

    /// <summary>
    /// The storage of the name would overlap the storage of an existing name.
    /// </summary>
    Overlap = 2
}
