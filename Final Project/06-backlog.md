# Project Backlog

> Stack: Flutter (mobile) + Nest.js (backend) + TypeORM + PostgreSQL + Redis. University MVP, 3 people × 1 month.
> Reference docs: `docs/01-game-design.md`, `docs/02-requirements.md`, `docs/03-architecture.md` §16, `docs/04-api-contracts.md`, `docs/05-combat-spec.md`.
> Skill: `.opencode/skills/auto-chess-game/SKILL.md`.

## Implementation status today

- ✅ infra (`docker-compose`, 7 services, nginx `least_conn`, PG primary+replica)
- ✅ `auth/`, `user/`, `common/jwt-auth`, `common/health`, `redis/` (basic), `migrations/1736…-CreateUsers.ts`, `data-source.ts`
- ✅ Flutter `LoginScreen`, `RegisterScreen`, `HealthScreen`, `ApiClient`, `AuthRepository`, `AppConfig`
- ❌ no WebSocket, no matchmaking, no combat engine, no shop, no match lifecycle, no Pub/Sub, no tests, no CI

---

## Label taxonomy

| Group | Labels |
|---|---|
| **area** | `area:backend`, `area:frontend`, `area:devops`, `area:docs` |
| **priority** | `priority:p0`, `priority:p1`, `priority:p2`, `priority:p3` |
| **test** (what kind of test verifies this ticket) | `test:unit`, `test:widget`, `test:integration`, `test:e2e`, `test:manual`, `test:none` |

### Priority legend

| Priority | Meaning |
|---|---|
| **P0** | Critical path — game cannot function without it. |
| **P1** | Required for MVP launch but blocked by P0. |
| **P2** | Post-MVP features called out as out-of-scope in `docs/01-game-design.md §9` / `docs/02-requirements.md §4`. |
| **P3** | Quality / NFR / dev-experience (tests, CI, linting, observability, hardening). |

> Hard rule from `SKILL.md §4`: **all combat math on backend**. Any ticket that puts combat logic in Flutter is a violation — redirect.

---

## P0 — Backend

