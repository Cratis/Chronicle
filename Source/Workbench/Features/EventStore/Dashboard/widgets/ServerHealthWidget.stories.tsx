// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { StoryContainer } from '@cratis/arc.react/stories';
import { ServerHealthWidget } from './ServerHealthWidget';

const meta = {
    title: 'EventStore/Dashboard/ServerHealthWidget',
    component: ServerHealthWidget,
    parameters: {
        layout: 'padded',
        docs: {
            description: {
                component: 'Shows connection status, the current store/namespace context, and catalog counts - ' +
                    'the health-and-status overview panel of the event store dashboard.'
            }
        }
    },
    tags: ['autodocs'],
    render: args => <StoryContainer size='sm'><ServerHealthWidget {...args} /></StoryContainer>
} satisfies Meta<typeof ServerHealthWidget>;

export default meta;
type Story = StoryObj<typeof meta>;

/** The default, connected state with catalog counts loaded. */
export const Playground: Story = {
    args: {
        eventStore: 'Studio',
        namespace: 'Cratis',
        isConnected: true,
        tailSequenceNumber: 1688,
        eventTypeCount: 295,
        projectionCount: 69,
        readModelCount: 71,
        subscriptionCount: 2
    }
};

/** While the dashboard's queries have not resolved yet, the widget reports itself as disconnected. */
export const Disconnected: Story = {
    args: {
        eventStore: 'Studio',
        namespace: 'Cratis',
        isConnected: false,
        eventTypeCount: 0,
        projectionCount: 0,
        readModelCount: 0,
        subscriptionCount: 0
    }
};
