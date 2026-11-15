// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_PageBoundsSplitting.given;

public class events : Specification
{
    protected static AppendedEvent EventWith(EventSequenceNumber sequenceNumber, int contentLength)
    {
        IDictionary<string, object?> content = new ExpandoObject();
        content["data"] = new string('x', contentLength);
        return new(EventContext.Empty with { SequenceNumber = sequenceNumber }, (ExpandoObject)content);
    }
}
