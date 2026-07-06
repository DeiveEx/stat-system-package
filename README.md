# Description

A system controlling and modifying multiple status commonly seem in games, like HP, MP, Stamina, etc.

# Install

Use the "Add from git" option in the package manager and paste the following URL:
```
https://github.com/DeiveEx/stat-system-package.git
```

# Contents

## StatDefinition
- A value that can be modified by modifiers
- Has a base value and a current value
- The base value is the value without any modifiers
- The current value is the base value after modifiers are applied

## StatModifier
- Base class for modifying the current value of a stat
- Current value calculation is done by `IStatCurrentValueResolver`
- Provided modifiers:
  - Additive: `current = base + (A + B + C)`
  - Multiplicative: `current = base + (base * (A + B + C))`
  - Overrides: `current = A (based on priority)`
  - Custom: `current = CustomCalculation()`
- When additive and multiplicative modifiers are combined, the multiplicative part applies to the additive result, not the raw base: `current = (base + Σadditive) * (1 + Σmultiplicative)`

## DefaultStatResolver
- Default implementation of `IStatCurrentValueResolver`
- Resolves the Stat CurrentValue in the following order:
  1. Checks if there's any override modifier. If yes, set the CurrentValue to that since it makes no sense to calculate anything if it's just gonna be overriden. The override with the highest priority wins; between overrides with the same priority, the last applied wins
  2. Apply additive modifiers
  3. Apply multiplicative modifiers
  4. Apply custom calculations, in the order they were applied. While an override is active, only custom modifiers created with `applyOnOverride = true` are applied (on top of the override value)

## StatsContainer
- A container for stats
- Responsible for:
  - Holding/getting/setting the value of a stat
  - Adding/removing stats
  - Applying/removing modifiers to stats
    - `ApplyModifier` returns an `IDisposable` handle that removes that exact modifier instance when disposed (safe to dispose multiple times or ignore entirely)
    - Modifiers can be removed by ID (`removeAll` optionally removes every modifier sharing that ID) or by instance
  - Checking if a stat exists (`StatExists`) or has a given modifier (`HasModifier`, `GetModifierCount`)
  - Recalculating stats (`RecalculateStat`/`RecalculateAll`): if a modifier reads OTHER stats, the container can't know about that dependency, so call this to refresh the value when the other stat changes
  - Saving/loading base values (`GetBaseValueSnapshot`/`ApplySnapshot`). Modifiers are live objects, so re-applying them is the game's responsibility
  - Register a StatChangeHandlerDelegate to stats that is applied before calculating the CurrentValue.
    - Only one handler is allowed per stat: registering a new one replaces the previous handler
    - A simpler `Func<float, float>` overload exists for handlers that only care about the value

## StatChangeHandlerDelegate
- Function that can modify the final value of a stat before it's applied to the stat
- An example of it is if you need to clamp the final value. E.g.: clamping an "HP" stat to the "MaxHP" stat value