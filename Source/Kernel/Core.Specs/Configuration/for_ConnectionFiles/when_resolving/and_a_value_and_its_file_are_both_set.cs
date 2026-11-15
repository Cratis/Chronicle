// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionFiles.when_resolving;

public class and_a_value_and_its_file_are_both_set : given.connection_files
{
    const string FileContents = "mongodb://secret-from-file:27017";
    Exception _error;

    void Establish()
    {
        Setting("Storage:ConnectionDetails", "mongodb://localhost:27017");
        Setting("Storage:ConnectionDetailsFile", WriteFile("storage", FileContents));
    }

    void Because() => _error = Catch.Exception(AddConfiguration);

    [Fact] void should_stop_startup() => _error.ShouldBeOfExactType<InvalidConnectionFile>();
    [Fact] void should_name_the_setting() => _error.Message.ShouldContain("'storage.connectionDetails'");
    [Fact] void should_name_the_file_setting() => _error.Message.ShouldContain("'storage.connectionDetailsFile'");
    [Fact] void should_not_reveal_the_contents_of_the_file() => _error.Message.ShouldNotContain(FileContents);
}
