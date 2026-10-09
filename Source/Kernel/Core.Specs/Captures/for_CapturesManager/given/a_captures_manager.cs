// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Captures;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Captures.for_CapturesManager.given;

public class a_captures_manager : Specification
{
    protected static readonly EventStoreName _eventStore = "some-store";

    protected TestKitSilo _silo = new();
    protected CapturesManager _manager;
    protected ICapturesStorage _captures;
    protected ILanguageService _languageService;
    protected ICaptureValidator _validator;
    protected ICaptureEventsSubscriptions _subscriptions;
    protected ICapturer _capturer;
    protected Capture _events;
    protected Capture _poll;
    protected CaptureDefinition _eventsDefinition;
    protected CaptureDefinition _pollDefinition;
    protected List<string> _calls = [];

    async Task Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _captures = Substitute.For<ICapturesStorage>();
        storage.GetEventStore(_eventStore).Returns(eventStoreStorage);
        eventStoreStorage.Captures.Returns(_captures);
        _languageService = Substitute.For<ILanguageService>();
        _validator = Substitute.For<ICaptureValidator>();
        _validator.Validate(Arg.Any<EventStoreName>(), Arg.Any<CaptureDefinition>()).Returns(Task.FromResult<IEnumerable<CaptureValidationMessage>>([]));
        _subscriptions = Substitute.For<ICaptureEventsSubscriptions>();
        _capturer = Substitute.For<ICapturer>();

        _events = new Capture(CaptureId.New(), "Shipments", new CaptureDeclaration("events-declaration"), CaptureStatus.Stopped);
        _poll = new Capture(CaptureId.New(), "Customers", new CaptureDeclaration("poll-declaration"), CaptureStatus.Stopped);
        _eventsDefinition = Definition(_events, new SourceDefinition(SourceType.Events, Sequence: "inbox-fulfillment", Events: ["ShipmentDispatched"]));
        _pollDefinition = Definition(_poll, new SourceDefinition(SourceType.Api, Api: "customers", Poll: "5m"));
        _languageService.Compile("events-declaration").Returns(_eventsDefinition);
        _languageService.Compile("poll-declaration").Returns(_pollDefinition);
        Store(_events);
        Store(_poll);

        _captures.Save(Arg.Any<Capture>()).Returns(call =>
        {
            _calls.Add($"save:{call.Arg<Capture>().Status}");
            Store(call.Arg<Capture>());
            return Task.CompletedTask;
        });
        _subscriptions.Subscribe(_eventStore, Arg.Any<CaptureDefinition>()).Returns(_ =>
        {
            _calls.Add("subscribe");
            return Task.CompletedTask;
        });

        _silo.AddService(storage);
        _silo.AddService(_languageService);
        _silo.AddService(_validator);
        _silo.AddService(_subscriptions);
        _silo.AddService(NullLogger<CapturesManager>.Instance);
        _silo.AddProbe(_ => _capturer);
        _manager = await _silo.CreateGrainAsync<CapturesManager>(_eventStore.Value);
    }

    protected void StartedState(Capture capture) => Store(capture with { Status = CaptureStatus.Started });

    static CaptureDefinition Definition(Capture capture, SourceDefinition source) =>
        new(capture.Id, capture.Name, source, string.Empty, null, [], [], []);

    void Store(Capture capture)
    {
        _captures.Has(capture.Id).Returns(true);
        _captures.Get(capture.Id).Returns(capture);
        var all = new[] { _events, _poll }.Where(_ => _.Id != capture.Id).Append(capture).ToArray();
        _captures.GetAll().Returns(all);
        if (capture.Id == _events.Id)
        {
            _events = capture;
        }
        else
        {
            _poll = capture;
        }
    }
}
