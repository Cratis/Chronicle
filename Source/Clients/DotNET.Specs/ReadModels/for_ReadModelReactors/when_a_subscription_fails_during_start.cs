// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_a_subscription_fails_during_start : for_ReadModels.given.reducer_watch_routes
{
    readonly Subject<ReadModelChangeset<OtherModel>> _healthyFrames = new();
    ReadModelReactors _reactors;
    ServiceProvider _servicesProvider;
    int _invocations;

    void Establish()
    {
        var error = new InvalidOperationException("Subscription failed before acknowledgment");
        _watcher.Subscribed.Returns(Task.FromException(error));
        _watcher.Observable.Returns(Observable.Throw<ReadModelChangeset<Model>>(error));
        _reducers.HasFor<OtherModel>().Returns(true);
        _reducers.GetHandlerForReadModelType(typeof(OtherModel)).Returns(_handler);
        var healthyWatcher = Substitute.For<IReadModelWatcher<OtherModel>>();
        healthyWatcher.Observable.Returns(_healthyFrames);
        _readModelWatcherManager.GetWatcher<OtherModel>().Returns(healthyWatcher);
        _eventStore.Reducers.Returns(_reducers);
        _eventStore.ReadModels.Returns(_readModels);
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ReadModelReactors.Returns([typeof(FailedReactor), typeof(HealthyReactor)]);
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(FailedReactor)).Returns(
            new ActivatedArtifact(new FailedReactor(), typeof(FailedReactor), Substitute.For<ILogger<ActivatedArtifact>>()));
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(HealthyReactor)).Returns(
            new ActivatedArtifact(new HealthyReactor(), typeof(HealthyReactor), Substitute.For<ILogger<ActivatedArtifact>>()));
        var invoker = Substitute.For<IReadModelReactorInvoker>();
        invoker.Invoke(_eventStore, Arg.Any<object>(), Arg.Any<ReadModelReactorMethod>(), Arg.Any<object>(), Arg.Any<Events.EventContext>(), Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                _invocations++;
                return Task.CompletedTask;
            });
        _servicesProvider = new ServiceCollection().BuildServiceProvider();
        _reactors = new(_eventStore, artifacts, activator, _servicesProvider, invoker, _jsonSerializerOptions, Substitute.For<ILogger<ReadModelReactors>>());
    }

    async Task Because()
    {
        await Task.Run(_reactors.Start).WaitAsync(TimeSpan.FromSeconds(10));
        _healthyFrames.OnNext(new("tenant", "other", new OtherModel(), false, ReadModelChangeType.Added, _context));
    }

    [Fact] void should_subscribe_later_reactors_despite_the_failed_acknowledgment() => _invocations.ShouldEqual(1);

    void Destroy()
    {
        _reactors.Dispose();
        _healthyFrames.Dispose();
        _servicesProvider.Dispose();
    }

    public record OtherModel;

    public class FailedReactor : IReadModelReactor
    {
        public void Added(Model model) { }
    }

    public class HealthyReactor : IReadModelReactor
    {
        public void Added(OtherModel model) { }
    }
}
