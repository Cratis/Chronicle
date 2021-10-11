// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Extensions.Dolittle.EventStore
{
    public record EventHorizon(bool FromEventHorizon, uint ExternalEventLogSequenceNumber, DateTimeOffset Received, Guid Consent);
}
