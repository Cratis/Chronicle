// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;
using ContractICaptures = Cratis.Chronicle.Contracts.Captures.ICaptures;

namespace Cratis.Chronicle.Services.Captures;

/// <summary>
/// Represents an implementation of <see cref="ContractICaptures"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for reaching the captures manager.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing captures.</param>
/// <param name="languageService"><see cref="ILanguageService"/> for compiling capture declarations.</param>
/// <param name="captureValidator"><see cref="ICaptureValidator"/> for validating compiled captures.</param>
internal sealed class Captures(
    IGrainFactory grainFactory,
    IStorage storage,
    ILanguageService languageService,
    ICaptureValidator captureValidator) : ContractICaptures
{
    /// <inheritdoc/>
    public async Task<Contracts.Captures.SaveCaptureResponse> Save(Contracts.Captures.SaveCapture request, CallContext context = default)
    {
        var captures = storage.GetEventStore(request.EventStore).Captures;
        var captureId = ResolveCaptureId(request.Id);

        if (await captures.Has(captureId))
        {
            var existing = await captures.Get(captureId);
            if (existing.Status == CaptureStatus.Started)
            {
                return new()
                {
                    Messages = [new() { Message = "The capture is started - stop it before changing it" }]
                };
            }
        }

        var compilation = languageService.Compile(request.Declaration);
        return await compilation.Match(
            async definition =>
            {
                var capture = new Capture(captureId, definition.Name, request.Declaration, CaptureStatus.Stopped);
                await captures.Save(capture);
                var messages = await captureValidator.Validate(request.EventStore, definition with { Id = captureId });
                return new Contracts.Captures.SaveCaptureResponse
                {
                    Capture = capture.ToContract(),
                    Messages = messages.ToContract()
                };
            },
            errors => Task.FromResult(new Contracts.Captures.SaveCaptureResponse
            {
                Messages = errors.Errors.Select(error => new Contracts.Captures.CaptureValidationMessage
                {
                    Message = error.Message,
                    Line = error.Line,
                    Column = error.Column
                }).ToList()
            }));
    }

    /// <inheritdoc/>
    public Task Delete(Contracts.Captures.DeleteCapture request, CallContext context = default) =>
        GetManager(request.EventStore).Delete(ResolveCaptureId(request.Id));

    /// <inheritdoc/>
    public async Task<Contracts.Captures.StartCaptureResponse> Start(Contracts.Captures.StartCapture request, CallContext context = default)
    {
        var messages = await GetManager(request.EventStore).Start(ResolveCaptureId(request.Id));
        return new() { Messages = messages.ToContract() };
    }

    /// <inheritdoc/>
    public Task Stop(Contracts.Captures.StopCapture request, CallContext context = default) =>
        GetManager(request.EventStore).Stop(ResolveCaptureId(request.Id));

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Captures.Capture>> GetCaptures(Contracts.Captures.GetCapturesRequest request)
    {
        var captures = await storage.GetEventStore(request.EventStore).Captures.GetAll();
        return captures.Select(capture => capture.ToContract());
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<Contracts.Captures.Capture>> ObserveCaptures(Contracts.Captures.GetCapturesRequest request, CallContext context = default) =>
        storage.GetEventStore(request.EventStore)
            .Captures
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(captures => captures.Select(capture => capture.ToContract()).ToList());

    /// <inheritdoc/>
    public async Task<Contracts.Captures.ValidateCaptureDeclarationResponse> ValidateDeclaration(Contracts.Captures.ValidateCaptureDeclaration request, CallContext context = default)
    {
        var compilation = languageService.Compile(request.Declaration);
        var messages = await compilation.Match(
            definition => captureValidator.Validate(request.EventStore, definition),
            errors => Task.FromResult(errors.Errors.Select(error => new CaptureValidationMessage(error.Message, error.Line, error.Column))));

        return new() { Messages = messages.ToContract() };
    }

    static CaptureId ResolveCaptureId(string id) =>
        Guid.TryParse(id, out var guid) && guid != Guid.Empty ? new CaptureId(guid) : CaptureId.New();

    ICapturesManager GetManager(string eventStore) => grainFactory.GetGrain<ICapturesManager>(eventStore);
}
