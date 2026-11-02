// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_notifying;

public class and_there_are_no_subscribers : Specification
{
    TestKitSilo _silo;
    ProjectionChangesetNotifier _notifier;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _silo.AddService(Substitute.For<ILogger<ProjectionChangesetNotifier>>());
        _notifier = await _silo.CreateGrainAsync<ProjectionChangesetNotifier>("the-projection");
    }

    async Task Because() => await _notifier.Notify(
        "the-namespace",
        "parent",
        new JsonObject { ["value"] = "first" },
        new(ReadModelChangeType.Added, 1, DateTimeOffset.UnixEpoch, CorrelationId.New()));

    [Fact] void should_deactivate_when_idle() => _silo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
}
