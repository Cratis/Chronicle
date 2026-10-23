<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/record-capture-specifications.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# First sight and updates in record captures

Requires Screenplay 4.175.0 or later (`screenplay --version`); older reference evaluators treat fields of a first record as changed. Upgrade before relying on added-only outcomes.

For `source api`, webhook, poll and other record sources, a first record of a typed key is **added only**. `when <field>`, field-change `and`/`or` and transitions need an earlier record of that same key. For an update scenario, seed it with `given capture`. A baseline for another key does not count. To record both first sight and later changes, use separate appends with `when added` and `when <field>`; do not replace the update guard with `when added`.

This applies independently to each child identity in its parent's previous collection and each nested record: a missing/null previous nested value is new, and every child of a new parent is new. Expression guards still evaluate the mapped current record on first sight; maps and unguarded appends keep their meaning. These record fixtures are not valid for `source events`: use typed events instead (automations-and-translations [public event captures](../../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md)).

A capture **over events** carries a public event's list across the boundary two ways (decision 0098, no new ESM version): `lines = $.lines`
copies the whole list into a list property of the same shape (matched by property name), and `children lines identified by sku` with an
`append ... when added` makes one private event per element, so a repeated delivery appends nothing and one with an extra line appends only
that line. `nested <object>` scopes into an object the same way.

The complete model separates a new observation, an update and an unchanged repeat:

```screenplay
domain LegacyBilling
module Integration
  feature Invoices
    slice Translate ImportInvoiceStatus
      event InvoiceStatusObserved
        status String
      capture InvoiceRecords
        source api
          api LegacyBilling
          route /invoices
          poll 5m
        key id
        append InvoiceStatusObserved
          when added
            status = $.status
        append InvoiceStatusObserved
          when status
            status = $.status
      specification RecordingFirstSight
        when capture InvoiceRecords
          id = "inv-42"
          status = "sent"
        then InvoiceStatusObserved
          for "inv-42"
          status = "sent"
      specification RecordingAnUpdate
        given capture InvoiceRecords
          id = "inv-42"
          status = "sent"
        when capture InvoiceRecords
          id = "inv-42"
          status = "paid"
        then InvoiceStatusObserved
          for "inv-42"
          status = "paid"
      specification IgnoringAnUnchangedRecord
        given capture InvoiceRecords
          id = "inv-42"
          status = "paid"
        when capture InvoiceRecords
          id = "inv-42"
          status = "paid"
        then no events
```

Run `screenplay <model> --warnaserror` (zero errors/warnings), then `screenplay test <model>` (all three specifications passed). Each specification supplies its own immutable baseline; no last-seen record persists between runs. Reference execution never contacts the configured API and does not prove source acknowledgement or retry safety.
