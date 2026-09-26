// cratis-ai-managed: harnesses/pi/extensions/cratis-mcp/DiscoveredTool.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { TSchema } from 'typebox';

export interface DiscoveredTool {
    name: string;
    nativeName: string;
    description: string;
    parameters: TSchema;
    mutation: boolean;
}
