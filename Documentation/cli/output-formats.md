# Output Formats

Every command supports multiple output formats via the `-o` / `--output` flag. The CLI auto-detects the best format for your terminal by default.

## Available Formats

| Format | Flag | Description |
| ------ | ---- | ----------- |
| **text** | `-o text` | Rich tables with borders, rendered using your terminal's capabilities. Best for interactive use. This is the default in most terminals. |
| **plain** | `-o plain` | Tab-separated values with a header row. Designed for piping to `grep`, `awk`, `cut`, and other UNIX tools. Also the most token-efficient format for AI consumption. |
| **json** | `-o json` | Indented JSON. Contains the full server response including schemas, causation chains, and metadata. Best when you need structured data for programmatic parsing. |
| **json-compact** | `-o json-compact` | Compact (non-indented) JSON. Same content as `json` but without whitespace, reducing output size by roughly 30%. Useful when you need full structured data but want to minimize token usage. |

## Auto-Detection

When you don't specify `-o`, the CLI selects a format automatically:

- If `NO_COLOR` is set or the terminal doesn't support ANSI, it uses **plain**.
- Otherwise, it uses **text** (rich tables).

## Choosing a Format

For **interactive use**, let the default `text` format render tables:

```shell
cratis observers list -e MyStore
```

For **scripting and piping**, use `plain`:

```shell
cratis event-types list -o plain | grep UserRegistered
cratis observers list -o plain | awk -F'\t' '$3 == "Disconnected"'
```

For **programmatic parsing**, use `json`:

```shell
cratis config show -o json | jq '.eventStore'
cratis read-models get MyReadModel abc-123 -o json
```

For **AI agents**, use `plain` for most commands and `json` or `json-compact` when you need structured data. See [AI Agent Integration](./ai-integration.md) for detailed guidance.

## Messages and Errors

Commands that produce simple messages (e.g. confirmations, errors) adapt to the format too:

- **text/plain**: Human-readable colored output
- **json/json-compact**: Structured `{"message": "..."}` or `{"error": "...", "suggestion": "..."}` objects

This makes it safe to always parse JSON output programmatically — you will never encounter unstructured text mixed in.
