// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using static Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider.fallback_context;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_preserving_fallback_controls : Specification
{
    readonly fallback_context _shared = new();
    readonly fallback_context _privateHost = new();
    readonly object _framework = new();
    readonly helper _keyed = new(new tracker());
    readonly List<helper> _keyedResults = [];
    object _frameworkResult;
    helper _privateResult;
    unregistered _unregistered;
    sync_lifetime _sync;
    async_lifetime _async;
    Exception? _policyReplacement;
    Exception? _guardAfterClear;

    void Establish()
    {
        _shared.Build((services, _) => services.AddSingleton(_framework)
            .AddKeyedSingleton("key", _keyed)
            .AddTransient<helper>()
            .AddScoped<sync_lifetime>()
            .AddScoped<async_lifetime>());
        _privateHost.Build((services, _) => services.AddTransient<helper>(), shared: false);
        _shared.Advance();
        _privateHost.Advance();
    }

    async Task Because()
    {
        _frameworkResult = _shared.Provider.GetRequiredService<object>();
        _keyedResults.Add(_shared.Provider.GetRequiredKeyedService<helper>("key"));
        _unregistered = ActivatorUtilities.GetServiceOrCreateInstance<unregistered>(_shared.Provider);
        _privateResult = _privateHost.Provider.GetRequiredService<helper>();
        ResolveSynchronously();
        var asyncScope = _shared.Provider.CreateAsyncScope();
        _async = asyncScope.ServiceProvider.GetRequiredService<async_lifetime>();
        await asyncScope.DisposeAsync();
        _policyReplacement = Catch.Exception(() => _shared.Registry.InstallFallbackPolicy(ContainerFallbackPolicy.Empty));
        _shared.Registry.Clear();
        _guardAfterClear = Catch.Exception(() => _shared.Provider.GetRequiredService<helper>());
    }

    void ResolveSynchronously()
    {
        using var scope = _shared.Provider.CreateScope();
        using var nested = scope.ServiceProvider.CreateScope();
        _sync = nested.ServiceProvider.GetRequiredService<sync_lifetime>();
        _keyedResults.Add(nested.ServiceProvider.GetRequiredKeyedService<helper>("key"));
    }

    void Destroy()
    {
        _shared.Dispose();
        _privateHost.Dispose();
    }

    [Fact] void should_preserve_framework_identity() => _frameworkResult.ShouldBeSame(_framework);
    [Fact] void should_preserve_keyed_identity_including_nested_scopes() => Assert.All(_keyedResults, result => result.ShouldBeSame(_keyed));
    [Fact] void should_create_unregistered_types_with_current_dependencies() => _unregistered.Tracker.ShouldBeSame(_shared.Current);
    [Fact] void should_leave_non_shared_host_fallback_unchanged() => _privateResult.Tracker.ShouldBeSame(_privateHost.First);
    [Fact] void should_dispose_synchronous_scoped_services_exactly_once() => _sync.Disposals.ShouldEqual(1);
    [Fact] void should_dispose_asynchronous_scoped_services_exactly_once() => _async.Disposals.ShouldEqual(1);
    [Fact] void should_not_allow_replacing_the_installed_policy() => _policyReplacement.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_retain_the_policy_after_clear() => _guardAfterClear.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_construct_a_shared_helper_from_the_first_context() => _shared.First.Constructions.ShouldEqual(0);

    internal sealed class unregistered(tracker tracker)
    {
        public tracker Tracker { get; } = tracker;
    }

    internal sealed class sync_lifetime : IDisposable
    {
        public int Disposals { get; private set; }
        public void Dispose() => Disposals++;
    }

    internal sealed class async_lifetime : IAsyncDisposable
    {
        public int Disposals { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
