// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents what became of the client's declared artifacts when <see cref="IEventStore.RegisterAll"/> ran.
/// </summary>
/// <param name="HasRun">Whether <see cref="IEventStore.RegisterAll"/> has finished at least once, successfully or not.</param>
/// <param name="Artifacts">The declared artifacts, each carrying whether it registered and, if not, the failure that stopped it.</param>
/// <param name="Failure">
/// The <see cref="Exception"/> that stopped <see cref="IEventStore.RegisterAll"/> itself, or <see langword="null"/>
/// when it finished. This is not the same as an artifact that failed to build: those are isolated and reported in
/// <paramref name="Artifacts"/>, and registration carries on. This one ended the run.
/// </param>
/// <remarks>
/// <para>
/// This exists so that a consumer holding an <see cref="IEventStore"/> can tell three states apart that used to look
/// identical from outside: every artifact registered, some subset registered and the rest were dropped, and registration
/// has not run yet. A read model that cannot be built is isolated and logged rather than thrown, which is right - one
/// unbuildable read model must not cost the whole read side - but it turns a loud total failure into a quiet partial
/// one. This is the read-only seam for asking about it afterwards; it is not a failure policy, and nothing here decides
/// whether a failed artifact should abort start-up, fail a spec, or be tolerated. That is the consumer's call.
/// </para>
/// <para>
/// <b>What it covers.</b> Projection artifacts - both the fluent <see cref="Projections.IProjectionFor{TReadModel}"/>
/// implementations and the model-bound read models. Those are the artifacts whose registration round-trips to the
/// kernel inside <see cref="IEventStore.RegisterAll"/>, so an outcome for them is something the client can actually
/// observe rather than assume. <see cref="ArtifactRegistration.IsRegistered"/> means the client built a definition for
/// the artifact and the kernel accepted the batch it travelled in; the registration operation itself returns no
/// per-definition verdict, so a finer-grained claim than that would be invented.
/// </para>
/// <para>
/// <b>What it deliberately does not cover: reactors and reducers.</b> Their <c>Register()</c> only opens a duplex
/// stream - it marks itself registered and returns before any round trip to the kernel has happened. Reporting them
/// here would mean reporting a hope as a fact, so they are left out rather than represented optimistically. A consumer
/// that needs to know a reactor or reducer is live should wait on its observer state instead
/// (<c>ReactorWaitExtensions.WaitTillSubscribed</c>, <c>ReducerWaitExtensions.WaitTillActive</c>), which is answered by
/// the kernel.
/// </para>
/// <para>
/// <b>Do not reach for <see cref="Connections.IConnectionLifecycle.IsConnected"/> instead of this.</b> It looks like it
/// carries a registration verdict and it partly does - <c>ConnectionLifecycle.Connected()</c> rolls it back to
/// <see langword="false"/> and rethrows when one of the connected handlers failed, and
/// <see cref="IEventStore.RegisterAll"/> is one of those handlers. But it is set to <see langword="true"/>
/// <em>before</em> the handlers run, and only rolled back <em>after</em> they have all finished, so polling it races:
/// it reads <see langword="true"/> while registration is still in flight and while a registration that is about to be
/// reported as failed is still running. It answers "connected", which is necessary and not sufficient. Use
/// <see cref="IEventStore.Registration"/>, which only ever transitions once registration has completed.
/// </para>
/// </remarks>
public record RegistrationOutcome(bool HasRun, IImmutableList<ArtifactRegistration> Artifacts, Exception? Failure = null)
{
    /// <summary>
    /// The outcome for an event store whose <see cref="IEventStore.RegisterAll"/> has not finished yet.
    /// </summary>
    /// <remarks>
    /// Registration is wired to the connection lifecycle, so "not yet" and "never" look the same from outside - this
    /// value covers both. It does not cover "tried and failed": a run that throws still reports itself, carrying its
    /// <see cref="Failure"/>. Use <c>RegistrationWaitExtensions.WaitForRegistration</c> to wait for whichever arrives.
    /// </remarks>
    public static readonly RegistrationOutcome NotRun = new(false, ImmutableList<ArtifactRegistration>.Empty);

    /// <summary>
    /// Gets a value indicating whether registration ran, finished, and every declared artifact registered.
    /// </summary>
    public bool IsSuccess => HasRun && Failure is null && Artifacts.All(_ => _.IsRegistered);

    /// <summary>
    /// Gets the declared artifacts that did not register, each carrying the failure that stopped it.
    /// </summary>
    public IEnumerable<ArtifactRegistration> Failures => Artifacts.Where(_ => !_.IsRegistered);

    /// <summary>
    /// Compare with another <see cref="RegistrationOutcome"/> by value.
    /// </summary>
    /// <param name="other">The outcome to compare with.</param>
    /// <returns>True if the outcomes carry the same artifacts, false otherwise.</returns>
    /// <remarks>
    /// The generated record equality would compare <see cref="Artifacts"/> with the list's own equality, which is by
    /// reference - so two outcomes carrying the same artifacts came out unequal, and a record's headline promise did
    /// not hold for the one member that matters. It ships a <see cref="NotRun"/> sentinel that invites being compared
    /// against, so this is not theoretical.
    /// </remarks>
    public virtual bool Equals(RegistrationOutcome? other) =>
        other is not null &&
        HasRun == other.HasRun &&
        Equals(Failure, other.Failure) &&
        Artifacts.SequenceEqual(other.Artifacts);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        hashCode.Add(HasRun);
        hashCode.Add(Failure);
        foreach (var artifact in Artifacts)
        {
            hashCode.Add(artifact);
        }

        return hashCode.ToHashCode();
    }
}
