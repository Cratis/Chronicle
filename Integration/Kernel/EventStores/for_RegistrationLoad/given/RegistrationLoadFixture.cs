// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.EventStores.for_RegistrationLoad.given;

public class RegistrationLoadFixture : ChronicleInProcessFixture
{
    /// <inheritdoc/>
    /// <remarks>
    /// Each context gets its own kernel host, which is disposed with the context, so the small-pool kernel never outlives this spec.
    /// </remarks>
    public override bool UsesPerContextMongoDBHost => true;
}
