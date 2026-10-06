# Moar Supplies Release Checklist

Use this checklist for every Forge release.  Items marked **release blocker** are required for a publishable Forge submission or are necessary to prevent avoidable profile loss.

## 1. Version and metadata

- [x] **Release blocker:** Choose the release version using semantic versioning (`0.9.0`).
- [x] **Release blocker:** Set that exact version in both `MoarSupplies.csproj` (`<Version>`) and `Mod.cs` (`Version`).
- [x] **Release blocker:** Confirm `ModGuid` is exactly `com.anotherbudgetgamer.moarsupplies`; enter that same GUID for the Forge submission.
- [x] **Release blocker:** Set SptVersion to ~4.1.2, covering SPT versions >= 4.1.2 and < 4.2.0 (the tested 4.1.x line).
- [ ] **Release blocker:** Select the matching 4.1.x compatibility range on the Forge upload form.
- [x] **Release blocker:** Set `Mod.cs` `Url` to the public source repository before publishing; Forge requires source links for SPT 4.x C# server mods.
- [ ] Confirm the submitted display name and author comply with the current Forge naming requirements.
- [ ] Record dependencies and incompatibilities explicitly on the Forge version page. If there are none, state “No dependencies” and “No known incompatibilities.”
- [ ] Enable Forge's **Contains AI Content** flag and make sure the author has reviewed, understands, and can maintain all submitted code.

## 2. Build the distributable

- [ ] Start from the intended commit and verify `git status`; do not accidentally ship unrelated local changes.
- [x] Run `dotnet build -c Release` against the intended SPT runtime.
- [x] **Release blocker:** Confirm the build has zero errors and warnings.
- [ ] **Release blocker:** Inspect `ReleaseZip/AnotherBudgetGamer-MoarSupplies-0.9.0.zip` from the committed release candidate.
- [x] **Release blocker:** Confirm the archive root is exactly:

  ```text
  SPT_Runtime/user/mods/AnotherBudgetGamer-MoarSupplies/
  ```

- [x] **Release blocker:** Confirm it contains the compiled `MoarSupplies.dll`, `config/`, `wwwroot/`, and both static-web-assets manifest files.
- [x] Confirm the archive contains no source, `bin/`, `obj/`, development scripts, private logs, profiles, or local runtime assemblies.
- [ ] Optionally compute and record the archive SHA-256 for release notes and support triage.

## 3. Test exactly what users receive

- [ ] **Release blocker:** Extract the release ZIP with 7-Zip into a fresh SPT installation that has reached the main menu once.
- [ ] **Release blocker:** Start the server with no other mods installed; it must load without errors or unexpected warnings.
- [ ] Verify the server web interface exposes **Moar Supplies** at `/moar-supplies` and all three pages load: Home, Database, and Modify.
- [ ] Create, edit, disable, and delete a test definition in the workshop; verify expected validation errors are readable and failed writes do not damage the existing configuration.
- [ ] Complete every relevant scenario in `Milestone8_TestPlan.md`: startup, trader assortment, item behavior, buff timing, persistence, and trader refresh.
- [ ] Test a normal update from the previous public version with a profile containing each shipped stim.
- [ ] Verify server logs are concise in normal operation; retain a clean successful-startup excerpt for the release record.
- [ ] Check that the mod does not modify SPT files on disk outside its own mod directory, make network connections, change the in-game SPT version watermark, or introduce noticeable startup/gameplay performance regressions.

## 4. User-facing documentation and Forge page

- [ ] **Release blocker:** Publish the exact source used for the binary at the repository URL; tag the release commit.
- [ ] **Release blocker:** Add a version-specific changelog with user-visible changes, fixes, known issues, and any upgrade action.
- [ ] **Release blocker:** Give Forge a concise description that states: configurable custom stimulants, supported traders, bundled web workshop, and tested SPT version.
- [ ] **Release blocker:** Include install steps using 7-Zip: close SPT, extract the archive to the SPT root, start the server, and open `/moar-supplies`.
- [x] **Release blocker:** Add a prominent removal warning: custom items can remain in a profile; users should consume/remove all Moar Supplies items and back up their profile before uninstalling. Profile repair is not guaranteed.
- [ ] Add an update section. State whether users can overwrite files, whether config is preserved, and whether they must restart the server. Call out any renamed/moved/deleted files or schema migration.
- [ ] Add a short configuration section linking to `README.md`, including the restart requirement after workshop/JSON changes.
- [ ] State the dependency and incompatibility status, license, support channel, and what to include in a bug report (SPT version, mod version, relevant stim JSON, and server log excerpt).
- [ ] Upload a clear cover image and 3–5 current screenshots: workshop home, definition list, editor, and an in-game trader/item view. Ensure screenshots do not show personal information.
- [ ] Credit and document permission for every non-original image, icon, code, or other redistributed asset. Do not include EFT game files.
- [ ] Use an English, descriptive title, tags/category, and accurate feature claims only.

## 5. Submit and verify

- [ ] **Release blocker:** Upload the inspected ZIP, select the correct SPT compatibility, attach every required dependency, and match the GUID/version metadata.
- [ ] Re-read the rendered Forge page as a new user; test every link and ensure no placeholder text remains.
- [ ] Download the uploaded file from Forge and compare it with the locally inspected archive (name, size, and ideally SHA-256).
- [ ] Make one final clean-install smoke test with the Forge download if practical.
- [ ] Keep the release ZIP, source tag/commit, test evidence, and changelog together for support and patch releases.

## Current 0.9.0 audit

| Area | Status | Evidence / action |
| --- | --- | --- |
| Release build | Ready | `dotnet build -c Release` succeeds with zero warnings for the current tree. Rebuild from the committed release candidate before publishing. |
| Archive layout | Pending 0.9.0 build | The local release ZIP uses `SPT_Runtime/user/mods/AnotherBudgetGamer-MoarSupplies/` and must be reinspected after the 0.9.0 release candidate is built. |
| ZIP install/removal smoke test | Pending clean-package evidence | A fresh installation of the exact 0.9.0 ZIP is required before publishing. |
| Version alignment | Ready | Project and runtime metadata both declare `0.9.0`; the `~4.1.2` range covers SPT 4.1.x and testing is on 4.1.2. |
| Install/config/restart docs | Ready | README covers install, config format, supported values, restart behavior, and troubleshooting. |
| Static-container world loot | Confirmed | Newly created custom croutons and pineapple juice were found in raid containers during 0.8.5 testing. |
| World loot | Confirmed | Multiple newly created custom food, drinks, medical supplies, and stims were found in static containers, loose-loot spawns, and player/scav inventories during one 0.8.5 raid. |
| Regression plan | Needs expansion | `Milestone8_TestPlan.md` covers stims, persistence, and trader refresh. Add drinks, food, medical packs, surgery kits, and crafting before the final clean-package run. |
| Public source URL | Ready | Runtime metadata points to `https://github.com/AnotherBudgetGamer/moar-supplies`. |
| Forge metadata format | Ready | Runtime metadata uses the Forge-safe alphanumeric values `MoarSupplies` and `AnotherBudgetGamer`. |
| Uninstall guidance | Ready | README explains safe folder removal, profile backup, item cleanup, and post-removal profile verification. |
| Forge listing assets | Needs preparation | Prepare a cover image, current product screenshots, per-version notes, tags, and support details. |
| AI disclosure | Required | Enable the Forge “Contains AI Content” flag and verify author review/ownership before upload. |
