# Moar Supplies for SPT 4.1.2

## LLM-Oriented Design and Implementation Specification

**Project status:** Proof of concept / framework phase\
**Target game/server:** SPT 4.1.2\
**Primary implementation language:** C#\
**Primary development workflow:** LM Studio + Codex/local coding agents\
**Document purpose:** Provide a stable specification that an LLM can use
to generate, review, and extend the project without repeatedly
rediscovering the architecture.

------------------------------------------------------------------------

# 1. Project Summary

This project is an SPT server mod that provides a **configurable system
for creating custom consumable stimulants** from a user-friendly
definition.

The long-term goal is to expose a web UI where a user can create and
edit Moar Supplies stimulants without knowing:

-   Tarkov template IDs
-   SPT internal IDs
-   `BuffType` values
-   `SkillName` values
-   localization structures
-   trader-assort structures
-   item cloning details
-   database manipulation
-   SPT implementation details

The user should work with concepts such as:

-   Name
-   Short name
-   Description
-   Base item
-   Number of uses
-   Effects
-   Effect value
-   Duration
-   Delay
-   Trader
-   Price
-   Loyalty level

The mod translates those friendly definitions into the structures
required by SPT.

The first milestone is deliberately much smaller than the final product:

> Load one friendly stim definition from JSON, resolve its base item and
> effects, create a real SPT item, register its buffs, optionally add it
> to a trader, and verify it in-game.

Do **not** begin by building the web UI. The server-side framework must
work first.

------------------------------------------------------------------------

# 2. Core Design Principle

The project has two distinct worlds.

## User-facing world

This should be stable, readable, and independent of Tarkov
implementation details.

Example:

``` json
{
  "id": "super-propital",
  "identity": {
    "name": "Super Propital",
    "shortName": "S-Prop",
    "description": "A powerful regenerative stimulant."
  },
  "baseItem": "propital",
  "uses": 2,
  "buffs": [
    {
      "effect": "healthRate",
      "value": 3,
      "duration": 300,
      "delay": 0
    }
  ]
}
```

## SPT implementation world

This contains things such as:

``` text
template ID
BuffType
SkillName
StimulatorBuffs
Item
ItemProperties
Locales
Trader assort
BarterScheme
LoyalLevelItems
loot tables
spawn configuration
```

The boundary between these worlds is intentional.

**Never require the user-facing schema to expose raw SPT IDs when a
friendly abstraction is possible.**

------------------------------------------------------------------------

# 3. Non-Goals for the First Prototype

The first prototype should NOT attempt to implement all final features.

Do not initially build:

-   web UI
-   client-side BepInEx plugin
-   custom syringe textures
-   arbitrary 3D models
-   custom animations
-   automatic quest generation
-   complex crafting UI
-   automatic loot balancing
-   multiplayer functionality
-   remote configuration
-   account/user management
-   database persistence beyond the mod's configuration
-   automatic generation of every possible Tarkov effect
-   an elaborate plugin architecture

These can be added later.

The first goal is proving the complete server-side path:

``` text
JSON
  ↓
C# configuration models
  ↓
validation
  ↓
friendly-name resolution
  ↓
SPT item creation
  ↓
SPT buff registration
  ↓
trader registration
  ↓
game
```

------------------------------------------------------------------------

# 4. Reference Implementations

The project should use existing mods as implementation references, not
as code to copy blindly.

## Consumables Galore

Repository:

https://github.com/AlmightyTank/ConsumablesGalore

Forum:

https://hub.sp-tarkov.com/forum/thread/4271-consumables-galore/

Important architectural lesson:

Consumables Galore demonstrates that a consumable can be described
through JSON and that a framework can create items from those
definitions.

It is also designed to act as a dependency for other mods.

The project may use Consumables Galore as a dependency if that produces
a cleaner and more reliable implementation. This decision should be made
after inspecting the exact SPT 4.1.2-compatible source/API.

Do not make the project dependent on undocumented behavior merely
because an older release did something similar.

## Cooler Stims

Repository:

https://github.com/Vultify/CoolerStims

Server source:

https://raw.githubusercontent.com/Vultify/CoolerStims/main/CoolerStims/CoolerStims.cs

Client source:

https://raw.githubusercontent.com/Vultify/CoolerStims/main/CoolerStimsClient/CoolerStimsClient.cs

Cooler Stims is particularly useful for understanding the actual SPT
4.1.2 implementation.

It demonstrates:

-   `CustomItemService`
-   cloning an existing item
-   overriding item properties
-   `StimulatorBuffs`
-   registration of custom buff definitions
-   trader assort creation
-   loot registration
-   dependency/load-order considerations
-   client-side syringe texture replacement

The server implementation should be studied for SPT API usage, but the
architecture of this project should support arbitrary stimulant definitions.

