// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_the_file_is_a_directory : given.connection_files
{
    string _path;
    Exception _error;

    void Establish()
    {
        _path = Path.Combine(_directory, "not-a-file");
        Directory.CreateDirectory(_path);
        Setting("Storage:ConnectionDetailsFile", _path);
    }

    void Because() => _error = Catch.Exception(AddConfiguration);

    [Fact] void should_stop_startup() => _error.ShouldBeOfExactType<InvalidConnectionFile>();
    [Fact] void should_name_the_setting() => _error.Message.ShouldContain("'storage.connectionDetailsFile'");
    [Fact] void should_name_the_path() => _error.Message.ShouldContain(_path);
}
