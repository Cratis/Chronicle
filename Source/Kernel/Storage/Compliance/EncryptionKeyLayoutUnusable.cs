// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Exception that gets thrown when the layout of an encryption key store could not be confirmed to be free for the
/// paths a key would be written to, so nothing is written there.
/// </summary>
public class EncryptionKeyLayoutUnusable()
    : Exception("The layout of the encryption key store could not be confirmed, so no encryption key is written to it.");
