// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The outcome of replacing an isolated read model instance.
/// </summary>
public enum IsolatedReplaceResult
{
    /// <summary>The complete state was replaced.</summary>
    Replaced = 0,

    /// <summary>A newer stored watermark prevented the write.</summary>
    Superseded = 1,

    /// <summary>The instance was removed or was already absent.</summary>
    Removed = 2
}
