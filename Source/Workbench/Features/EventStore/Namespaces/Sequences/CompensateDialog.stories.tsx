// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { CompensateDialog } from './CompensateDialog';
import { AppendedEvent } from 'Api/Events';
import { EventContext } from 'Api/Events/EventContext';
import { EventType } from 'Api/Events/EventType';
import { DialogContext, DialogContextContent } from '@cratis/arc.react/dialogs';

const mockEventType = Object.assign(new EventType(), {
    id: 'user-registered',
    generation: 1,
    tombstone: false,
});

const mockEventContext = Object.assign(new EventContext(), {
    eventType: mockEventType,
    eventSourceType: 'User',
    eventSourceId: '00000000-0000-0000-0000-000000000001',
    sequenceNumber: 42,
    eventStreamType: 'default',
    eventStreamId: 'event-log',
    occurred: new Date('2024-01-15T10:30:00Z'),
    correlationId: '00000000-0000-0000-0000-000000000099',
    causation: [],
    causedBy: { subject: 'admin', name: 'Admin User', userName: 'admin' },
    tags: [],
});

const mockEvent = Object.assign(new AppendedEvent(), {
    context: mockEventContext,
    content: JSON.stringify({
        name: 'John Doe',
        email: 'john.doe@example.com',
        registeredAt: '2024-01-15T10:30:00Z',
    }),
});

const noop = (_result: unknown) => { return _result; };

const mockContextValue = new DialogContextContent(
    { event: mockEvent, eventStore: 'my-store', namespace: 'default' },
    noop
);

const meta: Meta<typeof CompensateDialog> = {
    title: 'Features/EventStore/Namespaces/Sequences/CompensateDialog',
    component: CompensateDialog,
    decorators: [
        (Story) => (
            <MemoryRouter>
                <DialogContext.Provider value={mockContextValue}>
                    <Story />
                </DialogContext.Provider>
            </MemoryRouter>
        ),
    ],
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof CompensateDialog>;

export const Default: Story = {
    render: () => <CompensateDialog />,
};

export const WithInvalidJson: Story = {
    decorators: [
        (Story) => (
            <MemoryRouter>
                <DialogContext.Provider
                    value={new DialogContextContent(
                        {
                            event: Object.assign(new AppendedEvent(), {
                                context: mockEventContext,
                                content: '{"name": "Jane Doe"}',
                            }),
                            eventStore: 'my-store',
                            namespace: 'default',
                        },
                        noop
                    )}>
                    <Story />
                </DialogContext.Provider>
            </MemoryRouter>
        ),
    ],
    render: () => <CompensateDialog />,
};

