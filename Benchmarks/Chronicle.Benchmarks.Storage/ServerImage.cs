// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The server a provider ran on.</summary>
/// <param name="Tag">The container image and tag, or a description for a file-backed provider.</param>
/// <param name="Digest">The image digest, or `unknown`.</param>
/// <param name="Version">The server version as the server reports it.</param>
public sealed record ServerImage(string Tag, string Digest, string Version);
