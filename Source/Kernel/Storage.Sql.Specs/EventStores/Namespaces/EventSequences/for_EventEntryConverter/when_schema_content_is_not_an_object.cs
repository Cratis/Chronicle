// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_schema_content_is_not_an_object : given.a_schema_aware_read
{
    readonly Dictionary<string, Exception> _exceptions = [];

    async Task Because()
    {
        foreach (var value in new[] { "[]", "42", "true", "\"text\"", "null" })
        {
            entry.Content = $$"""{"1":{{value}}}""";
            try
            {
                await Read();
            }
            catch (Exception exception)
            {
                _exceptions[value] = exception;
            }
        }
    }

    [Fact] void should_reject_every_non_object_value() => _exceptions.Count.ShouldEqual(5);
    [Fact] void should_preserve_the_array_failure() => _exceptions["[]"].GetType().ShouldEqual(typeof(InvalidOperationException));
    [Fact] void should_preserve_the_number_failure() => _exceptions["42"].GetType().ShouldEqual(typeof(InvalidOperationException));
    [Fact] void should_preserve_the_boolean_failure() => _exceptions["true"].GetType().ShouldEqual(typeof(InvalidOperationException));
    [Fact] void should_preserve_the_string_failure() => _exceptions["\"text\""].GetType().ShouldEqual(typeof(InvalidOperationException));
    [Fact] void should_preserve_the_null_failure() => _exceptions["null"].GetType().ShouldEqual(typeof(NullReferenceException));
    [Fact] void should_preserve_the_object_required_message() => _exceptions.Where(pair => pair.Key != "null").Select(pair => pair.Value.Message).Distinct().ShouldEqual(new[] { "The node must be of type 'JsonObject'." });
}
