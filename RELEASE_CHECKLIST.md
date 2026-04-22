# Loopfall — 1.0 Release Checklist

Everything that needs to land before we cut v1.0.0. Tick items as they're done; move notes inline. Grouped by theme, not priority — work across groups in parallel.

---

## 1. Content & Copy

- [ ] **Final string freeze.** Lock all in-game text in `Assets/Scripts/L10n.cs`. After freeze, adding a new string means re-translating 7 languages, so this gate matters.
- [ ] **Tier 2 languages (after freeze).** Add Latin/Cyrillic sets that are cheap: Turkish, Polish, Dutch, Ukrainian. Same file, same keys.
- [x] **Tutorial PNG set.** Per-platform sprites for the tutorial platform image slot (`tutorialPlatformImage` in `ScoreSync.cs`). Needed: iOS tap, iPad tap, tvOS Siri Remote, keyboard, MFi gamepad. One each.
- [ ] **Voice line variants.** Record and wire the v1/v2 alternates that were designed but not delivered. Round-robin selection so the same line doesn't repeat in a session.
- [ ] **Proofread translations.** Machine-quality output from this pass; a native reviewer per language catches the obvious misfires before they hit store reviews.

## 2. Store Metadata

- [x] **App Store Connect metadata in all 7 locales.** Title, subtitle, keywords, description, what's new. Keywords especially — each locale has its own search.
- [ ] **Google Play Store listing in 7 locales.** Same fields.
- [ ] **Steam store page copy** (English only is fine at launch; add locales later).
- [ ] **Screenshots per locale** (or at minimum per-orientation English set with localized overlays if scope is tight). Apple requires 6.9" + 6.5" iPhone, 13" iPad, tvOS 4K, macOS; Google wants phone + 7"/10" tablet.
- [ ] **Feature graphic / capsule art.** Steam header + capsule + library hero + library logo. Play Store feature graphic (1024×500). App Store promotional text (170 chars).
- [ ] **Trailer.** 15–30s gameplay capture. Apple/Google/Steam each want slightly different aspect ratios.
- [ ] **Privacy policy URL.** Required by all three stores even with zero data collection. One hosted static page covers all.
- [ ] **Terms of service URL.** Not strictly required but recommended; bundle with privacy policy.
- [ ] **App rating questionnaire.** Apple Age Rating, Google content rating (IARC), Steam content survey. All three.

## 3. App Icons & Branding

- [ ] **iOS app icon.** All sizes in `Assets.xcassets` — 20/29/40/60/76/83.5/1024pt at 1x/2x/3x. Dark + tinted variants for iOS 18+ optional but nice.
- [ ] **tvOS layered app icon.** Multi-layer PSD — parallax-capable. Top Shelf image + Top Shelf wide image required.
- [ ] **macOS `.icns`.** All sizes 16–1024, with @2x variants.
- [ ] **Android adaptive icon.** Foreground + background layers. Monochrome layer for Android 13+ themed icons.
- [ ] **Steam art pack.** Header (460×215), capsule small (231×87), capsule main (616×353), capsule vertical (374×448), library hero (3840×1240), library capsule (600×900), library logo (1280×720 with transparency), community icon (184×184).
- [ ] **Notification icon (Android).** Monochrome 24dp.

## 4. Platform-Specific

### iOS
- [ ] **iPad adaptive layout.** Run on an iPad to confirm UI isn't just a stretched iPhone view. Verify safe-area on 11"/13".
- [ ] **Audio interruption handling.** Incoming call mid-run → auto-pause + audio session reactivation on return.
- [ ] **Background/foreground lifecycle.** Home button mid-run should pause state correctly; returning shouldn't re-count a "dead" run.
- [ ] **Landscape/portrait lock.** Decide and enforce in Info.plist.
- [ ] **CoreHaptics polish.** Confirm gate-hit / death haptics fire on supported devices (iPhone 8+).
- [ ] **Dynamic Island / notch handling.** Already have safe-area HUD insets — verify on iPhone 15/16 Pro.
- [ ] **Low Power Mode detection.** Optionally drop post-processing / chromatic aberration intensity when enabled.

### tvOS
- [ ] **Focus navigation support.** Currently lacking. Siri Remote swipe must be able to move through title mode picker, settings rows, stats, etc. Each Button needs `Selectable.navigation` wired and a visible focus ring.
- [x] **Siri Remote tutorial variant.** Tutorial PNG + copy that makes sense without touch.
- [ ] **Top Shelf image + section** (tvOS app store surface).
- [ ] **MFi controller as primary input option.**
- [ ] **No keyboard assumption.** Audit any text the game shows that implies keyboard (already handled by `GetTapPrompt()` etc., but re-verify).

### macOS
- [ ] **Window resize / aspect handling.** Game must not break at extreme aspect ratios.
- [ ] **Keyboard + mouse support confirmation.**
- [ ] **Fullscreen toggle.** Already in Settings — verify on a real Retina display.
- [ ] **App notarization + hardened runtime** for Gatekeeper.

### Android
- [ ] **Adaptive icon + themed icon.**
- [ ] **Google Play Billing** (only if any IAP ships — currently no).
- [ ] **Back button behavior.** Should pause / back to title, not quit silently.
- [ ] **Multiple screen density pass.** Test on ldpi → xxxhdpi.
- [ ] **64-bit-only build confirmation** (required by Play Store).

