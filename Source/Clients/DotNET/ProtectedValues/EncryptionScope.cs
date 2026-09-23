// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Defines the identity a <see cref="EncryptedAttribute"/> value's encryption key is provisioned under.
/// </summary>
/// <remarks>
/// Only <see cref="Subject"/> is implemented today - see the remarks on <see cref="EncryptedAttribute"/>.
/// <see cref="Namespace"/> and <see cref="Global"/> are reserved so that code written against them compiles
/// once and keeps working when they are implemented, rather than requiring every caller of this API to be
/// revisited a second time.
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
    /// The key is provisioned once per event store namespace, independent of any document's subject. Reserved -
    /// not yet implemented.
    /// </summary>
    Namespace = 1,

    /// <summary>
    /// The key is provisioned once for the whole Chronicle installation, independent of event store or
    /// namespace. Reserved - not yet implemented.
    /// </summary>
    Global = 2
}
