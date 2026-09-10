// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using KernelRequestCausation = KernelCore::Cratis.Chronicle.Sequences.RequestCausation;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Builds the Arc <see cref="ICommandPipeline"/> the in-process kernel service implementations dispatch
/// commands through.
/// </summary>
/// <remarks>
/// The kernel's service implementations execute commands through the same pipeline the HTTP surface runs,
/// resolving each command handler's parameters from the pipeline's service provider. In-process scenarios
/// wrap those implementations, so the pipeline here carries the in-memory replacements those parameters
/// resolve from - the in-process grain factory, the in-memory storage, and the accessors a real request
/// would have provided.
/// </remarks>
internal static class InProcessCommandPipeline
{
    /// <summary>
    /// Creates an <see cref="ICommandPipeline"/> backed by the given in-memory kernel collaborators.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> command handlers append through.</param>
    /// <param name="storage">The <see cref="IStorage"/> command handlers read and write.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="configure">Optional callback registering additional services command handlers resolve.</param>
    /// <returns>The <see cref="ICommandPipeline"/>.</returns>
    internal static ICommandPipeline Create(
        IGrainFactory grainFactory,
        IStorage storage,
        JsonSerializerOptions jsonSerializerOptions,
        Action<IServiceCollection>? configure = default)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.Configure<ArcOptions>(_ => { });
        services.AddCratisArcCore();
        services.AddSingleton<IInstancesOf<ICommandExecutionScope>>(new KnownInstancesOf<ICommandExecutionScope>());
        services.AddSingleton(grainFactory);
        services.AddSingleton(storage);
        services.AddSingleton(jsonSerializerOptions);

        // One accessor instance serves both the request causation and anything else that asks for
        // IHttpContextAccessor. AddCratisArcCore() discovers the host application's own filters and
        // handlers through type discovery - in a test host that means the application under test's
        // types, whose construction can depend on the accessor exactly the way it does behind a real
        // request. Without this registration, any application type taking IHttpContextAccessor fails
        // to activate here with "Unable to resolve service for type
        // 'Microsoft.AspNetCore.Http.IHttpContextAccessor'" - which is what broke every Arc command
        // scenario in consuming applications (e.g. Cratis/Stagehand: 341 of 4411 specs).
        var httpContextAccessor = new HttpContextAccessor();
        services.AddSingleton(httpContextAccessor);
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        services.AddSingleton(new KernelRequestCausation(httpContextAccessor));
        services.AddSingleton<ICurrentPrincipalAccessor>(new InProcessCurrentPrincipalAccessor());
        configure?.Invoke(services);

        return services.BuildServiceProvider().GetRequiredService<ICommandPipeline>();
    }
}
