# AGENTS.md — Engineering Guidelines for Autonomous & Interactive Agents

This document is the authoritative engineering manual and operational contract for all AI coding agents working in this repository. Every agent session must consult and abide by the rules, architectural decisions, domain models, communication invariants, and workflow constraints established herein.

---

## 1. Project Overview & Engineering Context

**Boardy** is a high-performance, browser-based, real-time multiplayer board game platform designed for deep, stateful, server-authoritative tabletop experiences rendered in 3D browser graphics.

### Primary Objectives
- **Concurrent Multiplayer Sessions**: Support multiple simultaneous, isolated game rooms and active matches on a shared backend infrastructure.
- **Flexible Identity Management**: Support both registered users (persistent credentials, profiles, statistics) and guest users (low-friction, ephemeral or persistent guest tokens).
- **Frictionless Room Discovery & Joining**: Room-code-based joining (short alphanumeric codes) alongside direct links and lobby listings.
- **First-Class Spectator Mode**: Enable non-participating observers to watch live games with real-time state synchronization, zero interference with match flow, and strict filtering of private information.
- **Server-Authoritative Game State**: All game logic, state transitions, validation, and random number generation (RNG) occur strictly on the server. Clients submit player intents/actions; the server validates and dispatches outcomes.
- **Information Boundary Enforcement**: Separation of public game state, private player-visible state (e.g., hidden cards, secret objectives), and restricted/masked administrative state.
- **Fault-Tolerant State Recovery & Reconnection**: Seamless reconnection for dropped sockets, authoritative snapshot catch-up, and resumption of matches across temporary network disruptions and server restarts.
- **Extensible Game Engine Interface**: Clean, decoupled contracts allowing distinct board game implementations (e.g., Catan, Chess, Carcassonne) to be developed, tested, and maintained as pluggable modules without altering platform infrastructure.
- **Rich 3D Web Presentation**: Frontend game boards rendered using modern WebGL/WebGPU technologies (Three.js, React Three Fiber, Drei) driven by declarative state projections from the backend.
- **Initial Flagship Game**: **Catan** (board generation, hex tiles, settlements, roads, cities, development cards, dice rolling, resource distribution, robber mechanics, and trading).

---

## 2. Technology Stack & Technical Baseline

All agents must adhere to the selected technology stack. Introducing alternative core frameworks, ORMs, or state libraries without explicit architectural review is strictly prohibited.

| Layer / Component | Technology | Role & Notes |
| :--- | :--- | :--- |
| **Backend Language** | C# (latest C# features supported by .NET 10) | Strong typing, record types, pattern matching, async/await idioms. |
| **Backend Framework** | ASP.NET Core (.NET 10 LTS) | Modular HTTP web APIs, middleware pipeline, DI container. |
| **Real-Time Transport** | SignalR over WebSockets | Bidirectional event streaming, state push, and client action dispatch. |
| **Relational Database** | PostgreSQL | Authoritative persistent storage for accounts, rooms, matches, and outbox logs. |
| **Object-Relational Mapper** | Entity Framework Core (EF Core) | Code-First migrations, optimized relational querying, entity configuration. |
| **Authentication & Identity** | ASP.NET Core Identity | Cookie/JWT authentication, password hashing, claims-based authorization. |
| **Validation** | FluentValidation | Expressive, strongly typed command and DTO validation rules. |
| **Logging & Observability** | Serilog | Structured JSON logging with enriched correlation IDs and match contexts. |
| **Testing Framework** | xUnit, FluentAssertions, Moq/NSubstitute | Unit, domain logic, and integration testing suites. |
| **Local Infrastructure** | Docker Compose | Local PostgreSQL, migrations, and local auxiliary services. |
| **Frontend Framework** | React + TypeScript + Vite | Component architecture, strict TypeScript configuration, fast HMR. |
| **Frontend State Management** | Zustand | Lightweight, decoupled global stores for connection, room, and game projections. |
| **3D Rendering** | Three.js + React Three Fiber + @react-three/drei | Declarative 3D canvas, lighting, camera controls, materials, and procedural board meshes. |

