// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Selects the names used for Chronicle client spans.
/// </summary>
public enum ClientSpanNames
{
    /// <summary>Keep the existing client.* operation and display names.</summary>
    Legacy = 0,

    /// <summary>Use the shared cratis.chronicle.client.* convention names.</summary>
    Convention = 1
}