Cooler Stims has fixed code/configuration for five specific stims. This
project must instead support an arbitrary number of definitions.

------------------------------------------------------------------------

# 5. SPT Version Requirement

The initial target is:

``` text
SPT 4.1.2
```

All generated C# code must target the exact SPT version being developed
against.

Do not assume that code from:

-   SPT 3.x
-   SPT 4.0
-   SPT 4.2+
-   unrelated Tarkov modding examples

is compatible.

SPT 4.1 changed important server architecture, including dependency
injection and database access.

In particular:

-   server mods should use the SPT 4.1 APIs actually exposed by the
    referenced assemblies
-   database tables/configuration should be obtained using the current
    SPT 4.1 dependency-injection model
-   do not blindly use older `DatabaseService` examples
-   do not mix assemblies from different SPT releases

If there is uncertainty, inspect the actual SPT 4.1.2 DLLs/source/API
before generating code.

------------------------------------------------------------------------

# 6. Proposed Project Structure

Initial structure:

``` text
MoarSupplies/
├── MoarSupplies.csproj
├── Mod.cs
│
├── Models/
│   ├── ModConfig.cs
│   ├── StimDefinition.cs
│   ├── StimIdentity.cs
│   ├── BuffDefinition.cs
│   └── TraderDefinition.cs
│
├── Definitions/
│   ├── BuffMappings.cs
│   └── ItemMappings.cs
│
├── Services/
│   ├── StimService.cs
│   ├── BuffService.cs
│   └── ItemService.cs
│
├── Validation/
│   └── ConfigValidator.cs
│
└── config/
    └── stims.json
```

This is an initial organization, not a mandate to create unnecessary
abstractions.

If the SPT API makes a simpler structure more appropriate, prefer the
simpler structure.

------------------------------------------------------------------------

# 7. Configuration Model

The configuration should have a version number.

Example:

``` json
{
  "version": 1,
  "stims": [
    {
      "id": "super-propital",
      "enabled": true,
      "identity": {
        "name": "Super Propital",
        "shortName": "S-Prop",
        "description": "A powerful regenerative stimulant."
      },
      "baseItem": "propital",
      "uses": 2,
      "buffs": [
        {
          "effect": "maxStamina",
          "value": 15,
          "duration": 110,
          "delay": 0
        },
        {
          "effect": "healthRate",
          "value": 3,
          "duration": 300,
          "delay": 0
        }
      ],
      "trader": {
        "enabled": true,
        "trader": "therapist",
        "loyaltyLevel": 3,
        "price": 85000
      }
    }
  ]
}
```

The schema is intentionally simple.

------------------------------------------------------------------------

# 8. C# Configuration Models

Initial conceptual models:

``` csharp
public class ModConfig
{
    public int Version { get; set; }
    public List<StimDefinition> Stims { get; set; } = [];
}

public class StimDefinition
{
    public string Id { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public StimIdentity Identity { get; set; } = new();
    public string BaseItem { get; set; } = "";
    public int Uses { get; set; }
    public List<BuffDefinition> Buffs { get; set; } = [];
    public TraderDefinition? Trader { get; set; }
}

public class StimIdentity
{
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string Description { get; set; } = "";
}

public class BuffDefinition
{
    public string Effect { get; set; } = "";
    public double Value { get; set; }
    public int Duration { get; set; }
    public int Delay { get; set; }
}

public class TraderDefinition
{
    public bool Enabled { get; set; }
    public string Trader { get; set; } = "";
    public int LoyaltyLevel { get; set; }
    public int Price { get; set; }
}
```

These are starting models, not necessarily final models.

The LLM should not add fields simply because they might be useful
someday.

Add a field only when:

1.  the current implementation requires it,
2.  the user-facing configuration needs it, or
3.  it is required to preserve information when converting between
    layers.

------------------------------------------------------------------------

# 9. Why Buffs Are an Array

Buffs should be represented as an array/list:

``` json
"buffs": [
  {
    "effect": "maxStamina",
    "value": 15,
    "duration": 110,
    "delay": 0
  },
  {
    "effect": "healthRate",
    "value": 3,
    "duration": 300,
    "delay": 0
  }
]
```

Do not initially model them as:

``` json
"buffs": {
  "maxStamina": {},
  "healthRate": {}
}
```

An array is preferable because:

-   the number of effects is arbitrary
-   the UI can add/remove effect rows
-   effects can potentially occur more than once
-   staged effects can be represented
-   order can be preserved
-   the JSON structure maps naturally to `List<BuffDefinition>`

------------------------------------------------------------------------

# 10. Friendly Effect Registry

The user should never need to enter:

``` text
BuffType = SkillRate
SkillName = StressResistance
```

Instead they enter:

``` text
effect = skillStressResistance
```

The server resolves that to the SPT representation.

