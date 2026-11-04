// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_refreshing_the_shared_context : Specification
{
    readonly tracker _first = new();
    readonly tracker _second = new();
    readonly MutableServiceRegistry _registry = new();
    IServiceProvider _provider;
    TestClientArtifactsActivator _activator;
    Exception? _error;
    reactor _explicit;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_registry);
        services.AddSingleton(_first);
        services.AddTransient<reactor>();
        _registry.Update([ServiceDescriptor.Singleton(new reactor(_first))]);
        _provider = new FallbackServiceProviderFactory(typeof(reactor).Assembly).CreateServiceProvider(services);
        _activator = new TestClientArtifactsActivator(_provider, _registry, NullLoggerFactory.Instance);
        _activator.ActivateNonDisposable<reactor>(typeof(reactor)).AsT0.Deliver();
        _registry.Clear();
        _registry.Update([ServiceDescriptor.Singleton(_second)]);
    }

    void Because()
    {
        var result = _activator.ActivateNonDisposable<reactor>(typeof(reactor));
        result.TryGetException(out _error);
        _explicit = new reactor(_second);
        _registry.Update([ServiceDescriptor.Singleton(_explicit)]);
        _activator.ActivateNonDisposable<reactor>(typeof(reactor)).AsT0.Deliver();
    }

    void Destroy() => (_provider as IDisposable)?.Dispose();

    [Fact] void should_report_artifact_activation_failure() => _error.ShouldBeOfExactType<ClientArtifactActivationFailed>();
    [Fact] void should_explain_current_context_registration() => Assert.Contains("ConfigureServices", _error?.InnerException?.Message ?? string.Empty);
    [Fact] void should_name_the_rejected_service() => Assert.Contains(typeof(reactor).FullName!, _error?.InnerException?.Message ?? string.Empty);
    [Fact] void should_not_construct_a_stale_reactor() => _first.Constructions.ShouldEqual(1);
    [Fact] void should_deliver_only_once_to_the_first_context() => _first.Deliveries.ShouldEqual(1);
    [Fact] void should_deliver_to_the_explicit_current_reactor() => _second.Deliveries.ShouldEqual(1);

    public sealed class tracker
    {
        public int Constructions { get; set; }
        public int Deliveries { get; set; }
    }

    public sealed class reactor
    {
        readonly tracker _tracker;

        public reactor(tracker tracker)
        {
            _tracker = tracker;
            tracker.Constructions++;
        }

        public void Deliver() => _tracker.Deliveries++;
    }
}
