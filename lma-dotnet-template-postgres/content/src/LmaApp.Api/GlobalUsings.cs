// ─── LMA Global Usings ───────────────────────────────────────────────────────
// Centraliza os usings mais comuns da API para não repetir em cada arquivo.
// Não adicione usings muito específicos aqui — prefira declará-los no arquivo.

// Domain
global using LmaApp.Domain.Modulos._Common;

// Infrastructure
global using LmaApp.Api.Infrastructure.Database;

// Entity Framework
global using Microsoft.EntityFrameworkCore;

// FluentValidation
global using FluentValidation;

// Logging
global using Microsoft.Extensions.Logging;

// Cancellation
global using System.Threading;
global using System.Threading.Tasks;
