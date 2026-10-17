// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

public sealed class StartupHandler(IChronicleClient client, OccurrenceSignals signals) : IOccurrenceHandler<Startup>
{
    public Task Handle(Startup trigger, OccurrenceContext context, CancellationToken cancellationToken) => signals.Settle(client, context, cancellationToken);
}
