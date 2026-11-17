<!-- cratis-ai-managed: skills/cratis-screenplay-availability/references/complete-example.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Complete example: gates, an availability, a persona audience and a provided value

Gates sit at module, feature, slice, command and screen level and AND down the path. The persona
`audience` line is authoring metadata. The `provide` at application level is read by the
availability's `when` and by the command's gate; the slice-level query reads
`$context.arguments`, which only a query has.

```screenplay
audience Plus
  description "Customers on the Plus commercial package"

audience Preview
  description "Tenants taking part in the preview"

availability DataQuality
  description "Sold with the Plus package, once the vessel has reported"
  for Plus
  when $context.vessel.hasReported == true

availability Everything
  description "Anything sold or being previewed"
  for Plus or Preview

persona FleetManager
  audience Plus

provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId

module Fleet
  available with Everything

  feature DataQuality
    available with DataQuality

    slice StateChange ReportVessel
      available for Plus and (Preview or Plus)

      event VesselReported
        note String

      command ReportVessel
        available when $context.vessel.hasReported == true
        vesselId Uuid identifier
        note String
        produces VesselReported
          for vesselId
          note = note

      screen ReportScreen
        available for Plus
        action ReportVessel for screen

    slice StateView Vessels
      query VesselByKey => Vessel optional
        available for Plus
        by vesselId Uuid

      readmodel Vessel
        vesselId Uuid
        hasReported Bool

      event VesselRegistered
        vesselId Uuid

      projection Vessels => Vessel
        from VesselRegistered key vesselId
          vesselId = vesselId

      screen VesselScreen
        available for Preview
```

The application-level `provide` has to resolve per request for the command as well as the query;
`$context.arguments.vesselId` does not exist for a command, so state a slice-level override with
`$context.command.<key>` when a command needs the value.
