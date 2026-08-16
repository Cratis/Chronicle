// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type CSSProperties, type MouseEventHandler, type ReactNode } from 'react';
import { Button as PrimeButton } from 'primereact/button';
import type { ButtonProps as ButtonRootProps } from '@primereact/types/primitive/button';
import { Tooltip, type TooltipPosition } from '@cratis/components/Common';

/** The severities PrimeReact 11 accepts for a button. */
export type ButtonSeverity = 'secondary' | 'info' | 'success' | 'warn' | 'help' | 'danger' | 'contrast';

/**
 * Props for {@link Button}.
 */
export interface ButtonProps {
    /** The button's text. */
    label?: ReactNode;
    /**
     * The button's icon — either a PrimeIcons class name (`'pi pi-check'`) or an
     * element. Rendered before the label.
     */
    icon?: ReactNode;
    /** Replaces the icon with a spinner and disables the button. */
    loading?: boolean;
    /** Text shown on hover. */
    tooltip?: string;
    /** Placement of the tooltip. */
    tooltipOptions?: { position?: TooltipPosition; className?: string };
    /** PrimeReact pass-through for the underlying button. */
    pt?: ButtonRootProps['pt'];
    /** Renders the button borderless. */
    text?: boolean;
    /** Renders the button with an outline instead of a fill. */
    outlined?: boolean;
    /** Renders the button fully rounded. */
    rounded?: boolean;
    /** Controls the button's coloring. */
    severity?: ButtonSeverity;
    /** Sizes the button. */
    size?: 'small' | 'normal' | 'large';
    /** Whether the button is disabled. */
    disabled?: boolean;
    /** Native button type. */
    type?: 'button' | 'submit' | 'reset';
    /** Called when the button is activated. */
    onClick?: MouseEventHandler<HTMLButtonElement>;
    /** Applied to the button element. */
    className?: string;
    /** Applied to the button element. */
    style?: CSSProperties;
    /** Accessible name — required when the button renders an icon and no label. */
    'aria-label'?: string;
    /** Rendered inside the button, after the icon and label. */
    children?: ReactNode;
}

const renderIcon = (icon: ReactNode) =>
    typeof icon === 'string' ? <i className={icon} aria-hidden='true' /> : icon;

/**
 * A button carrying the `label` / `icon` / `loading` / `tooltip` authoring model.
 *
 * PrimeReact 11's `Button` takes its content as **children** and dropped `label`,
 * `icon`, `loading`, `tooltip` and `text` entirely. Because its props type is
 * generic over `React.ElementType`, those props are still *accepted by the
 * compiler* and silently ignored at runtime — a `<Button label="Save" />`
 * typechecks and renders an empty button. This wrapper closes that trap for the
 * whole Workbench rather than leaving ~30 call sites to be caught by eye.
 */
export const Button = ({
    label,
    icon,
    loading,
    tooltip,
    tooltipOptions,
    pt,
    text,
    outlined,
    rounded,
    severity,
    size,
    disabled,
    type = 'button',
    onClick,
    className,
    style,
    'aria-label': ariaLabel,
    children
}: ButtonProps) => {
    const variant = text ? 'text' : outlined ? 'outlined' : undefined;

    const button = (
        <PrimeButton
            type={type}
            variant={variant}
            rounded={rounded}
            severity={severity}
            size={size}
            iconOnly={!!icon && label === undefined && !children}
            disabled={disabled || loading}
            onClick={onClick}
            className={className}
            style={style}
            aria-label={ariaLabel}
            pt={pt}>
            {loading ? <i className='pi pi-spinner pi-spin' aria-hidden='true' /> : renderIcon(icon)}
            {label}
            {children}
        </PrimeButton>
    );

    return tooltip
        ? <Tooltip content={tooltip} position={tooltipOptions?.position} className={tooltipOptions?.className}>{button}</Tooltip>
        : button;
};
