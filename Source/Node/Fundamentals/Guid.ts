// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IEquatable } from './IEquatable';

const lookUpTable: string[] = [];
(() => {
    for (let i = 0; i < 256; i += 1) {
        lookUpTable[i] = (i < 16 ? '0' : '') + (i).toString(16);
    }
})();

const getString = (num: number) => {
    return num.toString(16).padStart(2, '0');
};

/**
 * Represents a Guid according to the http://www.ietf.org/rfc/rfc4122.txt
 *
 * @export
 * @class Guid
 */
export class Guid implements IEquatable {

    /**
     * Gets an empty {Guid}
     */
    static readonly empty = new Guid([0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

    private _stringVersion = '';


    /**
     * Initializes a new instance of the {Guid} class.
     * @param {number[]|Uint8Array} bytes - The 16 bytes that represents a {Guid}.
     */
    constructor(readonly bytes: number[] | Uint8Array) {
        this._stringVersion = '' +
            getString(bytes[3]) + getString(bytes[2]) + getString(bytes[1]) + getString(bytes[0]) +
            '-' +
            getString(bytes[5]) + getString(bytes[4]) +
            '-' +
            getString(bytes[7]) + getString(bytes[6]) +
            '-' +
            getString(bytes[8]) + getString(bytes[9]) +
            '-' +
            getString(bytes[10]) + getString(bytes[11]) + getString(bytes[12]) + getString(bytes[13]) + getString(bytes[14]) + getString(bytes[15]);
    }

    /**
     * Create a new {Guid}
     * @returns {Guid}
     */
    static create(): Guid {
        const d0 = Math.random() * 0xFFFFFFFF | 0;
        const d1 = Math.random() * 0xFFFFFFFF | 0;
        const d2 = Math.random() * 0xFFFFFFFF | 0;
        const d3 = Math.random() * 0xFFFFFFFF | 0;

        const bytes = [
            lookUpTable[d0 & 0xFF],
            lookUpTable[d0 >> 8 & 0xFF],
            lookUpTable[d0 >> 16 & 0xFF],
            lookUpTable[d0 >> 24 & 0xFF],
            lookUpTable[d1 & 0xFF],
            lookUpTable[d1 >> 8 & 0xFF],
            lookUpTable[d1 >> 16 & 0x0F | 0x40],
            lookUpTable[d1 >> 24 & 0xFF],
            lookUpTable[d2 & 0x3F | 0x80],
            lookUpTable[d2 >> 8 & 0xFF],
            lookUpTable[d2 >> 16 & 0xFF],
            lookUpTable[d2 >> 24 & 0xFF],
            lookUpTable[d3 & 0xFF],
            lookUpTable[d3 >> 8 & 0xFF],
            lookUpTable[d3 >> 16 & 0xFF],
            lookUpTable[d3 >> 24 & 0xFF]
        ];

        return new Guid(bytes.map(_ => parseInt(`0x${_}`, 16)));
    }

    /**
     * Parses a string and turns it into a {Guid}.
     * @param {string} guid String representation of guid.
     */
    static parse(guid: string): Guid {
        const bytes: number[] = [];
        guid.split('-').map((number, index) => {
            const bytesInChar = index < 3 ? number.match(/.{1,2}/g)?.reverse() : number.match(/.{1,2}/g);
            bytesInChar?.map((byte) => { bytes.push(parseInt(byte, 16)); });
        });

        return new Guid(bytes);
    }

    /**
     * Parses if the type is a string parse, otherwise pass through the input as desired output type.
     * @template T Type to handle for
     * @param {string|T} input String or the generic type.
     * @returns identifier Parsed or passed through
     */
    static as<T extends Guid = Guid>(input: string | T): T {
        if (typeof input === 'string') {
            return Guid.parse(input) as T;
        }
        return input as T;
    }

    /**
     * @inheritdoc
     */
    equals(other: any): boolean {
        return Guid.as(other).toString() === this.toString();
    }

    /**
     * Return a string representation of the {Guid} in the format: 00000000-0000-0000-0000-000000000000
     * @returns {string}
     */
    toString(): string {
        return this._stringVersion;
    }
}
