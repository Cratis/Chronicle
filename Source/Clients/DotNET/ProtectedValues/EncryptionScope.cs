// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Defines the identity a <see cref="EncryptedAttribute"/> value's encryption key is provisioned under.
/// </summary>
/// <remarks>
/// <see cref="PIIAttribute"/> never gains any scope beyond <see cref="Subject"/> - GDPR compliance is always resolved
/// against a compliance identity, and widening that would make the compliance subject boundary itself scope-
/// dependent. <see cref="EncryptionScope"/> exists precisely because <c language="csharp">[Encrypted]</c> values have no
/// data subject and no erasure obligation, so a wider key boundary is safe for them in a way it is not for PII.
/// </remarks>
public enum EncryptionScope
{
    /// <summary>
    /// The key is provisioned per compliance identity (the same identity - subject or, when none is set, event
    /// source id - PII values on the same document resolve to), and is looked up the same way. This is the
    /// default.
    /// </summary>
    Subject = 0,

    /// <summary>
    /// The key is provisioned once per event store namespace, independent of any document's subject or compliance
    /// identity. Every <c language="csharp">[Encrypted(EncryptionScope.Namespace)]</c> value in a namespace shares one key.
    /// </summary>
    Namespace = 1,

    /// <summary>
    /// The key is provisioned once for the whole Chronicle installation, independent of event store, namespace, or
    /// compliance identity. Every <c language="csharp">[Encrypted(EncryptionScope.Global)]</c> value across every event store
    /// and namespace shares one key.
    /// </summary>
    Global = 2
}
