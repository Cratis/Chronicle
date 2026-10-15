// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintProvider;

public class when_providing_with_conflicting_modes : Specification
{
    UniqueConstraintProvider _provider;
    Exception _error;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.UniqueConstraints.Returns([typeof(FirstVersionRegistered), typeof(SecondVersionRegistered)]);
        _provider = new(artifacts, Substitute.For<IEventTypes>(), new CamelCaseNamingPolicy());
    }

    void Because() => _error = Catch.Exception(() => _provider.Provide());

    [Fact] void should_reject_conflicting_retention_modes() => _error.ShouldBeOfExactType<ConflictingUniqueConstraintModes>();

    record FirstVersionRegistered([property: Unique("version", Mode = UniqueConstraintMode.PerValue)] string VersionId);
    record SecondVersionRegistered([property: Unique("version")] string VersionId);
}
