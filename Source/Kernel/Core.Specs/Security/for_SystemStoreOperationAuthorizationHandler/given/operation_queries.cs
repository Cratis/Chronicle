// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Chronicle.Concepts;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler.given;

[IgnoreConvention]
[Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
public static class operation_queries
{
    public static string Ordinary(string eventStore) => eventStore;

    [DestructiveOperation]
    public static string Destructive(EventStoreName eventStore) => eventStore.Value;

    public static string Kernel() => "kernel";

    [DestructiveOperation]
    public static string DestructiveKernel() => "kernel";

    public static string Optional(string eventStore = "System") => eventStore;

    public static string OrdinaryDefault(string eventStore = "application") => eventStore;

    public static string? NullDefault(string? eventStore = null) => eventStore;

    public static string BlankDefault(string eventStore = " ") => eventStore;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case", Justification = "Exercises refusal of ambiguous case-insensitive store parameters.")]
    public static string Ambiguous(string eventStore, string EventStore) => eventStore + EventStore;

    public static string AnotherStoreType(string eventStore, EventStoreName otherStore) => eventStore + otherStore.Value;

    public static string AnotherStoreConcept(EventStoreName eventStore, EventStoreName otherStore) => eventStore.Value + otherStore.Value;

    public static string AnotherOptionalStore(string eventStore, EventStoreName otherStore = null!) => eventStore + otherStore?.Value;

    public static object Injected(IServiceProvider eventStore) => eventStore;
}
