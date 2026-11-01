// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// A replay scenario for <see cref="ReplayChildLookupBenchmark"/>.
/// </summary>
/// <param name="Name">The scenario name.</param>
/// <param name="Roots">The number of root documents.</param>
/// <param name="Children">The number of children per root.</param>
/// <param name="GuidIds">Whether child ids are guids rather than strings.</param>
/// <param name="MissRoundTripMicroseconds">The simulated round trip of a lookup that falls through to the server.</param>
public sealed record ReplayChildLookupScenario(string Name, int Roots, int Children, bool GuidIds, int MissRoundTripMicroseconds)
{
    /// <inheritdoc/>
    public override string ToString() => Name;
}
