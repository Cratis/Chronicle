// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Represents the current version of a secret in a KV v2 mount.
/// </summary>
/// <param name="Version">The version.</param>
/// <param name="Data">The fields of the secret that hold text.</param>
/// <param name="OnlyText">Whether every field of the secret holds text. When false, <paramref name="Data"/> is not the whole secret.</param>
internal record VaultSecret(int Version, IReadOnlyDictionary<string, string> Data, bool OnlyText = true);
