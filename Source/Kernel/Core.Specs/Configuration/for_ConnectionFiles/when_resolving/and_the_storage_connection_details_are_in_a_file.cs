// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_the_storage_connection_details_are_in_a_file : given.connection_files
{
    const string ConnectionDetails = "mongodb://storage-from-file:27017";
    ChronicleOptions _options;

    void Establish()
    {
        Setting("Storage:Type", "MongoDB");
        Setting("Storage:ConnectionDetailsFile", WriteFile("storage", $"{ConnectionDetails}\n"));
    }

    void Because()
    {
        AddConfiguration();
        _options = Options();
    }

    [Fact] void should_use_the_connection_details_in_the_file() => _options.Storage.ConnectionDetails.ShouldEqual(ConnectionDetails);
}
