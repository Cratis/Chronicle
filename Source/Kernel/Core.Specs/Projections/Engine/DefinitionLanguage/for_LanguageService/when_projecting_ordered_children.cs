// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_projecting_ordered_children : given.a_runtime_language_service
{
    [Theory]
    [InlineData("insert first", "3,2,1")]
    [InlineData("insert last", "1,2,3")]
    [InlineData("sort by rank ascending", "3,1,2")]
    [InlineData("sort by rank descending", "1,2,3")]
    [InlineData("sort by details.rank ascending", "3,1,2")]
    public async Task should_preserve_insertion_order_after_round_trip_and_updates(string ordering, string expected)
    {
        await Compile($"""
            children notes identified by id
              {ordering}
              from Changed key id
                parent $eventSourceId
                name = name
                rank = rank
                details.rank = rank
            """);
        var state = new { id = "alert" }.AsExpandoObject();
        state = (await ProjectChild(new { id = 1, name = "first", rank = (double?)2 }, state)).CurrentState;
        state = (await ProjectChild(new { id = 2, name = "tie", rank = (double?)2 }, state)).CurrentState;
        state = (await ProjectChild(new { id = 3, name = "missing", rank = (double?)null }, state)).CurrentState;
        state = (await ProjectChild(new { id = 1, name = "updated", rank = (double?)0 }, state)).CurrentState;
        var notes = ((IEnumerable)((IDictionary<string, object?>)state)["notes"]!).Cast<IDictionary<string, object?>>();
        string.Join(',', notes.Select(note => note["id"])).ShouldEqual(expected);
    }
}
