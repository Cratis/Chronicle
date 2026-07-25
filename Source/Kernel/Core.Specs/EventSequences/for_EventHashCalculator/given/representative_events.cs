// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator.given;

public class representative_events : Specification
{
    protected EventHashCalculator _calculator;
    protected EventTypeId _eventTypeId;
    protected EventSourceId _eventSourceId;

    protected ExpandoObject _empty;
    protected ExpandoObject _scalars;
    protected ExpandoObject _unicode;
    protected ExpandoObject _nested;
    protected ExpandoObject _arrays;
    protected ExpandoObject _unsortedKeys;

    void Establish()
    {
        _calculator = new EventHashCalculator();
        _eventTypeId = "the-event-type";
        _eventSourceId = "the-event-source";

        _empty = new ExpandoObject();

        _scalars = new ExpandoObject();
        var scalars = (IDictionary<string, object>)_scalars!;
        scalars["text"] = "hello";
        scalars["integer"] = 42;
        scalars["longValue"] = 9_000_000_000L;
        scalars["floating"] = 3.14d;
        scalars["decimalValue"] = 12.5m;
        scalars["flag"] = true;
        scalars["missing"] = null!;
        scalars["identifier"] = Guid.Parse("11112222-3333-4444-5555-666677778888");
        scalars["moment"] = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);

        _unicode = new ExpandoObject();
        var unicode = (IDictionary<string, object>)_unicode!;
        unicode["norwegian"] = "blåbærsyltetøy";
        unicode["japanese"] = "日本語";
        unicode["emoji"] = "🚀✨";
        unicode["quote"] = "he said \"hi\" & left";

        _nested = new ExpandoObject();
        var nested = (IDictionary<string, object>)_nested!;
        nested["name"] = "John";
        var address = new ExpandoObject();
        var addressDict = (IDictionary<string, object>)address!;
        addressDict["street"] = "Main Street";
        addressDict["city"] = "Oslo";
        nested["address"] = address;

        _arrays = new ExpandoObject();
        var arrays = (IDictionary<string, object>)_arrays!;
        arrays["tags"] = new[] { "developer", "architect", "lead" };
        arrays["numbers"] = new[] { 3, 1, 2 };
        var first = new ExpandoObject();
        ((IDictionary<string, object>)first!)["value"] = 1;
        var second = new ExpandoObject();
        ((IDictionary<string, object>)second!)["value"] = 2;
        arrays["items"] = new[] { first, second };

        _unsortedKeys = new ExpandoObject();
        var unsorted = (IDictionary<string, object>)_unsortedKeys!;
        unsorted["zebra"] = "z";
        unsorted["alpha"] = "a";
        unsorted["middle"] = "m";
    }
}
