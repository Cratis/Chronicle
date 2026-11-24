// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_GenerationProtectionSchemas.when_resolving;

public class and_the_schema_is_recursive : Specification
{
    const string Definition = """{"type":"object","properties":{"Name":{"type":"string"},"Next":{"$ref":"#/definitions/Node"}}}""";

    JsonSchema _own;
    JsonSchema _higher;
    JsonSchema _effective;
    bool _completed;

    void Establish()
    {
        _own = JsonSchema.FromJson(Schema());
        _higher = JsonSchema.FromJson(Schema());
    }

    void Because()
    {
        var thread = new Thread(() => _effective = GenerationProtectionSchemas.For(1, [((EventTypeGeneration)1, _own), ((EventTypeGeneration)2, _higher)]), 256 * 1024);
        thread.Start();
        _completed = thread.Join(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_complete() => _completed.ShouldBeTrue();
    [Fact] void should_return_the_registered_instance() => _effective.ShouldBeSame(_own);

    static string Schema() =>
        """{"type":"object","properties":{"Name":{"type":"string"},"Next":{"$ref":"#/definitions/Node"}},"definitions":{"Node":""" + Definition + "}}";
}
