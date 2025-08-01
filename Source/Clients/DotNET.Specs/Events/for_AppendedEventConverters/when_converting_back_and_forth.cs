// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Events.for_AppendedEventConverters;

public class when_converting_back_and_forth : Specification
{
    AppendedEvent _original;
    Contracts.Events.AppendedEvent _contract;
    AppendedEvent _roundTripped;

    void Establish()
    {
        var context = EventContext.EmptyWithEventSourceId(Guid.NewGuid()) with { SequenceNumber = 42 };
        var content = new ExpandoObject();
        _original = new(context, content);

        _contract = _original.ToContract();
    }

    void Because() => _roundTripped = _contract.ToClient();

    [Fact] void should_preserve_context() => _roundTripped.Context.ShouldEqual(_original.Context);
    [Fact] void should_preserve_content() => _roundTripped.Content.ShouldEqual(_original.Content);
}
