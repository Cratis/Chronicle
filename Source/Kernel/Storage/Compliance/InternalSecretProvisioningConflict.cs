// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when an internal secret lost a concurrent creation and the winner cannot be read back.
/// </summary>
/// <remarks>
/// The candidate was never persisted, so it is not handed back: callers would otherwise use a secret no other silo has.
/// </remarks>
/// <param name="purpose">The <see cref="InternalSecretPurpose"/> of the secret.</param>
public class InternalSecretProvisioningConflict(InternalSecretPurpose purpose)
    : Exception($"The internal secret '{purpose}' was created concurrently, but the stored secret could not be read back.");
