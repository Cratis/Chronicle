// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_the_appended_generation_is_unknown : given.an_event_converter
{
    AppendedEvent _result;
    async Task Because() => _result = await _converter.ToAppendedEvent(CreateEvent());
    [Fact] void should_not_infer_an_appended_generation() => _result.Context.AppendedGeneration.ShouldBeNull();
}
