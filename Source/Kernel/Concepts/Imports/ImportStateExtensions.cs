// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Imports;

/// <summary>
/// Extension methods for <see cref="ImportState"/>.
/// </summary>
public static class ImportStateExtensions
{
    /// <summary>
    /// Gets whether the import accepts more chunks.
    /// </summary>
    /// <param name="state">The <see cref="ImportState"/>.</param>
    /// <returns>True while staging is open.</returns>
    public static bool AcceptsChunks(this ImportState state) => state is ImportState.Open or ImportState.Staging;

    /// <summary>
    /// Gets whether the import has reached a state it never leaves.
    /// </summary>
    /// <param name="state">The <see cref="ImportState"/>.</param>
    /// <returns>True for completed, failed and abandoned imports.</returns>
    public static bool IsTerminal(this ImportState state) => state is ImportState.Completed or ImportState.Failed or ImportState.Abandoned;
}
