// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleMongoDBDistributedApplicationBuilderExtensions.given;

public class a_distributed_application_builder : Specification
{
    protected IDistributedApplicationBuilder _builder;

    void Establish() => _builder = DistributedApplication.CreateBuilder([]);

    protected static async Task<IEnumerable<string>> ArgumentsFor(IResource resource)
    {
        var args = new List<object>();
        var context = new CommandLineArgsCallbackContext(args);
        foreach (var annotation in resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }
        return args.Select(_ => _.ToString()!);
    }
}
