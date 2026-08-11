// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire.for_ChronicleAspireBuilderExtensions.given;

public class a_distributed_application_builder : Specification
{
    protected IDistributedApplicationBuilder _builder;
    protected string _appHostDirectory;

    void Establish()
    {
        _appHostDirectory = Path.Combine(Path.GetTempPath(), $"chronicle-apphost-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_appHostDirectory);
        _builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            ProjectDirectory = _appHostDirectory
        });
    }

    void Destroy()
    {
        if (Directory.Exists(_appHostDirectory))
        {
            Directory.Delete(_appHostDirectory, recursive: true);
        }
    }

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

    protected static ContainerMountAnnotation[] MountsFor(IResource resource, string containerPath) =>
        [.. MountsFor(resource).Where(_ => _.Target == containerPath)];

    protected string CertificateFileIn(string relativePath)
    {
        var path = InAppHostDirectory(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "The AppHost only checks that this file is there - it never reads it.");
        return relativePath;
    }

    protected string InAppHostDirectory(string relativePath) => Path.GetFullPath(relativePath, _appHostDirectory);
}
