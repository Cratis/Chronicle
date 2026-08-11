// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

/// <summary>
/// The exception that is thrown by a substituted backing store standing in for one whose erase did not complete.
/// </summary>
public class ErasureFailed() : Exception("The backing encryption key store failed to erase the key.");
