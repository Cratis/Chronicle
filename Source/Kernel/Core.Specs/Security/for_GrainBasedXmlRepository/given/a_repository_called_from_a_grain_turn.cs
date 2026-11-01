// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_GrainBasedXmlRepository.given;

/// <summary>
/// Calls the repository from a single-threaded scheduler, the way a grain turn runs, against a keys grain whose reply
/// completes on whatever scheduler the call was made from - as the outgoing grain call filters do.
/// </summary>
public class a_repository_called_from_a_grain_turn : Specification
{
    protected const string Key = "<key id=\"1\" />";
    protected static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    protected IDataProtectionKeys _keys;
    protected GrainBasedXmlRepository _repository;
    protected TaskScheduler _turn;
    protected TaskScheduler? _replyScheduler;

    void Establish()
    {
        _turn = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
        _keys = Substitute.For<IDataProtectionKeys>();
        _keys.GetAllKeys().Returns(_ => ReplyOnCallingScheduler<IEnumerable<string>>([Key]));
        _keys.StoreKey(Arg.Any<string>(), Arg.Any<string>()).Returns(_ => ReplyOnCallingScheduler(true));
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IDataProtectionKeys>(Arg.Any<string>(), Arg.Any<string>()).Returns(_keys);
        _repository = new(grainFactory);
    }

    protected Task<T> InGrainTurn<T>(Func<T> work) =>
        Task.Factory.StartNew(work, CancellationToken.None, TaskCreationOptions.None, _turn).WaitAsync(Bound, TimeProvider.System);

    Task<T> ReplyOnCallingScheduler<T>(T value)
    {
        var calling = TaskScheduler.Current;
        return Task.Factory.StartNew(
            () =>
            {
                _replyScheduler = calling;
                return value;
            },
            CancellationToken.None,
            TaskCreationOptions.None,
            calling);
    }
}
