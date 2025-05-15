# Project Tracker

## Table of Contents
1. [Current Branches](#current-branches)
2. [Recent Commits](#recent-commits)
3. [Uncommitted Changes](#uncommitted-changes)
4. [Task Status](#task-status)
5. [Important Notes](#important-notes)
6. [Project Structure](#project-structure)

## Current Branches
- **dev**: Main development branch.
- **feature/add-cursor-settings**: Current active feature branch.

## Recent Commits
- **1054bdb**: Add twoTransactions API
- **ad5d8bc**: Fix crossChainTransfer methodName
- **dd038e4**: Use IPFS image for SGR
- **0d77398**: Add NFT item API
- **e923460**: Fix assets-NFT items balance

## Uncommitted Changes
- **Modified**: `src/EoaServer.HttpApi.Host/appsettings.json`
- **Untracked**: `test/EoaServer.Application.Tests/MongoConvertTest.cs`

## Task Status
- **Feature: Add Cursor Settings**
  - **Status**: In Progress
  - **Tasks**:
    - Implement cursor settings functionality

## Important Notes
- Ensure all changes are committed before merging into 'dev'.
- Review untracked files for relevance to the current feature.

## Project Structure
- **EoaServer.HttpApi.Host**: Hosts the HTTP API for the server.
- **EoaServer.Application**: Contains application logic and services.
- **EoaServer.DbMigrator**: Handles database migrations.
- **EoaServer.Application.Contracts**: Defines contracts for application services.
- **EoaServer.Domain.Shared**: Shared domain logic and entities.
- **EoaServer.HttpApi**: Implements the HTTP API endpoints.
- **EoaServer.EntityEventHandler**: Manages entity events.
- **EoaServer.Silo**: Manages the server silo for distributed systems.
- **EoaServer.EntityEventHandler.Core**: Core logic for entity event handling.
- **EoaServer.HttpApi.Client**: Client for interacting with the HTTP API.
- **EoaServer.AuthServer**: Handles authentication services.
- **EoaServer.MongoDB**: MongoDB integration and data access.
- **EoaServer.Grains**: Implements grains for Orleans.
- **EoaServer.Domain**: Core domain logic and entities. 