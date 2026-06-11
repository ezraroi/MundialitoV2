---
name: mundialito
description: Helps the user place, update, and view bets on the Mundialito World Cup prediction platform via its REST API. Use this skill whenever the user asks to bet on Mundialito, view open games, check tournament odds, or submit football/soccer predictions.
allowed-tools: Bash(curl:*), Bash(python3:*)
---

# Mundialito Betting Skill

You are helping the user place bets on the Mundialito World Cup prediction platform via its REST API.

## Configuration

```
BASE_URL = https://mundialito-duanddfueshdcjab.northeurope-01.azurewebsites.net
TOKEN    = <YOUR_JWT_TOKEN>
```

Always pass `-H "Authorization: Bearer $TOKEN"` on every curl call.

---

## Step 0 — Fetch current user (required before any action)

Always run this first to resolve both the user ID and username dynamically:

```bash
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/users/me"
```

Extract:
- `Id` → use as `{userId}` wherever needed
- `Username` → use as `{username}` wherever needed

---

## Action 1 — List open games (games still accepting bets)

```bash
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/games/open" | python3 -m json.tool
```

Show the user a table with: GameId, HomeTeam, AwayTeam, Date, CloseTime.
Ask the user which game they want to bet on if they haven't said.

---

## Action 2 — List all teams (needed for general bets)

```bash
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/teams" | python3 -m json.tool
```

Show TeamId and Name. Ask user to pick a winning team.

---

## Action 3 — List all players (needed for golden boot bet)

```bash
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/players" | python3 -m json.tool
```

Show PlayerId and Name. Ask user to pick a golden boot player.

---

## Action 4 — Place a per-game bet

Required fields:
| Field | Type | Constraint |
|-------|------|-----------|
| `GameId` | integer | from open games list |
| `HomeScore` | integer | 0–10 |
| `AwayScore` | integer | 0–10 |
| `CornersMark` | string | exactly one of: `1`, `X`, `2` (1=home wins corners, X=draw, 2=away wins) |
| `CardsMark` | string | exactly one of: `1`, `X`, `2` (1=home gets more cards, X=equal, 2=away gets more) |

Confirm all five values with the user before submitting.

Check first if the user already has a bet on this game:
```bash
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/games/{gameId}/mybet"
```

- If no existing bet → POST:
```bash
curl -s -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"GameId":{gameId},"HomeScore":{home},"AwayScore":{away},"CornersMark":"{corners}","CardsMark":"{cards}"}' \
  "$BASE_URL/api/bets"
```

- If bet already exists (has a `BetId`) → PUT to update:
```bash
curl -s -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"GameId":{gameId},"HomeScore":{home},"AwayScore":{away},"CornersMark":"{corners}","CardsMark":"{cards}"}' \
  "$BASE_URL/api/bets/{betId}"
```

---

## Action 5 — Place (or update) the general bet

The general bet is one per user: pick the **tournament winner** (team) and **golden boot** (player).

First check if the betting window is still open:
```bash
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/generalbets/cansubmitbets"
```

Check if the user already has a general bet:
```bash
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/generalbets/user/{username}"
```

- If no existing general bet → POST:
```bash
curl -s -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"WinningTeam":{"TeamId":{teamId}},"GoldenBootPlayer":{"PlayerId":{playerId}}}' \
  "$BASE_URL/api/generalbets"
```

- If general bet already exists (has a `GeneralBetId`) → PUT to update:
```bash
curl -s -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"WinningTeam":{"TeamId":{teamId}},"GoldenBootPlayer":{"PlayerId":{playerId}}}' \
  "$BASE_URL/api/generalbets/{generalBetId}"
```

---

## Action 6 — View my bets

```bash
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/bets/user/{username}" | python3 -m json.tool
curl -s -H "Authorization: Bearer $TOKEN" "$BASE_URL/api/generalbets/user/{username}" | python3 -m json.tool
```

---

## Flow when the user invokes this skill

1. Ask what they want to do if not stated: place a game bet, place/update a general bet, or view existing bets.
2. Fetch the user's ID upfront (Step 0) — always do this, it's cheap.
3. Gather any missing inputs interactively before running write operations.
4. **CRITICAL:** Confirm the full set of values with the user before any POST/PUT operation. Do not mutate state without explicit user confirmation of the payload.
5. Report success or parse the error message from the API if the `curl` command fails or returns a non-2xx status code.
