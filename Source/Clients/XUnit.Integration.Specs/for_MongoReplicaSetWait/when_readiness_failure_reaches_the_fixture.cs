// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoReplicaSetWait;

public class when_readiness_failure_reaches_the_fixture : Specification
{
    readonly MongoReplicaSetReadinessFailed _readinessFailure = new("MongoDB readiness deadline exhausted");
    Exception _failure;
    int _builds;
    int _starts;
    int _disposals;

    void Because()
    {
        var hooks = new FixtureLifecycleHooks
        {
            BeforeBuild = () => _builds++,
            Start = (_, _) =>
            {
                _starts++;
                return Task.FromException(_readinessFailure);
            },
            Dispose = (_, _) =>
            {
                _disposals++;
                return ValueTask.CompletedTask;
            }
        };
        _failure = Catch.Exception(() => _ = new FailingFixture(hooks));
    }

    [Fact] void should_preserve_the_diagnostic_failure() => _failure.ShouldEqual(_readinessFailure);
    [Fact] void should_build_only_one_container() => _builds.ShouldEqual(1);
    [Fact] void should_attempt_start_only_once() => _starts.ShouldEqual(1);
    [Fact] void should_dispose_the_failed_container() => _disposals.ShouldEqual(1);

    sealed class FailingFixture(FixtureLifecycleHooks hooks) : ChronicleFixture(hooks)
    {
        protected override IContainer BuildContainer(INetwork network)
        {
            var container = Substitute.For<IContainer>();
            container.Image.Returns(new DockerImage("mongo:8.2"));
            return container;
        }
    }
}