| ID | Title | Labels | Files / Notes | Maps to |
|---|---|---|---|---|
| P0-BE-01 | WebSocket gateway (`ws/`) — namespace `/game`, `game:*` events, JWT in handshake (`auth.token`), `WsAuthGuard` (extends `JwtAccessGuard`), per-user socket dedup | `area:backend` `priority:p0` `test:integration` | `backend/src/ws/{ws.module,ws.gateway,ws.dto}.ts`; install `@nestjs/websockets`, `@nestjs/platform-socket.io`, `socket.io`, `socket.io-redis-adapter` | docs/03 §5, R16 |
| P0-BE-02 | Pub/Sub bridge (`runtime/pubsub.bridge.ts`) — every Nest PSUBSCRIBEs `match:*:events` on boot; `Map<matchId, Set<socketId>>` for fan-out; late subscribers fetch `match:<id>:combat-result` (60 s TTL) | `area:backend` `priority:p0` `test:integration` | `backend/src/runtime/pubsub.bridge.ts` | docs/03 §12, R14, R15 |
| P0-BE-03 | Lua: `match_pair.lua` — atomic `ZRANGE 0 1` + `ZREM` (FIFO pop) | `area:backend` `priority:p0` `test:unit` | `backend/src/redis/scripts/match_pair.lua` | R4, R5 |
| P0-BE-04 | Lua: `phase_flip.lua` — atomic phase transition + combat-lock tag | `area:backend` `priority:p0` `test:unit` | `backend/src/redis/scripts/phase_flip.lua` | R6 |
| P0-BE-05 | Lua: `combat_done.lua` — idempotent per-player ack, returns count | `area:backend` `priority:p0` `test:unit` | `backend/src/redis/scripts/combat_done.lua` | R12 |
| P0-BE-06 | Lua: `action_log.lua` — idempotency per `clientActionId` | `area:backend` `priority:p0` `test:unit` | `backend/src/redis/scripts/action_log.lua` | R7 |
| P0-BE-07 | Matchmaking module (`matchmaking/`) — WS `game:matchmaking:join/leave`; BullMQ 1 s poller calling `match_pair.lua`; creates `Match` row; broadcasts `game:match:phase` | `area:backend` `priority:p0` `test:integration` | `backend/src/matchmaking/{matchmaking.module,matchmaking.service,matchmaking.worker}.ts` | FR-MATCH-1..4 |
| P0-BE-08 | `Match` entity + migration — `id`, `player1Id`, `player2Id`, `winnerId?`, `status`, `p1State jsonb`, `p2State jsonb`, `matchSeed`, `wipeIndexP1`, `wipeIndexP2`, `createdAt`, `finishedAt` | `area:backend` `priority:p0` `test:integration` | `backend/src/match/match.entity.ts`, `backend/src/migrations/<ts>-CreateMatches.ts` | docs/03 §3.2 |
| P0-BE-09 | Shop module (`shop/`) — server-side offer gen with seeded RNG (`matchSeed + round + playerId`), pool % Fighter 40 / Healer 40 / Ranger 15 / Tank 5, 5 offers/round, gold ledger | `area:backend` `priority:p0` `test:unit` `test:integration` | `backend/src/shop/{shop.module,shop.service}.ts` | FR-GAME-2, docs/01 §4 |
| P0-BE-10 | Combat engine (`game/`) — pure functions: `runBattle(state): CombatEvent[]`, `pickTarget`, `applyDamage` (+ Tank Revive), Fighter Lifesteal, Ranger Pierce + 2★ global, Tank 2★ Prioritize + Revive, Healer heal + 2★ heal-two + Slow, tie-tick init. Inject `rng: SeededRng`; **no `Math.random`** | `area:backend` `priority:p0` `test:unit` | `backend/src/game/{engine.ts,abilities.ts,targeting.ts,types.ts,seeded-rng.ts}` | FR-COMBAT-1..5, docs/05 §1–§6 |
| P0-BE-11 | Combat single-runner (`runtime/combat.coordinator.ts`) — `SET combat-lock:<id> <instId> NX EX 30`; write `match:<id>:combat-result` (60 s TTL); `PUBLISH match:<id>:events` | `area:backend` `priority:p0` `test:integration` | `backend/src/runtime/combat.coordinator.ts` | R11, R14, R15 |
| P0-BE-12 | Match runtime adapter (`runtime/match.runtime.adapter.ts`) — single entry for every WS mutation: read `HGETALL match:<id>:runtime`, validate phase+round, dispatch via Lua, publish events | `area:backend` `priority:p0` `test:integration` | `backend/src/runtime/match.runtime.adapter.ts` | R8, R9, R10 |
| P0-BE-13 | Match actions WS handlers — `game:shop:{buy,sell,refresh,fuse}`, `game:match:{place,ready,combat_done}` in `ws.gateway.ts` delegating to runtime adapter; validation per `docs/04 §3` | `area:backend` `priority:p0` `test:integration` | `backend/src/ws/ws.gateway.ts` | FR-GAME-3..7 |
| P0-BE-14 | BullMQ workers (`queue/`) — repeatable 1 s matchmaking poller; delayed 60 s `combat_done_timeout`; 30 s disconnect detector; match cleanup job | `area:backend` `priority:p0` `test:integration` | `backend/src/queue/{queue.module,queue.service}.ts` | R18 |
| P0-BE-15 | Wipe/tie damage resolver — wipe index `[5,10,15,20,25]` capped; tie = 5 each, no index advance | `area:backend` `priority:p0` `test:unit` | `backend/src/game/damage.ts` | docs/01 §6 |
| P0-BE-16 | Seedable RNG utility — `seedrandom` or xorshift; used for shop and tie-tick init; passed in via `state.rng` | `area:backend` `priority:p0` `test:unit` | `backend/src/game/seeded-rng.ts` | FR-COMBAT-5, NFR-4 |
| P0-BE-17 | Disconnect handler — WS `disconnect` event → if user in active match, emit `game:match:end` with `reason: 'disconnect'`; opponent wins | `area:backend` `priority:p0` `test:integration` | `backend/src/ws/ws.gateway.ts` | FR-END-2, NFR-12 |
| P0-BE-18 | Health: add WS readiness gate — `/health` already reports pg+redis; add WS listener readiness once `ws/` lands | `area:backend` `priority:p0` `test:manual` | `backend/src/common/health.controller.ts` | — |

---

## P0 — Frontend

