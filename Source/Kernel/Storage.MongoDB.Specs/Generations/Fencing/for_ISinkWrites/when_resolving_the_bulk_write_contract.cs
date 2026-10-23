// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_ISinkWrites;

public class when_resolving_the_bulk_write_contract : Specification
{
    MethodInfo?[] _legacyMembers;
    MethodInfo _guardedMember;

    void Because()
    {
        Type[] legacyParameters = [typeof(IMongoCollection<BsonDocument>), typeof(IReadOnlyList<WriteModel<BsonDocument>>), typeof(CancellationToken)];
        Type[] implementations = [typeof(ISinkWrites), typeof(UnfencedSinkWrites), typeof(FencedSinkWrites)];
        _legacyMembers = implementations.Select(type => type.GetMethod(nameof(ISinkWrites.BulkWrite), legacyParameters)).ToArray();
        _guardedMember = typeof(ISinkWrites).GetMethod(nameof(ISinkWrites.BulkWrite), [legacyParameters[0], legacyParameters[1], typeof(IReadOnlySet<WriteModel<BsonDocument>>), typeof(CancellationToken)])!;
    }

    [Fact] void should_keep_the_original_interface_and_implementation_methods() => _legacyMembers.Count(member => member is not null).ShouldEqual(3);
    [Fact] void should_not_require_existing_implementers_to_add_a_method() => _guardedMember.IsAbstract.ShouldBeFalse();
}