---

## 3. Architectural Approach & Dependency Boundaries

The backend is organized as a **Modular Monolith** adhering to **Clean Architecture** and **Domain-Driven Design (DDD)** principles, utilizing conventional C# object-oriented programming.

### Architectural Dependency Rule
Dependencies strictly point inward. Outer layers depend on inner layers; inner layers must never depend on outer layers.

```
[ API Layer (Web API, SignalR Hubs, Controllers) ]
       │                      │
       ▼                      ▼
[ Infrastructure Layer ] ───► [ Application Layer ]
       │                      │
       ▼                      ▼
           [ Core Domain Layer ]
                     ▲
                     │
    [ Games Layer (Pluggable Game Engines) ]
```

### Layer Responsibilities & Boundaries

1. **Domain Layer (`Boardy.Domain`)**:
   - Contains pure enterprise logic, domain entities, value objects, domain events, domain exceptions, and repository/service interfaces.
   - Zero external dependencies (no EF Core, no ASP.NET Core, no SignalR, no HTTP libraries).
   - Enforces core invariants around rooms, players, identities, and match lifecycles.

2. **Application Layer (`Boardy.Application`)**:
   - Implements application use cases, command and query handlers (CQS pattern), DTOs, projection mappers, and orchestration.
   - Defines output interfaces (e.g., `IRoomRepository`, `IMatchNotifier`, `IUnitOfWork`, `IOutboxService`).
   - Handles cross-cutting use case validation (FluentValidation) and transaction boundaries.
   - Depends only on the Domain Layer.

3. **Infrastructure Layer (`Boardy.Infrastructure`)**:
   - Implements interfaces defined by the Application and Domain layers.
   - Contains database contexts (`AppDbContext`), EF Core entity type configurations, migrations, transactional outbox tables, repository implementations, and external integration adapters.
   - Depends on the Application and Domain layers.

4. **API / Presentation Layer (`Boardy.Api`)**:
   - The executable host and composition root.
   - Contains ASP.NET Core Controllers, SignalR Hubs (`RoomHub`, `MatchHub`), middleware (exception handling, authentication/authorization, logging enrichers), and dependency injection wire-up.
   - Translates incoming WebSocket/SignalR invocations and HTTP requests into application commands/queries.
   - Depends on Application and Infrastructure layers (for DI configuration only).

5. **Games Layer (`Boardy.Games.*`, e.g., `Boardy.Games.Catan`)**:
   - Encapsulates isolated, self-contained board game engines implementing core domain engine contracts (e.g., `IGameEngine<TState, TAction>`).
   - Responsible for game rule enforcement, deterministic state transitions, player action validation, victory condition evaluations, and projection filtering (public vs. private views).
   - Game engines must not perform I/O, database queries, or network calls directly. They accept a state and an action, returning a validated next state, domain events, and state mutations.

---

## 4. Domain Concepts & Core Decisions

Agents must preserve the clear conceptual boundaries defined below. Mixing or conflating these concepts is an architectural error.

### 4.1 Concept Distinctions

- **User vs. Connection**:
  - `User`: A persistent or authenticated identity (`UserId`), either registered or guest. Represents a real-world participant across sessions.
  - `Connection`: A transient physical transport channel (`ConnectionId` in SignalR). A single user can hold multiple simultaneous connections (e.g., desktop browser tab + mobile device).
  - *Rule*: Authorization, permissions, and turns belong to the `User`, never to a `ConnectionId`.

- **Room vs. Room Membership**:
  - `Room`: A shared social and preparatory container identified by a unique `RoomId` and a human-friendly `RoomCode`. Manages membership, privacy, game configuration, and match launches.
  - `RoomMembership`: The associative relationship between a `User` and a `Room`, tracking joined timestamp, ready state, seat/slot index, and member role (Host, Player, Spectator).