| ID | Title | Labels | Files / Notes | Maps to |
|---|---|---|---|---|
| P0-FE-01 | WebSocket client wrapper (`core/ws/`) — `socket_io_client` v2; connect to `AppConfig.wsBaseUrl`; pass `auth.token = accessToken`; auto-reconnect with backoff; expose `Stream<dynamic>` per event | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/core/ws/{ws_client.dart,ws_event.dart}` | docs/03 §5 |
| P0-FE-02 | Riverpod WS provider — `StreamProvider<MatchEvent>` family keyed by event name; reusable across screens | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/core/ws/ws_providers.dart` | — |
| P0-FE-03 | Dart DTOs mirroring `docs/04-api-contracts.md` — `Unit`, `ShopOffer`, `MatchState`, `CombatEvent` (sealed), `MatchEnd`, `MatchPhase`, `MatchDamage` | `area:frontend` `priority:p0` `test:unit` | `mobile/lib/shared/models/*.dart` | docs/04 §2 |
| P0-FE-04 | Matchmaking screen — "Find match" → emit `game:matchmaking:join`; status "Searching…" / "Match found!"; cancel emits `game:matchmaking:leave` | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/features/matchmaking/matchmaking_screen.dart` | FR-MATCH-1, FR-MATCH-4 |
| P0-FE-05 | Match screen scaffold — receives `game:match:phase`; phase header + 40 s countdown; tabs `Shop` / `Board`; "Ready" button → `game:match:ready` | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/features/match/match_screen.dart` | FR-GAME-1, FR-GAME-4 |
| P0-FE-06 | Shop UI — 5 cards, gold display, `Buy`/`Sell`/`Refresh`/`Fuse`; `clientActionId` UUID per action; optimistic update + rollback on `game:error` | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/features/match/shop/*.dart` | FR-GAME-3 |
| P0-FE-07 | Board UI — 2×3 grid per player (mirror), 8-slot bench, drag unit board↔bench↔shop, sends `game:match:place` | `area:frontend` `priority:p0` `test:widget` `test:integration` | `mobile/lib/features/match/board/*.dart` | FR-GAME-3 |
| P0-FE-08 | Battle playback — receive `game:combat:events`; animate events sequentially (movement flashes, attack hits, death, heal, slow, pierce, revive); on finish emit `game:match:combat_done {clientActionId}` | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/features/match/battle/*.dart` | FR-GAME-5 |
| P0-FE-09 | Damage + end-of-round overlay — render `game:match:damage` and `game:match:end`; HP bars, wipe-damage popup, "You won / You lost" modal | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/features/match/result/*.dart` | FR-GAME-7, FR-END-1 |
| P0-FE-10 | Token-refresh interceptor — extend Dio interceptor: 401 → `AuthRepository.tryRefresh()`, retry once; on refresh failure → logout | `area:frontend` `priority:p0` `test:unit` | `mobile/lib/core/api/api_client.dart` | NFR-7 |
| P0-FE-11 | Reconnect on WS drop — on reconnect, refetch match state (or accept `match:<id>:combat-result` fallback); resubscribe to current `matchId` events | `area:frontend` `priority:p0` `test:integration` | `mobile/lib/core/ws/ws_client.dart` | R18 |
| P0-FE-12 | Routing migration — replace `Navigator.pushNamed` with `go_router`; guards `/lobby`, `/match/:id`; auth-redirect on missing token | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/core/router.dart`, `mobile/lib/main.dart` | — |
| P0-FE-13 | Session restoration — on app launch: read tokens, attempt silent `tryRefresh()` then go to `/lobby`; else `/login` | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/main.dart`, `mobile/lib/core/auth/auth_repository.dart` | US-2 |
| P0-FE-14 | Shared widgets — `PhaseTimerRing` (animated 40 s), `HealthBar` (board + result), `UnitAvatar` (placeholder sprites OK for MVP) | `area:frontend` `priority:p0` `test:widget` | `mobile/lib/shared/widgets/*.dart` | — |

---

## P0 — DevOps

> No new P0 infra tickets — `docker-compose.yml`, `nginx.conf`, and the 7-service topology already exist and do not block P0-BE / P0-FE. First DevOps ticket is **P3-DO-01**.

---

## P1 — Backend

| ID | Title | Labels | Files / Notes | Maps to |
|---|---|---|---|---|
| P1-BE-01 | `MatchRound` entity + migration — `matchId`, `roundNumber`, `events jsonb` (replay source) | `area:backend` `priority:p1` `test:unit` | `backend/src/match/match-round.entity.ts` | docs/03 §3.3 |
| P1-BE-02 | `GET /match/history` — last 50 matches for current user; JWT-guarded | `area:backend` `priority:p1` `test:integration` | `backend/src/match/match.controller.ts` | FR-HIST-1 |
| P1-BE-03 | `GET /match/:matchId` — match detail + round list | `area:backend` `priority:p1` `test:integration` | same | FR-HIST-2 |
| P1-BE-04 | ELO rating update on match end — `UserService.updateRating(id, delta)`, K=32; orchestrator calls it for both players | `area:backend` `priority:p1` `test:unit` | `backend/src/user/user.service.ts`, `backend/src/match/match.service.ts` | NFR-1, FR-END-3 |
| P1-BE-05 | WS payload validation server-side — `class-validator` DTOs for every incoming `game:*` payload; rejected → `game:error { code: 'invalid_payload' }` | `area:backend` `priority:p1` `test:integration` | `backend/src/ws/ws.dto.ts` | NFR-6 |
| P1-BE-06 | Global exception filter — REST `{code, message}` shape, WS emits `game:error` with same shape | `area:backend` `priority:p1` `test:unit` | `backend/src/common/filters/` | NFR-7, docs/04 §5 |
| P1-BE-07 | Structured logging with `pino` — replace `Logger` defaults; per-request id; per-match id propagated through orchestrator | `area:backend` `priority:p1` `test:manual` | `backend/src/common/logger.module.ts` | NFR-8 |
| P1-BE-08 | Rate limiting — per-user WS message throttle (e.g. 30 msg/s); `rate.limited` error | `area:backend` `priority:p1` `test:integration` | `backend/src/common/throttle.guard.ts` | NFR-7, docs/04 §5 |

---

## P1 — Frontend

| ID | Title | Labels | Files / Notes | Maps to |
|---|---|---|---|---|
| P1-FE-01 | Match history list screen — `GET /match/history`; paginated list; tap → detail | `area:frontend` `priority:p1` `test:widget` | `mobile/lib/features/history/` | FR-HIST-1 |
| P1-FE-02 | Match detail screen — `GET /match/:matchId`; round-by-round winner + damage | `area:frontend` `priority:p1` `test:widget` | same | FR-HIST-2 |
| P1-FE-03 | Profile / settings screen — username, email, rating; logout | `area:frontend` `priority:p1` `test:widget` | `mobile/lib/features/profile/` | — |
| P1-FE-04 | Toast / inline error UX — surface `game:error` codes with human-readable strings; auto-dismiss; never crash | `area:frontend` `priority:p1` `test:widget` | `mobile/lib/shared/widgets/error_banner.dart` | NFR-7 |
| P1-FE-05 | Loading + empty states — matchmaking waiting, history empty, "no offers" | `area:frontend` `priority:p1` `test:widget` | shared widgets | — |

---

## P2 — Backend (post-MVP / `docs/01 §9`)

| ID | Title | Labels | Files / Notes | Maps to |
|---|---|---|---|---|
| P2-BE-01 | Replay endpoint — `GET /match/:matchId/replay` returning `MatchRound.events[]` | `area:backend` `priority:p2` `test:integration` | docs/01 §9 | — |
| P2-BE-02 | Surrender endpoint — `game:match:surrender`, applies `forfeit` | `area:backend` `priority:p2` `test:integration` | docs/01 §6.3 | — |
| P2-BE-03 | Matchmaking rating buckets — replace FIFO with nearest-rating matching (currently display-only per `docs/03 §4.3`) | `area:backend` `priority:p2` `test:integration` | docs/01 §8 | — |
| P2-BE-04 | Items system — unit items, drop tables | `area:backend` `priority:p2` `test:integration` | docs/01 §9 | — |
| P2-BE-05 | XP / level-up system | `area:backend` `priority:p2` `test:integration` | docs/01 §9 | — |
| P2-BE-06 | 3+ player mode | `area:backend` `priority:p2` `test:integration` | docs/01 §9 | — |
| P2-BE-07 | Spectator mode | `area:backend` `priority:p2` `test:integration` | docs/01 §9 | — |
| P2-BE-08 | Multi-region deployment | `area:backend` `priority:p2` `test:e2e` | docs/01 §9 | — |
| P2-BE-09 | Refresh token blacklist / rotation | `area:backend` `priority:p2` `test:integration` | docs/02 §4; `SKILL.md §4.12` | — |
| P2-BE-10 | Push notifications | `area:backend` `priority:p2` `test:integration` | docs/02 §4 | — |

---

## P2 — Frontend (post-MVP)

| ID | Title | Labels | Files / Notes | Maps to |
|---|---|---|---|---|
| P2-FE-01 | Surrender button on match screen | `area:frontend` `priority:p2` `test:widget` | docs/01 §6.3 | — |
| P2-FE-02 | Replay viewer — playback `CombatEvent[]` from `/match/:id/replay`; scrubber | `area:frontend` `priority:p2` `test:widget` | docs/01 §9 | — |
| P2-FE-03 | Cosmetic animations / unit skins | `area:frontend` `priority:p2` `test:manual` | docs/01 §9 | — |
| P2-FE-04 | Spectator mode UI | `area:frontend` `priority:p2` `test:widget` | docs/01 §9 | — |
| P2-FE-05 | Push notification opt-in | `area:frontend` `priority:p2` `test:manual` | docs/02 §4 | — |

---

## P3 — Backend (quality / NFR / dev-experience)

| ID | Title | Labels | Files / Notes | Maps to |
|---|---|---|---|---|
| P3-BE-01 | Combat engine unit tests — `engine.spec.ts` + `abilities.spec.ts`; cover targeting matrix, every ability at every star, all edge cases E1–E16, fixed-seed full-battle regression (byte-for-byte) | `area:backend` `priority:p3` `test:unit` | — | **NFR-9 ≥ 90 % lines in `backend/src/game/`** |
| P3-BE-02 | Lua script tests — `match_pair`, `phase_flip`, `combat_done`, `action_log` against a local Redis (`ioredis-mock`) | `area:backend` `priority:p3` `test:unit` | — | NFR-13 |
| P3-BE-03 | Shop RNG determinism tests — same `matchSeed + round + playerId` → identical 5 offers | `area:backend` `priority:p3` `test:unit` | — | FR-COMBAT-5 |
| P3-BE-04 | ESLint config + CI gate | `area:backend` `priority:p3` `test:integration` | — | NFR-10 |
| P3-BE-05 | Nest `@nestjs/swagger` — auto-OpenAPI for REST; CI builds `openapi.json` artifact | `area:backend` `priority:p3` `test:manual` | — | DX |
| P3-BE-06 | Helmet + rate-limit on REST | `area:backend` `priority:p3` `test:integration` | — | NFR-7 |
| P3-BE-07 | CORS tightening — replace `*` with explicit allowlist (nginx already permissive per `docs/03 §16.1`) | `area:backend` `priority:p3` `test:manual` | — | NFR-7 |
| P3-BE-08 | Graceful shutdown — close socket, drain BullMQ workers, release combat-lock | `area:backend` `priority:p3` `test:integration` | — | NFR-13 |
| P3-BE-09 | Pagination for `/match/history` (cursor) | `area:backend` `priority:p3` `test:integration` | — | NFR-7 |
| P3-BE-10 | Health `/ready` vs `/live` split for k8s | `area:backend` `priority:p3` `test:integration` | — | NFR-13 |
| P3-BE-11 | Anti-cheat: server-side shop RNG audit log | `area:backend` `priority:p3` `test:unit` | — | docs/01 §9 |
| P3-BE-12 | Anti-cheat: validate `clientActionId` uniqueness + payload schema | `area:backend` `priority:p3` `test:integration` | — | docs/02 NFR-1 |

---

## P3 — Frontend (quality / NFR / dev-experience)

| ID | Title | Labels | Files / Notes | Maps to |
|---|---|---|---|---|
| P3-FE-01 | Widget tests for screens — `LoginScreen`, `RegisterScreen`, `HealthScreen`, `MatchmakingScreen`, `ShopScreen`, `BoardScreen` smoke tests | `area:frontend` `priority:p3` `test:widget` | — | NFR-10 |
| P3-FE-02 | `flutter analyze` clean + CI gate | `area:frontend` `priority:p3` `test:integration` | — | NFR-10 |
| P3-FE-03 | Local error logger — Sentry-like dump for unhandled exceptions | `area:frontend` `priority:p3` `test:manual` | — | NFR-8 |
| P3-FE-04 | Accessibility pass — semantic labels on board slots, shop cards, HP bars | `area:frontend` `priority:p3` `test:manual` | — | UX |
| P3-FE-05 | iOS support (currently Android/Chrome only per README) | `area:frontend` `priority:p3` `test:manual` | — | docs/01 |
| P3-FE-06 | Settings: WS auto-reconnect toggle, theme (dark/light) | `area:frontend` `priority:p3` `test:widget` | — | UX |
| P3-FE-07 | Localization (en + fa/other) | `area:frontend` `priority:p3` `test:manual` | — | UX |

---

## P3 — DevOps

| ID | Title | Labels | Files / Notes | Maps to |
|---|---|---|---|---|
| P3-DO-01 | GitHub Actions CI — 2 jobs: `backend` (`npm ci && npm run build && npm test`); `mobile` (`flutter pub get && flutter analyze && flutter test`) | `area:devops` `priority:p3` `test:integration` | — | NFR-10 |
| P3-DO-02 | GitHub Actions CD — build & publish Nest image to GHCR on `main`; semantic version tag | `area:devops` `priority:p3` `test:manual` | — | DX |
| P3-DO-03 | `.github/ISSUE_TEMPLATE` + PR template referencing FR/NFR IDs | `area:devops` `priority:p3` `test:none` | — | DX |
| P3-DO-04 | `.github/CODEOWNERS` — split backend / mobile / docs | `area:devops` `priority:p3` `test:none` | — | DX |
| P3-DO-05 | Dependabot for npm and pub | `area:devops` `priority:p3` `test:manual` | — | NFR-5 |
| P3-DO-06 | Production hardening — strong `JWT_SECRET` (32-byte random) per env; HTTPS termination in nginx; secret rotation; pgBouncer | `area:devops` `priority:p3` `test:manual` | — | NFR-5 |
| P3-DO-07 | Backup policy for `pg_primary_data` volume | `area:devops` `priority:p3` `test:manual` | — | NFR-13 |
| P3-DO-08 | Observability — Prometheus `/metrics` from Nest; Grafana dashboards; Loki for logs | `area:devops` `priority:p3` `test:integration` | — | NFR-2, NFR-14 |
| P3-DO-09 | Load test — k6 script verifying NFR-3 (≥ 50 concurrent matches), NFR-2 (p95 < 500 ms), NFR-14 (combat < 500 ms p95) | `area:devops` `priority:p3` `test:e2e` | — | NFR-2, NFR-3, NFR-14 |
| P3-DO-10 | `docker-compose.prod.yml` — strip pgadmin; build args for prod image; separate network policy | `area:devops` `priority:p3` `test:manual` | — | ops |
| P3-DO-11 | README badges — CI status, coverage, latest tag | `area:devops` `priority:p3` `test:none` | — | DX |
| P3-DO-12 | `.dockerignore` tightening in `mobile/` and `nginx/` | `area:devops` `priority:p3` `test:none` | — | DX |
| P3-DO-13 | Pre-commit hooks — `prettier` (mobile), `eslint --fix` (backend) | `area:devops` `priority:p3` `test:manual` | — | DX |
| P3-DO-14 | Re-init replication check in CI — boot stack in CI runner; assert WAL streaming | `area:devops` `priority:p3` `test:e2e` | — | NFR-13 |
| P3-DO-15 | Security scan — `npm audit` + Trivy image scan in CI | `area:devops` `priority:p3` `test:integration` | — | NFR-5, NFR-7 |

---

## Recommended execution order (P0 only)

```
P0-BE-01 (WS gateway)
P0-BE-02 (Pub/Sub bridge)
P0-BE-03..06 (4 Lua scripts)
P0-BE-08 (Match entity + migration)
P0-BE-16 (seeded RNG utility)
P0-BE-10 (combat engine — paired with P3-BE-01 tests)
P0-BE-15 (damage resolver)
P0-BE-11 (combat single-runner)
P0-BE-12 (match runtime adapter)
P0-BE-13 (WS action handlers)
P0-BE-09 (shop)
P0-BE-07 (matchmaking)
P0-BE-14 (BullMQ)
P0-BE-17 (disconnect forfeit)

P0-FE-01 (WS client)
P0-FE-03 (DTOs)
P0-FE-12 (router) + P0-FE-13 (session restoration)
P0-FE-10 (refresh interceptor)
P0-FE-02 (StreamProvider)
P0-FE-04 (matchmaking screen)
P0-FE-05 (match screen scaffold)
P0-FE-06 (shop UI)
P0-FE-07 (board UI)
P0-FE-08 (battle playback)
P0-FE-09 (damage overlay)
P0-FE-11 (WS reconnect)
```

---

## GitHub Project layout

```
Board columns: Backlog → P0 → P1 → P2 → P3 → In Progress → Review → Done
Labels (apply all matching per ticket):
  area:backend, area:frontend, area:devops, area:docs
  priority:p0, priority:p1, priority:p2, priority:p3
  test:unit, test:widget, test:integration, test:e2e, test:manual, test:none
```
