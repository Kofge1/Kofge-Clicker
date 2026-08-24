(() => {
  const VERSION = 'v0.19.5';
  const RELEASE_URL = 'https://github.com/Kofge1/Kofge-Clicker/releases/latest';
  const isRu = document.documentElement.lang === 'ru';

  const initFavicon = () => {
    const iconHref = `${window.location.origin}/Kofge-Clicker/assets/kofge-clicker-icon.png`;

    if (!document.querySelector('link[rel="icon"]')) {
      const icon = document.createElement('link');
      icon.rel = 'icon';
      icon.type = 'image/png';
      icon.href = iconHref;
      document.head.appendChild(icon);
    }

    if (!document.querySelector('link[rel="apple-touch-icon"]')) {
      const apple = document.createElement('link');
      apple.rel = 'apple-touch-icon';
      apple.href = iconHref;
      document.head.appendChild(apple);
    }
  };

  const initSitePolishStyles = () => {
    if (document.getElementById('kofge-site-polish')) return;

    const style = document.createElement('style');
    style.id = 'kofge-site-polish';
    style.textContent = `
      .release-meta {
        width: fit-content;
        max-width: 100%;
        margin: 17px auto 0;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-wrap: wrap;
        gap: 7px 12px;
        color: var(--muted);
        font-size: 13px;
      }
      .release-meta > span:not(.release-live)::before {
        content: "·";
        margin-right: 12px;
        color: #5f6979;
      }
      .release-meta a {
        color: #cfe1ff;
        text-decoration: none;
        font-weight: 700;
      }
      .release-meta a:hover { color: var(--text); }
      .release-live {
        display: inline-flex;
        align-items: center;
        gap: 7px;
        color: #dce5f4;
        font-weight: 750;
      }
      .release-dot {
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background: var(--accent-2);
        box-shadow: 0 0 12px rgba(134, 239, 172, .55);
      }

      .gallery-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 14px;
      }
      .gallery-card {
        min-width: 0;
        overflow: hidden;
        border: 1px solid var(--line);
        border-radius: 18px;
        background: linear-gradient(180deg, rgba(255,255,255,.045), rgba(255,255,255,.02));
        color: var(--text);
        text-decoration: none;
        box-shadow: 0 16px 46px rgba(0,0,0,.16);
        transition: transform .18s ease, border-color .18s ease, box-shadow .18s ease;
      }
      .gallery-card:hover {
        transform: translateY(-3px);
        border-color: rgba(255,255,255,.18);
        box-shadow: 0 22px 52px rgba(0,0,0,.24);
      }
      .gallery-card img {
        width: 100%;
        aspect-ratio: 1100 / 635;
        object-fit: cover;
        border-bottom: 1px solid var(--line);
        transition: transform .22s ease;
      }
      .gallery-card:hover img { transform: scale(1.012); }
      .gallery-card-copy {
        display: grid;
        gap: 3px;
        padding: 14px 16px 16px;
      }
      .gallery-card-copy strong { font-size: 15px; }
      .gallery-card-copy small {
        color: var(--muted);
        font-size: 13px;
        line-height: 1.45;
      }

      .mobile-download-bar { display: none; }

      @media (max-width: 680px) {
        .hero-shot {
          aspect-ratio: 1.78 / 1;
          overflow: hidden;
        }
        .hero-shot img {
          width: 100%;
          height: 100%;
          max-width: none;
          object-fit: cover;
          object-position: center center;
        }

        .release-meta {
          margin-top: 15px;
          gap: 6px 9px;
          font-size: 12px;
        }
        .release-meta > span:not(.release-live)::before { margin-right: 9px; }
        .release-meta a {
          flex-basis: 100%;
          margin-top: 2px;
        }

        .gallery { overflow: hidden; }
        .gallery-grid {
          display: flex;
          gap: 12px;
          width: auto;
          margin-inline: -14px;
          padding: 2px 14px 12px;
          overflow-x: auto;
          scroll-snap-type: x mandatory;
          scroll-padding-inline: 14px;
          overscroll-behavior-inline: contain;
          -webkit-overflow-scrolling: touch;
          scrollbar-width: none;
        }
        .gallery-grid::-webkit-scrollbar { display: none; }
        .gallery-card {
          flex: 0 0 min(84vw, 360px);
          scroll-snap-align: start;
          border-radius: 16px;
        }
        .gallery-card:hover { transform: none; }
        .gallery-card-copy { padding: 12px 14px 14px; }

        .mobile-download-bar {
          position: fixed;
          left: 12px;
          right: 12px;
          bottom: calc(10px + env(safe-area-inset-bottom, 0px));
          z-index: 85;
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 10px;
          padding: 9px 9px 9px 13px;
          border: 1px solid rgba(255,255,255,.12);
          border-radius: 17px;
          background: rgba(17, 21, 28, .94);
          box-shadow: 0 18px 54px rgba(0,0,0,.46);
          backdrop-filter: blur(18px);
          transform: translateY(calc(100% + 34px));
          opacity: 0;
          pointer-events: none;
          transition: transform .22s ease, opacity .18s ease;
        }
        .mobile-download-bar.is-visible {
          transform: translateY(0);
          opacity: 1;
          pointer-events: auto;
        }
        .mobile-download-copy {
          min-width: 0;
          display: grid;
          line-height: 1.2;
        }
        .mobile-download-copy strong { font-size: 13px; }
        .mobile-download-copy span {
          margin-top: 3px;
          color: var(--muted);
          font-size: 11px;
          white-space: nowrap;
        }
        .mobile-download-bar .btn {
          flex: 0 0 auto;
          min-height: 42px;
          padding-inline: 15px;
          border-radius: 11px;
          font-size: 13px;
        }
      }

      @media (max-width: 360px) {
        .mobile-download-copy strong { display: none; }
        .mobile-download-copy span { margin-top: 0; }
        .mobile-download-bar .btn { padding-inline: 13px; }
      }

      @media (prefers-reduced-motion: reduce) {
        .gallery-card,
        .gallery-card img,
        .mobile-download-bar { transition: none; }
      }
    `;
    document.head.appendChild(style);
  };

  const initReleaseMeta = () => {
    const hero = document.querySelector('.hero');
    const actions = hero?.querySelector('.actions');
    const trust = hero?.querySelector('.trust-row');
    if (!hero || !actions || !trust || hero.querySelector('.release-meta')) return;

    const meta = document.createElement('div');
    meta.className = 'release-meta';
    meta.setAttribute('aria-label', isRu ? 'Информация о последнем релизе' : 'Latest release information');
    meta.innerHTML = isRu
      ? `<span class="release-live"><span class="release-dot" aria-hidden="true"></span>Последняя версия ${VERSION}</span><span>Windows x64</span><span>Single-file EXE</span><a href="${RELEASE_URL}">Что нового →</a>`
      : `<span class="release-live"><span class="release-dot" aria-hidden="true"></span>Latest ${VERSION}</span><span>Windows x64</span><span>Single-file EXE</span><a href="${RELEASE_URL}">What's new →</a>`;
    actions.insertAdjacentElement('afterend', meta);
  };

  const initGallery = () => {
    const gallery = document.querySelector('.gallery');
    const container = gallery?.querySelector('.container');
    if (!gallery || !container || container.querySelector('.gallery-grid')) return;

    const details = Array.from(container.children).filter((node) => node.tagName === 'DETAILS');
    if (!details.length) return;

    const head = container.querySelector('.section-head');
    const heading = head?.querySelector('h2');
    const intro = head?.querySelector('p');
    if (heading) heading.textContent = isRu ? 'Посмотрите Kofge-Clicker в работе' : 'See Kofge-Clicker in action';
    if (intro) {
      intro.textContent = isRu
        ? 'На компьютере — компактная сетка превью. На телефоне карточки можно листать свайпом. Нажмите на скриншот, чтобы открыть его полностью.'
        : 'Desktop gets a compact preview grid. On mobile, swipe through the cards. Tap any screenshot to open the full image.';
    }

    const descriptions = isRu
      ? {
          'Click Patterns': 'Настройка поведения и таймингов кликов.',
          'Hotkeys': 'Клавиатура, кнопки мыши и сочетания клавиш.',
          'Profiles': 'Сохранение и быстрое переключение конфигураций.',
          'Window Targeting & Options': 'Привязка поведения к выбранному окну приложения.'
        }
      : {
          'Click Patterns': 'Tune click timing and behavior.',
          'Hotkeys': 'Keyboard, mouse buttons and modifier combinations.',
          'Profiles': 'Save and switch reusable configurations.',
          'Window Targeting & Options': 'Bind behavior to a selected application window.'
        };

    const grid = document.createElement('div');
    grid.className = 'gallery-grid';

    details.forEach((detail) => {
      const title = detail.querySelector('summary')?.textContent?.trim() || 'Kofge-Clicker';
      const source = detail.querySelector('img');
      if (!source) return;

      const card = document.createElement('a');
      card.className = 'gallery-card';
      card.href = source.currentSrc || source.src;
      card.target = '_blank';
      card.rel = 'noopener';

      const image = source.cloneNode(true);
      image.loading = 'lazy';

      const copy = document.createElement('span');
      copy.className = 'gallery-card-copy';
      copy.innerHTML = `<strong>${title}</strong><small>${descriptions[title] || ''}</small>`;

      card.append(image, copy);
      grid.appendChild(card);
    });

    details.forEach((detail) => detail.remove());
    container.appendChild(grid);
  };

  const ensureMobileDownloadBar = () => {
    if (document.querySelector('[data-mobile-download-bar]') || !document.querySelector('.hero')) return;

    const bar = document.createElement('div');
    bar.className = 'mobile-download-bar';
    bar.dataset.mobileDownloadBar = 'true';
    bar.setAttribute('aria-hidden', 'true');
    bar.innerHTML = `
      <div class="mobile-download-copy">
        <strong>Kofge-Clicker</strong>
        <span>${VERSION} · Windows x64</span>
      </div>
      <a class="btn btn-primary" href="${RELEASE_URL}">${isRu ? 'Скачать' : 'Download'}</a>
    `;
    document.body.appendChild(bar);
  };

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
    toggle.setAttribute('aria-label', isRu ? 'Открыть меню' : 'Open menu');
    toggle.innerHTML = '<span></span><span></span><span></span>';
    controls.appendChild(toggle);
    nav.appendChild(controls);

    const setOpen = (open) => {
      links.classList.toggle('is-open', open);
      toggle.classList.toggle('is-open', open);
      toggle.setAttribute('aria-expanded', String(open));
      toggle.setAttribute('aria-label', isRu
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

  const initMobileDownloadBar = () => {
    const bar = document.querySelector('[data-mobile-download-bar]');
    const hero = document.querySelector('.hero');
    if (!bar || !hero) return;

    const download = document.querySelector('#download');
    const footer = document.querySelector('footer');
    const media = window.matchMedia('(max-width: 680px)');
    let ticking = false;

    const overlapsViewport = (element, padding = 0) => {
      if (!element) return false;
      const rect = element.getBoundingClientRect();
      return rect.bottom > padding && rect.top < window.innerHeight - padding;
    };

    const sync = () => {
      ticking = false;
      const pastHero = hero.getBoundingClientRect().bottom < 96;
      const hideForDownload = overlapsViewport(download, 48);
      const hideForFooter = overlapsViewport(footer, 0);
      const visible = media.matches && pastHero && !hideForDownload && !hideForFooter;
      bar.classList.toggle('is-visible', visible);
      bar.setAttribute('aria-hidden', String(!visible));
    };

    const requestSync = () => {
      if (ticking) return;
      ticking = true;
      window.requestAnimationFrame(sync);
    };

    window.addEventListener('scroll', requestSync, { passive: true });
    window.addEventListener('resize', requestSync, { passive: true });
    media.addEventListener?.('change', requestSync);
    sync();
  };

  const initSiteExperience = () => {
    initFavicon();
    initSitePolishStyles();
    initReleaseMeta();
    initGallery();
    ensureMobileDownloadBar();
    initMobileNav();
    initMobileDownloadBar();
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initSiteExperience, { once: true });
  } else {
    initSiteExperience();
  }
})();