Conceptually:

``` csharp
public class BuffMapping
{
    public string BuffType { get; init; } = "";
    public string SkillName { get; init; } = "";
    public bool RequiresValue { get; init; } = true;
}
```

Example registry:

``` csharp
private static readonly Dictionary<string, BuffMapping> BuffMappings =
    new()
    {
        ["maxStamina"] = new BuffMapping
        {
            BuffType = "MaxStamina"
        },

        ["healthRate"] = new BuffMapping
        {
            BuffType = "HealthRate"
        },

        ["skillStressResistance"] = new BuffMapping
        {
            BuffType = "SkillRate",
            SkillName = "StressResistance"
        },

        ["tunnelVision"] = new BuffMapping
        {
            BuffType = "QuantumTunnelling",
            RequiresValue = false
        }
    };
```

The exact supported effect list must be verified against SPT 4.1.2.

Do not invent `BuffType` values.

------------------------------------------------------------------------

# 11. Initial Known Buff Mappings

These mappings have been observed in the Cooler Stims implementation and
are starting references.

``` text
Friendly effect             SPT representation

maxStamina                  BuffType = MaxStamina

staminaRate                 BuffType = StaminaRate

stressResistance            BuffType = SkillRate
                            SkillName = StressResistance

energyRate                  BuffType = EnergyRate

healthRate                  BuffType = HealthRate

skillHealth                 BuffType = SkillRate
                            SkillName = Health

vitality                    BuffType = SkillRate
                            SkillName = Vitality

weightLimit                 BuffType = WeightLimit

quantumTunnelling            BuffType = QuantumTunnelling
```

This list is not a promise that all of these are valid for every SPT
version.

Before implementation, verify them against the SPT 4.1.2 types and
behavior.

------------------------------------------------------------------------

# 12. Buff Conversion

The user-facing:

``` json
{
  "effect": "stressResistance",
  "value": 25,
  "duration": 110,
  "delay": 0
}
```

must eventually become an SPT `Buff` resembling:

``` text
BuffType     = SkillRate
SkillName    = StressResistance
Value        = 25
Duration     = 110
Delay        = 0
Chance       = 1
AbsoluteValue = appropriate value
```

The conversion belongs in a service such as:

``` text
BuffService
```

The configuration model should not know about SPT's `Buff` class.

The SPT `Buff` class should not leak into the user-facing configuration
model.

------------------------------------------------------------------------

# 13. Item Resolution

The configuration contains:

``` json
"baseItem": "propital"
```

It should NOT contain:

``` json
"baseItem": "5c0e534186f7747fa1419867"
```

The friendly name is resolved internally.

Possible architecture:

``` text
ItemMappings
     ↓
"propital"
     ↓
SPT template ID
     ↓
TemplateTable
     ↓
source item
```

The first prototype may use a small explicit registry:

``` text
propital → verified Propital template ID
morphine → verified Morphine template ID
```

Later, the project may build or validate the registry from the SPT
database.

Do not make automatic database discovery unnecessarily complicated
during the first prototype.

------------------------------------------------------------------------

# 14. Important Item-Cloning Concept

The most practical initial implementation is to clone an existing
consumable.

Cooler Stims demonstrates this pattern with:

``` text
CustomItemService.CreateItemFromClone(...)
```

The clone provides a large amount of existing Tarkov item behavior.

The project then overrides only the properties required for the new
stim.

Typical overrides include:

-   display/localization information
-   background color
-   stim buffs
-   prefab
-   use time
-   resource count
-   damage effects

The exact set of overrides must be determined from the selected base
item and the SPT 4.1.2 API.

Do not blindly copy every property from Cooler Stims.

------------------------------------------------------------------------

# 15. Generated Item IDs

Each custom stim needs a unique SPT item/template ID.

The user-facing `id`:

``` text
super-propital
```

is NOT necessarily the SPT template ID.

The implementation should have a deterministic strategy for generated
IDs.

Possible first implementation:

-   use a stable generated identifier stored in configuration, or
-   generate a UUID-style identifier at first creation and persist it.

Do not regenerate an item's SPT ID every server start if doing so would
cause existing profiles/configurations to reference a different item.

This is a critical persistence consideration.

The exact persistence strategy should be finalized before the project
reaches public-release status.

------------------------------------------------------------------------

# 16. Buff Registration

The custom item generally references a buff collection by key.

Conceptually:

``` text
Stim item
    ↓
StimulatorBuffs = "custom_stim_x"
    ↓
GlobalTable.Configuration.Health.Effects.Stimulator.Buffs
    ↓
List<Buff>
```

The key must be unique per custom stim.

Example conceptual key:

``` text
MoarSupplies_super-propital
```

Do not use a hard-coded key for every generated stim.

The key-generation rules should be deterministic.

