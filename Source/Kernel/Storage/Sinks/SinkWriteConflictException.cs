// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Exception that gets thrown when a <see cref="SinkWriteMode.FencedGlobal"/> write found the instance changed since it was read.
/// </summary>
/// <remarks>
/// Nothing was written. The caller reads the instance again and recomputes the changes from what it finds.
/// </remarks>
/// <param name="key">The <see cref="Key"/> of the instance that changed.</param>
public class SinkWriteConflictException(Key key) : Exception($"The read model instance '{key}' changed since it was read, so the write was not applied")
{
    /// <summary>
    /// Gets the <see cref="Key"/> of the instance that changed.
    /// </summary>
    public Key Key { get; } = key;
}
