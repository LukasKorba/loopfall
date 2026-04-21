# Loopfall — marketing site

The public-facing website for Loopfall, deployed as a static site from this `/docs/` folder via GitHub Pages.

No frameworks, no build step, no npm. Just HTML + CSS + vanilla JS.

## Structure

```
docs/
├── index.html        Main promo page (hero, trailer, features, gallery, press kit)
├── privacy.html      Privacy policy
├── support.html      Support & FAQ
├── styles.css        All styles (shared across pages)
├── script.js         All interactions (scroll-reveal, lightbox, copy-to-clipboard)
├── README.md         This file
└── assets/
    ├── screenshots/  Drop gameplay screenshots here
    ├── icons/        App icon, favicon, Open Graph image
    └── press/        Press-kit zips (icon pack, screenshot pack, logos)
```

## Deploy to GitHub Pages

1. In the repository on GitHub: **Settings → Pages**.
2. Under "Build and deployment", set **Source** to `Deploy from a branch`.
3. Set **Branch** to `main` and the folder to `/docs`.
4. Save. Your site will be live at `https://<username>.github.io/<repo>/` within a minute.

### Custom domain (optional)

1. In your DNS provider, create either:
   - an `A` record pointing your apex domain to GitHub Pages IPs:
     `185.199.108.153`, `185.199.109.153`, `185.199.110.153`, `185.199.111.153`
   - or a `CNAME` record pointing a subdomain (e.g. `www`) to `<username>.github.io`.
2. In **Settings → Pages**, enter your domain under "Custom domain" and save.
3. GitHub will create a `CNAME` file inside `/docs/` for you.
4. Tick **Enforce HTTPS** once the cert provisions (usually under an hour).

## Placeholders to replace

Everything below is pre-filled with placeholder content. Search & replace, or edit the files directly.

### Assets to drop in

- [ ] `assets/icons/favicon.png` — 32×32 or 64×64 PNG
- [ ] `assets/icons/apple-touch-icon.png` — 180×180 PNG
- [ ] `assets/icons/og-image.png` — 1200×630 social share image
- [ ] `assets/screenshots/screenshot-1.png` through `screenshot-6.png` — 16:9 gameplay shots
- [ ] `assets/press/loopfall-icon.zip` — app icon bundle for press
- [ ] `assets/press/loopfall-screenshots.zip` — full screenshot pack
- [ ] `assets/press/loopfall-logo.zip` — logo / wordmark in SVG + PNG

### URLs to swap

- [ ] **App Store URL** — `index.html`, both App Store badges (`href="#"` → live listing). Search for `aria-label="Download on the App Store"`.
- [ ] **Steam URL** — `index.html`, "Available on Steam" button (`href="#"`). Search for `aria-label="Available on Steam"`.
- [ ] **YouTube video ID** — `index.html`, trailer iframe. Search for `VIDEO_ID` and replace.
- [ ] **Social links** — `index.html` footer: Twitter/X, Instagram, YouTube, Discord. All currently `href="#"`.

### Text / contact to review

- [ ] **Email addresses** — currently `press@loopfall.game`, `support@loopfall.game`, `hello@loopfall.game`, `privacy@loopfall.game`. Swap in the real ones across all three HTML files.
- [ ] **Developer name / location** — `index.html` press kit ("Lukas Korba / Prague"). Edit if needed.
- [ ] **Privacy policy effective date** — `privacy.html`, top of the page.
- [ ] **Version number** — `support.html` under "Current version". Bump with each release.
- [ ] **Release date** — `index.html` press kit fact sheet currently says "TBA".

## How to update content

- **Copy and typography** — edit the HTML files directly. Shared text (nav, footer) is duplicated across pages; remember to update all three.
- **Colors** — change the CSS custom properties at the top of `styles.css` under `:root`. The whole site repaints.
- **Add a feature card** — duplicate one of the `<article class="feature">` blocks in `index.html`.
- **Add an FAQ** — duplicate a `<details class="faq__item">` block in `support.html`.
- **Add a screenshot** — drop the file in `assets/screenshots/` and add another `<figure class="shot">` block in `index.html`. The placeholder auto-hides when an image loads.

## Local preview

Any static server will do. A couple of quick options:

```bash
# Python (built-in, no install)
cd docs && python3 -m http.server 8000
# then open http://localhost:8000

# Node (if you have npx)
npx serve docs
```

## Notes

- `.gitkeep` files exist in empty asset folders so git tracks them. Delete as you add real content.
- Images use `loading="lazy"` so off-screen screenshots don't block initial paint.
- The trailer uses `youtube-nocookie.com` so embedding doesn't drop tracking cookies before a user plays the video.
- Animations respect `prefers-reduced-motion` — everything still works, it just stops moving.
- The chromatic-aberration title effect in the hero is pure CSS text-shadow (no images). Scales to any viewport.
