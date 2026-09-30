// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactElement } from 'react';
import { Link } from 'react-router-dom';
import { MdCheckCircle, MdErrorOutline, MdHourglassBottom, MdInfo, MdLinkOff, MdNotificationImportant } from 'react-icons/md';
import { AttentionItem, AttentionKind } from './attention';
import { WidgetShell } from './WidgetShell';
import strings from 'Strings';

export interface IAttentionWidget {

    /**
     * The items needing attention, or undefined while loading.
     */
    items: AttentionItem[] | undefined;

    /**
     * The path the namespace pages live under.
     */
    namespacePath: string;

    /**
     * Additional class names.
     */
    className?: string;
}

const texts = strings.eventStore.dashboard;

const pageFor: Record<AttentionKind, string> = {
    failedPartition: 'failed-partitions',
    observer: 'observers',
    lagging: 'observers',
    recommendation: 'recommendations'
};

const iconFor: Record<AttentionKind, ReactElement> = {
    failedPartition: <MdErrorOutline size={18} />,
    observer: <MdLinkOff size={18} />,
    lagging: <MdHourglassBottom size={18} />,
    recommendation: <MdInfo size={18} />
};

const detailFor = (item: AttentionItem): string => {
    switch (item.kind) {
        case 'observer': return item.detail === 'quarantined' ? texts.observerQuarantined : texts.observerDisconnected;
        case 'lagging': return texts.observerBehind;
        default: return item.detail;
    }
};

const asideFor = (item: AttentionItem): string | undefined => {
    if (item.count === undefined) return undefined;
    return item.kind === 'lagging'
        ? texts.eventsBehindCount.replace('{count}', item.count.toLocaleString())
        : texts.attemptsCount.replace('{count}', item.count.toLocaleString());
};

/**
 * What needs looking at on a namespace - failed partitions, stopped observers, recommendations and observers that
 * are behind - each leading to the page where it can be acted on.
 */
export const AttentionWidget = ({ items, namespacePath, className }: IAttentionWidget) => {
    const severe = items?.some(_ => _.tone === 'danger') ?? false;
    const any = (items?.length ?? 0) > 0;

    return (
        <WidgetShell
            title={texts.needsAttentionTitle}
            icon={<MdNotificationImportant />}
            tone={items === undefined ? 'neutral' : severe ? 'danger' : any ? 'warning' : 'success'}
            className={className}>
            {items === undefined && <span className='dashboard-widget__empty'>{texts.loading}</span>}
            {items !== undefined && !any && (
                <span className='dashboard-widget__empty flex items-center gap-2'>
                    <MdCheckCircle style={{ color: 'var(--dashboard-success)' }} /> {texts.nothingNeedsAttention}
                </span>
            )}
            {any && (
                <div className='dashboard-list'>
                    {items!.map(item => (
                        <Link key={item.key} to={`${namespacePath}/${pageFor[item.kind]}`} className={`dashboard-list__item dashboard-list__item--${item.tone}`}>
                            <span className='dashboard-list__icon'>{iconFor[item.kind]}</span>
                            <span className='dashboard-list__text'>
                                <span className='dashboard-list__title' title={item.subject}>{item.subject}</span>
                                <span className='dashboard-list__detail' title={detailFor(item)}>{detailFor(item)}</span>
                            </span>
                            {asideFor(item) && <span className='dashboard-list__aside'>{asideFor(item)}</span>}
                        </Link>
                    ))}
                </div>
            )}
        </WidgetShell>
    );
};
