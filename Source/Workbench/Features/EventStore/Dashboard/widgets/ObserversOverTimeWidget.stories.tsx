// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { StoryContainer } from '@cratis/arc.react/stories';
import { ObserversOverTimeWidget } from './ObserversOverTimeWidget';
import { type ObserverSample } from '../ObserverSample';

const samples: ObserverSample[] = [
    { active: 90, total: 113 },
    { active: 94, total: 113 },
    { active: 96, total: 113 },
    { active: 98, total: 113 },
    { active: 95, total: 113 },
    { active: 98, total: 113 }
];

const meta = {
    title: 'EventStore/Dashboard/ObserversOverTimeWidget',
    component: ObserversOverTimeWidget,
    parameters: {
        layout: 'padded',
        docs: {
            description: {
                component: 'Plots a bounded, session-scoped ring buffer of active vs. total observer counts - ' +
                    'there is no persisted observer-state history in Chronicle to chart against.'
            }
        }
    },
    tags: ['autodocs'],
    render: args => <StoryContainer size='md' asCard><ObserversOverTimeWidget {...args} /></StoryContainer>
} satisfies Meta<typeof ObserversOverTimeWidget>;

export default meta;
type Story = StoryObj<typeof meta>;

/** A rolling window of active/total observer samples. */
export const Playground: Story = {
    args: { samples }
};

/** Before any sample has been recorded yet. */
export const Empty: Story = {
    args: { samples: [] }
};
