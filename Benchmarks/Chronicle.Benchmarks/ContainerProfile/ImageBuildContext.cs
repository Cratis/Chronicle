// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The shared output, target and ownership settings for scratch image variants.</summary>
/// <param name="Results">The image-build results root.</param>
/// <param name="Architecture">The target Linux architecture.</param>
/// <param name="Sha">The source commit abbreviation.</param>
/// <param name="Session">The image-build session identity.</param>
internal sealed record ImageBuildContext(string Results, string Architecture, string Sha, string Session);
