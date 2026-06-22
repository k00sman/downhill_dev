# Downhill

Downhill is a fast-paced downhill mountain biking game with a horror twist. The player must descend a long, dangerous mountain trail while being pursued by a monster. The experience is built around speed, pressure, and split-second trail reading: some sections reward commitment and momentum, while others force the player to slow down and survive highly technical terrain without falling off the bike.

For the gameplay prototype, the goal is to validate three things: whether the bike handling feels tense and readable at speed, whether the monster creates meaningful pressure, and whether modular trail segments can be stitched together into replayable runs.

## Core fantasy

The player is alone on a night descent, pushing deeper down an unmaintained mountain trail with a creature somewhere behind them. Safety comes from momentum, but too much speed turns every root, tree, jump, and hairpin into a lethal hazard.

The game should feel like a constant tradeoff:

- Ride too slowly and the monster catches up.
- Ride too aggressively and a crash can kill the player.
- Read the trail well and the player stays alive long enough to reach the end.

## Design pillars

### Momentum is survival

Forward motion is the central source of tension. Speed is not only a tool for traversal; it is also the player's main defense against the pursuing monster.

### Fear comes from pursuit, not jump scares

The horror is created through pressure, sound, visibility, and the knowledge that slowing down is dangerous. The monster should feel inevitable and threatening, not random or gimmicky.

### Readability over realism

The player must be able to parse the trail at high speed. Terrain, hazards, path splits, and safe lines must be visually legible even in darkness.

### Trail reading matters more than tricks

The core skill is choosing the correct line and controlling the bicycle under pressure. Stylish play is secondary to survival.

### Stylized presentation supports gameplay

A stylized PS1/PS2-era 3D look is a feature. It supports horror atmosphere, stronger visual readability, and a distinct identity for the prototype.

## Prototype goals

The prototype should answer the following questions:

- Is the core downhill handling satisfying and understandable?
- Does the chase create tension without feeling unfair?
- Do modular level segments connect cleanly enough to support replayability?
- Is the input scheme distinct and enjoyable, or unnecessarily awkward?

## Prototype scope

### Included

- 1 region
- 5 playable level segments
- Segment shuffling with no repeated segment in a run
- Basic bicycle handling
- Basic crash and health systems
- Basic following monster behavior
- Basic trail readability support through lighting and markers
- Placeholder environmental art, sound, and props

### Out of scope

- Saving and loading progression
- Bicycle upgrades or unlocks
- Narrative sequences or cutscenes
- Advanced monster navigation or obstacle avoidance
- Multiplayer
- Advanced trick systems
- Multiple monster types
- Difficulty modes

## Level structure

The full game is played on a long downhill trail divided into multiple regions. Each region is divided into segments. A segment is a modular terrain level that acts as a reusable building block.

This structure allows each new run to reorder the selected segments within a region. Not every available segment needs to appear in every playthrough, which keeps runs shorter and improves replayability.

Example:

```text
load_levels
  intro,
  region_1_level_7, region_1_level_3, region_1_level_5, region_1_level_1, region_1_level_9,
  region_1_to_region_2_transition,
  region_2_level_3, region_2_level_5, region_2_level_8, region_2_level_2, region_2_level_7,
  region_2_to_region_3_transition,
  # more_levels
  level_ending
```

No segment is repeated within a single playthrough.

### Segment rules

Each segment must:

- Begin at a compatible entry point.
- End at a compatible exit point.
- Support seamless connection to any other valid segment in the same region.
- Contain a clear main trail with readable route options.
- Introduce a distinct challenge, terrain condition, or line choice.

Within a region, segments should converge into a common connection area before transitioning into the next piece. That shared connection space allows segments to be chained smoothly while still supporting variation inside each segment.

### Segment schema

Each segment should be defined using a small set of tags or metadata:

- Region
- Segment ID
- Entry socket
- Exit socket
- Difficulty rating
- Expected speed band
- Hazard tags, such as jumps, hairpins, narrow trail, poor terrain, dense trees
- Visibility/readability tags
- Primary route type, such as safe, fast, or technical

This metadata will make it easier to control run composition and prevent broken or repetitive segment combinations.

### Replayability note

The original design idea aimed to produce many possible playthroughs. For the prototype, the important takeaway is not the exact formula but that ordered, non-repeating segment selection creates substantial variation between runs.

## Trail readability

Trail readability is critical because the player is expected to make high-speed decisions in low visibility.

Guidelines:

