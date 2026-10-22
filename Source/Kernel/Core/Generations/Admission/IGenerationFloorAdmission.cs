// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Generations.Admission;

/// <summary>Read-only admission before any startup mutation.</summary>
public interface IGenerationFloorAdmission
{
    /// <summary>Checks every store, including System, failing closed on read errors.</summary>
    /// <param name="cancellationToken">Startup cancellation.</param>
    /// <returns>The admission task.</returns>
    Task Admit(CancellationToken cancellationToken);
}
