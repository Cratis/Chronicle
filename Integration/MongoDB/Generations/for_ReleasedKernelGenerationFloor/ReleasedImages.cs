// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Generations.for_ReleasedKernelGenerationFloor;

/// <summary>
/// The released kernel images used as the "other binary" against a store's durable generation floor, pinned by digest.
/// </summary>
/// <remarks>
/// Generation floor admission at startup shipped in v19.106.6, the earliest floor-aware release and the pinned floor-aware binary.
/// v19.115.0 did not become healthy under the shared fixture configuration, so it is not used. v19.106.2 predates floor
/// admission and ignores the floor entirely.
/// The digests are image-index digests taken from registry metadata; the source-to-image provenance is not verified.
/// </remarks>
public static class ReleasedImages
{
    /// <summary>Gets the floor-aware released kernel (v19.106.6).</summary>
    public const string FloorAware = "cratis/chronicle:19.106.6-development-slim@sha256:2f5d8a840e83b34d4ddb92c51f09ce9ffb5f76f498436da28c8e01bed1c3d275";

    /// <summary>Gets the floor-unaware released kernel (v19.106.2).</summary>
    public const string FloorUnaware = "cratis/chronicle:19.106.2-development-slim@sha256:3dcf0cf274200619809573faa16395195364568a1162d9c210e815b5ba5a9643";
}
