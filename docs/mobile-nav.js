(() => {
  const initMobileNav = () => {
    const nav = document.querySelector('.site-header .nav');
    const links = nav?.querySelector('.nav-links');
    if (!nav || !links || nav.querySelector('[data-mobile-nav-toggle]')) return;

    if (!links.id) links.id = 'primary-navigation';

    const controls = document.createElement('div');
    controls.className = 'mobile-nav-controls';

    const language = links.querySelector('.lang');
    if (language) {
      const languageClone = language.cloneNode(true);
      languageClone.classList.add('mobile-lang');
      languageClone.removeAttribute('id');
      controls.appendChild(languageClone);
    }

    const toggle = document.createElement('button');
    toggle.type = 'button';
    toggle.className = 'mobile-nav-toggle';
    toggle.dataset.mobileNavToggle = 'true';
    toggle.setAttribute('aria-controls', links.id);
    toggle.setAttribute('aria-expanded', 'false');
    toggle.setAttribute('aria-label', document.documentElement.lang === 'ru' ? 'Открыть меню' : 'Open menu');
    toggle.innerHTML = '<span></span><span></span><span></span>';
    controls.appendChild(toggle);
    nav.appendChild(controls);

    const setOpen = (open) => {
      links.classList.toggle('is-open', open);
      toggle.classList.toggle('is-open', open);
      toggle.setAttribute('aria-expanded', String(open));
      toggle.setAttribute('aria-label', document.documentElement.lang === 'ru'
        ? (open ? 'Закрыть меню' : 'Открыть меню')
        : (open ? 'Close menu' : 'Open menu'));
      document.body.classList.toggle('mobile-nav-open', open);
    };

    toggle.addEventListener('click', () => setOpen(!links.classList.contains('is-open')));

    links.addEventListener('click', (event) => {
      if (event.target.closest('a')) setOpen(false);
    });

    document.addEventListener('click', (event) => {
      if (links.classList.contains('is-open') && !nav.contains(event.target)) setOpen(false);
    });

    document.addEventListener('keydown', (event) => {
      if (event.key === 'Escape') setOpen(false);
    });

    window.addEventListener('resize', () => {
      if (window.innerWidth > 900) setOpen(false);
    }, { passive: true });
  };

  const initMobileHeroCrop = () => {
    if (!document.querySelector('.hero-shot') || document.getElementById('kofge-mobile-hero-crop')) return;

    const style = document.createElement('style');
    style.id = 'kofge-mobile-hero-crop';
    style.textContent = `
      @media (max-width: 680px) {
        .hero-shot {
          aspect-ratio: 1.85 / 1;
          overflow: hidden;
        }

        .hero-shot img {
          width: 100%;
          height: 100%;
          max-width: none;
          object-fit: cover;
          object-position: center center;
        }
      }
    `;
    document.head.appendChild(style);
  };

  const initMobileExperience = () => {
    initMobileNav();
    initMobileHeroCrop();
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initMobileExperience, { once: true });
  } else {
    initMobileExperience();
  }
})();
