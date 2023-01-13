// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS8019

global using Aksio.Cratis.Applications.Commands;
global using Aksio.Cratis.Applications.Queries;
global using Aksio.Cratis.Applications.Queries.MongoDB;
global using Aksio.Cratis.Applications.Rules;
global using Aksio.Cratis.Concepts;
global using Aksio.Cratis.Events;
global using Aksio.Cratis.EventSequences;
global using Aksio.Cratis.EventSequences.Outbox;
global using Aksio.Cratis.Observation;
global using Aksio.Cratis.Projections;
global using Aksio.Cratis.Integration;
global using Aksio.Cratis.Models;
global using Aksio.Cratis.Serialization;
global using AutoMapper;
global using FluentValidation;
global using Microsoft.AspNetCore.Mvc;
global using MongoDB.Driver;
