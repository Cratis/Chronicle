// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Extensions.Dolittle.EventStore
{
    public record EventStreamId(Guid Value) : ConceptAs<Guid>(Value)
    {
        public static readonly EventStreamId EventLog = new(Guid.Empty);
    }
}
