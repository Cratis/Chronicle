// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// An earlier secret whose copy was read back and compared.
/// </summary>
/// <param name="Entry">The <see cref="LegacyEntry"/>.</param>
/// <param name="SourceVersion">The version of the earlier secret that was copied.</param>
internal record VerifiedCopy(LegacyEntry Entry, string SourceVersion);
