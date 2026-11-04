// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_PropertyValidator.when_validating_context_paths;

public class and_the_path_is_invalid : Specification
{
    Dictionary<string, Exception?> _results;
    string[] _paths;

    void Establish() => _paths = ["Occurred.Unknown", "Occurred.Unknown()", "Missing.Week", "SequenceNumber.Week", "Occurred.Day.Week", "Occurred.Week.Day", "Occurred.Week().Day", "Occurred.Week(1)", "SequenceNumber.WeekYear", "Occurred.WeekYear.Day", "Week", "Occurred."];

    void Because() => _results = _paths.ToDictionary(path => path, path => Catch.Exception(() => PropertyValidator.ValidateContextPropertyPath<EventContext>(path)));

    [Fact] void should_reject_every_invalid_path() => _results.Values.ShouldEachConformTo(error => error is InvalidPropertyForType);
    [Fact] void should_include_the_original_path() => _results.ShouldEachConformTo(result => result.Value.Message.Contains(result.Key));
    [Fact] void should_include_the_root_type() => _results.Values.ShouldEachConformTo(error => error.Message.Contains(typeof(EventContext).FullName));
}
