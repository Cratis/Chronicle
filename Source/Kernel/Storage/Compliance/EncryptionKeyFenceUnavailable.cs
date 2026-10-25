// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception thrown when the current erasure fence cannot be read safely.
/// </summary>
/// <param name="cause">The failed fence read.</param>
public class EncryptionKeyFenceUnavailable(Exception cause)
    : Exception("The encryption key erasure fence could not be read; protected material cannot be used.", cause);
