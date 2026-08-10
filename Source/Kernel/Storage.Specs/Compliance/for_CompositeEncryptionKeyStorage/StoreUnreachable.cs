// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage;

/// <summary>
/// The exception that is thrown by a substituted inner store standing in for one that cannot be reached.
/// </summary>
public class StoreUnreachable() : Exception("The inner encryption key store cannot be reached.");
