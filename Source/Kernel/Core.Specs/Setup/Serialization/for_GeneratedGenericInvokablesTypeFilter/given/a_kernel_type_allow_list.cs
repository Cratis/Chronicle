// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Orleans.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Serialization;
using Orleans.Serialization.Configuration;
using Orleans.Serialization.TypeSystem;

namespace Cratis.Chronicle.Setup.Serialization.for_GeneratedGenericInvokablesTypeFilter.given;

public class a_kernel_type_allow_list : Specification
{
    protected TypeConverter _converter;
    protected GeneratedGenericInvokablesTypeFilter _filter;
    protected Type _subscribeInvokable;

    void Establish()
    {
        // The kernel's type filters, with the allow-all switch this spec assembly's manifest provider turns on switched back off.
        var services = new ServiceCollection()
            .AddSerializer(builder => builder.Services
                .AddSingleton<ITypeFilter, CratisTypesFilter>()
                .AddSingleton<ITypeFilter, GeneratedGenericInvokablesTypeFilter>())
            .Configure<TypeManifestOptions>(options => options.AllowAllTypes = false)
            .BuildServiceProvider();

        _converter = services.GetRequiredService<TypeConverter>();
        _filter = new(services.GetRequiredService<IOptions<TypeManifestOptions>>());
        _subscribeInvokable = typeof(IObserver).Assembly.GetTypes().First(type =>
            type.IsGenericTypeDefinition &&
            type.Name.StartsWith("Invokable_IObserver_", StringComparison.Ordinal) &&
            type.GetGenericArguments()[0].GetGenericParameterConstraints().Contains(typeof(IObserverSubscriber)));
    }
}
