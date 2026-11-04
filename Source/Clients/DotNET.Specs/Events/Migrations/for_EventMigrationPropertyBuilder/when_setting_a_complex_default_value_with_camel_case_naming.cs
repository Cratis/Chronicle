// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder;

public class when_setting_a_complex_default_value_with_camel_case_naming : Specification
{
    record ImageResource(string Url, string? AltText)
    {
        public Guid? ImageId { get; init; }
    }

    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new EventMigrationPropertyBuilder(new CamelCaseNamingPolicy());

    void Because() => _builder.DefaultValue("image", new ImageResource(string.Empty, null));

    [Fact] void should_emit_the_value_in_payload_shape_keeping_nulls() =>
        _builder.Properties["image"]["$defaultValue"]!.ToJsonString().ShouldEqual("""{"url":"","altText":null,"imageId":null}""");
}
