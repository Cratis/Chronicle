// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_the_file_does_not_exist : given.connection_files
{
    string _path;
    Exception _error;

    void Establish()
    {
        _path = Path.Combine(_directory, "missing");
        Setting("Sinks:Configurations:reporting:Type", "SQL");
        Setting("Sinks:Configurations:reporting:ConnectionDetailsFile", _path);
    }

    void Because() => _error = Catch.Exception(AddConfiguration);

    [Fact] void should_stop_startup() => _error.ShouldBeOfExactType<InvalidConnectionFile>();
    [Fact] void should_name_the_setting() => _error.Message.ShouldContain("'sinks.configurations.reporting.connectionDetailsFile'");
    [Fact] void should_name_the_path() => _error.Message.ShouldContain(_path);
}
