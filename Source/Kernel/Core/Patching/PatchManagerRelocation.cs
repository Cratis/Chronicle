// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Carries the newer caller's silo as an additive hint, ignored by older kernels.
/// </summary>
internal static class PatchManagerRelocation
{
    /// <summary>
    /// The request-context key for the newer caller's silo address.
    /// </summary>
    internal const string CallerSiloKey = "Cratis.Chronicle.Patching.CallerSilo";

    /// <summary>
    /// Reads the caller's silo only when it is a typed Orleans address.
    /// </summary>
    /// <returns>The supplied address, or null for older callers and invalid hints.</returns>
    internal static SiloAddress? CallerSilo() => RequestContext.Get(CallerSiloKey) as SiloAddress;
}
