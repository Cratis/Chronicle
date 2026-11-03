// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// The exception that is thrown when a new name would share a resource with an existing one.
/// </summary>
/// <param name="kind">The <see cref="ResourceNameConflictKind"/>.</param>
public class ResourceNameConflict(ResourceNameConflictKind kind) : Exception(GetMessage(kind))
{
    /// <summary>
    /// Gets the <see cref="ResourceNameConflictKind"/>.
    /// </summary>
    public ResourceNameConflictKind Kind { get; } = kind;

    static string GetMessage(ResourceNameConflictKind kind) => kind switch
    {
        ResourceNameConflictKind.EventStoreCaseVariant => ResourceNames.EventStoreCaseVariantMessage,
        ResourceNameConflictKind.NamespaceCaseVariant => ResourceNames.NamespaceCaseVariantMessage,
        _ => ResourceNames.OverlapMessage
    };
}
