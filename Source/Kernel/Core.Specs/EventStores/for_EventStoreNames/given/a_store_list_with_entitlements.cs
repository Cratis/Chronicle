// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Subjects;
using System.Security.Claims;
using System.Threading.Channels;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.given;

public class a_store_list_with_entitlements : Specification
{
    protected readonly ClaimsPrincipal _caller = new(new ClaimsIdentity([new Claim("sub", "caller", ClaimValueTypes.String, "https://authority.example")], ExternalAuthority.AuthenticationType));
    protected readonly Subject<Unit> _changes = new();
    protected readonly IStoreEntitlementsCache _cache = Substitute.For<IStoreEntitlementsCache>();
    protected readonly Authentication _authentication = new()
    {
        Authority = "https://authority.example",
        EnforceStoreEntitlements = true,
        ExternalAdministratorClaim = new() { Type = "role", Value = "operator" }
    };
    protected ServiceProvider _services;
    protected IEnumerable<EventStoreNames> _snapshot;
    protected IEnumerable<EventStoreNames> _observed;
    protected readonly IStorage _storage = Substitute.For<IStorage>();
    protected readonly IEventStoreInitialization _initialization = Substitute.For<IEventStoreInitialization>();
    readonly ReplaySubject<IEnumerable<EventStoreName>> _stores = new(1);
    readonly Channel<IEnumerable<EventStoreNames>> _emissions = Channel.CreateUnbounded<IEnumerable<EventStoreNames>>();
    IDisposable _subscription;

    void Establish()
    {
        _cache.IsFresh.Returns(true);
        _cache.Changes.Returns(_changes);
        Grant("first");
        _storage.GetEventStores().Returns([(EventStoreName)"first", "second", EventStoreName.System]);
        _storage.ObserveEventStores().Returns(_stores);
        _stores.OnNext([(EventStoreName)"first", "second", EventStoreName.System]);
        _initialization.EnsureAll(Arg.Any<IEnumerable<EventStoreName>>()).Returns(call => call.Arg<IEnumerable<EventStoreName>>());
        Configure(_authentication);
    }

    protected void Configure(Authentication authentication, bool inProcess = false)
    {
        _services?.Dispose();
        var services = new ServiceCollection().AddLogging();
        if (inProcess) services.AddSingleton(new InProcessKernelHost());
        services.AddSingleton(Options.Create(new ChronicleOptions { Authentication = authentication }));
        services.AddSingleton(_cache);
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = _caller } });
        services.AddSingleton<IAuthorizationDecisions, AuthorizationDecisions>();
        services.AddSingleton<IAdministratorAuthorization, AdministratorAuthorization>();
        services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
        _services = services.BuildServiceProvider();
        _services.GetRequiredService<IHttpContextAccessor>().HttpContext.RequestServices = _services;
    }

    protected void Grant(string store) => _cache.Snapshot.Returns(new StoreEntitlementsSnapshot(new(1,
        [new(PrincipalKey.From(_caller, _authentication), store, null, StoreAccess.Read, EventStoreAccessGrantSource.Command)])));

    protected async Task ReadLists()
    {
        _snapshot = await EventStoreNames.AllEventStores(_storage, _initialization, _services);
        await Observe();
    }

    protected async Task Observe()
    {
        _subscription = EventStoreNames.ObserveEventStores(_storage, _initialization, _services)
            .Subscribe(names => _emissions.Writer.TryWrite(names), error => _emissions.Writer.TryComplete(error));
        await ReadEmission();
    }

    protected async Task ReadEmission() => _observed = await _emissions.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

    void Destroy()
    {
        _subscription?.Dispose();
        _services?.Dispose();
        _stores.Dispose();
        _changes.Dispose();
    }
}
