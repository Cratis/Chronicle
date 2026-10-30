// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_iso_week_keys_at_year_boundaries : given.a_resolver
{
    IDictionary<string, object> _january;
    IDictionary<string, object> _december;
    IDictionary<string, object> _previousYear;

    void Establish()
    {
        var contextResolver = new EventContextPropertyExpressionResolver();
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), Arg.Any<string>())
            .Returns(call => contextResolver.Resolve(call.ArgAt<string>(1)));
    }

    async Task Because()
    {
        _january = await Resolve(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _december = await Resolve(new DateTimeOffset(2024, 12, 30, 0, 0, 0, TimeSpan.Zero));
        _previousYear = await Resolve(new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact] void should_use_the_next_week_year_in_late_december() => _december["WeekYear"].ShouldEqual(2025);
    [Fact] void should_use_week_one_in_late_december() => _december["Week"].ShouldEqual(1);
    [Fact] void should_use_the_previous_week_year_in_early_january() => _previousYear["WeekYear"].ShouldEqual(2020);
    [Fact] void should_use_week_fifty_three_in_early_january() => _previousYear["Week"].ShouldEqual(53);
    [Fact] void should_keep_the_january_week_year() => _january["WeekYear"].ShouldEqual(2024);
    [Fact] void should_keep_the_january_week_number() => _january["Week"].ShouldEqual(1);
    [Fact] void should_not_merge_different_iso_weeks_of_the_same_calendar_year() => _december["WeekYear"].ShouldNotEqual(_january["WeekYear"]);
    [Fact] void should_leave_the_calendar_year_unchanged() => new EventContextPropertyExpressionResolver()
        .Resolve("$eventContext(Occurred.Year)")(EventOn(new DateTimeOffset(2024, 12, 30, 0, 0, 0, TimeSpan.Zero))).ShouldEqual(2024);

    async Task<IDictionary<string, object>> Resolve(DateTimeOffset occurred)
    {
        var result = await _resolver.Resolve(_projection, "$composite(WeekYear=$eventContext(Occurred.WeekYear),Week=$eventContext(Occurred.Week))", "id")(null!, null!, EventOn(occurred));
        return (IDictionary<string, object>)((ResolvedKey)result).Key.Value;
    }

    static AppendedEvent EventOn(DateTimeOffset occurred) => new(EventContext.Empty with { Occurred = occurred }, new());
}
