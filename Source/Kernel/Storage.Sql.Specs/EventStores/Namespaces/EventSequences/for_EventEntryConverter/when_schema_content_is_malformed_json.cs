// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_schema_content_is_malformed_json : given.a_schema_aware_read
{
    Exception? _exception;

    void Establish() => entry.Content = "{\"1\":{";

    async Task Because()
    {
        try
        {
            await Read();
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_reject_invalid_json() => _exception!.GetType().ShouldEqual(typeof(JsonException));
    [Fact] void should_not_resolve_a_schema() => schema_resolutions.ShouldEqual(0);
}
