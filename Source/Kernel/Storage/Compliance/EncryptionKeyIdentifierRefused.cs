// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The base for exceptions a key store throws when it refuses an encryption key identifier outright, rather than
/// failing to reach its backing storage.
/// </summary>
/// <remarks>
/// A refusal is never outvoted by another store: a composite surfaces it instead of answering from a member that has
/// no opinion on the identifier, so a key the refusing store cannot vouch for is never served. Messages never name the
/// identifier, because identifiers are subject data.
/// </remarks>
/// <param name="message">The message, which must not contain the identifier.</param>
public abstract class EncryptionKeyIdentifierRefused(string message) : Exception(message);
