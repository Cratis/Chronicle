// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

internal sealed class fallback_context : IDisposable
{
    public MutableServiceRegistry Registry { get; } = new();
    public tracker First { get; } = new();
    public tracker Current { get; } = new();
    public IServiceProvider Provider { get; private set; }

    public void Build(Action<IServiceCollection, tracker> configure, bool shared = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Registry);
        services.AddSingleton(First);
        configure(services, First);
        Provider = new FallbackServiceProviderFactory(shared ? typeof(fallback_context).Assembly : null).CreateServiceProvider(services);
    }

    public void Advance()
    {
        Registry.Clear();
        Registry.Update([ServiceDescriptor.Singleton(Current)]);
    }

    public void Dispose() => (Provider as IDisposable)?.Dispose();

    public sealed class tracker
    {
        public int Constructions { get; set; }
    }

    public sealed class helper
    {
        public helper(tracker tracker)
        {
            Tracker = tracker;
            tracker.Constructions++;
        }

        public tracker Tracker { get; }
    }

    public sealed class free;

    public class generic<T>;

    public sealed class dependent_generic<T> : generic<T>
    {
        public dependent_generic(tracker tracker) => tracker.Constructions++;
    }
}
