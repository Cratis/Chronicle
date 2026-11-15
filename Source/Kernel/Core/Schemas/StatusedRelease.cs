// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents released content with the status of each protected property boundary.
/// </summary>
/// <param name="Content">The released content, retaining ciphertext at erased boundaries.</param>
/// <param name="Statuses">Release outcomes keyed by dotted property paths with bracketed array indexes, such as <c language="text">contacts[0].email</c>.</param>
public record StatusedRelease(JsonObject Content, IReadOnlyDictionary<string, ProtectedValueReleaseStatus> Statuses);
