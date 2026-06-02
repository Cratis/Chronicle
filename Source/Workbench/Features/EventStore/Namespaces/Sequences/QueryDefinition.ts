// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
export type QueryDefinition = {
    id?: string;
    name: string;
    eventSequenceId: string;
    eventSourceId?: string;
    eventTypes?: string[];
    startTime?: Date;
    endTime?: Date;
    folderId?: string;
    isUnsaved?: boolean;
};
