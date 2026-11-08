// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_a_named_sink_connection_details_are_in_a_file : given.connection_files
{
    const string ConnectionDetails = "Host=reporting-from-file;Database=reporting";
    ChronicleOptions _options;

    void Establish()
    {
        Setting("Sinks:ReadModels", "reporting");
        Setting("Sinks:Configurations:reporting:Type", "SQL");
        Setting("Sinks:Configurations:reporting:ConnectionDetailsFile", WriteFile("reporting", ConnectionDetails));
        Setting("Sinks:Configurations:primary:Type", "MongoDB");
        Setting("Sinks:Configurations:primary:ConnectionDetails", "mongodb://primary:27017");
    }

    void Because()
    {
        AddConfiguration();
        _options = Options();
    }

    [Fact] void should_use_the_connection_details_in_the_file_for_that_sink() => _options.Sinks.Configurations["reporting"].ConnectionDetails.ShouldEqual(ConnectionDetails);
    [Fact] void should_keep_the_connection_details_of_other_sinks() => _options.Sinks.Configurations["primary"].ConnectionDetails.ShouldEqual("mongodb://primary:27017");
}
