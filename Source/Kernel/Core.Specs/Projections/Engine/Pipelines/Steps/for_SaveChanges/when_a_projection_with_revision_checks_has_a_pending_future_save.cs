// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_a_projection_with_revision_checks_has_a_pending_future_save : given.a_save_changes_step
{
    Exception _exception;

    void Establish()
    {
        _projection.SupportsFencedGlobalWrites.Returns(true);
        var futureChangeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        futureChangeset.HasChanges.Returns(true);
        futureChangeset.Changes.Returns([]);
        _context.AddPendingFutureSave(new Key("future-key", ArrayIndexers.NoIndexers), futureChangeset, ProjectionFutureId.New());
    }

    async Task Because() => _exception = await Catch.Exception(async () => await CreateStep(guardWritesOnWatermark: true).Perform(_projection, _context));

    [Fact] void should_fail_with_a_fenced_write_not_supported_exception() => _exception.ShouldBeOfExactType<FencedWriteNotSupportedException>();
    [Fact] void should_write_nothing() => _sink.WriteModes.ShouldBeEmpty();
}
