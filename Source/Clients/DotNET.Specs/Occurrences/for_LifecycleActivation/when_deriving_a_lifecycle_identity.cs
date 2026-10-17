// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Occurrences.for_LifecycleActivation;

public class when_deriving_a_lifecycle_identity : Specification
{
    string _application;
    string _namespace;

    void Because()
    {
        _application = LifecycleIdentity.Calculate("startup", new("rollout"), null);
        _namespace = LifecycleIdentity.Calculate("shutdown", new("rollout"), "tenant");
    }

    [Fact] void should_hash_kind_and_activation() => _application.ShouldEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("7:startup|7:rollout|"))));
    [Fact] void should_include_the_namespace_for_fan_out() => _namespace.ShouldEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("8:shutdown|7:rollout|6:tenant|"))));
}
