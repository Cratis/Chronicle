// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Meta, StoryObj } from '@storybook/react';
import { ProjectionAction } from './ProjectionAction';
import strings from 'Strings';

const meta: Meta<typeof ProjectionAction> = {
    title: 'Features/EventStore/General/Projections/ProjectionAction',
    component: ProjectionAction,
    tags: ['autodocs'],
    argTypes: { onClick: { action: 'execute' } },
    parameters: {
        docs: {
            description: {
                component: 'Hover or Tab to a disabled action to read its reason. Enter, Space, and clicking must not execute it. Clear disabledReason in Controls to enable it; the old reason must disappear and clicking must execute it.',
            },
        },
    },
};

export default meta;
type Story = StoryObj<typeof ProjectionAction>;

export const CodeOwnedReadModel: Story = {
    args: {
        label: strings.eventStore.general.projections.actions.save,
        disabledReason: strings.eventStore.general.projections.saveDisabledReasons.readModelOwnedByCode,
    },
};

export const InvalidSave: Story = {
    args: {
        label: strings.eventStore.general.projections.actions.save,
        disabledReason: strings.eventStore.general.projections.saveDisabledReasons.validationErrors,
    },
};

export const InvalidPreview: Story = {
    args: {
        label: strings.eventStore.general.projections.actions.preview,
        disabledReason: strings.eventStore.general.projections.previewDisabledReasons.validationErrors,
    },
};

export const Enabled: Story = {
    args: { label: strings.eventStore.general.projections.actions.save },
};
