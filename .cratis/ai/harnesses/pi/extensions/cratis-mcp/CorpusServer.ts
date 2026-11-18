// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/CorpusServer.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A validated stdio server from the corpus descriptor; command, args and env are used verbatim. */
export interface CorpusServer {
    id: string;
    command: string;
    args: string[];
    env: Record<string, string>;
    /** `mcpServers.<id>.eager`: register the tools at session start instead of on demand through `cratis_enable_mcp`. */
    eager: boolean;
}

/** The servers to start for a project plus the explicit messages for everything skipped. */
export interface ServerSelection {
    servers: CorpusServer[];
    messages: string[];
}
