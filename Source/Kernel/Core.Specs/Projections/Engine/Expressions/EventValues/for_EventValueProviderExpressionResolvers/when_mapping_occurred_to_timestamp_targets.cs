// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_mapping_occurred_to_timestamp_targets : given.an_appended_event
{
    [Theory]
    [InlineData("date-time", false, typeof(DateTime))]
    [InlineData("date-time?", true, typeof(DateTime))]
    [InlineData("date-time-offset", false, typeof(DateTimeOffset))]
    [InlineData("date-time-offset?", true, typeof(DateTimeOffset))]
    [InlineData("date", false, typeof(DateOnly))]
    [InlineData("date?", true, typeof(DateOnly))]
    [InlineData("time", false, typeof(TimeOnly))]
    [InlineData("time?", true, typeof(TimeOnly))]
    [InlineData(null, false, typeof(string))]
    [InlineData(null, true, typeof(string))]
    void should_preserve_the_utc_instant_or_invariant_offset_text(string? format, bool nullable, Type targetType)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            // The spec project runs in globalization-invariant mode, so use a custom non-invariant format.
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            culture.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            culture.DateTimeFormat.DateSeparator = "/";
            CultureInfo.CurrentCulture = culture;
            var timestamp = new DateTimeOffset(2024, 1, 2, 23, 4, 5, TimeSpan.FromHours(-7));
            @event = @event with { Context = @event.Context with { Occurred = timestamp } };
            var property = new JsonSchemaProperty
            {
                Type = nullable ? JsonObjectType.String | JsonObjectType.Null : JsonObjectType.String,
                Format = format
            };
            var resolvers = new EventValueProviderExpressionResolvers(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());

            var result = resolvers.Resolve(property, "$eventContext(Occurred)")(@event);

            result.GetType().ShouldEqual(targetType);
            var utc = timestamp.UtcDateTime;
            object expected = targetType switch
            {
                _ when targetType == typeof(DateTime) => utc,
                _ when targetType == typeof(DateTimeOffset) => timestamp,
                _ when targetType == typeof(DateOnly) => DateOnly.FromDateTime(utc),
                _ when targetType == typeof(TimeOnly) => TimeOnly.FromDateTime(utc),
                _ => timestamp.ToString("O", CultureInfo.InvariantCulture)
            };
            result.ShouldEqual(expected);
            if (targetType == typeof(DateTime))
            {
                ((DateTime)result).Kind.ShouldEqual(DateTimeKind.Utc);
            }
            if (targetType == typeof(DateTimeOffset))
            {
                ((DateTimeOffset)result).Offset.ShouldEqual(timestamp.Offset);
                ((DateTimeOffset)result).UtcTicks.ShouldEqual(timestamp.UtcTicks);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
