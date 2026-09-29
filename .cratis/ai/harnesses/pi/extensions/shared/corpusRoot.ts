// cratis-ai-managed: harnesses/pi/extensions/shared/corpusRoot.ts
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * The corpus this extension tree ships in: `.cratis/ai` for a managed installation and the repository,
 * `package/corpus` inside the published `@cratis/pi` package. Both place `harnesses/pi/extensions/shared`
 * four levels below it.
 */
export const corpusRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..', '..', '..');
