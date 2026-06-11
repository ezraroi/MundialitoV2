# Mundialito Skill for Claude Code

This skill lets Claude Code place, update, and view bets on the Mundialito World Cup prediction platform via its REST API.

## What it does

- View open games still accepting bets
- Place or update per-game bets (score, corners mark, cards mark)
- Place or update the general bet (tournament winner + golden boot player)
- View your existing bets

## Installation

### 1. Locate your Claude skills directory

```
~/.claude/skills/
```

If the directory doesn't exist, create it:

```bash
mkdir -p ~/.claude/skills/mundialito
```

### 2. Copy the skill file

```bash
cp SKILL.md ~/.claude/skills/mundialito/SKILL.md
```

### 3. Update the token

Open `~/.claude/skills/mundialito/SKILL.md` and replace the `TOKEN` value in the Configuration section with your own JWT token.

To extract your token from the Mundialito website:

1. Open the Mundialito website in Chrome and log in
2. Open DevTools (`F12` or `Cmd+Option+I` on Mac)
3. Go to the **Application** tab
4. In the left sidebar, expand **Local Storage** and click on the site's origin
5. Find the `accessToken` key — the value is your JWT token
6. Copy the token value and paste it in place of the existing `TOKEN` in `SKILL.md`

## Usage

Once installed, invoke the skill in any Claude Code session by typing:

```
/mundialito
```

Or just ask naturally — Claude will trigger the skill automatically when you mention betting on Mundialito, viewing open games, or submitting football predictions.

## Requirements

- `curl` available in your shell
- `python3` available in your shell (used for pretty-printing JSON responses)
- A valid Mundialito account and JWT token
