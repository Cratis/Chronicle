// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { StoryContainer } from '@cratis/arc.react/stories';
import { EventThroughputWidget } from './EventThroughputWidget';

const samples = [4, 9, 3, 12, 20, 15, 8, 6, 22, 30, 18, 10];

const meta = {
    title: 'EventStore/Dashboard/EventThroughputWidget',
    component: EventThroughputWidget,
    parameters: {
        layout: 'padded',
        docs: {
            description: {
                component: 'Plots a bounded, session-scoped ring buffer of event throughput samples derived from ' +
                    'polling the event log\'s tail sequence number - there is no persisted throughput metric in ' +
                    'Chronicle to chart against.'
            }
        }
    },
    tags: ['autodocs'],
    render: args => <StoryContainer size='md' asCard><EventThroughputWidget {...args} /></StoryContainer>
} satisfies Meta<typeof EventThroughputWidget>;

export default meta;
type Story = StoryObj<typeof meta>;

/** A rolling window of throughput samples. */
export const Playground: Story = {
    args: { samples }
};

/** Before any sample has been recorded yet. */
export const Empty: Story = {
    args: { samples: [] }
};