- **Match vs. Match Participation**:
  - `Match`: An authoritative instance of an active, played game, having a defined lifecycle, ruleset, outcome, and historical record.
  - `MatchParticipation`: The participation record linking a `User` to a specific player seat/color in a `Match`, holding match-specific scores, turn status, and connectivity health.

- **Match vs. Game Session**:
  - `Match`: The durable domain entity tracking metadata, competitors, start/end timestamps, scores, and permanent match history in PostgreSQL.
  - `GameSession`: The runtime in-memory execution unit that hosts the active game engine state machine, serializes player actions, generates versions, and handles real-time delta broadcasts.

- **Game Session vs. Game Definition**:
  - `GameDefinition`: Static metadata, ruleset parameters, asset references, and constraints for a game type (e.g., Catan standard rules, min 3, max 4 players).
  - `GameSession`: A live, dynamic execution instance executing a `GameDefinition` with specific participants and mutable state.

- **Participant vs. Spectator**:
  - `Participant (Player)`: Bound to a playable seat. Can issue state-mutating game actions when permitted by game rules. Receives private state projections (their own cards/secrets) along with public state.
  - `Spectator`: Read-only observer. Cannot submit gameplay actions. Only receives sanitized public state projections. Can join or leave without disrupting turns, timers, or player counts.

### 4.2 Lifecycles & State Transitions

#### Room Lifecycle
```
[Created] ──► [Lobby / Waiting] ◄────► [Match In Progress]
                      │                         │
                      ▼                         ▼
                  [Closed]                [Match Ended]
```
1. **Created**: Room initialized by a Host; assigned a unique `RoomCode`.
2. **Lobby / Waiting**: Members join, select roles/seats, toggle Ready status, and configure game rules.
3. **Match In Progress**: Host triggers launch; all ready players are enrolled into a `Match`. Room enters active match mode.
4. **Match Ended**: Match concludes or is aborted; room returns to Lobby state for post-game chat or rematch, or transitions to Closed.
5. **Closed**: Host leaves, all members disconnect for longer than retention threshold, or room is explicitly disbanded.

#### Match Lifecycle
```
[Initializing] ──► [InProgress] ◄──► [Paused / Disconnected]
                         │
        ┌────────────────┼────────────────┐
        ▼                ▼                ▼
   [Completed]      [Forfeited]       [Aborted]
```
1. **Initializing**: Game engine initializes initial state (e.g., procedural Catan board generation).
2. **InProgress**: Active turn loop running; state versions increment with each accepted action.
3. **Paused / Disconnected**: Optional temporary pause state if key participants disconnect during critical timed windows.
4. **Completed**: Natural game termination meeting victory criteria (e.g., player reaches 10 victory points).
5. **Forfeited**: One or more participants forfeit (manually or via timeout), awarding victory under game-specific rules.
6. **Aborted**: Match terminated prematurely by host or administrative exception without recording standard rankings.

### 4.3 Identity & Admission Policies
- **Guest Identities**: Issued a cryptographically signed, persistent token (stored in browser local storage or secure cookie) allowing reconnects without forcing full account registration.
- **Room Admission**:
  - Private rooms require an exact alphanumeric `RoomCode`.
  - Max player capacity strictly enforced by room settings.
  - Late-joining users when a match is in progress are automatically admitted as **Spectators** only.
- **Forfeiture Behavior**:
  - Closing a browser does not cause instantaneous forfeiture. A reconnection grace period (e.g., 60-120 seconds) must elapse before initiating turn skipping or auto-forfeiture.
  - Voluntary forfeit immediately resigns the player and triggers forfeiture rules.
- **Spectator Permissions**:
  - Spectators can send room/chat messages (if enabled) but are barred from invoking game command endpoints.

---

## 5. Non-Negotiable Communication & Architecture Invariants

The following 20 decisions are established architectural invariants. They must be treated as hard constraints.

