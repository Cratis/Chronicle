// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Reactors.for_EventHandlerMethods.when_checking_if_a_method_has_an_event_handler_shape;

public class and_it_is_not_shaped_like_a_handler : Specification
{
    static readonly Type[] _eventTypes = [typeof(TheEvent)];

    IDictionary<string, bool> _results;

    void Because() => _results = typeof(TheReactor)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .ToDictionary(method => method.Name, method => method.HasEventHandlerShape(_eventTypes));

    [Fact] void should_not_have_the_shape_without_parameters() => _results["NoParameters"].ShouldBeFalse();
    [Fact] void should_not_have_the_shape_when_an_unknown_type_comes_first() => _results["UnknownTypeFirst"].ShouldBeFalse();
    [Fact] void should_not_have_the_shape_for_an_object_parameter() => _results["ObjectParameter"].ShouldBeFalse();
    [Fact] void should_not_have_the_shape_for_a_special_name_method() => _results["get_Item"].ShouldBeFalse();

    public record TheEvent(string Something);

    public class TheReactor
    {
#pragma warning disable CA1043 // The indexer exists to produce a special-name method.
        public int this[TheEvent @event] => 0;
#pragma warning restore CA1043

        public int NoParameters() => 0;
        public int UnknownTypeFirst(string something, TheEvent @event) => 0;
        public int ObjectParameter(object something) => 0;
    }
}
