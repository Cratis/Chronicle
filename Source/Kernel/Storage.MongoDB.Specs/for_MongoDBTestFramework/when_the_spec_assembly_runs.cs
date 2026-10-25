// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBTestFramework;

public class when_the_spec_assembly_runs : Specification
{
    TestFrameworkAttribute[] _frameworks = [];
    bool _perFactLifecycle;

    void Because()
    {
        var assembly = typeof(MongoDBTestFramework).Assembly;
        _frameworks = assembly.GetCustomAttributes<TestFrameworkAttribute>().ToArray();
        _perFactLifecycle = assembly.IsDefined(typeof(PerFactSpecificationLifecycleAttribute));
    }

    [Fact] void should_register_one_test_framework() => _frameworks.Length.ShouldEqual(1);
    [Fact] void should_register_the_mongodb_test_framework() => _frameworks.Single().FrameworkType.ShouldEqual(typeof(MongoDBTestFramework));
    [Fact] void should_derive_from_the_specification_test_framework() => typeof(SpecificationTestFramework).IsAssignableFrom(typeof(MongoDBTestFramework)).ShouldBeTrue();
    [Fact] void should_not_opt_the_assembly_out_of_the_shared_lifecycle() => _perFactLifecycle.ShouldBeFalse();
}
