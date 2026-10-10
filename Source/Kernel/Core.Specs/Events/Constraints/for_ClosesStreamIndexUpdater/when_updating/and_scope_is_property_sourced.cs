// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamIndexUpdater.when_updating;

public class and_scope_is_property_sourced : given.a_closing_index
{
    ExpandoObject _content;

    void Establish()
    {
        _definition = _definition with { EventStreamIdFrom = "period" };
        _content = new();
        ((IDictionary<string, object?>)_content)["period"] = "previous-month";
    }

    async Task Because() => await new ClosesStreamIndexUpdater(_definition, ContextFor("Closed", _content), _storage).Update(EventSequenceNumber.First);

    [Fact] async Task should_close_the_payload_stream_not_the_append_stream() => (await _storage.GetAll()).Single().Scope.ShouldEqual(_scope with { EventStreamId = "previous-month" });
}
