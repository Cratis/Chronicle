// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTablePlacement.when_comparing_default_ports;

public class and_the_port_is_spelled_differently : Specification
{
    [Theory]
    [InlineData("Host=db;Database=rm;Username=u", "Host=db;Port=5432;Database=rm;Username=u")]
    [InlineData("Server=db;Database=rm;User Id=u", "Server=db,1433;Database=rm;User Id=u")]
    [InlineData("Server=db;Database=rm;User Id=u", "Server=tcp:db,1433;Database=rm;User Id=u")]
    [InlineData("Server=tcp:db;Database=rm;User Id=u", "Server=db;Database=rm;User Id=u")]
    void should_overlap(string left, string right) => Overlap(left, right).ShouldBeTrue();

    [Theory]
    [InlineData("Host=db;Database=rm;Username=u", "Host=db;Port=5433;Database=rm;Username=u")]
    [InlineData("Server=db;Database=rm;User Id=u", "Server=db,1434;Database=rm;User Id=u")]
    [InlineData("Server=db;Database=rm;User Id=u", "Server=db\\other;Database=rm;User Id=u")]
    void should_not_overlap(string left, string right) => Overlap(left, right).ShouldBeFalse();

    static bool Overlap(string left, string right)
    {
        var first = ReadModelTablePlacement.Of(left, "dbo");
        var second = ReadModelTablePlacement.Of(right, "dbo");
        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        return first.Value.Overlaps(second.Value);
    }
}
