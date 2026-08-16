// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type ReactNode } from 'react';
import { Message as PrimeMessage } from 'primereact/message';

/** The severities the Workbench uses for inline messages. */
export type MessageSeverity = 'info' | 'success' | 'warn' | 'error' | 'secondary' | 'contrast';

/**
 * Props for {@link Message}.
 */
export interface MessageProps {
    /** Controls the message's coloring. */
    severity?: MessageSeverity;
    /** The message text. */
    text?: ReactNode;
    /** Rendered in place of `text` when given. */
    children?: ReactNode;
    /** Applied to the message root. */
    className?: string;
}

/**
 * A declarative inline message over PrimeReact 11's compositional `Message` parts.
 *
 * PrimeReact 10's `<Message severity text />` became `Message.Root` +
 * `Message.Content` + `Message.Text` in 11; this preserves the flat call shape
 * the Workbench already uses.
 */
export const Message = ({ severity = 'info', text, children, className }: MessageProps) => (
    <PrimeMessage.Root severity={severity} className={className}>
        <PrimeMessage.Content>
            <PrimeMessage.Text>{children ?? text}</PrimeMessage.Text>
        </PrimeMessage.Content>
    </PrimeMessage.Root>
);
