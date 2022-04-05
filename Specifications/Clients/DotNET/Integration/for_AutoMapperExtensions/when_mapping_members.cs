// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Aksio.Cratis.Integration.for_AutoMapperExtensions;

public class when_mapping_members : Specification
{
    IMapper mapper;

    SourceAsClass source;
    DestinationAsClass result;

    void Establish()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg
                .CreateMap<SourceAsClass, DestinationAsClass>()
                .MapMember(_ => _.DestinationInteger, _ => _.SourceInteger)
                .MapMember(_ => _.DestinationString, _ => _.SourceString);
        });

        mapper = configuration.CreateMapper();
        source = new()
        {
            SourceInteger = 42,
            SourceString = "Forty Two"
        };
    }

    void Because() => result = mapper.Map<DestinationAsClass>(source);

    [Fact] void should_map_integer_value() => result.DestinationInteger.ShouldEqual(source.SourceInteger);
    [Fact] void should_map_string_value() => result.DestinationString.ShouldEqual(source.SourceString);
}
