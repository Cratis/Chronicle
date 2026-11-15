---
title: Cratis Chronicle scenario recipes
description: Choose a Chronicle recipe to rebuild a read model, enforce uniqueness, evolve an event type, or recover processing, with links to the relevant APIs.
---

Cratis Chronicle scenarios show how to make a specific change to an event-sourced application, such as rebuilding a read model after a mapping fix or rejecting a duplicate email address. Choose the outcome you need below; each recipe explains the approach and links to the API details for carrying it out.

If you have not connected an application and appended events yet, begin with [Get started](../get-started/). The [core concepts](../concepts/) explain how stored events become the read models your application queries.

| Recipe | When to use it |
|---|---|
| [Rebuild a read model](./rebuild-a-read-model) | You changed a projection and need the read model to reflect the new logic |
| [Enforce a unique value](./enforce-a-unique-value) | Different event sources must not claim the same email, ISBN or username |
| [React to an event](./react-to-an-event) | An event should trigger a notification, external call, or follow-up fact |
| [Get read models](./real-time-query) | You need one instance, a collection, a page, or live updates |
| [Test a slice](./test-a-slice) | You want to verify event appends and derived read model state in-process |
| [Evolve an event's shape](./evolve-an-event) | An existing event type needs a new payload shape, and consumers still need older history |
| [Close the books](./close-the-books) | A period or version is finished and further events must go to the next one |
| [Recover a failed partition](../operations/recover-a-failed-partition) | An observer stopped on a failing partition and you need to choose between retry, replay and clearing quarantine |

For append behavior beyond these recipes, continue with the [event feature guides](../events/). If processing or a query is not behaving as expected, start with [Troubleshooting](../troubleshooting/) to investigate before choosing a recovery action.
