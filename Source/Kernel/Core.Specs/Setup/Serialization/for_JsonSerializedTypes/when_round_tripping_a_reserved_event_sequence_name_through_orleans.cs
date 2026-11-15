// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Sequences;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_JsonSerializedTypes;

public class when_round_tripping_a_reserved_event_sequence_name_through_orleans : Specification
{
    Exception _original;
    Exception _result;

    void Establish()
    {
        try
        {
            throw new ReservedEventSequenceName("observers");
        }
        catch (ReservedEventSequenceName ex)
        {
            _original = ex;
        }
    }

    void Because()
    {
        var services = new ServiceCollection();
        services.AddExceptionSerialization();
        services.AddSerializer(builder => builder.AddJsonSerializer(JsonSerializedTypes.Includes, new JsonSerializerOptions()));
        var serializer = services.BuildServiceProvider().GetRequiredService<Serializer>();
        _result = serializer.Deserialize<Exception>(serializer.SerializeToArray(_original));
    }

    [Fact] void should_keep_the_exception_type() => _result.ShouldBeOfExactType<ReservedEventSequenceName>();
    [Fact] void should_keep_the_message() => _result.Message.ShouldEqual(ReservedEventSequenceRules.Message("observers"));
    [Fact] void should_keep_the_validation_message() => ((IValidationFailure)_result).ValidationResult.Message.ShouldEqual(_result.Message);
    [Fact] void should_keep_the_validation_member() => ((IValidationFailure)_result).ValidationResult.Members.ShouldContain("eventSequenceId");
}
