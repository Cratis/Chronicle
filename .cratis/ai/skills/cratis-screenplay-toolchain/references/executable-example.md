<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/executable-example.md -->
# Executable example (StateChange and StateView)

A small model that compiles with 0 diagnostics and binds executable (`authoringAccepted` true, `executableReady` true, 0 executable diagnostics) on both tools when opened through MCP with a fixed root (see [mcp-loop.md](../../cratis-screenplay-model-authoring/references/mcp-loop.md) and [versions.md](versions.md)). Prose: [executable-subset.md](executable-subset.md).

```screenplay
// Executable example: StateChange + StateView, command and query protection, reference specifications.
concept ArtworkId : Uuid
concept ArtworkTitle : String
  validate
    not empty  message "An artwork needs a title"

policy IsCurator
  require role "Curator"

module Gallery
  feature Registration
    slice StateChange RegisterArtwork
      command RegisterArtwork
        artworkId ArtworkId identifier
        title        ArtworkTitle
        insuredValue Decimal
        authorize IsCurator
        validate
          insuredValue > 0  message "An insured value must be positive"
        produces event ArtworkRegistered
          title ArtworkTitle = title
          insuredValue Decimal = insuredValue
        produces when insuredValue > 100000
          HighValueArtworkFlagged
            for artworkId
            insuredValue = insuredValue
      event HighValueArtworkFlagged
        insuredValue Decimal
      constraint UniqueArtworkTitle
        unique title on ArtworkRegistered
      specification RegisteringAnArtwork
        given caller
          authenticated
          role "Curator"
        when RegisterArtwork
          artworkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          title        = "Starry Night"
          insuredValue = 500
        then ArtworkRegistered
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          title        = "Starry Night"
          insuredValue = 500
      specification RejectingAZeroValue
        given caller
          authenticated
          role "Curator"
        when RegisterArtwork
          artworkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          title        = "Starry Night"
          insuredValue = 0
        then error "An insured value must be positive"
      specification RejectingAnEmptyTitle          // one concept rejection spec, through this command
        given caller
          authenticated
          role "Curator"
        when RegisterArtwork
          artworkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          title        = ""
          insuredValue = 500
        then error "An artwork needs a title"
      specification RefusingAnOutsider
        given caller
          authenticated
          role "Guest"
        when RegisterArtwork
          artworkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          title        = "Starry Night"
          insuredValue = 500
        then denied
    slice StateView ArtworkDetails
      readmodel ArtworkSummary
        artworkId ArtworkId
        title        ArtworkTitle
        insuredValue Decimal
      query ArtworkById => ArtworkSummary optional
        by artworkId ArtworkId
        authorize IsCurator
      projection Artworks => ArtworkSummary
        from ArtworkRegistered
          artworkId = $eventSourceId
          title        = title
          insuredValue = insuredValue
      specification RefusingAnOutsiderLookup
        given caller
          authenticated
          role "Guest"
        then query ArtworkById
          arguments
            artworkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        then denied
      specification SeeingARegisteredArtwork
        given caller
          authenticated
          role "Curator"
        given ArtworkRegistered
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          title        = "Starry Night"
          insuredValue = 500
        then query ArtworkById
          arguments
            artworkId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          result
            title        = "Starry Night"
```
