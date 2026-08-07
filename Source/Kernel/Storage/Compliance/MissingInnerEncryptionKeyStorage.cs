// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when a <see cref="CompositeEncryptionKeyStorage"/> is created without any inner store.
/// </summary>
/// <remarks>
/// A composite with nothing to compose would accept every key and hold none of them, so a value could be
/// protected under a key that was never persisted and can never be read back.
/// </remarks>
public class MissingInnerEncryptionKeyStorage()
    : Exception("A composite encryption key storage needs at least one inner store to compose.");
