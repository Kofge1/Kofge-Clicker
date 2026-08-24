(() => {
  const FALLBACK_VERSION = 'v0.19.5';
  const RELEASE_URL = 'https://github.com/Kofge1/Kofge-Clicker/releases/latest';
  const RELEASE_API = 'https://api.github.com/repos/Kofge1/Kofge-Clicker/releases/latest';
  const RELEASE_CACHE_KEY = 'kofge-latest-release-v1';
  const RELEASE_CACHE_TTL = 30 * 60 * 1000;
  const isRu = document.documentElement.lang === 'ru';
  const scriptBase = (() => {
    try {
      return new URL('./', document.currentScript?.src || `${window.location.origin}/Kofge-Clicker/mobile-nav.js`);
    } catch {
      return new URL('/Kofge-Clicker/', window.location.origin);
    }
  })();

  const initEnhancementStyles = () => {
    if (document.querySelector('link[data-kofge-site-polish]')) return;
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.dataset.kofgeSitePolish = 'true';
    link.href = new URL('site-polish.css?v=20260824-polish2', scriptBase).href;
    document.head.appendChild(link);
  };

  const initFavicon = () => {
    const iconHref = new URL('assets/kofge-clicker-icon.png', scriptBase).href;

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

  const initReleaseMeta = () => {
    const hero = document.querySelector('.hero');
    const actions = hero?.querySelector('.actions');
    const trust = hero?.querySelector('.trust-row');
    if (!hero || !actions || !trust || hero.querySelector('.release-meta')) return;

    const meta = document.createElement('div');
    meta.className = 'release-meta';
    meta.setAttribute('aria-label', isRu ? 'Информация о последнем релизе' : 'Latest release information');
    meta.innerHTML = isRu
      ? `<span class="release-live"><span class="release-dot" aria-hidden="true"></span>Последняя версия <span data-release-version>${FALLBACK_VERSION}</span></span><span>Windows x64</span><span>Single-file EXE</span><a data-release-notes href="${RELEASE_URL}">Что нового →</a>`
      : `<span class="release-live"><span class="release-dot" aria-hidden="true"></span>Latest <span data-release-version>${FALLBACK_VERSION}</span></span><span>Windows x64</span><span>Single-file EXE</span><a data-release-notes href="${RELEASE_URL}">What's new →</a>`;
    actions.insertAdjacentElement('afterend', meta);
  };

  const initQuickStart = () => {
    const download = document.querySelector('#download');
    if (!download || document.querySelector('.quick-start-section')) return;

    const section = document.createElement('section');
    section.className = 'quick-start-section';
    section.innerHTML = isRu
      ? `<div class="container">
          <div class="section-head center">
            <div class="section-kicker">Быстрый старт</div>
            <h2>От загрузки до первого клика — три шага</h2>
            <p>Без установщика, регистрации и обязательной первоначальной настройки.</p>
          </div>
          <div class="quick-start-grid">
            <article class="quick-start-card"><span class="quick-start-number">01</span><h3>Скачайте .exe</h3><p>Возьмите последний официальный релиз с GitHub и запустите файл.</p></article>
            <article class="quick-start-card"><span class="quick-start-number">02</span><h3>Выберите CPS и хоткей</h3><p>Настройте скорость, режим и удобную клавишу или кнопку мыши.</p></article>
            <article class="quick-start-card"><span class="quick-start-number">03</span><h3>Запускайте</h3><p>Используйте Toggle или Hold и при необходимости сохраните настройку в профиль.</p></article>
          </div>
        </div>`
      : `<div class="container">
          <div class="section-head center">
            <div class="section-kicker">Quick start</div>
            <h2>From download to your first click in three steps</h2>
            <p>No installer, account or mandatory setup wizard.</p>
          </div>
          <div class="quick-start-grid">
            <article class="quick-start-card"><span class="quick-start-number">01</span><h3>Download the .exe</h3><p>Get the latest official GitHub release and run the file.</p></article>
            <article class="quick-start-card"><span class="quick-start-number">02</span><h3>Choose CPS and a hotkey</h3><p>Set the speed, mode and the keyboard or mouse button you want to use.</p></article>
            <article class="quick-start-card"><span class="quick-start-number">03</span><h3>Start clicking</h3><p>Use Toggle or Hold and save the setup as a profile when you want to reuse it.</p></article>
          </div>
        </div>`;
    download.insertAdjacentElement('beforebegin', section);
  };

  const initDownloadCard = () => {
    const panel = document.querySelector('#download .panel');
    if (!panel || panel.querySelector('.release-file-card')) return;

    panel.classList.add('download-panel');
    const intro = panel.querySelector('p');
    const card = document.createElement('div');
    card.className = 'release-file-card';
    card.innerHTML = isRu
      ? `<div class="release-file-head">
          <span class="release-file-icon" aria-hidden="true">EXE</span>
          <div class="release-file-copy"><strong data-release-file>Kofge-Clicker.exe</strong><span><span data-release-version>${FALLBACK_VERSION}</span> · Windows x64</span></div>
          <span class="release-file-badge">Official GitHub</span>
        </div>
        <div class="release-file-actions">
          <a class="btn btn-primary" data-release-download href="${RELEASE_URL}">Скачать .exe</a>
          <a class="btn btn-secondary" data-release-notes href="${RELEASE_URL}">Описание релиза</a>
        </div>
        <div class="release-file-details"><span data-release-size>Self-contained</span><span data-release-date>Последний релиз</span><span>Без установщика</span></div>
        <div class="release-digest-row" data-release-digest-row hidden><span class="release-digest-label">SHA-256</span><code data-release-digest></code><button class="release-copy-button" type="button" data-copy-digest>Копировать</button></div>`
      : `<div class="release-file-head">
          <span class="release-file-icon" aria-hidden="true">EXE</span>
          <div class="release-file-copy"><strong data-release-file>Kofge-Clicker.exe</strong><span><span data-release-version>${FALLBACK_VERSION}</span> · Windows x64</span></div>
          <span class="release-file-badge">Official GitHub</span>
        </div>
        <div class="release-file-actions">
          <a class="btn btn-primary" data-release-download href="${RELEASE_URL}">Download .exe</a>
          <a class="btn btn-secondary" data-release-notes href="${RELEASE_URL}">Release notes</a>
        </div>
        <div class="release-file-details"><span data-release-size>Self-contained</span><span data-release-date>Latest release</span><span>No installer</span></div>
        <div class="release-digest-row" data-release-digest-row hidden><span class="release-digest-label">SHA-256</span><code data-release-digest></code><button class="release-copy-button" type="button" data-copy-digest>Copy</button></div>`;

    if (intro) intro.insertAdjacentElement('afterend', card);
    else panel.appendChild(card);

    panel.querySelector('.actions')?.remove();

    card.querySelector('[data-copy-digest]')?.addEventListener('click', async (event) => {
      const digest = card.querySelector('[data-release-digest]')?.textContent?.trim();
      if (!digest) return;
      const button = event.currentTarget;
      try {
        await navigator.clipboard.writeText(digest);
        const original = isRu ? 'Копировать' : 'Copy';
        button.textContent = isRu ? 'Скопировано' : 'Copied';
        window.setTimeout(() => { button.textContent = original; }, 1400);
      } catch {
        // Clipboard can be blocked by browser permissions; the hash remains selectable.
      }
    });
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
      const strong = document.createElement('strong');
      strong.textContent = title;
      const small = document.createElement('small');
      small.textContent = descriptions[title] || '';
      copy.append(strong, small);

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
        <span><span data-release-version>${FALLBACK_VERSION}</span> · Windows x64</span>
      </div>
      <a class="btn btn-primary" data-release-download href="${RELEASE_URL}">${isRu ? 'Скачать' : 'Download'}</a>
    `;
    document.body.appendChild(bar);
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

  const humanSize = (bytes) => {
    if (!Number.isFinite(bytes) || bytes <= 0) return null;
    const units = ['B', 'KB', 'MB', 'GB'];
    let value = bytes;
    let unit = 0;
    while (value >= 1024 && unit < units.length - 1) {
      value /= 1024;
      unit += 1;
    }
    const digits = value >= 10 || unit === 0 ? 0 : 1;
    return `${value.toFixed(digits)} ${units[unit]}`;
  };

  const formatDate = (iso) => {
    if (!iso) return null;
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return null;
    return new Intl.DateTimeFormat(isRu ? 'ru-RU' : 'en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    }).format(date);
  };

  const applyReleaseInfo = (info) => {
    const version = info.version || FALLBACK_VERSION;
    document.querySelectorAll('[data-release-version]').forEach((node) => { node.textContent = version; });
    document.querySelectorAll('[data-release-notes]').forEach((node) => { node.href = info.releaseUrl || RELEASE_URL; });
    document.querySelectorAll('[data-release-download]').forEach((node) => { node.href = info.downloadUrl || info.releaseUrl || RELEASE_URL; });

    const fileNode = document.querySelector('[data-release-file]');
    if (fileNode && info.fileName) fileNode.textContent = info.fileName;

    const sizeNode = document.querySelector('[data-release-size]');
    const size = humanSize(info.size);
    if (sizeNode && size) sizeNode.textContent = size;

    const dateNode = document.querySelector('[data-release-date]');
    const date = formatDate(info.publishedAt);
    if (dateNode && date) dateNode.textContent = date;

    const digestRow = document.querySelector('[data-release-digest-row]');
    const digestNode = document.querySelector('[data-release-digest]');
    if (digestRow && digestNode && info.digest) {
      digestNode.textContent = info.digest.replace(/^sha256:/i, '');
      digestRow.hidden = false;
    }
  };

  const normalizeRelease = (release) => {
    const assets = Array.isArray(release?.assets) ? release.assets : [];
    const asset = assets.find((item) => /\.exe$/i.test(item?.name || ''))
      || assets.find((item) => item?.browser_download_url)
      || null;

    return {
      version: release?.tag_name || FALLBACK_VERSION,
      releaseUrl: release?.html_url || RELEASE_URL,
      publishedAt: release?.published_at || release?.created_at || null,
      fileName: asset?.name || 'Kofge-Clicker.exe',
      size: Number(asset?.size) || 0,
      digest: typeof asset?.digest === 'string' ? asset.digest : null,
      downloadUrl: asset?.browser_download_url || release?.html_url || RELEASE_URL
    };
  };

  const loadReleaseInfo = async () => {
    try {
      const cached = JSON.parse(localStorage.getItem(RELEASE_CACHE_KEY) || 'null');
      if (cached?.savedAt && cached?.info && Date.now() - cached.savedAt < RELEASE_CACHE_TTL) {
        applyReleaseInfo(cached.info);
        return;
      }
    } catch {
      // Ignore malformed or unavailable local storage.
    }

    try {
      const response = await fetch(RELEASE_API, {
        headers: { Accept: 'application/vnd.github+json' },
        cache: 'no-store'
      });
      if (!response.ok) throw new Error(`GitHub API ${response.status}`);
      const info = normalizeRelease(await response.json());
      applyReleaseInfo(info);
      try {
        localStorage.setItem(RELEASE_CACHE_KEY, JSON.stringify({ savedAt: Date.now(), info }));
      } catch {
        // The site still works if storage is blocked.
      }
    } catch {
      applyReleaseInfo({ version: FALLBACK_VERSION, releaseUrl: RELEASE_URL, downloadUrl: RELEASE_URL });
    }
  };

  const initMainPageEnhancements = () => {
    if (!document.querySelector('.hero')) return;
    initReleaseMeta();
    initQuickStart();
    initDownloadCard();
    initGallery();
    ensureMobileDownloadBar();
    initMobileDownloadBar();
    loadReleaseInfo();
  };

  const init = () => {
    initMobileNav();
    initMainPageEnhancements();
  };

  initEnhancementStyles();
  initFavicon();

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init, { once: true });
  } else {
    init();
  }
})();
