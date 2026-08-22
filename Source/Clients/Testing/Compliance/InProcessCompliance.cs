// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Types;
using KernelCompliance = KernelCore::Cratis.Chronicle.Compliance;
using KernelEvents = KernelCore::Cratis.Chronicle.Events;
using KernelGDPR = KernelCore::Cratis.Chronicle.Compliance.GDPR;
using KernelReadModels = KernelCore::Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.Compliance;

/// <summary>
/// Represents the compliance stack an in-process scenario runs its events and read models through.
/// </summary>
/// <remarks>
/// <para>
/// The kernel's <see cref="KernelCompliance::JsonComplianceManager"/> builds its dispatch table from the
/// property value handlers it is given, so constructing it with none makes every <c>[PII]</c> value pass
/// through in plaintext — silently, because a missing handler is indistinguishable from a value that
/// carries no compliance metadata. The real <see cref="KernelGDPR::PIICompliancePropertyValueHandler"/>
/// is registered here so an in-process scenario encrypts at rest and releases on read exactly the way a
/// deployed Chronicle does.
/// </para>
/// <para>
/// Every collaborator that needs compliance in one scenario shares this instance, because they have to
/// share the <see cref="InMemoryEncryptionKeyStorage"/> behind it: the grain encrypts on append and the
/// event sequences service releases on read, and a second key store would mint a second key that cannot
/// decrypt what the first one wrote. The key store is scoped to the scenario rather than the process, so
/// one scenario's crypto-shredding can never reach another's subjects.
/// </para>
/// <para>
/// <see cref="InMemoryEncryptionKeyStorage"/> is chosen over the other in-memory store
/// (<c>Storage.InMemory</c>'s <c>EncryptionKeyStorage</c>) because its <c>GetOrAddFor</c> is atomic under
/// its lock — the same get-or-create guarantee the persistent stores give — and it is the one the kernel's
/// own compliance specs exercise.
/// </para>
/// </remarks>
internal sealed class InProcessCompliance
{
    readonly ExpandoObjectConverter _expandoObjectConverter = new(new TypeFormats());

    /// <summary>
    /// Initializes a new instance of the <see cref="InProcessCompliance"/> class.
    /// </summary>
    public InProcessCompliance()
    {
        KeyStorage = new InMemoryEncryptionKeyStorage();
        Manager = new KernelCompliance::JsonComplianceManager(
            new KnownInstancesOf<KernelCompliance::IJsonCompliancePropertyValueHandler>(
            [
                new KernelGDPR::PIICompliancePropertyValueHandler(KeyStorage, new KernelCompliance::Encryption())
            ]),
            NullLogger<KernelCompliance::JsonComplianceManager>.Instance);
    }

    /// <summary>
    /// Gets the <see cref="KernelCompliance::JsonComplianceManager"/> every collaborator in the scenario shares.
    /// </summary>
    public KernelCompliance::JsonComplianceManager Manager { get; }

    /// <summary>
    /// Gets the <see cref="IEncryptionKeyStorage"/> holding the per-subject keys for the scenario.
    /// </summary>
    public IEncryptionKeyStorage KeyStorage { get; }

    /// <summary>
    /// Creates the <see cref="KernelEvents::EventCompliance"/> that releases event content on read.
    /// </summary>
    /// <returns>A new <see cref="KernelEvents::EventCompliance"/> over the shared manager.</returns>
    public KernelEvents::EventCompliance CreateEventCompliance() => new(Manager, _expandoObjectConverter);

    /// <summary>
    /// Creates the <see cref="KernelReadModels::ReadModelsCompliance"/> that applies and releases read model content.
    /// </summary>
    /// <returns>A new <see cref="KernelReadModels::ReadModelsCompliance"/> over the shared manager.</returns>
    public KernelReadModels::ReadModelsCompliance CreateReadModelsCompliance() => new(Manager, _expandoObjectConverter);
}
