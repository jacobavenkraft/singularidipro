# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

The canonical agent guide for `singularidipro` lives in [`AGENTS.md`](./AGENTS.md) and is shared across Claude Code, Codex, Gemini, Cursor, and Copilot. **Read it first** — everything below is Claude-specific addenda and assumes that context.

## Critical reminders

- **Don't edit this directory tree directly.** Open a worktree under `..\worktrees\singularidipro-<task>` first. See `AGENTS.md` for the full lifecycle.
- **Multi-platform desktop only:** Windows, Linux, macOS. No iOS / iPadOS / Android.
- **Unity 6000.4.6f1.** Don't bump without explicit approval.

## Cross-repo context

- The shared agent meta repo lives at [`..\singularidi_agents\`](../singularidi_agents/). Skills, rules, and port-planning notes go there, not here.
- The legacy project at [`..\singularidi\`](../singularidi/) is the source of truth for features being ported. Its `CLAUDE.md` is the most concise architectural spec available — read it before designing port work.

## Skills and tooling

User-level Claude Code config (skills, plugins, hooks) lives in `~/.claude/`. The `superpowers` plugin is installed there. Don't try to recreate user-scope tooling inside this repo.

If a skill is genuinely shared across this project and its sibling worktrees, check it into `..\singularidi_agents\skills\` so every worktree (and any agent invocation) sees the same version.
