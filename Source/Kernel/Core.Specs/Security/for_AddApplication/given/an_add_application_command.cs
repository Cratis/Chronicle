// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Security.for_AddApplication.given;

public class an_add_application_command : Specification
{
    protected static readonly Guid ApplicationIdentifier = Guid.Parse("7f4a3d1c-6f4e-4d0a-9c1b-2b3c4d5e6f70");
    protected const string ClientIdentifier = "some-client";
    protected const string Secret = "some-secret";

    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IApplicationStorage _applications;
    protected IEventSequence _eventLog;
    protected AddApplication _command;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _applications = Substitute.For<IApplicationStorage>();
        _storage.System.Applications.Returns(_applications);

        _eventLog = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventLog);
        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>()).Returns(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));

        _command = new AddApplication(ApplicationIdentifier, ClientIdentifier, Secret);
    }
}
