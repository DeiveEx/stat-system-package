# Description

A system controlling and modifying multiple status commonly seem in games, like HP, MP, Stamina, etc.

# Contents

## Stat
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
- Responsible for getting/setting the value of a stat, applying/removing modifiers to stats and checking if a stat exists
- Can also register a special handler to stats. Only one handler is allowed per stat

## StatValueChangeHandler
- Function that can modify the final value of a stat before it's applied to the stat
- An example of it is if you need to clamp the final value. E.g.: clamping an "HP" stat to the "MaxHP" stat value