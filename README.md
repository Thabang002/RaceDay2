# RaceDay — Event Management & Race Timing System

A full-stack race-event management platform built with ASP.NET Core 10, Entity Framework Core, and SQL Server. RaceDay allows organisers to create and manage events, categories, and results, while participants can browse events, enrol in categories, and track their race results.

The system consists of three projects:

| Project | Purpose |
|---------|---------|
| RaceDay.Api | REST API — powers the entire system (Part 2) |
| RaceDay.Web | MVC front end consuming the API (Part 3) |
| RaceDay.Tests | xUnit integration & unit tests (Part 2 verification) |

---

## Table of Contents

1. [Features](#features)
2. [Tech Stack](#tech-stack)
3. [Solution Structure](#solution-structure)
4. [Getting Started](#getting-started)
5. [Configuration](#configuration)
6. [Database Setup](#database-setup)
7. [Running the API](#running-the-api)
8. [Running the Web Front End](#running-the-web-front-end)
9. [API Endpoints](#api-endpoints)
10. [Authentication & Roles](#authentication--roles)
11. [Testing](#testing)
12. [CI/CD](#cicd)
13. [Troubleshooting](#troubleshooting)
14. [License](#license)
15. [Author](#author)
16. [Acknowledgements](#acknowledgements)

---

## Features

### Participants
- Register and log in securely (JWT-based auth)
- Browse all public events with filters (date, location, status)
- View event details, categories, and weather forecast
- Enrol in event categories (with capacity checks)
- Withdraw from enrolments
- View personal race results
- View public leaderboards per event/category

### Organisers
- Create, update, and soft-delete events
- Manage locations
- Define race categories (distance, fee, capacity)
- View all enrolments per event
- Update payment statuses
- Capture and update race results (finish time, position, pace)
- View full event results ranked by position

### System
- Role-based access control (Organiser vs. Participant)
- Password hashing with BCrypt
- In-memory DB for tests (no SQL Server dependency during testing)
- Swagger UI for interactive API exploration
- Automated test suite (xUnit + WebApplicationFactory)
- GitHub Actions CI pipeline

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Language | C# 13 |
| Framework | .NET 10.0 |
| API | ASP.NET Core Web API |
| Web | ASP.NET Core MVC + Razor Views |
| ORM | Entity Framework Core 10 (Code-First) |
| Database | SQL Server (LocalDB for dev, InMemory for tests) |
| Auth | JWT Bearer tokens |
| Password Hashing | BCrypt.Net-Next |
| API Docs | Swashbuckle (Swagger UI) |
| Testing | xUnit, Microsoft.AspNetCore.Mvc.Testing |
| CI/CD | GitHub Actions |

---

## Solution Structure
