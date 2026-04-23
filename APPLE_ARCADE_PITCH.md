# Apple Arcade Pitch — Homework Guide

**Target submission date:** within 7 days from 2026-04-23 (i.e. by ~2026-04-30).
**GTM backstop:** 2026-06-30 dual-launch Apple + Steam if Arcade silent after ~45–60 days.

The Arcade submission form takes text + a TestFlight link + a video link + screenshots. Everything below is what has to exist before you hit submit.

---

## Phase 0 — Prerequisites (30 min, do first)

- [x] Apple Developer membership paid & active
- [x] App Store Connect app record exists for bundle `com.lukaskorba.loopfall` (iOS). If not: App Store Connect → My Apps → `+` → New App → iOS + macOS, SKU = `loopfall`, primary language = English
- [ ] `v1.0.0` set in Unity → Project Settings → Player → iOS → Identification (Version + Build = 1)
- [x] App icon 1024×1024 PNG ready (no transparency, no rounded corners — Apple adds them)
- [ ] Confirm game contains NO IAP and NO ads anywhere in code (`PlatformManager`, `GameCenterManager`, etc.) — Arcade rejects on sight otherwise
- [ ] Export compliance: game uses only Apple's built-in TLS → in Info.plist set `ITSAppUsesNonExemptEncryption = NO`

---

## Phase 1 — TestFlight build (half-day, blocks submission)

The Arcade team plays it via TestFlight. Public link required.

1. **Archive in Xcode**
   - Unity → Build Settings → iOS → Build (generates Xcode project)
   - In Xcode: scheme = Release, Product → Archive
   - Organizer window → Distribute App → App Store Connect → Upload