1. **SignalR over WebSockets**: WebSockets is the mandatory primary transport. Long polling is strictly a fallback for restricted environments.
2. **Multiple Simultaneous Connections per User**: A user may connect from multiple tabs, windows, or devices. The system must route events to all active connections belonging to that `UserId`.
3. **Stable Authenticated User Identity**: Gameplay authorization is anchored to `UserId`. Connection IDs are volatile and must never be treated as the identity key.
4. **Room Membership Independent of Connections**: Room membership resides in domain state. Dropping a WebSocket connection does NOT automatically remove a player from room or match membership.
5. **SignalR Groups are Delivery Mechanisms, Not Authorization Boundaries**: SignalR groups (`room-{id}`, `match-{id}`) are broadcast optimization utilities. Every incoming action must be independently authorized against domain state regardless of group membership.
6. **Server-Authoritative Game State**: Clients are thin visualizers. State transitions, validations, dice rolls, tile shuffles, and victory checks occur strictly on the server.
7. **Separate Public, Private, and Restricted Information**: The server maintains full master state. Game state must be filtered before transmission:
   - *Public*: Board tiles, visible settlements, open resource counts, turn tracker.
   - *Private*: Specific resource cards in hand, unplayed development cards, hidden agendas.
   - *Restricted*: System seeds, internal rollback buffers, upcoming deck orders.
8. **Recipient-Specific State Projections**: Never broadcast master game state to a shared room group if it contains private information. The server projects state per recipient `UserId` before dispatch.
9. **Real-Time Spectator Updates**: Spectators receive real-time public updates alongside players with zero latency penalty, receiving only public projections.
10. **Snapshot + Event Synchronization**: State sync combines periodic/on-demand authoritative snapshots with continuous sequential delta events.
11. **One Authoritative Match-Wide State-Version Sequence**: Every discrete state mutation in a match increments a monotonic integer version sequence (`StateVersion`: 1, 2, 3...). Clients track their local version to detect missed events.
12. **Unique Action IDs for State-Changing Commands**: Every client command must carry a client-generated UUID `ActionId`.
13. **Idempotent Command Processing**: Duplicate command transmissions with the same `ActionId` must return the previously computed outcome or acknowledge without re-executing logic.
14. **Serialized State-Changing Operations per Game Session**: State modifications for a given `GameSession` must execute serially (via actor pattern, dedicated channel, or per-session synchronization locks) to prevent race conditions.
15. **At-Least-Once Delivery Attempts for Important Events**: Critical match events (turn changes, game over, critical trades) require guaranteed delivery mechanisms and client sequence tracking.
16. **Transactional Outbox Pattern**: When database writes and event broadcasts must both occur, state changes and outgoing event records are committed within the same database transaction before publication.
17. **PostgreSQL as the Initial Durable Source of Truth**: All permanent state, user accounts, room records, match histories, and outbox logs are stored in PostgreSQL.
18. **Authoritative Snapshot Recovery after Reconnection**: Upon reconnecting or detecting a gap in the `StateVersion` sequence, the client requests a full authoritative state snapshot for its `UserId`.
19. **Active Game Session Recovery after Backend Restarts**: In-progress game state must be periodically checkpointed or reconstituted from event logs so that backend restarts allow active matches to resume.
20. **Server Restarts Must Not Automatically Trigger Match Abandonment**: Transient server restarts or deployments must retain match states in persistent storage; players rejoining within a reconnect window continue the game.

---

## 6. Engineering Standards & Agent Operational Rules

Agents operating in this repository must maintain high code hygiene, clean separation of concerns, and conservative operational discipline.

### 6.1 Code Modification & Design Principles
- **Inspect Before Modifying**: Always read relevant existing source code, schemas, and configurations before proposing or making edits.
- **Narrowly Scoped Changes**: Implement only the immediate task requested. Do not introduce premature generalizations, speculative helpers, or unused abstractions.
- **No Unrelated Refactoring**: Do not reformat, rename, or reorganize code outside the direct boundary of the current task.
- **Preserve Architectural Boundaries**: Never bypass layer constraints (e.g., never query `DbContext` directly inside a domain entity, game engine, or SignalR hub).
- **Asynchronous I/O & Cancellation**: All I/O operations must be asynchronous (`async`/`await`) and accept a `CancellationToken` parameter where applicable.
- **Domain Modeling**: Favor strong typing, records, and domain value objects (e.g., `RoomCode`, `UserId`, `PlayerColor`) over primitive obsession (`string`, `int`, `Guid`).
- **Validation**: Place business invariants inside domain entities/aggregates; place request format and boundary validation inside Application FluentValidation validators.

