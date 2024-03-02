// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Storage.Sinks;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="ISink"/> is used.
/// </summary>
public class UnknownSink : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownSink"/> class.
    /// </summary>
    /// <param name="typeId">The unknown <see cref="SinkTypeId"/>.</param>
    public UnknownSink(SinkTypeId typeId) : base($"Projection sink type of '{typeId}' is unknown.")
    {
    }
}
