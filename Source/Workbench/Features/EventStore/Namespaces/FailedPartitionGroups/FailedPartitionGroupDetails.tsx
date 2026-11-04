// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { Link } from 'react-router-dom';
import { FailedPartitionGroup } from 'Features/Observation';
import { IDetailsComponentProps } from '@cratis/components/DataPage';
import { getFailureKindLabel } from './getFailureKindLabel';
import css from '../FailedPartitions/FailedPartitionDetails.module.css';

export const FailedPartitionGroupDetails = ({ item }: IDetailsComponentProps<FailedPartitionGroup>) => {
    const detailStrings = strings.eventStore.namespaces.failedPartitionGroups.details;

    const properties = [
        { label: detailStrings.observer, value: item.observerId },
        { label: detailStrings.kind, value: getFailureKindLabel(item.kind) },
        { label: detailStrings.partitions, value: item.partitionCount.toString() },
        { label: detailStrings.quarantined, value: item.quarantinedCount.toString() },
        { label: detailStrings.firstObserved, value: item.firstObserved?.toLocaleString() ?? '' },
        { label: detailStrings.lastObserved, value: item.lastObserved?.toLocaleString() ?? '' }
    ];

    return (
        <div className={css.failedPartitionDetails}>
            <h2 className={css.title}>{item.observerId}</h2>
            <dl className={css.properties}>
                {properties.map(property => (
                    <div key={property.label} className={css.property}>
                        <dt className={css.propertyLabel}>{property.label}</dt>
                        <dd className={css.propertyValue}>{property.value}</dd>
                    </div>
                ))}
            </dl>

            <h3 className={css.attemptsTitle}>{detailStrings.examples}</h3>
            <ul className={css.messages}>
                {item.examplePartitions.map(partition => (
                    <li key={partition} className={css.message}>{partition}</li>
                ))}
            </ul>
            <Link to='../failed-partitions'>{detailStrings.viewFailedPartitions}</Link>
        </div>
    );
};
