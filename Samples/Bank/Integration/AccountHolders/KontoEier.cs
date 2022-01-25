// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Integration.AccountHolders
{
    public record KontoEier(string Fnr, string Fornavn, string Etternavn, DateTime FodselsDato, string Adresse, string By, string PostNr, string Land);
}
