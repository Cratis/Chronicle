// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

public partial class Reducers
{
    long _connectionGeneration;
    ImmutableHashSet<Type> _registeredTypes = [];
    ImmutableHashSet<Type> _registerOnReconnect = [];

    /// <summary>
    /// Registers reducers that were registered before a reconnect, for stores without automatic registration.
    /// </summary>
    /// <returns>A task representing the registration.</returns>
    /// <exception cref="AggregateException">Multiple reducers could not register again.</exception>
    internal Task RegisterAgainAfterReconnect()
    {
        if (!CanRegisterAgain() || _registerOnReconnect.IsEmpty)
        {
            return Task.CompletedTask;
        }

        List<Exception> failures = [];
        WithCurrentServices(services =>
        {
            if (!CanRegisterAgain())
            {
                return;
            }

            foreach (var handler in _handlersByType.Values.Where(ShouldRegisterAfterReconnect).ToList())
            {
                RegisterAgain(handler, services, failures);
            }
        });

        ThrowRegistrationFailures(failures);
        return Task.CompletedTask;
    }

    static ObjectDisposedException ReleasedRegistration() =>
        new(nameof(Reducers), "Cannot register a reducer after its event store has been released.");

    static void ThrowRegistrationFailures(List<Exception> failures)
    {
        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }
        if (failures.Count > 1)
        {
            throw new AggregateException(failures);
        }
    }

    /// <summary>
    /// Runs a registration against the current services. Mirrors Reactors.WithCurrentServices: services are
    /// resolved outside the ownership monitor because resolving them can connect synchronously, and connection
    /// callbacks must be able to take the monitor. Retries when the connection changed in between.
    /// </summary>
    /// <param name="register">The registration, run under the monitor with services of the current connection.</param>
    void WithCurrentServices(Action<IServices> register)
    {
        while (true)
        {
            long generation;
            lock (ReadModelBuilderOwnership.SyncFor(_eventStore, ReadModelOwnershipSync))
            {
                if (_released)
                {
                    return;
                }
                generation = _connectionGeneration;
            }

            var services = _servicesAccessor.Services;
            lock (ReadModelBuilderOwnership.SyncFor(_eventStore, ReadModelOwnershipSync))
            {
                if (_released)
                {
                    return;
                }
                if (generation != _connectionGeneration)
                {
                    continue;
                }
                register(services);
                return;
            }
        }
    }

    void MarkRegistered(Type reducerType)
    {
        _registeredTypes = _registeredTypes.Add(reducerType);
        _registerOnReconnect = _registerOnReconnect.Remove(reducerType);
    }

    void MarkRegisteredOnPreviousConnection()
    {
        _connectionGeneration++;
        _registerOnReconnect = _registerOnReconnect.Union(_registeredTypes);
        _registeredTypes = [];
    }

    void RegisterAgain(IReducerHandler handler, IServices services, List<Exception> failures)
    {
        _logger.RegisteringReducerAgainAfterReconnect(handler.Id);
        try
        {
            RegisterWhenDispatchable(handler, services);
            MarkRegistered(handler.ReducerType);
        }
        catch (Exception ex)
        {
            _logger.RegisteringReducerAgainAfterReconnectFailed(handler.Id, ex);
            failures.Add(ex);
        }
    }

    bool CanRegisterAgain() => !_released && _eventStore.Connection.Lifecycle.IsConnected;

    bool ShouldRegisterAfterReconnect(IReducerHandler handler) =>
        _registerOnReconnect.Contains(handler.ReducerType) &&
        !_registeredTypes.Contains(handler.ReducerType) &&
        !handler.CancellationToken.IsCancellationRequested;
}