------------------------------------------------------------------------

# 17. Trader Registration

Trader configuration should remain user-friendly:

``` json
"trader": {
  "enabled": true,
  "trader": "therapist",
  "loyaltyLevel": 3,
  "price": 85000
}
```

Internally this must become the structures expected by SPT:

``` text
Trader assort item
BarterScheme
LoyalLevelItems
```

The project should have a friendly trader registry similar to the effect
registry.

Example concept:

``` text
therapist → SPT Therapist trader ID
prapor    → SPT Prapor trader ID
skier     → SPT Skier trader ID
```

Again, IDs must be resolved internally.

The user should not need to know them.

------------------------------------------------------------------------

# 18. Load Order

Load order matters.

Cooler Stims deliberately loads its custom items before certain
profile/handbook validation stages because profiles containing custom
items can otherwise be rejected or removed if registration happens too
late.

The implementation must therefore verify the correct SPT 4.1.2
load-order mechanism.

Do not guess the priority.

Inspect:

-   the SPT 4.1.2 mod-loading interfaces
-   `OnLoadAsync`
-   `OnLoadOrder`
-   `TraderRegistration`
-   the actual reference implementation

The goal is:

``` text
custom item registration
        ↓
SPT validation/profile initialization
        ↓
normal server operation
```

------------------------------------------------------------------------

# 19. Main Service Responsibilities

## Mod.cs

Responsible for:

-   SPT mod metadata
-   dependency injection entry point
-   loading configuration
-   invoking validation
-   invoking the main stim service

It should NOT contain the implementation of every feature.

## StimService

Responsible for orchestration:

``` text
for each enabled stim:
    validate
    resolve base item
    create item
    create buffs
    register buffs
    register trader if enabled
```

## ItemService

Responsible for:

-   resolving the base item
-   cloning the item
-   applying item properties
-   creating localization
-   registering the resulting item

## BuffService

Responsible for:

-   resolving friendly effect names
-   converting `BuffDefinition` to SPT `Buff`
-   registering the buff list
-   reporting unsupported effects

## ConfigValidator

Responsible for rejecting invalid configuration before modifying the SPT
database.

------------------------------------------------------------------------

# 20. Validation Rules

Validation should happen before attempting to create items.

At minimum:

``` text
version must be supported

stim ID:
    required
    unique
    stable
    contains only safe identifier characters

name:
    required
    non-empty

shortName:
    required
    non-empty

baseItem:
    required
    must exist in item registry

uses:
    must be greater than zero

buff:
    effect must exist in effect registry

buff duration:
    must be >= 0

buff delay:
    must be >= 0

buff value:
    required when the effect requires it

trader:
    if enabled:
        trader must exist
        price must be valid
        loyalty level must be valid
```

Validation errors should clearly identify the stim and field.

Example:

``` text
Stim 'super-propital':
  buffs[1].effect 'fooBar' is not a supported effect.
```

Do not allow one bad definition to produce a partially registered item.

------------------------------------------------------------------------

# 21. Error Handling Philosophy

The mod should fail safely.

Preferred behavior:

``` text
Load config
    ↓
Validate everything
    ↓
If invalid:
    log clear errors
    do not partially register invalid definitions
```

Avoid:

``` text
Create item 1
Create item 2
Create item 3
discover item 4 is invalid
crash halfway through
```

For the prototype, it is acceptable to abort all custom-stim
registration if configuration validation fails.

A later version may support per-stim isolation.

------------------------------------------------------------------------

# 22. Logging

Logging should be useful to someone debugging a mod installation.

Example:

``` text
[MoarSupplies] Loading configuration...
[MoarSupplies] Found 1 stim definition.
[MoarSupplies] Validating 'super-propital'...
[MoarSupplies] Resolved base item 'propital'.
[MoarSupplies] Creating item 'Super Propital'.
[MoarSupplies] Registered 2 buffs.
[MoarSupplies] Added 'Super Propital' to Therapist.
[MoarSupplies] Successfully registered 1 custom stim.
```

Errors should include actionable information.

Avoid dumping enormous SPT objects into logs unless debug logging is
enabled.

------------------------------------------------------------------------

# 23. Initial Proof-of-Concept Stim

The first test item should be deliberately simple.

Suggested definition:

``` json
{
  "version": 1,
  "stims": [
    {
      "id": "test-propital",
      "enabled": true,
      "identity": {
        "name": "Test Propital",
        "shortName": "T-Prop",
        "description": "Proof-of-concept custom stimulant."
      },
      "baseItem": "propital",
      "uses": 2,
      "buffs": [
        {
          "effect": "healthRate",
          "value": 3,
          "duration": 60,
          "delay": 0
        }
      ],
      "trader": {
        "enabled": true,
        "trader": "therapist",
        "loyaltyLevel": 1,
        "price": 10000
      }
    }
  ]
}
```

