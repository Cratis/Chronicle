// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>One resource, GC and image configuration.</summary>
/// <param name="Cpus">The CPU quota.</param>
/// <param name="Memory">The hard memory and swap limit.</param>
/// <param name="Gc">The GC variant.</param>
/// <param name="Image">The publish variant.</param>
internal sealed record MatrixCell(int Cpus, string Memory, GcVariant Gc, ImageVariant Image)
{
    /// <summary>Gets the stable, filterable cell identifier.</summary>
    public string Id => $"{Cpus}cpu-{Memory}-{Gc.Name}-{Image.Name}";
}
