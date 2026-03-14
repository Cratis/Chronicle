// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { AddWebhookDialog } from './AddWebhookDialog';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useEffect } from 'react';

const AddWebhookDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog(AddWebhookDialog);

    useEffect(() => {
        void showDialog();
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof AddWebhookDialog> = {
    title: 'Features/EventStore/General/Webhooks/AddWebhookDialog',
    component: AddWebhookDialog,
    decorators: [
        (Story) => (
            <MemoryRouter initialEntries={['/event-store/my-store/default']}>
                <Routes>
                    <Route path='/event-store/:eventStore/:namespace/*' element={<Story />} />
                </Routes>
            </MemoryRouter>
        ),
    ],
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof AddWebhookDialog>;

export const Default: Story = {
    render: () => <AddWebhookDialogStory />,
};
