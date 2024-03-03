// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Sinks;

namespace Cratis.Kernel.Storage.Sinks;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="ISink"/> is used.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnknownSink"/> class.
/// </remarks>
/// <param name="typeId">The unknown <see cref="SinkTypeId"/>.</param>
public class UnknownSink(SinkTypeId typeId) : Exception($"Projection sink type of '{typeId}' is unknown.")
{
}
