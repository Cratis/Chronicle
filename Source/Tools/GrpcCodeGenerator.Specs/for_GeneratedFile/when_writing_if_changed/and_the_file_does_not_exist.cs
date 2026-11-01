// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_GeneratedFile.when_writing_if_changed;

public class and_the_file_does_not_exist : given.an_output_file
{
    void Because() => _written = GeneratedFile.WriteIfChanged(_path, "generated");

    [Fact] void should_write_the_content() => File.ReadAllText(_path).ShouldEqual("generated");
    [Fact] void should_report_that_it_wrote() => _written.ShouldBeTrue();
    [Fact] void should_write_utf8_without_a_bom() => File.ReadAllBytes(_path).ShouldContainOnly(System.Text.Encoding.UTF8.GetBytes("generated"));
}