- The trail must stand out from surrounding terrain.
- Critical turns, splits, and hazards must be visually telegraphed early enough to react.
- Reflective markers and tape can be placed along the route to indicate progression.
- These reflective markers should catch the player's headlamp and help guide navigation at night.
- Visual readability takes priority over strict environmental realism.

## Gameplay loop

The player controls a mountain bicycle moving downhill under a combination of gravity and pedaling. Most of the game takes place on descending terrain.

The core loop is simple:

1. Maintain speed.
2. Read the trail.
3. Avoid collisions.
4. Stay ahead of the monster.
5. Reach the end of the run.

The player fails by either being caught by the monster or suffering fatal crash damage.

## Player state and failure

### Health

Health is an internal value and is not shown directly to the player.

- The player starts at full health.
- The monster setting contact with the player reduces health to 0 immediately.
- Collisions reduce health based on impact severity and speed.
- Health begins regenerating after 5 seconds without taking damage.
- If health reaches 0, the player dies.

### Crash outcomes

Crashes serve two purposes: they create danger and they create opportunities for the monster to close distance.

- At moderate-to-high collision speeds, the player can be thrown off the bicycle.
- If the crash is severe enough, the player dies immediately.
- If the player survives, they must recover and return to the bicycle.
- Recovery may be represented by a simple QTE or short recovery sequence in the prototype.

### Restart loop

For the prototype, death should immediately lead to a fast restart flow. The emphasis should be on repeated runs and rapid iteration, not persistence systems.

## Controls and bike handling

The bicycle control model is intentionally distinctive and should be treated as a prototype risk worth validating.

### Planned inputs

- Pedal with LMB/RMB on mouse and LT/RT on controller, left input for left leg and right input for right leg.
- Brake with W/S on keyboard and LB/RB on controller, with separate front and rear brake behavior.
- Jump with Space or the south face button.
- Turn with A/D or the left stick.
- Use freelook while riding.

### Handling goals

- The bicycle can reach high speeds where turning and jumping become more dangerous.
- Turning sensitivity should change with speed to preserve control while still feeling unstable at high velocity.
- Excessive speed should create a death wobble effect.
- Overusing the front brake at high speed can throw the player forward.
- Landing from jumps should temporarily destabilize the bicycle and increase handling difficulty.

## Monster behavior

The monster is a pressure system first and a character second.

For the prototype:

- The monster does not need complex pathfinding.
- It can move toward the player in a simplified manner, including floating if necessary.
- Its speed should broadly mirror the player's pace, but react more slowly to acceleration and deceleration.
- The delayed response is what allows the monster to catch up when the player slows down or crashes.
- The monster should have configurable minimum and maximum speed caps.
- Physical contact with the player results in death.

## Technical implementation

The following systems and content are required for the prototype.

### Core systems

- Basic bicycle control system
- Player health system
- Crash and recovery system
- Player headlamp
- Basic following monster AI
- Level loading and segment sequencing system

### Level requirements

- 5 prototype levels or segments
- All levels must connect properly
- All levels must slope downward
- All levels must contain readable trails and obvious hazards
- All levels must include enough environmental decoration to sell speed, danger, and atmosphere

### Basic assets

- Rocks and boulders
- Bushes and trees
- Ground and trail textures
- Reflective trail markers and tape
- Basic sounds
- Main character mountain bicycle

## Audio and atmosphere

Audio is a major contributor to the horror tone.

The prototype should emphasize:

- Wind and speed noise
- Tire, dirt, and impact sounds
- Distant or approaching monster audio cues
- Sparse music used to raise tension rather than constantly fill silence
- A strong contrast between calm stretches and panic moments

## Metrics to evaluate the prototype

The prototype should gather a few useful playtest metrics:

- Average run duration
- Average speed
- Number of crashes per run
- Number of deaths by crash versus monster
- Time spent at low speed
- Monster distance over time
- Completion rate for each segment
- Player feedback on the control scheme

## Open design risks

The following areas are the highest design risk and should be tested early:

- Separate left/right pedaling may be novel but awkward.
- Separate front/rear braking may feel authentic but too demanding.
- The monster may feel unfair if its speed logic is not readable.
- Crash recovery may break pacing if it happens too often.
- Dark visuals may hurt trail readability if atmosphere is prioritized too aggressively.

## Team

**Lakeside Entertainment Studios Inc**

- Pavel "Pablo" Kirillov — software engineering; IT
- Kamilla "Kam" Usmanova — level design; 3D modeling; textures
- Savely "Sovka" Konarev — audio and music; QA testing
