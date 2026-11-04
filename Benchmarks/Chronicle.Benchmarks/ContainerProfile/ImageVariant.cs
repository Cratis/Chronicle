// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>A kernel publish variant.</summary>
/// <param name="Name">The stable variant name.</param>
/// <param name="ReadyToRun">Whether to publish native managed headers.</param>
internal sealed record ImageVariant(string Name, bool ReadyToRun)
{
    /// <summary>The ReadyToRun variant.</summary>
    public static readonly ImageVariant R2R = new("r2r", true);

    /// <summary>The IL-only variant.</summary>
    public static readonly ImageVariant Jit = new("jit", false);

    /// <summary>Constructs a scratch image tag.</summary>
    /// <param name="sha">The source commit abbreviation.</param>
    /// <returns>The image reference.</returns>
    public string Reference(string sha) => $"chronicle-container-profile:{sha}-{Name}";
}
