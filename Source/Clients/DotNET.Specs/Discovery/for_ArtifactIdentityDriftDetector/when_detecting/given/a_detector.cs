// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Discovery.for_ArtifactIdentityDriftDetector.when_detecting.given;

public class a_detector : Specification
{
    protected IChronicleServicesAccessor _services;
    protected ILogger _logger;
    private protected ArtifactIdentityDriftDetector _detector;
    protected DiscoveryReport _current;
    protected IImmutableList<ArtifactIdentityDrift> _result;

    void Establish()
    {
        _services = Substitute.For<IChronicleServicesAccessor>();
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _services.Services.Observers.GetObservers(Arg.Any<AllObserversRequest>(), Arg.Any<CallContext>())
            .Returns([new ObserverInformation { Id = "Original.SendEmail", EventSequenceId = "event-log", Type = ObserverType.Reactor, Owner = ObserverOwner.Client }]);
        _services.Services.EventTypes.AllEventTypes(Arg.Any<AllEventTypesRequest>(), Arg.Any<CallContext>())
            .Returns(QueryResult<IEnumerable<EventTypeDetailsResponse>>.Success(Guid.NewGuid(), []));
        _services.Services.Observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>(), Arg.Any<CallContext>())
            .Returns(new ObserverInformation { IsSubscribed = false });
        _current = new(true, ImmutableList.Create(new DiscoveredArtifact(ArtifactKind.Reactor, "Renamed.SendEmail", null)));
        _detector = new(_services, "Testing", "default", Substitute.For<Events.IEventTypes>(), _logger);
    }

    protected static bool LoggedAt(ILogger logger, LogLevel level) => logger.ReceivedCalls()
        .Any(_ => _.GetMethodInfo().Name == nameof(ILogger.Log) && Equals(_.GetArguments()[0], level));

    public class KernelUnavailable() : Exception("Kernel unavailable");
}
