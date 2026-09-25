// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_a_shared_type;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_type_contains_collections : given.a_shared_type_generator
{
    IReadOnlyList<Diagnostic> _errors;
    object _message;

    void Because()
    {
        var code = _generator.GenerateSharedType(typeof(CollectionPayload), _outputDir);
        var result = for_ServiceImplementationGenerator.given.GeneratedSourceCompiler.Compile(code);
        _errors = result.Errors;
        if (result.Assembly is not null)
        {
            _message = Activator.CreateInstance(result.Assembly.GetTypes().Single(type => type.Name == nameof(CollectionPayload)))!;
        }
    }

    [Fact] void should_compile_without_errors() => _errors.ShouldBeEmpty();

    [Fact]
    void should_initialize_every_collection()
    {
        _message.ShouldNotBeNull();
        foreach (var property in _message.GetType().GetProperties())
        {
            var value = property.GetValue(_message);
            value.ShouldNotBeNull();
            ((IEnumerable)value!).ShouldBeEmpty();
        }
    }

    [Fact]
    void should_supply_mutable_enumerables_for_deserialization()
    {
        _message.ShouldNotBeNull();
        var values = (ICollection<string>)_message.GetType().GetProperty(nameof(CollectionPayload.Values))!.GetValue(_message)!;
        values.Add("value");
        values.ShouldContainOnly("value");
    }
}
