// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aspire.for_ChronicleContainerImageTags;

/// <summary>
/// Pins the published image coordinates as literals. Every other spec compares against these constants, so
/// without this a changed constant would rename the image the Aspire integration pulls while leaving the
/// whole suite green.
/// </summary>
public class when_referencing_the_published_images : Specification
{
    [Fact] void should_pull_from_docker_hub() => ChronicleContainerImageTags.Registry.ShouldEqual("docker.io");
    [Fact] void should_pull_the_cratis_chronicle_image() => ChronicleContainerImageTags.Image.ShouldEqual("cratis/chronicle");
    [Fact] void should_publish_the_production_image_as_latest() => ChronicleContainerImageTags.Tag.ShouldEqual("latest");
    [Fact] void should_publish_the_development_image_as_latest_development() => ChronicleContainerImageTags.DevelopmentTag.ShouldEqual("latest-development");
    [Fact] void should_publish_the_slim_development_image_as_latest_development_slim() => ChronicleContainerImageTags.DevelopmentSlimTag.ShouldEqual("latest-development-slim");
    [Fact] void should_pull_the_mongo_db_image() => ChronicleContainerImageTags.MongoDBImage.ShouldEqual("mongo");
    [Fact] void should_pull_the_pinned_mongo_db_tag() => ChronicleContainerImageTags.MongoDBTag.ShouldEqual("8.0");
}
