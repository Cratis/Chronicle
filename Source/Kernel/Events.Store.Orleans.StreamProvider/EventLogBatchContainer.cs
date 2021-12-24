// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Cratis.Events.Store.Orleans.StreamProvider
{
    public class EventLogBatchContainer : IBatchContainer
    {
        readonly IEnumerable<object> _events;

        public EventLogBatchContainer(Guid streamGuid, IEnumerable<object> events)
        {
            StreamGuid = streamGuid;
            _events = events;
        }

        public Guid StreamGuid { get; }

        public string StreamNamespace => "greetings";

        public StreamSequenceToken SequenceToken => new EventSequenceToken(0);

        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
        {
            return _events.Cast<T>().Select(_ => new Tuple<T, StreamSequenceToken>(_, new EventSequenceToken(0))).ToArray();
        }

        public bool ImportRequestContext() => true;

        public bool ShouldDeliver(IStreamIdentity stream, object filterData, StreamFilterPredicate shouldReceiveFunc) => true;

    }
}
