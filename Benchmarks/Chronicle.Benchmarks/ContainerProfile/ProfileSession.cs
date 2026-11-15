// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The shared resource scope and source identity of a profiling sweep.</summary>
/// <param name="Id">The session identity used for resource ownership.</param>
/// <param name="Results">The session results directory.</param>
/// <param name="Sha">The source commit abbreviation.</param>
/// <param name="MongoImage">The selected MongoDB image.</param>
internal sealed record ProfileSession(string Id, string Results, string Sha, string MongoImage);
