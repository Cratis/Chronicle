// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog, ConfirmationDialog } from '@cratis/components/Dialogs';
import { TimeMachineDialog } from './TimeMachineDialog';
import { ReadModelDefinition } from 'Api/ReadModelTypes/ReadModelDefinition';
import { ReadModelOwner, ReadModelSource } from 'Api/ReadModelTypes';

const mockReadModel: ReadModelDefinition = {
    identifier: 'SampleReadModel',
    displayName: 'Sample Read Model',
    containerName: 'SampleReadModels',
    generation: 1,
    owner: ReadModelOwner.client,
    source: ReadModelSource.user,
    indexes: [],
    schema: JSON.stringify({
        type: 'object',
        properties: {
            id: { type: 'string' },
            name: { type: 'string' },
            value: { type: 'number' },
        },
    }),
};

const meta: Meta<typeof TimeMachineDialog> = {
    title: 'Components/TimeMachineDialog',
    component: TimeMachineDialog,
    decorators: [
        (Story) => (
            <MemoryRouter initialEntries={['/event-store/my-store/default']}>
                <Routes>
                    <Route
                        path='/event-store/:eventStore/:namespace/*'
                        element={
                            <DialogComponents confirmation={ConfirmationDialog} busyIndicator={BusyIndicatorDialog}>
                                <Story />
                            </DialogComponents>
                        }
                    />
                </Routes>
            </MemoryRouter>
        ),
    ],
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof TimeMachineDialog>;

export const Default: Story = {
    args: {
        readModel: mockReadModel,
        readModelKey: 'sample-key-1',
    },
};
