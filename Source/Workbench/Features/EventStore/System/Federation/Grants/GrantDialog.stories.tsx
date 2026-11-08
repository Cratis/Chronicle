// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter } from 'react-router-dom';
import { useEffect } from 'react';
import { useDialog } from '@cratis/arc.react/dialogs';
import { GrantDialog, GrantDialogRequest } from './GrantDialog';

const GrantDialogStory = () => {
    const [DialogWrapper, showDialog] = useDialog<object, GrantDialogRequest>(GrantDialog);

    useEffect(() => {
        void showDialog({});
    }, [showDialog]);

    return <DialogWrapper />;
};

const meta: Meta<typeof GrantDialog> = {
    title: 'Features/EventStore/System/Federation/GrantDialog',
    component: GrantDialog,
    decorators: [
        (Story) => (
            <MemoryRouter>
                <Story />
            </MemoryRouter>
        ),
    ],
    tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof GrantDialog>;

export const Default: Story = {
    render: () => <GrantDialogStory />,
};
