// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventTypeProperty } from "./EventTypeProperty";

export type EventTypeSchema = {
    displayName: string;
    generation: number;
    properties: { [key: string]: EventTypeProperty; };
    required: string[];
    type: string;
};
