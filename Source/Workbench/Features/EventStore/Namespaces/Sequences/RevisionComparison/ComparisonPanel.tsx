// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useId } from 'react';
import strings from 'Strings';
import { ComparisonViewModel } from './ComparisonViewModel';
import { RevisionChoice } from './RevisionChoice';
import { statusMessage } from './statusMessage';
import css from './Comparison.module.css';

const text = () => strings.eventStore.namespaces.sequences.details.compare;

const choiceLabel = (choice: RevisionChoice) => choice.ordinal === 0
    ? text().original
    : text().revision.replace('{n}', String(choice.ordinal));

const describe = (choice: RevisionChoice | undefined) => choice && [
    choiceLabel(choice),
    text().generation.replace('{n}', String(choice.generation)),
    text().occurred.replace('{value}', choice.occurred || '-'),
    text().correlation.replace('{id}', choice.correlationId || '-')
].join(' · ');

interface SideSelectProps {
    label: string;
    value?: string;
    choices: RevisionChoice[];
    onChange: (key: string) => void;
}

const SideSelect = ({ label, value, choices, onChange }: SideSelectProps) => {
    const id = useId();
    return (
        <div className={css.side}>
            <label htmlFor={id}>{label}</label>
            <select id={id} value={value ?? ''} onChange={event => onChange(event.target.value)}>
                {value === undefined && <option value='' disabled></option>}
                {value !== undefined && !choices.some(choice => choice.key === value) && <option value={value} disabled>{text().unavailableChoice}</option>}
                {choices.map(choice => <option key={choice.key} value={choice.key}>{describe(choice)}</option>)}
            </select>
        </div>
    );
};

/**
 * Presents a {@link ComparisonViewModel}: an explicit open control, two labelled revision pickers,
 * and the bounded differences as text. Payload content is only ever rendered as React text.
 * @param props The view model to present.
 * @returns The comparison panel.
 */
export const ComparisonPanel = ({ viewModel }: { viewModel: ComparisonViewModel }) => {
    const compare = text();
    const { revisions, comparison } = viewModel;
    const message = statusMessage(revisions, comparison);
    const showRows = comparison && (comparison.outcome === 'different' || comparison.outcome === 'limited');

    if (!viewModel.isOpen) {
        return <div className={css.panel}><button type='button' onClick={() => viewModel.open()}>{compare.open}</button></div>;
    }
    return (
        <section className={css.panel} aria-label={compare.title}>
            <div className={css.heading}>
                <h3>{compare.title}</h3>
                <button type='button' onClick={() => viewModel.close()}>{compare.close}</button>
            </div>
            {revisions.state === 'ready' && (
                <div className={css.selectors}>
                    <SideSelect label={compare.left} value={viewModel.leftKey} choices={revisions.choices} onChange={key => viewModel.selectLeft(key)} />
                    <SideSelect label={compare.right} value={viewModel.rightKey} choices={revisions.choices} onChange={key => viewModel.selectRight(key)} />
                </div>
            )}
            {revisions.ambiguous > 0 && <p>{compare.status.ambiguous.replace('{count}', String(revisions.ambiguous))}</p>}
            <p role='status'>{message}</p>
            {showRows && (
                <div className={css.results} role='region' aria-label={compare.scroll} tabIndex={0}>
                    <table>
                        <caption>{compare.table}</caption>
                        <thead>
                            <tr>
                                <th scope='col'>{compare.columns.path}</th>
                                <th scope='col'>{compare.columns.change}</th>
                                <th scope='col'>{compare.columns.before} — {describe(viewModel.left)}</th>
                                <th scope='col'>{compare.columns.after} — {describe(viewModel.right)}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {comparison.rows.map((row, index) => (
                                <tr key={index}>
                                    <th scope='row'><code>{row.path === '' ? compare.root : row.path}</code></th>
                                    <td>{compare.kinds[row.kind]}</td>
                                    <td>{row.before ?? <em>{compare.absent}</em>}</td>
                                    <td>{row.after ?? <em>{compare.absent}</em>}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </section>
    );
};
