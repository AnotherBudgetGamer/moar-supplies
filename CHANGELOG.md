# Changelog

## 1.0.0

Moar Supplies 1.0 delivers the stable, editable custom-supply workflow for SPT 4.1.x.

### Highlights

- Create vanilla-base stimulants, food, drinks, medical items, surgery kits, bleeding treatments, splints, and balms from the workshop or JSON.
- Configure trader availability, generated flea offers, flea-only items, hideout crafts, and world-loot weighting.
- Add flea-only items with a custom base price and an optional price multiplier; flea-enabled supplies are added to the handbook under their vanilla base category.
- Keep normal blacklist and economy mods in control: Moar Supplies registers only its initial flea price and does not rewrite their settings.
- Keep stable template IDs visible in the Supply Database for blacklist and economy-mod configuration.
- Use the standardized profile-safety warning before changing an existing definition.

### Upgrade notes

- Close SPT before updating, extract the archive into the SPT installation, and restart the SPT server before playing.
- The existing `config/` folder contains custom definitions. Back it up before overwriting the mod folder.
- Do not rename, disable, or delete a definition while copies remain in a PMC or scav inventory or stash. Remove or consume those copies first, or back up the profile. If necessary, use SPT's `removeModItemsFromProfile` recovery setting as described in the README.

### Known behavior

- SPT shows enabled trader offers in flea-market search. Disabling generated flea registration prevents generated offers but does not hide an enabled trader's own offer.
- Fence stock is refreshed by SPT; a configured Fence offer is not guaranteed to persist across a refresh.
