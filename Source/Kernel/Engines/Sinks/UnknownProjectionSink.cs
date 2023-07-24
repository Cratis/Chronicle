// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Engines.Sinks;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="ISink"/> is used.
/// </summary>
public class UnknownProjectionSink : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownProjectionSink"/> class.
    /// </summary>
    /// <param name="typeId">The unknown <see cref="SinkTypeId"/>.</param>
    public UnknownProjectionSink(SinkTypeId typeId) : base($"Projection sink type of '{typeId}' is unknown.")
    {
    }
}
