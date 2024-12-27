# Quickstart Console

## Client

Chronicle is accessed through its client called `ChronicleClient`.
From this instance you can get the event store you want to work with.

The simplest thing you can do is to rely on the automatic discovery of artifacts by telling
the event store to discover and register everything automatically.

The following snippet configures the minimum and discovers everything for you.

{{snippet:Quickstart-Setup}}

## Events

Defining an event is very simple. You can either use a C# `class` or a `record` type.
We recommend using a `record` type, since records are immutable, much like an [event](../../concepts/event.md)
should be.

With the type defined you simply add the `[EventType]` attribute for the new type.
The reason you do this is for the discovery system to be able to pick up all the event types
automatically.

Below defines a set of events we want to use for our library sample.

{{snippet:Quickstart-Events}}
