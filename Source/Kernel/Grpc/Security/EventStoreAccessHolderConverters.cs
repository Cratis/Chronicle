// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>Converts explicitly addressed grant holders across the transport boundary.</summary>
internal static class EventStoreAccessHolderConverters
{
    /// <summary>Converts an explicit transport holder to its administration representation.</summary>
    /// <param name="holder">The transport holder.</param>
    /// <returns>The administration holder.</returns>
    public static Chronicle.Security.EventStoreAccessHolder ToApi(this Contracts.Security.EventStoreAccessHolder holder) => new(holder.Issuer, holder.Subject, holder.IsClient);

    /// <summary>Converts an administration holder to its transport representation.</summary>
    /// <param name="holder">The administration holder.</param>
    /// <returns>The transport holder.</returns>
    public static Contracts.Security.EventStoreAccessHolder ToContract(this Chronicle.Security.EventStoreAccessHolder holder) => new() { Issuer = holder.Issuer, Subject = holder.Subject, IsClient = holder.IsClient };
}
