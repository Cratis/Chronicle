// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.for_StoreEntitlementsSnapshot;

public class when_matching_grants : Specification
{
    static readonly PrincipalKey _holder = new(PrincipalKey.LocalIssuer, "app");

    [Theory]
    [InlineData("Shop", null, StoreAccess.Read, "shop", "tenant", StoreAccess.Read, true)]
    [InlineData("shop", null, StoreAccess.Read, "shop2", null, StoreAccess.Read, false)]
    [InlineData("shop", null, StoreAccess.Read, "sho", null, StoreAccess.Read, false)]
    [InlineData("shop", null, StoreAccess.Read, "shop ", null, StoreAccess.Read, false)]
    [InlineData("shop", "one", StoreAccess.Read, "shop", "two", StoreAccess.Read, false)]
    [InlineData("shop", "one", StoreAccess.Read, "shop", "one", StoreAccess.Read, true)]
    [InlineData("shop", "one", StoreAccess.Read, "shop", null, StoreAccess.Read, false)]
    [InlineData(null, null, StoreAccess.Read, "another", null, StoreAccess.Read, true)]
    [InlineData(null, null, StoreAccess.Read, "system", null, StoreAccess.Read, false)]
    [InlineData("System", null, StoreAccess.Read, "System", null, StoreAccess.Read, false)]
    [InlineData("shop", null, StoreAccess.Read, "shop", null, StoreAccess.Append, false)]
    [InlineData("shop", null, StoreAccess.Read, "shop", null, StoreAccess.Manage, false)]
    [InlineData("shop", null, StoreAccess.Manage, "shop", null, StoreAccess.Read, false)]
    [InlineData("shop", null, StoreAccess.Read | StoreAccess.Append, "shop", null, StoreAccess.Read | StoreAccess.Append, true)]
    public void should_match_only_the_granted_scope_and_access(string? grantedStore, string? grantedNamespace, StoreAccess grantedAccess, string store, string? ns, StoreAccess requested, bool expected)
    {
        var snapshot = new StoreEntitlementsSnapshot(new(1, [new(_holder, grantedStore is null ? null : new EventStoreName(grantedStore), grantedNamespace is null ? null : new EventStoreNamespaceName(grantedNamespace), grantedAccess, EventStoreAccessGrantSource.Command)]));
        snapshot.Allows(_holder, store, ns is null ? null : new EventStoreNamespaceName(ns), requested).ShouldEqual(expected);
    }

    [Fact]
    void should_keep_issuers_distinct() => new StoreEntitlementsSnapshot(new(1, [new(_holder, null, null, StoreAccess.Read, EventStoreAccessGrantSource.Command)]))
        .Allows(new("https://other", _holder.Subject), "shop", null, StoreAccess.Read).ShouldBeFalse();
}
