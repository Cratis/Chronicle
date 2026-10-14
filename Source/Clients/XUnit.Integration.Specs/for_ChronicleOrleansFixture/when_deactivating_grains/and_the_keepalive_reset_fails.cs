// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleOrleansFixture.when_deactivating_grains;

public class and_the_keepalive_reset_fails : Specification
{
    IServiceProvider _services;
    IManagementGrain _management;
    reset_fixture _fixture;
    OrleansException _failure;
    Exception _exception;

    void Establish()
    {
        _failure = new OrleansException("KeepAlive activation inventory failed");
        var grainFactory = Substitute.For<IGrainFactory>();
        _management = Substitute.For<IManagementGrain>();
        _management.GetDetailedGrainStatistics().Returns(Task.FromException<DetailedGrainStatistic[]>(_failure));
        grainFactory.GetGrain<IManagementGrain>(0).Returns(_management);
        _services = new ServiceCollection().AddSingleton(grainFactory).BuildServiceProvider();
        _fixture = new(Substitute.For<IResetChronicleFixture>(), new test_web_application_factory(_services));
    }

    async Task Because() => _exception = await Catch.Exception(_fixture.Deactivate);

    void Destroy()
    {
        _fixture.Close();
        (_services as IDisposable)?.Dispose();
    }

    [Fact] void should_surface_the_failed_reset_instead_of_wiping_again() => _exception.ShouldBeSame(_failure);
    [Fact] async Task should_not_collect_activations_after_a_failed_reset() => await _management.DidNotReceive().ForceActivationCollection(Arg.Any<TimeSpan>());

    public interface IResetChronicleFixture : IChronicleFixture;

    sealed class reset_fixture(IResetChronicleFixture fixture, test_web_application_factory factory) : ChronicleOrleansFixture<IResetChronicleFixture>(fixture)
    {
        public Task Deactivate() => DeactivateAllGrains();

        public void Close()
        {
            Dispose();
            _webApplicationFactory = null;
        }

        protected override IAsyncDisposable CreateWebApplicationFactory() => factory;
    }
}
