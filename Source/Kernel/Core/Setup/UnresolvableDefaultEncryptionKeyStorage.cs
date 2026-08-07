// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// The exception that is thrown when the <see cref="IEncryptionKeyStorage"/> registered by the general storage
/// backend cannot be built, so it cannot be composed with a dedicated compliance key store.
/// </summary>
public class UnresolvableDefaultEncryptionKeyStorage()
    : Exception("The encryption key storage registered by the general storage backend declares neither an instance, a factory nor an implementation type, so it cannot be composed with a dedicated compliance key store.");
