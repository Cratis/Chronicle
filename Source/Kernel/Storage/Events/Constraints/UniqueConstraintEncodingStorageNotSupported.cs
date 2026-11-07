// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// The exception that is thrown when a provider does not support unique-constraint encoding-key storage.
/// </summary>
public sealed class UniqueConstraintEncodingStorageNotSupported() : Exception("The storage provider does not support unique-constraint encoding-key storage.");
