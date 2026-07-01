// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the supported source types for captures.
/// </summary>
public enum SourceType
{
    /// <summary>
    /// An API source.
    /// </summary>
    Api = 0,

    /// <summary>
    /// A webhook source.
    /// </summary>
    Webhook = 1,

    /// <summary>
    /// A message source.
    /// </summary>
    Message = 2
}
