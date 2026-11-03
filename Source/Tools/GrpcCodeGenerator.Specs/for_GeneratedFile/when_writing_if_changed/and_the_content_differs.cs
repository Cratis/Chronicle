// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_GeneratedFile.when_writing_if_changed;

public class and_the_content_differs : given.an_output_file
{
    void Establish() => File.WriteAllText(_path, "old");

    void Because() => _written = GeneratedFile.WriteIfChanged(_path, "generated");

    [Fact] void should_update_the_content() => File.ReadAllText(_path).ShouldEqual("generated");
    [Fact] void should_report_that_it_wrote() => _written.ShouldBeTrue();
}
