// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A fake inbox that records what was appended and can be told to fail.
/// </summary>
public class FakeInbox : IFederationInbox
{
    public FederationResumePoint? Resume { get; set; }

    public List<(string Origin, ulong SequenceNumber)> Appended { get; } = [];

    public int FailuresRemaining { get; set; }

    public int ResumeLookups { get; private set; }

    public Task<FederationResumePoint?> GetResumePoint(FederationBindingState binding)
    {
        ResumeLookups++;
        return Task.FromResult(Resume);
    }

    public Task<string?> Append(FederationBindingState binding, string originEventStore, FederatedEvent @event)
    {
        if (FailuresRemaining > 0)
        {
            FailuresRemaining--;
            return Task.FromResult<string?>("Inbox append error");
        }

        Appended.Add((originEventStore, @event.SequenceNumber));
        return Task.FromResult<string?>(null);
    }
}
