// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Identifies a value's protection boundary with stable serialized names.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProtectionKind>))]
public enum ProtectionKind
{
    /// <summary>
    /// Personal data protected by its subject's erasable key.
    /// </summary>
    [JsonStringEnumMemberName("pii")]
    Pii = 0,

    /// <summary>
    /// Confidentiality scoped to a subject.
    /// </summary>
    [JsonStringEnumMemberName("encrypted-subject")]
    EncryptedSubject = 1,

    /// <summary>
    /// Confidentiality scoped to a namespace.
    /// </summary>
    [JsonStringEnumMemberName("encrypted-namespace")]
    EncryptedNamespace = 2,

    /// <summary>
    /// Confidentiality scoped to the installation.
    /// </summary>
    [JsonStringEnumMemberName("encrypted-global")]
    EncryptedGlobal = 3
}
