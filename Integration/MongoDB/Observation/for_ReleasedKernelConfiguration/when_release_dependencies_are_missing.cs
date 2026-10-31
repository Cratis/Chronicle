// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelConfiguration;

public class when_release_dependencies_are_missing : Cratis.Specifications.Specification
{
    ReleasedConfigurationLoadContext _context;
    Exception _cratisError;
    Exception _orleansError;

    void Establish() => _context = new(Path.Combine(AppContext.BaseDirectory, Guid.NewGuid().ToString("N")));

    void Because()
    {
        _cratisError = Catch.Exception(() => _context.LoadFromAssemblyName(typeof(ChronicleOptions).Assembly.GetName()));
        _orleansError = Catch.Exception(() => _context.LoadFromAssemblyName(typeof(IGrainFactory).Assembly.GetName()));
    }

    [Fact] void should_not_fall_back_to_head_cratis() => _cratisError.ShouldBeOfExactType<FileNotFoundException>();
    [Fact] void should_not_fall_back_to_host_orleans() => _orleansError.ShouldBeOfExactType<FileNotFoundException>();

    void Destroy() => _context.Unload();
}
