// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Collections;
using Aksio.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Boot;

/// <summary>
/// Represents an implementation of <see cref="IBootProcedures"/>.
/// </summary>
public class BootProcedures : IBootProcedures
{
    readonly IInstancesOf<IPerformBootProcedure> _procedures;
    readonly ILogger<BootProcedures> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="BootProcedures"/>.
    /// </summary>
    /// <param name="procedures"><see cref="IInstancesOf{T}"/> <see cref="IPerformBootProcedure"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public BootProcedures(
        IInstancesOf<IPerformBootProcedure> procedures,
        ILogger<BootProcedures> logger)
    {
        _procedures = procedures;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        _logger.PerformingBootProcedures();
        _procedures.ForEach(_ => _.Perform());
    }
}
