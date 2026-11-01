// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    RegistrationPrecedence? _pendingPrecedence;
    string _incomingRegistrationVersion = string.Empty;

    async Task ReportOwnershipSwitch(DefinitionOwnershipSwitch change, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        if (change.Outcome == RegistrationPrecedenceOutcome.Unordered)
        {
            logger.UnorderedRegistrationSwitchedSequence(change.ObserverId, change.From, change.To, change.IncomingVersion, change.OwnerVersion);
            await ObserverRegistrationConflicts.Report(GrainFactory, _observerKey.EventStore, namespaces, new()
            {
                ObserverId = change.ObserverId,
                IncomingEventSequenceId = change.To,
                OwnerEventSequenceId = change.From,
                IncomingVersion = change.IncomingVersion,
                OwnerVersion = change.OwnerVersion,
                Outcome = "unordered"
            });
        }
        else
        {
            logger.RegistrationSwitchedSequence(change.ObserverId, change.From, change.To, change.Outcome);
        }
    }
}
