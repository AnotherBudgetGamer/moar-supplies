# Milestone 8 — Multi-definition and persistence test

This test confirms that the generic registration path works for more than the original single-stim case.

## Definitions under test

| ID | Enabled | Base | Effects | Trader |
| --- | --- | --- | --- | --- |
| `hydra` | Yes | Propital | `healthRate`, `energyRate`, `staminaRate` | Therapist LL1 |
| `baldur` | Yes | Morphine | `stressResistance`, `vitality`, `concussion`, `handsTremor` | Prapor LL1 |
| `svarog` | Yes | Propital | `weightLimit`, `staminaRate`, `hydrationRate`, `energyRate` | Skier LL1 |
| `argus` | Yes | Adrenaline | `attention`, `aiming`, `maxStamina`, `hearingDistance`, `tunnelVision` | Therapist LL1 |
| `ravana` | Yes | PNB | combat skills, pain suppression, resource drains, delayed tremor | Therapist LL1 |
| `disabled-control` | No | Propital | `maxStamina` | None |

## Startup test

1. Build and deploy the mod.
2. Start SPT with a valid profile.
3. Confirm configuration validation passes and reports six definitions.
4. Confirm five items, five buff keys, and five trader assorts are registered.
5. Confirm `disabled-control` logs that registration was skipped.
6. Confirm the server reaches normal startup without errors.

## Trader and item test

1. Confirm `Hydra` is sold by Therapist at LL1 for 10,000 roubles.
2. Confirm `Baldur` is sold by Prapor at LL1 for 15,000 roubles.
3. Confirm `Svarog` is sold by Skier at LL1 for 20,000 roubles.
4. Confirm `Argus` is sold by Therapist at LL1 for 25,000 roubles.
5. Confirm `Ravana` is sold by Therapist at LL1 for 20,000 roubles.
6. Confirm the disabled control stim is not offered by any trader.
7. Inspect all five items and verify their name, short name, description, and inherited model.
8. Confirm `Argus` uses the Adrenaline model and `Ravana` uses the PNB model.

## Gameplay test

1. Use a freshly purchased `Hydra` twice and confirm its resource is depleted only after the second use.
2. Confirm `Hydra` restores health while draining energy and stamina for 300 seconds.
3. Use `Baldur` and confirm Stress Resistance and Vitality increase while concussion and hand tremor occur for 60 seconds.
4. Use `Svarog` and confirm carrying capacity and stamina recovery increase while hydration and energy drain for 300 seconds.
5. Use `Argus` and confirm Attention, Aim Drills, and maximum stamina increase for 300 seconds.
6. Confirm `Argus` reduces the Perception-derived hearing distance during its 300-second effect.
7. Confirm `Argus` tunnel vision begins after 60 seconds and persists for 120 seconds.
8. Use `Ravana` and confirm its combat-skill modifiers, 30-second pain suppression, and immediate resource drains.
9. Confirm Ravana's hand tremor begins after 120 seconds and ends 60 seconds later.
10. Confirm each effect ends after its configured duration.

## Profile persistence test

1. Buy at least one of each enabled stim.
2. Leave one or more custom stims in the PMC stash.
3. Exit the game and shut down the SPT server normally.
4. Restart the server without changing the configuration.
5. Load the same profile and confirm every retained custom stim is still present.
6. Confirm the stims remain usable and the traders still offer them after restart.

This persistence check is critical because custom template IDs must be registered before SPT validates stored profile items.

## Trader refresh test

1. Allow a trader refresh to occur, or use the normal SPT mechanism to trigger one.
2. Reopen Therapist, Prapor, and Skier.
3. Confirm each custom offer is still present with the correct price and loyalty requirement.