2. **Wait for processing** — usually 15–60 min, sometimes up to 4 hours. You'll get an email when processed.
3. **Fill required App Store metadata** (TestFlight external testing won't open without this):
   - App description (short — can reuse pitch paragraph)
   - Keywords
   - Support URL (`https://loopfall.com/support` — already exists in `docs/support.html`)
   - Privacy URL (`https://loopfall.com/privacy` — already exists in `docs/privacy.html`)
   - App category: Games → Action or Arcade
   - Age rating questionnaire (fill honestly, likely 4+)
   - Privacy "Nutrition" label: Game Center data only → minimal disclosure
4. **Enable TestFlight**
   - App Store Connect → TestFlight tab
   - Internal testing group: add yourself → install via TestFlight app → smoke test 5 min on device
   - External testing group: create "Apple Arcade Reviewers" group → add the build → **enable public link** (toggle at the top of the group page)
   - Copy the `https://testflight.apple.com/join/XXXX` link
5. **Verify the public link works** — open it in a private browser window, should show Join screen

**Gotchas:**
- v1.0 already has Beta App Review approval → build-number-only bumps are available within ~1h, no 24–48h re-review. TestFlight is NOT the longest pole anymore. Longest pole is now Phase 2 (video).
- Only add significant new content/changes if you're willing to trigger re-review. For pitch purposes, ship the current approved build with a build bump.
- If crash reports fire on launch, you burn 2–3 days fixing + re-uploading. Test locally on a physical iPhone before archiving.

---

## Phase 2 — Gameplay video (half-day, can parallel Phase 1)

60–90 seconds. Shows them in order: *what it is → how it plays → what's unique*.

**Shot list (target ~75s):**
1. Title screen / logo reveal (2s)
2. First run in Gates to Hell — show the nudge mechanic, a few gate passes, the swing voice firing on a good streak (20s)
3. Death + seamless restart — prove "one more run" (5s)
4. Open mode picker → Path to Redemption (Blitz) — highlight two-mode scope (3s)
5. Blitz run — beams firing, orb pickups, a cannon upgrade with voice line, shield deploy (25s)
6. Leaderboard screen (Game Center) — social proof shot (5s)
7. End card: "LOOPFALL — iOS · macOS · 15 languages" (5s)

**Recording:**
- Physical iPhone (not simulator — Arcade team can tell)
- Settings → Control Center → add Screen Recording → swipe down from top-right to record
- Record with game audio ON — SFX + voice lines are a feature
- Record 3–4 takes of each segment; pick best

**Editing:**
- iMovie or Final Cut is fine; no fancy motion graphics needed
- Keep your game audio under everything; optional ambient music bed at low volume
- Export 1080p 60fps H.264

**Upload:**
- YouTube unlisted OR Vimeo (private-with-link). YouTube is easier for Apple reviewers.
- Title: `LOOPFALL — Apple Arcade Submission Gameplay`
- Copy link, verify it plays in an incognito window

---

## Phase 3 — Screenshots (1 hour, parallelizable)

Apple Arcade form wants 3–5 screenshots. Also needed for App Store Connect listing.

- [ ] Capture on iPhone 15 Pro Max or iPhone 15 Pro (6.7" and 6.1" required sizes)
- [ ] Shots to take:
   1. Gates to Hell mid-run, ball with trail, gates in distance (signature shot)
   2. Blitz combat — beams firing, orbs on screen
   3. Cannon upgrade moment with "FULL POWER" HUD text
   4. Leaderboard screen
   5. Mode select screen — shows two-mode scope
- [ ] Crop/export via Screenshots app → Export → PNG

---

## Phase 4 — Revised pitch text (separate draft)

Keep the 2014 origin paragraph. Lead with two-mode scope. Move localization to *shipped*. Explicit "no IAP, no ads — Arcade-ready." Full draft lives in `APPLE_ARCADE_PITCH_TEXT.md` (next artifact — ask me to write it when you're ready).

---

## Phase 5 — Submit the form

1. Go to https://developer.apple.com/apple-arcade/ → *Submit your game*
2. Fill:
   - Game name, studio name, contact email
   - Pitch text (paste from draft)
   - TestFlight link (Phase 1.5)
   - Video link (Phase 2)
   - Screenshots (Phase 3)
   - Launch status: "Unreleased" (critical — Arcade does not accept already-released iOS games from solo devs)
   - Platforms: iOS, iPadOS, tvOS, macOS
   - Genres: Arcade, Action
3. Submit → log the submission date → set calendar reminders:
   - **+30 days** (2026-05-30): soft check, no action if silent
   - **+45 days** (2026-06-14): start preparing Steam page + finalizing dual-launch build
   - **+60 days** (2026-06-29): ship dual-launch on 2026-06-30 regardless of response

---

## Phase 6 — Post-submission polish (parallel, 2 months)

The Apple launch checklist continues regardless of Arcade response. Prioritize by risk:

**Must-have for Apple launch:**
- [ ] Audio interruption handling (phone calls, Siri, other apps)
- [ ] Background/foreground lifecycle (pause state, save on backgrounding)
- [ ] v1/v2 voice line variants (already prepping)
- [ ] iCloud sync end-to-end test (two devices, same Apple ID)
- [ ] TestFlight cohort of 10–20 external testers — get early signal
- [ ] Thermal throttling pass on older device (iPhone 12 or earlier)
- [ ] Notarization + hardened runtime for macOS build
- [ ] Privacy nutrition label filled accurately

**Nice-to-have:**
- [ ] Low Power Mode detection → reduce particles, fps cap to 30
- [ ] Color-blind theme variants
- [ ] Haptics toggle in settings

**Steam prep (start after +45 day mark if no Arcade response):**
- [ ] Steam page assets (capsule, header, screenshots, trailer re-cut from Phase 2)
- [ ] Steamworks integration verification
- [ ] Steam achievements via fastlane pipeline
- [ ] Wishlist push 14 days before launch

---

## Critical dependencies (visual)

```
Phase 0 Prereqs ──┐
                  ├──> Phase 1 TestFlight ──┐
                  ├──> Phase 2 Video        ├──> Phase 5 Submit Form ──> Phase 6 Polish
                  ├──> Phase 3 Screenshots  │
                  └──> Phase 4 Pitch text ──┘
```

Phases 1–4 are parallelizable after 0. With v1.0 already Beta-Review-approved, TestFlight is a same-day turnaround on a build-number bump. **Longest pole is now Phase 2 (gameplay video capture + edit)** — start recording takes as soon as the new build is on your device.
