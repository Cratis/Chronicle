// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Security;
using ProtoBuf;

namespace Cratis.Chronicle.ExternalServices.for_ExternalService;

public class when_preserving_the_original_shape : Specification
{
    ExternalService _details;
    object?[] _deconstructed;
    readonly object[] _originalValues = ["service", "Service", ExternalServiceEndpointType.Http, "https://example.com", AuthorizationType.None, new Dictionary<string, string>(), "host", 1433, "database", "user", new Dictionary<string, string>()];

    void Because()
    {
        Type[] signature = [typeof(string), typeof(string), typeof(ExternalServiceEndpointType), typeof(string), typeof(AuthorizationType), typeof(IDictionary<string, string>), typeof(string), typeof(int), typeof(string), typeof(string), typeof(IDictionary<string, string>)];
        _details = (ExternalService)typeof(ExternalService).GetConstructor(signature)!.Invoke(_originalValues);
        var deconstruct = typeof(ExternalService).GetMethod(nameof(ExternalService.Deconstruct), signature.Select(_ => _.MakeByRefType()).ToArray())!;
        _deconstructed = new object?[11];
        deconstruct.Invoke(_details, _deconstructed);
    }

    [Fact] void should_keep_the_original_constructor_and_deconstruction() => _deconstructed.ShouldEqual(_originalValues);
    [Fact] void should_default_legacy_construction_to_no_metadata() => _details.HeaderValuesSet.ShouldBeEmpty();
    [Fact] void should_append_metadata_to_the_generation_constructor() => typeof(ExternalService).GetConstructors()[0].GetParameters().Length.ShouldEqual(12);
    [Fact] void should_append_metadata_at_field_twelve() => typeof(ExternalServiceResponse).GetProperty(nameof(ExternalService.HeaderValuesSet))!.GetCustomAttribute<ProtoMemberAttribute>()!.Tag.ShouldEqual(12);
    [Fact] void should_keep_options_at_field_eleven() => typeof(ExternalServiceResponse).GetProperty(nameof(ExternalService.Options))!.GetCustomAttribute<ProtoMemberAttribute>()!.Tag.ShouldEqual(11);
}
