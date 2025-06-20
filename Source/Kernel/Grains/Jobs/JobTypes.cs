// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CodeDom.Compiler;
using System.Reflection;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.DependencyInjection;
using Cratis.Monads;
using Cratis.Reflection;
using Cratis.Types;
namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobTypes"/>.
/// </summary>
[Singleton]
public class JobTypes : IJobTypes
{
    readonly Dictionary<JobType, Type> _jobTypes = [];
    readonly Dictionary<Type, JobType> _jobTypePerType = [];
    readonly Dictionary<JobType, Type> _jobRequestTypes = [];

    /// <summary>
    /// Initializes an instance of the <see cref="JobTypes"/> class.
    /// </summary>
    /// <param name="types">The <see cref="ITypes"/>.</param>
    public JobTypes(ITypes types)
    {
        InitializeMap(types);
        Instance = this;
    }

    /// <summary>
    /// Gets the singleton instance of <see cref="IJobTypes"/>.
    /// </summary>
    public static IJobTypes Instance { get; private set; } = null!;

    /// <inheritdoc />
    public Result<JobType, IJobTypes.GetForError> GetFor(Type type)
    {
        if (type.IsInterface)
        {
            return _jobTypePerType.TryGetValue(type, out var jobType)
                ? jobType
                : IJobTypes.GetForError.NoAssociatedJobType;
        }
        var result = _jobTypePerType.Keys.FirstOrDefault(type.Implements);
        return result is not null
            ? _jobTypePerType[result]
            : IJobTypes.GetForError.NoAssociatedJobType;
    }

    /// <inheritdoc />
    public Result<Type, IJobTypes.GetClrTypeForError> GetClrTypeFor(JobType type)
    {
        if (type == JobType.NotSet)
        {
            return IJobTypes.GetClrTypeForError.JobTypeIsNotSet;
        }

        return _jobTypes.TryGetValue(type, out var jobClrType)
            ? jobClrType
            : IJobTypes.GetClrTypeForError.CouldNotFindType;
    }

    /// <inheritdoc />
    public Result<Type, IJobTypes.GetRequestClrTypeForError> GetRequestClrTypeFor(JobType type)
    {
        if (type == JobType.NotSet)
        {
            return IJobTypes.GetRequestClrTypeForError.JobTypeIsNotSet;
        }

        return _jobRequestTypes.TryGetValue(type, out var jobRequestClrType)
            ? jobRequestClrType
            : IJobTypes.GetRequestClrTypeForError.CouldNotFindType;
    }

    void InitializeMap(ITypes types)
    {
        PopulateJobTypes(types);
        PopulateJobRequestTypes();
    }

    void PopulateJobTypes(ITypes types)
    {
        foreach (var jobClrType in types.FindMultiple<IJob>()
                     .Where(type => type is { IsClass: true, IsAbstract: false, IsInterface: false, IsGenericType: false }
                                    && !type.HasAttribute<GeneratedCodeAttribute>() && type != typeof(NullJob) && type.Assembly.FullName != typeof(IJob).Assembly.FullName))
        {
            var jobTypeAttribute = jobClrType.GetCustomAttribute<JobTypeAttribute>();
            var jobType = jobTypeAttribute?.JobType ?? jobClrType;
            if (jobType == JobType.NotSet)
            {
                throw new ArgumentException($"JobType for type {jobClrType} is not set");
            }
            var jobInterface = jobClrType.GetInterfaces().Single(jobInterfaceType => jobInterfaceType.GetInterfaces().Any(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IJob<>)));
            if (!_jobTypes.TryAdd(jobType, jobInterface))
            {
                throw new JobTypeAlreadyExists(jobType, _jobTypes[jobType], jobInterface);
            }
            _jobTypePerType.Add(jobInterface, jobType);
        }
    }
    void PopulateJobRequestTypes()
    {
        foreach (var (jobType, jobClrType) in _jobTypes)
        {
            var jobInterfaces = jobClrType.GetInterfaces()
                .Where(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IJob<>))
                .ToList();

            switch (jobInterfaces.Count)
            {
                case > 1:
                    throw new JobTypeCanOnlyHaveOneRequestType(jobType, jobClrType);
                case 0:
                    throw new JobTypeMustHaveARequestType(jobType, jobClrType);
                default:
                    // First generic argument of IJob<TRequest> is the type of the request
                    var requestType = jobInterfaces[0].GetGenericArguments()[0];
                    _jobRequestTypes.Add(jobType, requestType);
                    break;
            }
        }
    }
}