The exact values are unimportant.

The purpose is to test the pipeline.

------------------------------------------------------------------------

# 24. Proof-of-Concept Milestones

Implement in this order.

## Milestone 1 --- Project loads

Create:

``` text
MoarSupplies.csproj
Mod.cs
```

Confirm:

-   project builds
-   DLL loads into SPT 4.1.2
-   `OnLoadAsync` executes
-   a log message appears

Do not create custom items yet.

## Milestone 2 --- Configuration loading

Add:

``` text
Models/
config/stims.json
```

Confirm:

-   JSON loads
-   configuration deserializes
-   stim count is logged
-   stim name is logged

## Milestone 3 --- Validation

Add validation.

Confirm:

-   valid configuration passes
-   missing base item fails
-   unknown effect fails
-   invalid duration/value fails
-   duplicate IDs fail

## Milestone 4 --- Base-item resolution

Implement:

``` text
"propital"
    ↓
verified SPT template ID
```

Confirm the source item can be retrieved from the SPT template table.

## Milestone 5 --- Item cloning

Create one actual item.

Confirm:

-   item is registered
-   item appears in the relevant database structures
-   server starts cleanly
-   item appears correctly in-game

## Milestone 6 --- Buff registration

Add one buff.

Confirm:

-   item points at the custom buff key
-   buff exists in the global stim buff collection
-   using the stim produces the expected effect

## Milestone 7 --- Trader

Add Therapist.

Confirm:

-   item appears in the trader assort
-   price is correct
-   loyalty requirement is correct
-   item can be purchased

## Milestone 8 --- Multiple effects

Add two or more buffs.

Confirm that arbitrary list entries work.

## Milestone 9 --- Second stim

Create a second definition using a different base item.

This is the critical arbitrary-definition test.

If adding the second stim requires copying an entire method such as:

``` text
CreateAPEX()
CreateAEGIS()
CreateIRON()
```

the architecture is wrong.

The desired result is:

``` text
one shared CreateStim(...)
+
two JSON definitions
=
two working stims
```

------------------------------------------------------------------------

# 25. Arbitrary Definition Test

The most important architectural test is this:

Adding a new stim should require **configuration**, not new C# logic.

Bad:

``` csharp
CreateAPEX();
CreateAEGIS();
CreateIRON();
CreateARGUS();
```

Good:

``` csharp
foreach (StimDefinition stim in config.Stims)
{
    _stimService.CreateStim(stim);
}
```

The implementation should not know that a particular stim is called
APEX, AEGIS, Propital+, or anything else.

------------------------------------------------------------------------

# 26. Relationship to Cooler Stims

Cooler Stims currently represents a fixed set of predefined stimulants.

Conceptually it does:

``` text
CreateAPEX()
AddAPEXBuffs()
AddAPEXToTherapist()

CreateAEGIS()
AddAEGISBuffs()
AddAEGISToTherapist()

...
```

This project should transform that architecture into:

``` text
CreateStim(stimDefinition)
```

where the definition determines:

-   base item
-   identity
-   uses
-   buffs
-   trader
-   later loot
-   later crafting
-   later appearance

The implementation should contain the mechanics once.

------------------------------------------------------------------------

# 27. Client Plugin Strategy

A client-side BepInEx plugin may eventually be used for:

-   custom syringe textures
-   custom item appearance
-   custom models
-   other visual changes

This is explicitly **not required for version 1**.

Cooler Stims' client plugin uses a hard-coded mapping:

``` text
custom template ID → texture
```

That approach does not scale well to arbitrary user-created stims.

Do not design the server framework around a client plugin.

Later, a client system could potentially use:

``` text
stable custom item ID
    ↓
appearance definition
    ↓
client asset
```

but this should be treated as a separate project layer.

------------------------------------------------------------------------

# 28. Future Web UI

The final architecture should eventually look like:

``` text
Browser
   ↓
Web UI
   ↓
friendly JSON/config model
   ↓
validation
   ↓
StimService
   ↓
SPT
```

The web UI should not directly manipulate SPT objects.

The UI should know things like:

``` text
Effect: Stress Resistance
Value: 25
Duration: 110
Delay: 0
```

and the server should translate that into:

``` text
BuffType = SkillRate
SkillName = StressResistance
```

The UI should be able to obtain effect options from the server's
registry rather than maintaining a second hard-coded list whenever
practical.

------------------------------------------------------------------------

# 29. Future Schema Expansion

Potential later fields:

``` json
{
  "appearance": {},
  "item": {},
  "trader": {},
  "loot": {},
  "craft": {},
  "quests": {}
}
```

Do not implement these until the core item/buff pipeline works.

Possible future configuration:

