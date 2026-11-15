// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_the_file_ends_with_a_newline : given.connection_files
{
    const string ConnectionDetails = "Host=pg;Database=reporting;Password=with spaces ";
    ChronicleOptions _options;

    void Establish()
    {
        Setting("Sinks:Configurations:reporting:Type", "SQL");
        Setting("Sinks:Configurations:reporting:ConnectionDetailsFile", WriteFile("reporting", $"{ConnectionDetails}\r\n"));
    }

    void Because()
    {
        AddConfiguration();
        _options = Options();
    }

    [Fact] void should_trim_only_the_trailing_newline() => _options.Sinks.Configurations["reporting"].ConnectionDetails.ShouldEqual(ConnectionDetails);
}
