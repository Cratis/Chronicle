// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_two_silos_bootstrap_the_administrator : Specification
{
    IUserStorage _users;
    IEventSequence _eventLog;
    AuthenticationService _first;
    AuthenticationService _second;
    List<EventSourceId> _attemptedUserIds;

    void Establish()
    {
        _users = Substitute.For<IUserStorage>();
        _users.GetAll().Returns([]);
        _eventLog = Substitute.For<IEventSequence>();
        _attemptedUserIds = [];
        var initialAppendCount = 0;
        _eventLog.Append(
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventType>(),
            Arg.Any<JsonObject>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<ConcurrencyScope>()).Returns(callInfo =>
            {
                _attemptedUserIds.Add(callInfo.ArgAt<EventSourceId>(1));
                initialAppendCount++;
                return initialAppendCount == 1
                    ? AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First)
                    : AppendResult.Failed(
                        CorrelationId.New(),
                        new ConcurrencyViolation(
                            callInfo.ArgAt<EventSourceId>(1),
                            EventSequenceNumber.BeforeFirst,
                            EventSequenceNumber.First));
            });

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventLog);
        var serializer = Substitute.For<IEventSerializer>();
        serializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        var options = Options.Create(new Configuration.ChronicleOptions());
        var applications = Substitute.For<IApplicationStorage>();
        _first = new AuthenticationService(
            _users,
            applications,
            grainFactory,
            options,
            serializer,
            NullLogger<AuthenticationService>.Instance);
        _second = new AuthenticationService(
            _users,
            applications,
            grainFactory,
            options,
            serializer,
            NullLogger<AuthenticationService>.Instance);
    }

    async Task Because()
    {
        await _first.EnsureDefaultAdminUser();
        await _second.EnsureDefaultAdminUser();
    }

    [Fact] void should_use_one_deterministic_user_id() => _attemptedUserIds.Distinct().ShouldContainOnly([(EventSourceId)UserId.InitialAdministrator]);
    [Fact] void should_attempt_the_same_event_source_from_both_silos() => _attemptedUserIds.Count.ShouldEqual(2);
}
