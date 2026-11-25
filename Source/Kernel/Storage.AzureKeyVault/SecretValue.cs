// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// A secret value with the attributes of the version it was read from.
/// </summary>
/// <param name="Value">The value.</param>
/// <param name="Info">The <see cref="SecretVersionInfo"/>.</param>
public record SecretValue(string Value, SecretVersionInfo Info);
