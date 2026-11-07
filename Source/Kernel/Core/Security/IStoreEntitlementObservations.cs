// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Security;

/// <summary>Retains bounded, non-authoritative audit evidence for the lifetime of one grain activation.</summary>
[Version(1)]
public interface IStoreEntitlementObservations : IGrainWithStringKey
{
    /// <summary>The cluster observation grain key.</summary>
    public const string Key = "store-entitlement-observations";

    /// <summary>Records a rate-limited observation without granting access.</summary>
    /// <param name="holder">The principal.</param>
    /// <param name="eventStore">The ordinary event store.</param>
    /// <param name="namespace">The observed namespace.</param>
    /// <param name="access">The attempted access.</param>
    /// <returns>The completion.</returns>
    Task Record(PrincipalKey holder, EventStoreName eventStore, EventStoreNamespaceName? @namespace, StoreAccess access);

    /// <summary>Gets the retained evidence.</summary>
    /// <returns>A detached snapshot.</returns>
    Task<IEnumerable<StoreEntitlementObservation>> GetSnapshot();
}
