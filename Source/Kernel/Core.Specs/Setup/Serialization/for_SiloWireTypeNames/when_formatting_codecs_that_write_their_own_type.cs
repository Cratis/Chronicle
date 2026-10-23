// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Serialization.TypeSystem;

namespace Cratis.Chronicle.Setup.Serialization.for_SiloWireTypeNames;

/// <summary>
/// These codecs write their own type name into every value they serialize, so a silo can only read a value from
/// another silo that names the codec the same way. Moving or renaming one is a break between releases: rolling upgrades
/// from 19.4 to 19.13 failed when the concept codec moved from Cratis.Chronicle.Core into Cratis.Orleans (#4325). This
/// is where such a move shows up first. Change an expected name only in a major release.
/// </summary>
public class when_formatting_codecs_that_write_their_own_type : Specification
{
    TypeConverter _converter;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSerializer(builder => builder.Services.AddConceptSerializer());
        _converter = services.BuildServiceProvider().GetRequiredService<TypeConverter>();
    }

    [Fact] void should_name_the_concept_codec_as_released() => _converter.Format(typeof(ConceptSerializer)).ShouldEqual("Cratis.Orleans.Serialization.ConceptSerializer,Cratis.Orleans");
    [Fact] void should_name_the_one_of_codec_as_released() => _converter.Format(typeof(OneOfSerializer)).ShouldEqual("Cratis.Orleans.Serialization.OneOfSerializer,Cratis.Orleans");
}
