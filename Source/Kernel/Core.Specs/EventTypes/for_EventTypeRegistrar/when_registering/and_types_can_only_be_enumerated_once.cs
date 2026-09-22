// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

/// <summary xml:lang="en">
/// Reproduces #86 - the wire-deserialized sequence backing a repeated gRPC field is not guaranteed
/// to support more than one enumeration, and Register used to enumerate the incoming registrations
/// once to validate and once to build what gets stored. A caller passing a single-use sequence saw
/// the second pass silently see nothing - no exception, no registration - while the command still
/// reported success.
/// </summary>
public class and_types_can_only_be_enumerated_once : given.all_dependencies
{
    Exception _exception;

    async Task Because() =>
        _exception = await Catch.Exception(async () => await _subject.Register(
            "test-store",
            SingleUseSequenceOf(
                new EventTypeRegistration
                {
                    Type = new() { Id = "some-event", Generation = 1 },
                    Schema = "{}"
                }),
            false,
            _storage,
            _eventTypesCacheClient,
            _patternCapture));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();

    [Fact] void should_still_register_the_event_type() =>
        _systemEventSequence.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == "some-event"),
            Arg.Is<EventTypeAdded>(@event => @event.EventTypeId.Value == "some-event"));

    /// <summary xml:lang="en">
    /// A sequence that throws on a second call to <see cref="IEnumerable{T}.GetEnumerator"/>, the same
    /// shape a single-pass, wire-backed deserialization can take.
    /// </summary>
    /// <param name="items">The items the sequence yields on its one and only enumeration.</param>
    /// <returns>A sequence that can be enumerated exactly once.</returns>
    static IEnumerable<EventTypeRegistration> SingleUseSequenceOf(params EventTypeRegistration[] items) =>
        new SingleUseEnumerable(items);

    sealed class SingleUseEnumerable(EventTypeRegistration[] items) : IEnumerable<EventTypeRegistration>
    {
        bool _enumerated;

        public IEnumerator<EventTypeRegistration> GetEnumerator()
        {
            if (_enumerated)
            {
                throw new InvalidOperationException("This sequence can only be enumerated once.");
            }

            _enumerated = true;
            return items.AsEnumerable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
