// Loopfall — site interactions. Vanilla JS, no deps, no build step.

(() => {
  'use strict';

  // ---------- Scroll-reveal via IntersectionObserver ----------
  const revealTargets = document.querySelectorAll('.reveal');
  if (revealTargets.length && 'IntersectionObserver' in window) {
    const io = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-visible');
          io.unobserve(entry.target);
        }
      });
    }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });
    revealTargets.forEach((el) => io.observe(el));
  } else {
    // Fallback: just show everything.
    revealTargets.forEach((el) => el.classList.add('is-visible'));
  }

  // ---------- Lightbox for screenshots ----------
  const lightbox = document.querySelector('.lightbox');
  if (lightbox) {
    const lightboxImg = lightbox.querySelector('img');
    const closeBtn = lightbox.querySelector('.lightbox__close');

    const open = (src, alt) => {
      lightboxImg.src = src;
      lightboxImg.alt = alt || '';
      lightbox.classList.add('is-open');
      document.body.style.overflow = 'hidden';
    };
    const close = () => {
      lightbox.classList.remove('is-open');
      document.body.style.overflow = '';
      // Delay clearing src until fade-out is done so the image doesn't blink away.
      setTimeout(() => { lightboxImg.src = ''; }, 240);
    };

    document.querySelectorAll('.shot').forEach((shot) => {
      shot.addEventListener('click', () => {
        const img = shot.querySelector('img');
        if (img && img.getAttribute('src')) open(img.src, img.alt);
      });
    });

    closeBtn.addEventListener('click', close);
    lightbox.addEventListener('click', (e) => { if (e.target === lightbox) close(); });
    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && lightbox.classList.contains('is-open')) close();
    });
  }

  // ---------- Copy-to-clipboard for press descriptions ----------
  document.querySelectorAll('.copy-btn').forEach((btn) => {
    btn.addEventListener('click', async () => {
      const targetId = btn.getAttribute('data-copy-target');
      const el = targetId ? document.getElementById(targetId) : null;
      if (!el) return;
      try {
        await navigator.clipboard.writeText(el.textContent.trim());
        const original = btn.textContent;
        btn.textContent = 'Copied ✓';
        btn.classList.add('is-copied');
        setTimeout(() => {
          btn.textContent = original;
          btn.classList.remove('is-copied');
        }, 1600);
      } catch {
        // Clipboard API can fail on http://. Fall back to selection.
        const range = document.createRange();
        range.selectNodeContents(el);
        const sel = window.getSelection();
        sel.removeAllRanges();
        sel.addRange(range);
      }
    });
  });

  // ---------- Dynamic year in footer ----------
  const yearEl = document.getElementById('js-year');
  if (yearEl) yearEl.textContent = new Date().getFullYear();
})();