### Steam
- [ ] **Steamworks SDK integration.** Still missing. Wire via `#if !DISABLESTEAMWORKS` guards already present.
- [ ] **Steam App ID registered + `steam_appid.txt` committed** (the file already exists at repo root — verify its ID matches the registered app).
- [ ] **Steam Input manifest.** Map controller actions so Steam Deck + generic controllers work out of the box.
- [ ] **Steam Cloud** for PlayerPrefs-equivalent data. Versioned save schema.
- [ ] **Steam Rich Presence.** "In the Tunnel — 1,243 gates" style status.
- [ ] **Steam Achievements.** Mirror of Apple/Google achievement set.
- [ ] **Steam Leaderboards.** Mirror of Apple/Google leaderboard set.
- [ ] **Steam Deck Verified pass.** Controller-first UI, 1280×800 layout, no on-screen text reads below ~24pt, all bindings discoverable.
- [ ] **Screenshot / recording hotkey support.** Default Steam bindings should Just Work — verify no weird input capture.

## 5. Services & Data

- [ ] **Achievement list finalization.** Walk the current set, kill dead ones, add Blitz-specific ones. Make sure IDs match across Apple Game Center / Google Play Games / Steam.
- [ ] **Leaderboard list finalization.** Per mode (Gates to Hell, Path to Redemption). Per-mode stats already flow through — confirm IDs and display names translated where the platform supports it.
- [x] **iCloud sync (iOS/macOS/tvOS).** NSUbiquitousKeyValueStore syncs best-score lists (per mode) and lifetime counters (taps, runs, blitz obstacles). Settings stay device-local.
- [ ] **Google Play Games save sync (Android).**
- [ ] **Steam Cloud save sync (Steam).**
- [ ] **Save schema versioning.** Add a `SaveVersion` PlayerPrefs key now so 1.0.x → 1.1.x migrations are clean.
- [ ] **Crash / error reporting.** Even a minimal opt-in (Sentry or similar) — zero telemetry means zero signal when the post-launch queue of 1-star "crashes on boot" reviews comes in.

## 6. Accessibility & Settings

- [ ] **Reduce-motion toggle.** Chromatic aberration + camera swing is the game identity, but offer an escape hatch. Settings → Preferences. Keep default ON.
- [ ] **Reduce-flash toggle** (optional). For photosensitive players — dampen the bright death flashes.
- [ ] **Color-blind-aware theme option.** Or at least verify existing themes pass a quick CB simulator pass — gates, trail, and track must stay distinguishable under protan/deutan/tritan.
- [ ] **Haptics on/off toggle** (mobile).
- [ ] **Subtitle / caption support** for voice lines? Probably skip at 1.0, but flag for 1.1.

## 7. Quality & Performance

- [ ] **TestFlight beta cohort.** 5–10 real-device testers for ~1 week before cutting 1.0. Collect feedback in one place.
- [ ] **Google Play internal testing track** cohort.
- [ ] **Steam Playtest** or closed beta branch.
- [ ] **Thermal / battery pass on iPhone.** 30-minute session on an iPhone 12-ish device — if it's sauna-hot, cap frame rate or drop effects under thermal pressure.
- [ ] **Cold-start time under 3s** on oldest supported devices.
- [ ] **Memory ceiling check.** No leaks across mode swaps (Gates ↔ Blitz many times).
- [ ] **Frame pacing / stutter check.** Especially on Android mid-tier and on battery-saver modes.

## 8. Legal & Compliance

- [ ] **Export compliance.** Apple requires a yes/no on encryption usage (Info.plist `ITSAppUsesNonExemptEncryption`).
- [ ] **ATT (App Tracking Transparency).** If we add any analytics that tracks, needs `NSUserTrackingUsageDescription` and a prompt. If we ship zero tracking, explicitly set to `false`.
- [ ] **Google Data Safety declaration.**
- [ ] **Apple App Privacy "nutrition label"** on App Store Connect.
- [ ] **Third-party licenses.** Already have `Assets/StreamingAssets/LICENSES.txt` with Phosphor Icons. Add entries for any additional libraries bundled before 1.0 (Noto CJK font if that lands, Steamworks.NET, etc.).
- [ ] **Age gates / content rating matches regional requirements** (PEGI, ESRB, CERO, USK).

## 9. Marketing & Launch

- [ ] **Press kit.** One downloadable zip with logo, screenshots, a 30s trailer, fact sheet.
- [ ] **Launch trailer.**
- [ ] **Social accounts set up** (at minimum one — X / Bluesky / TikTok) for launch week posts.
- [ ] **Release date alignment across stores.** Apple + Google + Steam pre-order / wishlist windows synced.
- [ ] **Changelog template** for in-app "what's new" post-launch.

---

## Deferred (post-1.0 explicitly)

- Tier 1 CJK languages (Noto Sans CJK font integration, ~8MB binary add after subsetting).
- Arabic / RTL support (HarfBuzz shaping + mirrored UI).
- In-app purchases / tip jar.
- Analytics beyond crash reporting.
- Subtitles for voice lines.
