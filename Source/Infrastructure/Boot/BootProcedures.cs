// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;
using Aksio.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Boot;

/// <summary>
/// Represents an implementation of <see cref="IBootProcedures"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="BootProcedures"/>.
/// </remarks>
/// <param name="procedures"><see cref="IInstancesOf{T}"/> <see cref="IPerformBootProcedure"/>.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class BootProcedures(
    IInstancesOf<IPerformBootProcedure> procedures,
    ILogger<BootProcedures> logger) : IBootProcedures
{
    /// <inheritdoc/>
    public void Perform()
    {
        logger.PerformingBootProcedures();
        procedures.ForEach(_ => _.Perform());
    }
}
