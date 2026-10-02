// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_the_prefix_makes_the_name_invalid : Specification
{
    Exception _invalidCharacter;
    Exception _tooLong;

    void Because()
    {
        _invalidCharacter = Catch.Exception(() => DatabaseNames.WithPrefix("chronicle", "bad/"));
        _tooLong = Catch.Exception(() => DatabaseNames.ForEventStore(new EventStoreName("Ada"), new string('x', 60)));
    }

    [Fact] void should_reject_invalid_prefix_characters() => _invalidCharacter.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_validate_the_length_including_the_prefix() => _tooLong.ShouldBeOfExactType<InvalidDatabaseName>();
}
