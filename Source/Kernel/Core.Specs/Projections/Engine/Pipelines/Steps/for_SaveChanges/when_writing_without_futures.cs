// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SaveChanges;

public class when_writing_without_futures : given.a_save_changes_step
{
    ISink _writingSink;

    void Establish() => _writingSink = Substitute.For<ISink>();

    async Task Because() => await new SaveChanges(_writingSink, Substitute.For<IChangesetStorage>(), true, Substitute.For<ILogger<SaveChanges>>()).Perform(_projection, _context);

    [Fact] void should_use_the_bufferable_write() => _writingSink.Received(1).ApplyChanges(_context.Key, _context.Changeset, _context.EventSequenceNumber, SinkWriteMode.OnlyWhenAdvancingWatermark);
    [Fact] void should_not_request_an_immediate_write() => _writingSink.DidNotReceive().ApplyChangesImmediately(_context.Key, _context.Changeset, _context.EventSequenceNumber, SinkWriteMode.OnlyWhenAdvancingWatermark);
}
