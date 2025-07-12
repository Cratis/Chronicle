// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.for_DatabaseIsolation;

[Collection(ChronicleCollection.Name)]
public class when_creating_multiple_fixtures(ChronicleInProcessFixture chronicleInProcessFixture) : IntegrationSpecificationContext(chronicleInProcessFixture)
{
    [Fact]
    public void should_have_unique_identifier()
    {
        ChronicleFixture.UniqueId.ShouldNotBeNull();
        ChronicleFixture.UniqueId.Length.ShouldEqual(8);
    }

    [Fact]
    public void should_have_database_names_with_unique_prefix()
    {
        var eventStoreName = Constants.GetEventStore(ChronicleFixture.UniqueId);
        var eventStoreDatabaseName = Constants.GetEventStoreDatabaseName(ChronicleFixture.UniqueId);
        var eventStoreNamespaceDatabaseName = Constants.GetEventStoreNamespaceDatabaseName(ChronicleFixture.UniqueId);

        eventStoreName.Substring(0, 7).ShouldEqual("testing");
        eventStoreName.Length.ShouldEqual(15); // "testing" (7) + uniqueId (8)

        eventStoreDatabaseName.ShouldEqual($"{eventStoreName}-es");
        eventStoreNamespaceDatabaseName.ShouldEqual($"{eventStoreName}-es+Default");
    }

    [Fact]
    public void should_use_unique_database_names_in_fixture_properties()
    {
        var expectedEventStoreDb = Constants.GetEventStoreDatabaseName(ChronicleFixture.UniqueId);
        var expectedEventStoreNsDb = Constants.GetEventStoreNamespaceDatabaseName(ChronicleFixture.UniqueId);
        var expectedReadModelsDb = Constants.GetReadModelsDatabaseName(ChronicleFixture.UniqueId);

        // These properties internally use the Constants methods with the fixture's UniqueId
        EventStoreDatabase.Database.DatabaseNamespace.DatabaseName.ShouldEqual(expectedEventStoreDb);
        EventStoreForNamespaceDatabase.Database.DatabaseNamespace.DatabaseName.ShouldEqual(expectedEventStoreNsDb);
        ReadModelsDatabase.Database.DatabaseNamespace.DatabaseName.ShouldEqual(expectedReadModelsDb);
    }
}