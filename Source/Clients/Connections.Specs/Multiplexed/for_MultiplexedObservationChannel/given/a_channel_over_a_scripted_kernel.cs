// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.Multiplexed.for_MultiplexedObservationChannel.given;

/// <summary>
/// A channel whose kernel is a script: the spec says what the kernel sends and sees what the client wrote.
/// </summary>
public class a_channel_over_a_scripted_kernel : Specification
{
    protected MultiplexedObservationChannel _channel;
    protected IMultiplexedObservation _kernel;
    protected List<Subject<MultiplexedServerMessage>> _calls;
    protected List<MultiplexedClientMessage> _writes;
    readonly Lock _writesLock = new();

    void Establish()
    {
        _calls = [];
        _writes = [];
        _kernel = Substitute.For<IMultiplexedObservation>();
        _kernel.Observe(Arg.Any<IObservable<MultiplexedClientMessage>>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(call =>
        {
            var messages = call.Arg<IObservable<MultiplexedClientMessage>>();
            var server = new Subject<MultiplexedServerMessage>();
            _calls.Add(server);
            messages.Subscribe(write =>
            {
                lock (_writesLock)
                {
                    _writes.Add(write);
                }
            });
            return server;
        });
        _channel = new(_kernel, NullLogger<MultiplexedObservationChannel>.Instance) { CoalesceDelay = TimeSpan.Zero };
    }

    protected static ReactorMessage Register(string id) => new(new(new RegisterReactor { Reactor = new Contracts.Observation.Reactors.ReactorDefinition { ReactorId = id } }));

    protected static async Task WaitUntil(Func<bool> condition)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition())
        {
            if (deadline.IsCancellationRequested)
            {
                throw new TimeoutException("The condition was not met in time.");
            }

            await Task.Delay(10, deadline.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    protected int WrittenMessages
    {
        get
        {
            lock (_writesLock)
            {
                return _writes.Sum(_ => _.Messages.Count);
            }
        }
    }
}
