// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The exception that is thrown when hosted federation needs to wrap or unwrap a private key and has no key-encryption key.
/// </summary>
/// <param name="reason">Why there is none. Never contains key material.</param>
public class FederationKeyEncryptionKeyUnavailable(string reason) : Exception($"Federation has no key-encryption key: {reason}");
