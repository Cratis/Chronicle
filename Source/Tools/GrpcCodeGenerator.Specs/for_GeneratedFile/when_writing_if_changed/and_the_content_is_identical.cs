// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_GeneratedFile.when_writing_if_changed;

public class and_the_content_is_identical : given.an_output_file
{
    readonly DateTime _timestamp = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    void Establish()
    {
        File.WriteAllText(_path, "generated");
        File.SetLastWriteTimeUtc(_path, _timestamp);
    }

    void Because() => _written = GeneratedFile.WriteIfChanged(_path, "generated");

    [Fact] void should_preserve_the_timestamp() => File.GetLastWriteTimeUtc(_path).ShouldEqual(_timestamp);
    [Fact] void should_report_that_it_did_not_write() => _written.ShouldBeFalse();
}
