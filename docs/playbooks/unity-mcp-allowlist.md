# Playbook: unity-mcp-allowlist

**Type:** Reference / safety policy — read, not an invocable skill. The `AGENTS.md`
"Unity MCP" note points here.

**Purpose:** Define which Unity MCP tools an agent may call freely vs. which need
an explicit plan step + user confirmation, so connecting Claude Code / Codex to a
running Editor stays safe. The bridge is the **official Unity MCP**
(`com.unity.ai.assistant`), **Editor-open only** — not a headless/CI path.

## Three safety layers

1. **Connection approval.** A client (Claude Code, Codex) appears as a **Pending
   Connection** in `Edit > Project Settings > AI > Unity MCP` and must be
   **Accepted** before it can invoke any tool. Approve only clients you launched.
2. **Per-tool toggle.** The same page lists every tool with an on/off checkbox. It
   is **not** default-deny — it ships with `Unity_RunCommand` (arbitrary exec) and
   `Unity_AssetGeneration_GenerateAsset` (mutating) **on**. Curate down to the
   read-only baseline below; enable a mutating/exec tool only for a specific,
   confirmed task, then turn it back off.
3. **This allowlist.** Per-call policy once connected, since Unity MCP does not
   prompt per tool. Mirrors the repo's global risky-operations rule.

## Classification (verb-based)

- **SAFE — read-only, call freely.** Reads/queries/observes: get/list/find/inspect
  scenes, GameObjects, components; read scripts and console logs; query project or
  editor state; screenshots; profiler stats. Never changes project state.
- **GATED — mutating; explicit plan step + confirmation each time.** Changes state:
  create/modify/move/delete/save assets; create/open/save/unload scenes; edit or
  create scripts; generate code; add/remove packages; instantiate/destroy
  GameObjects; modify components; clear console; run tests / enter play mode.
- **HIGH-RISK — never without specific per-call approval.** Executes arbitrary code
  or methods in the Editor (`Unity_RunCommand`, or any "run C#" / "invoke method"
  tool). `rm -rf`-tier — confirm the exact code/target every time.

**`Manage*` tools are GATED even for reads.** `Unity_ManageScene`,
`Unity_ManageGameObject`, `Unity_ManageScript`, `Unity_ManageAsset`,
`Unity_ManageEditor` bundle read and write under one name — you can't allowlist
their read path separately. For pure reads prefer the dedicated read tools:
`Unity_GetProjectData`, `Unity_GetConsoleLogs` / `Unity_ReadConsole`,
`Unity_FindProjectAssets` / `Unity_Grep` / `Unity_FindInFile`,
`Unity_PackageManager_GetData`, `Unity_Camera_Capture` /
`Unity_SceneView_Capture*`.

**Read-only baseline.** Keep on only the read/query/capture tools. Keep **off**:
`Unity_RunCommand`, `Unity_DeleteScript`, all `Manage*`, and the script-edit tools
(`CreateScript` / `ApplyTextEdits` / `ScriptApplyEdits` / `ManageScript`) — agents
author C# via direct file edits (reviewable diffs + linter), which beats the MCP
script path. The bridge's value is runtime introspection, not writing text.

## Running tests via MCP

The official package exposes **no test-runner tool** — there is no `run_tests`
tool or Testing group. PlayMode tests via MCP are therefore not available
out-of-box; run them from the **Unity Test Runner window** (per `AGENTS.md`). To
add an agent path later, register a small custom MCP tool wrapping `TestRunnerApi`
that returns a structured pass/fail (stays gated, Editor-open). Don't drive tests
through `Unity_RunCommand` — high-risk and no guaranteed structured result.

## Setup (human, Editor-open — not committed)

Kept out of the repo to stay third-party-free and avoid machine-specific paths.

1. Install **`com.unity.ai.assistant`** via Package Manager (Unity 6000.0+) and
   sign in with your Unity account. MCP use does not consume AI chat credits.
2. `Edit > Project Settings > AI > Unity MCP` → confirm **Unity Bridge: Running**
   (green); **Start** if stopped. The relay auto-installs to `~/.unity/relay/`
   (Linux binary: `relay_linux`).
3. **Auto-configure:** expand **Integrations**, select your client (Claude Code or
   Codex), **Configure**. This writes `mcpServers.unity-mcp` to `~/.claude.json`
   and `[mcp_servers.unity_mcp]` to `~/.codex/config.toml`, each running
   `relay_linux --mcp`.
4. **Codex manual fallback** (if your Unity version lacks the Codex integration):
   ```toml
   [mcp_servers.unity_mcp]
   command = "/home/<you>/.unity/relay/relay_linux"  # macOS arm: ~/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64 ; Win: %USERPROFILE%\.unity\relay\relay_win.exe
   args = ["--mcp"]
   ```
5. **Accept** the Pending Connection on first connect.
6. **Reload the client** — a server added mid-session doesn't load; restart it
   (`claude --continue` reloads config and keeps the conversation).
7. **Verify:** ask for a read-only call (e.g. `Unity_GetProjectData`). Don't commit
   machine-specific config.

## Done check (session that used MCP)

- Connected client was explicitly **Accepted**.
- Enabled set curated to the read-only baseline (exec/mutators off).
- Only SAFE tools used without confirmation; every GATED / HIGH-RISK call had an
  explicit plan step + confirmation.
- Any test run reported its actual pass/fail, not assumed.
