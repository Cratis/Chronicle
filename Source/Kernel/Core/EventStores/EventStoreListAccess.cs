// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventStores;

/// <summary>Filters event store lists using the caller's existing store capability decisions.</summary>
internal sealed class EventStoreListAccess
{
    readonly IServiceProvider _services;
    readonly ClaimsPrincipal? _caller;
    readonly bool _enforce;

    /// <summary>Initializes a new instance of the <see cref="EventStoreListAccess"/> class.</summary>
    /// <param name="services">The services of the query.</param>
    internal EventStoreListAccess(IServiceProvider services)
    {
        var context = services.GetService<IHttpContextAccessor>()?.HttpContext;
        _services = context?.RequestServices ?? services;
        _caller = SystemStoreRouting.CallerFor(_services, context?.User);
        _enforce = _services.GetService<IOptions<ChronicleOptions>>()?.Value.Authentication.EnforceStoreEntitlements is true;
    }

    /// <summary>Retains only the event stores the caller may read when entitlements are enforced.</summary>
    /// <param name="stores">The initialized stores.</param>
    /// <returns>The visible stores.</returns>
    internal async Task<IEnumerable<EventStoreNames>> Filter(IEnumerable<EventStoreNames> stores)
    {
        if (!_enforce) return stores;

        var visible = new List<EventStoreNames>();
        try
        {
            foreach (var store in stores)
            {
                if (await StoreCapabilityDecision.Permits(_services, _caller, WellKnownCapabilities.ReadEventStore.Value, store.Name, null)) visible.Add(store);
            }
        }
        catch
        {
            return [];
        }

        return visible;
    }

    /// <summary>Re-evaluates the latest initialized list after each entitlement change.</summary>
    /// <param name="stores">The initialized store stream.</param>
    /// <returns>The visible store stream.</returns>
    internal IObservable<IEnumerable<EventStoreNames>> Observe(IObservable<IEnumerable<EventStoreNames>> stores)
    {
        if (!_enforce) return stores;

        var changes = _services.GetService<IStoreEntitlementsCache>()?.Changes ?? Observable.Never<Unit>();
        return stores.CombineLatest(changes.StartWith(Unit.Default), (names, _) => names)
            .Select(names => Observable.FromAsync(() => Filter(names)))
            .Switch();
    }
}
