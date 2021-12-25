// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cratis.Events.Store.MongoDB
{
    public class EventLogBatchContainer : IBatchContainer
    {
        readonly IEnumerable<object> _events;
        readonly IDictionary<string, object> _requestContext;

        public EventLogBatchContainer(StreamSequenceToken token, Guid streamGuid, IEnumerable<object> events, IDictionary<string, object> requestContext)
        {
            StreamGuid = streamGuid;
            _events = events;
            _requestContext = requestContext;
            SequenceToken = token;
        }

        public Guid StreamGuid { get; }

        public string StreamNamespace => null!;

        public StreamSequenceToken SequenceToken { get; }

        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
        {
            return _events.Cast<T>().Select(_ => new Tuple<T, StreamSequenceToken>(_, new EventSequenceToken(0))).ToArray();
        }

        public bool ImportRequestContext()
        {
            foreach (var (key, value) in _requestContext)
            {
                RequestContext.Set(key, value);
            }
            return true;
        }

        public bool ShouldDeliver(IStreamIdentity stream, object filterData, StreamFilterPredicate shouldReceiveFunc) => true;
    }
}
