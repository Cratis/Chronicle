# Event Types

Defining an event is straightforward. You can use either a C# `class` or a `record` type.
We recommend using a `record` type because records are immutable, which aligns with the nature of an [event](../concepts/event.md).

To define an event type, simply add the `[EventType]` attribute to the new type. This attribute allows the discovery system to automatically detect all event types. You can read more about event types [here](../concepts/event-type.md).

Below is a set of events we will use for our library sample.

{{snippet:Quickstart-Events}}