``` json
{
  "item": {
    "backgroundColor": "red",
    "useTime": 2.0,
    "uses": 2
  },
  "loot": {
    "enabled": true,
    "weight": 1.0
  },
  "craft": {
    "enabled": true,
    "productionTime": 1500
  }
}
```

The exact schema should be designed when those features are actually
implemented.

------------------------------------------------------------------------

# 30. Dependency Strategy

Consumables Galore is a candidate dependency, but the project should not
add it merely because it already solves some of the same problem.

Before choosing the dependency approach, answer:

1.  Is the current Consumables Galore source compatible with SPT 4.1.2?
2.  Does its API expose the functionality needed by this project?
3.  Can it accept the arbitrary definitions we want?
4.  Does relying on it constrain our schema?
5.  Does it simplify item registration enough to justify the dependency?
6.  What happens when Consumables Galore changes versions?
7.  Can our mod fail clearly when the dependency is absent or
    incompatible?

If the dependency becomes more restrictive than helpful, implement the
required functionality directly against SPT.

Do not duplicate large portions of another mod without understanding its
license and architecture.

Consumables Galore is MIT-licensed in the referenced repository.

------------------------------------------------------------------------

# 31. LLM Coding Rules

These rules are specifically for LM Studio/Codex or another coding LLM.

## Rule 1 --- Inspect before changing

Before modifying a file:

-   inspect the existing file
-   understand the current structure
-   identify its references
-   do not overwrite unrelated work

## Rule 2 --- Target SPT 4.1.2

Never silently target another SPT version.

If an API differs:

-   inspect the actual assembly/source
-   state the incompatibility
-   adapt to 4.1.2

## Rule 3 --- Do not invent APIs

Do not assume a class or method exists.

If uncertain, search the local SPT installation or referenced source.

Examples of APIs that must be verified rather than guessed:

``` text
CustomItemService
TemplateTable
GlobalTable
LocationTable
TraderHelper
ICloner
IMod
OnLoadAsync
OnLoadOrder
```

## Rule 4 --- Preserve the user-facing abstraction

Do not put raw IDs into `stims.json`.

Bad:

``` json
"baseItem": "5c0e534186f7747fa1419867"
```

Good:

``` json
"baseItem": "propital"
```

## Rule 5 --- Avoid premature abstraction

Do not create:

``` text
IStimFactoryFactory
IRegistryProviderFactory
IWhateverManager
```

unless there is a demonstrated need.

Prefer straightforward services and data models.

## Rule 6 --- Explain before large changes

Before generating a large implementation:

1.  explain the files being created/changed
2.  explain the data flow
3.  identify SPT APIs being used
4.  identify assumptions
5.  then implement

## Rule 7 --- Keep models independent from SPT

Configuration models should not inherit from or contain SPT database
classes.

The boundary should remain:

``` text
User model → conversion service → SPT model
```

## Rule 8 --- No hard-coded stim-specific methods

Do not create:

``` text
CreateAPEX()
CreateGRAFT()
CreateMYNEWSTIM()
```

unless the behavior is genuinely unique and cannot be expressed through
the schema.

## Rule 9 --- Fail clearly

Configuration errors should name:

-   stim ID
-   field
-   invalid value
-   expected value/range when possible

## Rule 10 --- Small commits / small changes

Implement one milestone at a time.

Do not generate the entire final project in one pass.

------------------------------------------------------------------------

# 32. LLM Prompt for Starting the Project

Use the following as the initial instruction to a coding LLM:

``` text
You are helping implement an SPT 4.1.2 server mod called MoarSupplies.

Read DESIGN.md completely before writing code.

The project is a configurable stimulant system. Users will eventually define
stims through a friendly web UI, but the first implementation is server-side only.

The first proof of concept must:

1. Load the mod in SPT 4.1.2.
2. Load config/stims.json.
3. Deserialize it into C# configuration models.
4. Validate the configuration.
5. Resolve a friendly base item name such as "propital" to its verified SPT item template.
6. Clone/create one custom item.
7. Register one or more definition-based stim buffs.
8. Optionally register the item with Therapist.
9. Log each stage clearly.
10. Work for arbitrary stim definitions without stim-specific methods.

Do not implement the web UI yet.
Do not implement the client plugin yet.
Do not implement custom textures yet.
Do not add loot/crafting/quest functionality yet.

Target SPT 4.1.2 exactly.

Before coding, inspect the available SPT 4.1.2 assemblies/source and verify the APIs
needed for item creation, database access, buff registration, and mod loading.

Do not invent APIs.

Do not expose raw SPT template IDs in the user-facing JSON.

Use the architecture and milestones in DESIGN.md as the source of truth.

Before making changes, explain:
- what files will be created/modified,
- what SPT APIs are being used,
- what assumptions are being made,
- and which milestone the change implements.

Then make only the smallest change required for that milestone.
```

