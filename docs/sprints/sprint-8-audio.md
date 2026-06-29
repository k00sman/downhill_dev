# Sprint 8 — Audio & Atmosphere

**Goal question:** Does audio sell speed, danger, and dread?

**Sprint status:** Not started

> Covers the README's "Audio and atmosphere" section — a major contributor to the
> horror tone. Audio/music is Savely's domain. **Tech decision: Unity built-in
> audio** (`AudioSource` / `AudioMixer` / mixer snapshots) — no third-party
> middleware. Adding FMOD/Wwise would be a new third-party dependency and must be
> flagged for sign-off per `AGENTS.md`; default to first-party. Clips are
> placeholder per the prototype scope.

---

### Ticket 8.1 - Set up audio foundation

**Goal:** Establish the mixer, buses, and a single entry point for playing gameplay audio.

**Dependencies:** None.

**Files:**
- `AudioManager` in a new `Downhill.Audio` asmdef under `Assets/Scripts/Audio/`
- `AudioMixer` asset with buses (e.g. Wind, Surface, Impact, Monster, Music)

**Research task for Opus:** Define the minimal mixer/bus layout and a thin `AudioManager` API (play one-shot, manage looping sources, set bus levels/snapshots) that the later tickets call into.

**Subagent tasks:**
- Research subagent: specify the bus list and the `AudioManager` surface.
- Coding subagent A: create the `AudioMixer` asset and buses.
- Coding subagent B: implement `AudioManager` and verify a placeholder clip plays through the correct bus.

**Acceptance criteria:**
- An `AudioMixer` exists with separate buses for the audio categories below.
- Gameplay code plays sounds only through `AudioManager`, not scattered raw `AudioSource` calls.
- A placeholder clip can be triggered and routed to its bus at runtime.

**Notes:**
- Keep it Unity-native. No FMOD/Wwise without explicit sign-off.

---

### Ticket 8.2 - Speed and wind audio

**Goal:** Make velocity audible so the player feels speed and its risk.

**Dependencies:** Tickets 8.1 and 1.3.

**Files:**
- `SpeedWindAudio`
- wind/speed tuning config

**Research task for Opus:** Specify how wind/speed-noise volume and pitch should scale with the bike's current speed, and where the speed signal is read from (`BikeMovementModel`).

**Subagent tasks:**
- Research subagent: define the speed→volume/pitch curve and clamps.
- Coding subagent A: implement a looping wind source driven by current speed.
- Coding subagent B: expose tuning and verify the loop rises/falls with the bike's pace.

**Acceptance criteria:**
- Wind/speed noise scales smoothly with bike speed (louder/higher-pitched faster).
- The effect is tunable without code changes.
- It reinforces the README pillar that speed is tense — fast riding sounds dangerous.

---

### Ticket 8.3 - Surface and impact audio

**Goal:** Make the trail surface and crashes audible.

**Dependencies:** Tickets 8.1 and 3.3.

**Files:**
- `SurfaceImpactAudio`
- surface/impact clip mapping config

**Research task for Opus:** Define a rolling tire/dirt loop tied to grounded movement and one-shot impact sounds triggered by the collision-severity tiers from Ticket 3.3.

**Subagent tasks:**
- Research subagent: map grounded/surface state to loop selection and severity tiers to impact one-shots.
- Coding subagent A: implement the grounded surface loop.
- Coding subagent B: hook crash events to severity-scaled impact one-shots.

**Acceptance criteria:**
- A tire/dirt loop plays while grounded and moving, and stops when airborne or stopped.
- Collision impacts play sounds scaled to crash severity (a bump and a severe crash sound different).
- Clip mapping and volumes are tunable.

**Notes:**
- Consumes the standardized crash events from Ticket 3.3 — don't re-detect collisions here.

---

### Ticket 8.4 - Monster pursuit audio cues

**Goal:** Use sound to make the monster feel inevitable and close.

**Dependencies:** Tickets 8.1 and 5.2.

**Files:**
- `MonsterAudioCues`
- distance/intensity tuning config

**Research task for Opus:** Specify how monster audio (distant approach cues, proximity intensity) should scale with the monster-to-player distance from the chase system.

**Subagent tasks:**
- Research subagent: define the distance→cue/intensity mapping and any spatialization.
- Coding subagent A: implement distance-driven monster audio.
- Coding subagent B: connect to the monster distance signal and tune the close/far contrast.

**Acceptance criteria:**
- Monster audio intensifies as it closes distance and recedes as the player pulls ahead.
- Cues read as "approaching threat," supporting the README pillar that fear comes from pursuit, not jump scares.
- Distance thresholds and intensity are tunable.

**Notes:**
- Reads monster distance from the chase system (Ticket 5.2 / the value also shown on the debug HUD).

---

### Ticket 8.5 - Tension music hooks

**Goal:** Use sparse, adaptive music to raise tension instead of constantly filling silence.

**Dependencies:** Tickets 8.1, 5.2, and 3.4.

**Files:**
- `TensionMusicController`
- mixer snapshots / music state config

**Research task for Opus:** Define a small set of music states (e.g. calm, rising, panic) and the gameplay signals that drive transitions (monster proximity, crash state, speed), implemented via `AudioMixer` snapshots.

**Subagent tasks:**
- Research subagent: define the music states and their trigger conditions.
- Coding subagent A: implement snapshot/state transitions in `TensionMusicController`.
- Coding subagent B: wire the gameplay signals and tune transition smoothing.

**Acceptance criteria:**
- Music stays sparse in calm stretches and rises during panic moments (monster close, crash, high speed).
- There is a clear audible contrast between calm and panic, per the README.
- Music states and transition thresholds are tunable.

**Notes:**
- Adaptive behavior via mixer snapshots/simple scripting — no music middleware.

---

## Sprint exit criteria

Sprint 8 is complete when:
- Speed/wind, surface, and impact audio respond to riding and crashes; monster cues track pursuit distance; and sparse tension music shifts between calm and panic.
- All audio routes through `AudioManager` / the mixer and is tunable without code changes.
- A playtester can answer: does audio make the descent feel fast, dangerous, and dreadful?
