// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Installs the spec mutation digest keys when the spec assembly loads.
/// </summary>
internal static class MutationDigestKeysModuleInitialization
{
    /// <summary>
    /// Gives every spec a per-event-store mutation digest key, so specs that compute keyed digests directly, or drive a
    /// provider registry without the key-resolving wrapper, do not each have to put a key in scope.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize() => EventSequenceMutationDigestTestKeys.Install();
}