------------------------------------------------------------------------

# 33. LLM Prompt for Code Review

Use this when asking the LLM to review implementation:

``` text
Review the current MoarSupplies implementation against DESIGN.md.

Do not rewrite the project automatically.

Check specifically:

1. SPT version compatibility with 4.1.2.
2. Correct dependency injection/API usage.
3. Configuration model correctness.
4. Validation behavior.
5. Friendly-name abstraction.
6. Buff conversion.
7. Item cloning/registration.
8. Trader registration.
9. Load order.
10. Generated/stable IDs.
11. Error handling.
12. Whether adding a second stim requires new C# code.
13. Whether raw SPT IDs have leaked into user-facing configuration.
14. Whether the code contains unnecessary abstractions.
15. Whether any APIs appear to have been guessed rather than verified.

Report findings first.

Classify findings as:
- CRITICAL
- HIGH
- MEDIUM
- LOW

Do not make changes until explicitly instructed.
```

------------------------------------------------------------------------

# 34. LLM Prompt for Adding a New Feature

Use this pattern for later work:

``` text
We need to add [FEATURE] to MoarSupplies.

Read DESIGN.md and inspect the existing implementation first.

Do not redesign unrelated systems.

Determine:
1. Whether the feature belongs in the user-facing schema.
2. Which model represents it.
3. Which service should implement it.
4. Which SPT 4.1.2 APIs are required.
5. Whether the feature requires new registry entries.
6. How it should be validated.
7. How it should be tested.

Explain the proposed change before coding.

Keep the implementation definition-driven so the feature applies to arbitrary stim
definitions rather than a specific named stim.

Do not expose SPT IDs in user configuration unless there is no reasonable
abstraction for the value.
```

------------------------------------------------------------------------

# 35. Testing Strategy

The project should eventually have several levels of testing.

## Configuration test

Input:

``` json
{
  "version": 1,
  "stims": []
}
```

Expected:

``` text
valid
```

## Invalid effect

Input:

``` json
"effect": "notARealEffect"
```

Expected:

``` text
validation failure
```

## Invalid base item

Input:

``` json
"baseItem": "does-not-exist"
```

Expected:

``` text
validation failure
```

## Multiple buffs

A stim with three effects should produce three SPT buffs.

## Multiple stims

Two independent definitions should produce two independent items.

## Restart test

Restarting the server should not unexpectedly create different IDs or
duplicate registrations.

## In-game test

Verify:

-   item exists
-   name/description are correct
-   use animation works
-   uses/resource count works
-   buffs apply
-   buffs expire
-   trader entry works

------------------------------------------------------------------------

# 36. Definition of Done for the Initial PoC

The first PoC is complete when all of the following are true:

-   [ ] SPT 4.1.2 loads the mod without errors.
-   [ ] JSON configuration loads.
-   [ ] Configuration is represented by strongly typed C# models.
-   [ ] Configuration validation works.
-   [ ] Friendly base-item names resolve correctly.
-   [ ] One custom item can be cloned/created.
-   [ ] The custom item is registered correctly.
-   [ ] At least one definition-based buff can be attached.
-   [ ] The buff actually works in-game.
-   [ ] Therapist registration works.
-   [ ] A second stim can be added only by changing JSON.
-   [ ] No stim-specific `CreateXYZ()` method is required.
-   [ ] User configuration contains no raw SPT IDs.
-   [ ] Logs make failures understandable.
-   [ ] The server can restart without corrupting the configuration or
    duplicating items.

------------------------------------------------------------------------

# 37. Long-Term Architecture

The intended mature architecture is:

``` text
                         ┌─────────────────────┐
                         │      Web UI          │
                         │ friendly controls    │
                         └──────────┬──────────┘
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │   Config / Models   │
                         │ stable user schema  │
                         └──────────┬──────────┘
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │     Validation      │
                         └──────────┬──────────┘
                                    │
                    ┌───────────────┼────────────────┐
                    ▼               ▼                ▼
             Item Registry    Effect Registry   Trader Registry
                    │               │                │
                    └───────────────┼────────────────┘
                                    ▼
                         ┌─────────────────────┐
                         │    Stim Service     │
                         │ orchestration       │
                         └──────────┬──────────┘
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │       SPT 4.1.2     │
                         │ database / services │
                         └─────────────────────┘

Optional later:

                         ┌─────────────────────┐
                         │ BepInEx Client Mod  │
                         │ visual customization │
                         └─────────────────────┘
```

The central architectural principle is:

> **The user defines what a stim should do. The mod determines how SPT
> must implement it.**

------------------------------------------------------------------------

# 38. Current Recommended Development Order

Follow this order unless a concrete SPT API constraint requires
otherwise:

