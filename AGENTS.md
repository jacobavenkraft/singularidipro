# AGENTS.md

Canonical instructions for AI coding agents (Claude Code, Codex, Gemini, Cursor, Copilot, etc.) working in the `singularidipro` repository. Tool-specific entry points (`CLAUDE.md`, `.cursor/rules/`, `.github/copilot-instructions.md`) should defer to this file rather than duplicate its content.

The cross-repo meta guide — repository map, why we use worktrees, where shared agent skills/rules live — is in the sibling [`singularidi_agents`](../singularidi_agents/AGENTS.md) repository. Read that for the wider context; this file covers what is specific to `singularidipro` itself.

## What this project is

`singularidipro` is a Unity-based MIDI visualizer. It is a port-in-progress of the legacy [`singularidi`](../singularidi/) Avalonia/.NET project, rebuilt on Unity to support richer real-time visualizations. See the [README](./README.md) for the product overview and planned visualization modes.

## Project constraints (apply when generating code)

- **Engine: Unity `6000.4.6f1`.** Pinned in `ProjectSettings/ProjectVersion.txt`. Don't bump without an explicit ask — package compatibility is brittle on minor Unity-6 jumps.
- **Language: C#** (standard Unity scripting). No second runtime languages without discussion.
- **Targets: Windows, Linux, and macOS desktop.** Build targets, packages, input bindings, and UI assumptions must work on those three platforms.
- **NOT targeted: iOS, iPadOS, Android.** Do not introduce mobile-only packages, touch-gesture-only controls, mobile build configurations, or platform-store integrations. If a Unity package is mobile-flavored, prefer a desktop-friendly alternative or ask first.
- **Input:** Unity's new Input System (`InputSystem_Actions.inputactions` is already wired up). Don't reach for the legacy `Input` API.

## Working on this repo: use a worktree

**AI agents must not edit this directory tree directly.** All non-trivial work happens in a git worktree branched off the canonical's default branch. The full lifecycle is documented in [`singularidi_agents/AGENTS.md`](../singularidi_agents/AGENTS.md#the-agent-workflow-work-in-worktrees-merge-back-to-canonical); the short version:

```powershell
cd D:\001_source\singularidi\singularidipro
git fetch origin
git worktree add ..\worktrees\singularidipro-<task> -b <task> origin/main
# ...do the work inside the worktree...
git worktree remove ..\worktrees\singularidipro-<task>   # after merge
```

Why this matters here specifically: Unity's `Library/`, `Temp/`, `obj/`, `Logs/`, and `Build/` are large, machine-specific, and gitignored. A worktree gets its own copy and rebuilds them on first open — keep one Unity Editor instance per worktree. Don't open the same project from two worktrees at once; the asset database will fight you.

## Build & run

The Unity Editor is the primary build/run surface. Open `singularidipro.slnx` (or the project folder) in Unity Hub, target the `Standalone` platform for Windows/Linux/Mac, and use **File → Build Profiles** to produce desktop builds. The `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` files at the root are Unity-generated; treat them as build artifacts and don't hand-edit them.

There are no command-line build or test scripts checked in yet. When test infrastructure is added, document the invocation here.

## The port from `singularidi/`

Most upcoming work will be migrating features out of the legacy Avalonia project. Before designing a port:

1. Read the legacy [`singularidi/CLAUDE.md`](../singularidi/CLAUDE.md). It documents the existing audio engines (`SoundFontAudioEngine`, `MidiDeviceAudioEngine`), the `MidiPlaybackEngine` state machine, the pluggable `IVisualizationEngine` (`VerticalFallEngine`, `HorizontalCrawlEngine`), the theming system, and the MP4 export pipeline. It is the most concise spec of what is being ported.
2. Map each legacy abstraction to its Unity equivalent. Examples: Avalonia `DispatcherTimer` → Unity `Update`/`FixedUpdate`; Avalonia `DrawingContext` → Unity render pipeline / `Mesh` + shader; `IAudioEngine` (NAudio + MeltySynth) → either retained as managed code with Unity-friendly buffer plumbing, or replaced with Unity's audio system depending on latency needs.
3. Stage the port plan in `singularidi_agents/port/` before opening a worktree to implement.

## When agents should ask before acting

- Bumping Unity, the .NET target, or any major package.
- Adding mobile build targets or mobile-only dependencies.
- Replacing the new Input System with the legacy `Input` API.
- Hand-editing `Assembly-CSharp*.csproj`, `*.slnx`, or `ProjectSettings/ProjectVersion.txt`.
- Working outside a worktree on anything beyond a typo fix.
