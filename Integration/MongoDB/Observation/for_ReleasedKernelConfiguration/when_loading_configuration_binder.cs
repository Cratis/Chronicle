// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelConfiguration;

public class when_loading_configuration_binder : Cratis.Specifications.Specification
{
    ReleasedConfigurationLoadContext _context;
    Assembly _assembly;

    void Establish() => _context = new(Path.Combine(AppContext.BaseDirectory, Guid.NewGuid().ToString("N")));

    void Because() => _assembly = _context.LoadFromAssemblyName(typeof(ConfigurationBinder).Assembly.GetName());

    [Fact] void should_share_the_default_binder_identity() => _assembly.ShouldBeSame(typeof(ConfigurationBinder).Assembly);
    [Fact] void should_load_binder_in_the_default_context() => AssemblyLoadContext.GetLoadContext(_assembly).ShouldBeSame(AssemblyLoadContext.Default);

    void Destroy() => _context.Unload();
}