``` text
1. Project skeleton
2. SPT 4.1.2 loading
3. Configuration loading
4. Strongly typed models
5. Validation
6. Friendly item registry
7. Item cloning
8. Buff registry
9. Buff conversion
10. Buff registration
11. Trader registry
12. Trader registration
13. Stable item IDs
14. Second-stim test
15. Expanded item properties
16. Loot
17. Crafting
18. Additional configuration
19. Persistence improvements
20. Web API/UI
21. Optional client visuals
```

Do not skip directly to step 20.

The web UI is only useful if the server-side definition engine is
already stable.

------------------------------------------------------------------------

# 39. Final Architectural Constraints

These constraints should be treated as project-level requirements.

### Required

-   Target SPT 4.1.2 for the initial implementation.
-   Definition-based configuration.
-   Friendly user-facing schema.
-   Strongly typed configuration models.
-   Validation before registration.
-   Internal ID/effect resolution.
-   Definition-driven item creation.
-   Definition-driven buff creation.
-   Stable generated item identifiers.
-   Clear logging.
-   Incremental implementation.

### Avoid

-   Raw IDs in user configuration.
-   Hard-coded methods for every stim.
-   Version mixing.
-   Guessing SPT APIs.
-   Premature web UI development.
-   Premature client plugin development.
-   Large unrelated refactors.
-   Excessive abstraction.
-   Silent configuration failures.
-   Partial registration after validation errors.

------------------------------------------------------------------------

# 40. Change-Control Rule

If an LLM believes the architecture should change, it must explain:

1.  What requirement is causing the problem.
2.  Why the current architecture cannot satisfy it.
3.  What alternative is proposed.
4.  What files/classes would change.
5.  What future functionality is affected.

The LLM must not silently change the schema or architecture.

This is especially important for:

-   configuration structure
-   identifier strategy
-   effect names
-   dependency strategy
-   SPT integration
-   web API boundaries

The design document is the baseline. Changes are deliberate decisions,
not incidental consequences of code generation.

------------------------------------------------------------------------

# 41. Immediate Next Step

The next coding session should begin with **Milestone 1 only**.

The coding agent should:

1.  inspect the actual SPT 4.1.2 installation/reference assemblies,
2.  create the minimal `.csproj`,
3.  create the minimal mod entry point,
4.  compile it,
5.  install the DLL in a test SPT instance,
6.  verify that the mod loads and logs successfully.

Do not create the item system until this foundation is confirmed.

Once Milestone 1 works, proceed to configuration loading.

------------------------------------------------------------------------

## Appendix A --- Known Reference Patterns

### Cooler Stims item creation

The reference implementation uses the SPT custom item service with a
clone definition conceptually containing:

``` text
ItemTplToClone
NewId
NewItemName
ParentId
HandbookParentId
HandbookPriceRoubles
FleaPriceRoubles
Locales
OverrideProperties
```

The exact API and property types must be verified against the SPT 4.1.2
assemblies.

### Cooler Stims buff structure

The reference implementation ultimately constructs SPT `Buff` objects
containing values such as:

``` text
Chance
Delay
Duration
Value
AbsoluteValue
BuffType
SkillName
```

Again, use the actual 4.1.2 type definitions.

### Special clone cases

Some effects require a specific base item because the base item's
existing behavior is useful.

For example, the Cooler Stims GRAFT implementation clones Morphine so it
can retain relevant medical/limb behavior while adding stim behavior.

This illustrates an important future principle:

> Base item selection can affect more than appearance; it can determine
> inherited gameplay behavior.

The first prototype should use a simple known-compatible stim base and
avoid special cases until the definition-driven path is proven.

------------------------------------------------------------------------

## Appendix B --- What the LLM Should Ask the Developer to Provide

If the coding environment does not already contain the required SPT
installation/reference assemblies, the LLM should ask for:

``` text
SPT 4.1.2 server installation path
SPT 4.1.2 DLL/reference assemblies
existing mod template if available
target output/mod installation directory
.NET SDK version used by the SPT 4.1.2 modding environment
```

It should not fabricate these values.

If the project already contains a known working `.csproj` from another
SPT 4.1.2 mod, inspect and use it as a compatibility reference before
inventing project references.

------------------------------------------------------------------------

# Appendix C --- Guiding Example

The desired user experience eventually looks like this:

``` text
Create Stim

Name:           Super Propital
Short Name:     S-Prop
Base Item:      Propital
Uses:           2

Effects:
  [Health Rate]
  Value:        3
  Duration:     300
  Delay:        0

  [+ Add Effect]

Trader:
  [x] Therapist
  Loyalty:      3
  Price:        85000

[Save]
```

The user should never have to see:

``` text
5c0e534186f7747fa1419867
54cb57776803fa99248b456e
SkillRate
StressResistance
StimulatorBuffs
```

Those are implementation details.

That separation is the core of the project.
