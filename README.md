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
- Modifies the current value of a stat
- Operations can be additive, multiplicative, overrides or custom calculations
- Order of operations is: additive > multiplicative > custom. If we have any override operation, only the one with the highest priority is applied, since it makes no sense to calculate anything if it's gonna be overridden. 
- Additive operations are just added to the base value. E.g.: base + (A + B + C)
- Multiplicative operations are added together and THEN multiplied by the base value. E.g.: base * (A + B + C)

## StatsContainer
- A container for stats
- Responsible for:
  - Holding/getting/setting the value of a stat
  - Applying/removing modifiers to stats
  - Checking if a stat exists
  - Register a StatChangeHandlerDelegate to stats that is applied before calculating the CurrentValue.
    - Only one handler is allowed per stat

## StatChangeHandlerDelegate
- Function that can modify the final value of a stat before it's applied to the stat
- An example of it is if you need to clamp the final value. E.g.: clamping an "HP" stat to the "MaxHP" stat value