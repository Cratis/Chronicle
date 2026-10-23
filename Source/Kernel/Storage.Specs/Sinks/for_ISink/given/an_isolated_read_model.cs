// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.given;

public abstract class an_isolated_read_model<THarness> : an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    protected static ExpandoObject State(int count)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = count;
        return state;
    }

    protected Task Seed(EventSequenceNumber watermark) => _sink.ApplyChanges(_key, ChangesetSettingCountTo(10), watermark);
}
