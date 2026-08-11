// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.given;

public class a_distributed_application_builder : Specification
{
    protected IDistributedApplicationBuilder _builder;

    void Establish() => _builder = DistributedApplication.CreateBuilder([]);

    protected static async Task<Dictionary<string, object>> EnvironmentFor(IResource resource)
    {
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            new Dictionary<string, object>());
        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }
        return context.EnvironmentVariables;
    }

    protected static IEnumerable<ContainerMountAnnotation> MountsFor(IResource resource) =>
        resource.Annotations.OfType<ContainerMountAnnotation>();
}