### 6.2 Testing Standards
- **Mandatory Test Coverage**: Write automated unit tests for all domain rules, state machines, and game logic alongside the implementation.
- **Isolated Game Engine Tests**: Game engines (e.g., Catan board generation, trade execution, resource distribution) must be tested exhaustively via pure unit tests with zero mocks and zero network/database dependencies.
- **Integration Tests**: Test API endpoints, EF Core queries, and SignalR hubs using `WebApplicationFactory` and in-memory or containerized PostgreSQL test harnesses.

### 6.3 Git & Environment Safety Rules
- **Never Overwrite Unrelated Work**: Always verify diffs before modifying files.
- **No Force Operations**: Never force-push (`git push -f`) or reinitialize the Git repository (`rm -rf .git`).
- **Commit Policy**: **Do not run `git commit` unless explicitly instructed by the user.** Leave changes staged or unstaged as requested.
- **No Destructive Database Changes**: EF Core migrations must preserve existing data unless a breaking schema migration is explicitly authorized.

---

## 7. Implementation Workflow for Future Tasks

Agents must execute tasks systematically in small, independently verifiable increments. Never jump ahead to unrequested implementation phases.

```
1. Understand ──► 2. Inspect ──► 3. Identify ──► 4. Implement ──► 5. Test ──► 6. Verify & Document
```

1. **Understand**: Clarify the specific scope, acceptance criteria, and edge cases of the user request.
2. **Inspect**: Read relevant existing files, models, and tests in the repository.
3. **Identify**: Determine the precise files and architectural layers that need additions or modifications.
4. **Implement**: Write clean, idiomatic code adhering to the 5-layer Clean Architecture and the 20 Invariants.
5. **Add / Update Tests**: Add matching unit or integration tests verifying the new functionality and error conditions.
6. **Build & Test**: Execute `dotnet test` / `npm test` and build tools to confirm no regressions or build breaks.
7. **Verify Acceptance Criteria**: Validate against the original prompt requirements.
8. **Update Documentation**: Update documentation, ADRs, or API contracts if architectural contracts changed.

---

## 8. Target Documentation Taxonomy

As the codebase evolves, documentation must remain organized, modular, and separated from source code. Future documentation additions should adhere to the following taxonomy under `docs/`:

```
docs/
├── architecture/          # High-level architecture, component diagrams, system context
│   └── modular-monolith.md
├── adr/                   # Architecture Decision Records (e.g., ADR-001-signalr-transport.md)
│   └── template.md
├── domain/                # Formal domain specifications, entity relationships, state machines
│   ├── rooms-and-matches.md
│   └── catan-engine.md
├── protocols/             # Real-time SignalR hub protocols, payload schemas, event catalogs
│   ├── room-hub-protocol.md
│   └── match-hub-protocol.md
├── api/                   # RESTful HTTP contracts, OpenApi specs, authentication flow
│   └── auth-and-lobby.md
└── roadmap/               # Implementation phases, milestone tracking, and task breakdowns
    └── implementation-phases.md
```

> **Note**: Do not create empty placeholder files or directories until actual documentation content is written.

---

## 9. Baseline Findings & Repository Status

- **Repository Initial State**: Initial commit on branch `main` containing a minimal `README.md`.
- **Existing Assets**: No legacy application code, obsolete frameworks, or conflicting architectures were present.
- **Implementation Status**: Greenfield state ready for Phase 1 foundational scaffolding (Core Domain, Application contracts, and local container infrastructure) in strict accordance with this document.

