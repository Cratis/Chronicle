// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS8019

global using Aksio.Applications.Commands;
global using Aksio.Applications.ModelBinding;
global using Aksio.Applications.Queries;
global using Aksio.Applications.Queries.MongoDB;
global using Aksio.Applications.Validation;
global using Aksio.Cratis.Events;
global using Aksio.Cratis.EventSequences;
global using Aksio.Cratis.EventSequences.Outbox;
global using Aksio.Cratis.Integration;
global using Aksio.Cratis.Observation;
global using Aksio.Cratis.Projections;
global using Aksio.Concepts;
global using Aksio.Models;
global using Aksio.Rules;
global using Aksio.Serialization;
global using AutoMapper;
global using FluentValidation;
global using Microsoft.AspNetCore.Mvc;
global using MongoDB.Driver;
