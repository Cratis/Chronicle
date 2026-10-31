// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Provides the stream declaration independent of its generic source type.
/// </summary>
internal interface IEventStreamReadModelDeclaration
{
    /// <summary>
    /// Gets the event source definition type.
    /// </summary>
    Type EventSource { get; }

    /// <summary>
    /// Gets the declared stream name.
    /// </summary>
    string Stream { get; }
}
