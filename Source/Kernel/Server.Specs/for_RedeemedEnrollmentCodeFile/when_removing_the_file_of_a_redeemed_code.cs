// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_RedeemedEnrollmentCodeFile;

public class when_removing_the_file_of_a_redeemed_code : Specification
{
    string _directory;
    string _file;
    string _notAFile;
    bool _removed;
    bool _removedWhenItCannotBe;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"chronicle-code-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _file = Path.Combine(_directory, "code");
        File.WriteAllText(_file, "spent");

        // A directory with something in it cannot be deleted as a file, whoever the process runs as.
        _notAFile = Path.Combine(_directory, "directory");
        Directory.CreateDirectory(_notAFile);
        File.WriteAllText(Path.Combine(_notAFile, "inside"), "x");
    }

    void Destroy() => Directory.Delete(_directory, true);

    void Because()
    {
        _removed = RedeemedEnrollmentCodeFile.TryRemove(_file);
        _removedWhenItCannotBe = RedeemedEnrollmentCodeFile.TryRemove(_notAFile);
    }

    [Fact] void should_report_that_it_removed_the_file() => _removed.ShouldBeTrue();
    [Fact] void should_have_deleted_the_file() => File.Exists(_file).ShouldBeFalse();
    [Fact] void should_report_without_throwing_when_it_cannot_remove_it() => _removedWhenItCannotBe.ShouldBeFalse();
}
