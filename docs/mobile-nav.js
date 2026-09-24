(() => {
  const FALLBACK_VERSION = 'v1.0.3';
  const RELEASE_URL = 'https://github.com/Kofge1/Kofge-Clicker/releases/latest';
  const DIRECT_DOWNLOAD_URL = 'https://github.com/Kofge1/Kofge-Clicker/releases/latest/download/Kofge-Clicker.exe';
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

  const addStylesheet = (file, marker) => {
    const attribute = marker.replace(/[A-Z]/g, (letter) => `-${letter.toLowerCase()}`);
    if (document.querySelector(`link[data-${attribute}]`)) return;
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.dataset[marker] = 'true';
    link.href = new URL(file, scriptBase).href;
    document.head.appendChild(link);
  };

  const initStyles = () => {
    addStylesheet('site-polish.css?v=20260924-visual1', 'kofgeSitePolish');
    addStylesheet('a11y-performance.css?v=20260923-ux1', 'kofgeA11yPerformance');
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

  const initSkipLink = () => {
    if (document.querySelector('.skip-link')) return;
    const main = document.querySelector('main');
    if (!main) return;
    if (!main.id) main.id = 'main-content';
    const link = document.createElement('a');
    link.className = 'skip-link';
    link.href = `#${main.id}`;
    link.textContent = isRu ? 'Перейти к содержимому' : 'Skip to content';
    document.body.insertBefore(link, document.body.firstChild);
  };

  const initImageHints = () => {
    const hero = document.querySelector('.hero-shot img');
    if (hero) {
      hero.decoding = 'async';
      hero.fetchPriority = 'high';
    }

    document.querySelectorAll('.gallery img').forEach((image) => {
      image.loading = 'lazy';
      image.decoding = 'async';
      image.fetchPriority = 'low';
    });
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
      const clone = language.cloneNode(true);
      clone.classList.add('mobile-lang');
      clone.removeAttribute('id');
      controls.appendChild(clone);
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
    links.addEventListener('click', (event) => { if (event.target.closest('a')) setOpen(false); });
    document.addEventListener('click', (event) => {
      if (links.classList.contains('is-open') && !nav.contains(event.target)) setOpen(false);
    });
    document.addEventListener('keydown', (event) => { if (event.key === 'Escape') setOpen(false); });
    window.addEventListener('resize', () => { if (window.innerWidth > 900) setOpen(false); }, { passive: true });
  };

  const normalizeMobileDownloadBar = () => {
    let bar = document.querySelector('[data-mobile-download-bar]');
    if (!bar && document.querySelector('.hero')) {
      bar = document.createElement('div');
      bar.className = 'mobile-download-bar';
      bar.dataset.mobileDownloadBar = 'true';
      document.body.appendChild(bar);
    }
    if (!bar) return;
    bar.setAttribute('aria-hidden', 'true');
    bar.inert = true;
    bar.innerHTML = `<div class="mobile-download-copy"><strong>Kofge-Clicker</strong><span><span data-release-version>${FALLBACK_VERSION}</span> · Windows x64</span></div><a class="btn btn-primary" data-release-download href="${DIRECT_DOWNLOAD_URL}">${isRu ? 'Скачать' : 'Download'}</a>`;
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
      const visible = media.matches
        && hero.getBoundingClientRect().bottom < 96
        && !overlapsViewport(download, 48)
        && !overlapsViewport(footer, 0);
      bar.classList.toggle('is-visible', visible);
      bar.setAttribute('aria-hidden', String(!visible));
      bar.inert = !visible;
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
    const units = isRu ? ['Б', 'КБ', 'МБ', 'ГБ'] : ['B', 'KB', 'MB', 'GB'];
    let value = bytes, unit = 0;
    while (value >= 1024 && unit < units.length - 1) { value /= 1024; unit += 1; }
    return `${value.toFixed(value >= 10 || unit === 0 ? 0 : 1)} ${units[unit]}`;
  };

  const formatDate = (iso) => {
    if (!iso) return null;
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return null;
    return new Intl.DateTimeFormat(isRu ? 'ru-RU' : 'en-GB', { day: 'numeric', month: 'short', year: 'numeric' }).format(date);
  };

  const updateStructuredData = (info) => {
    const node = document.querySelector('script[type="application/ld+json"]');
    if (!node) return;
    try {
      const data = JSON.parse(node.textContent || '{}');
      data.softwareVersion = info.version || FALLBACK_VERSION;
      data.downloadUrl = info.downloadUrl || info.releaseUrl || RELEASE_URL;
      node.textContent = JSON.stringify(data, null, 2);
    } catch {}
  };

  const applyReleaseInfo = (info) => {
    const version = info.version || FALLBACK_VERSION;
    document.querySelectorAll('[data-release-version]').forEach((node) => { node.textContent = version; });
    document.querySelectorAll('[data-release-notes]').forEach((node) => { node.href = info.releaseUrl || RELEASE_URL; });
    document.querySelectorAll('[data-release-download]').forEach((node) => { node.href = info.downloadUrl || info.releaseUrl || RELEASE_URL; });
    const fileNode = document.querySelector('[data-release-file]');
    if (fileNode && info.fileName) fileNode.textContent = info.fileName;
    const size = humanSize(info.size);
    if (size) document.querySelectorAll('[data-release-size]').forEach((node) => { node.textContent = size; });
    const dateNode = document.querySelector('[data-release-date]');
    const date = formatDate(info.publishedAt);
    if (dateNode && date) dateNode.textContent = date;
    const digestRow = document.querySelector('[data-release-digest-row]');
    const digestNode = document.querySelector('[data-release-digest]');
    if (digestRow && digestNode && info.digest) {
      digestNode.textContent = info.digest.replace(/^sha256:/i, '');
      digestRow.hidden = false;
    }
    updateStructuredData(info);
  };

  const normalizeRelease = (release) => {
    const assets = Array.isArray(release?.assets) ? release.assets : [];
    const asset = assets.find((item) => item?.name === 'Kofge-Clicker.exe')
      || assets.find((item) => /\.exe$/i.test(item?.name || '')) || null;
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
    } catch {}

    try {
      const response = await fetch(RELEASE_API, { headers: { Accept: 'application/vnd.github+json' }, cache: 'no-store' });
      if (!response.ok) throw new Error(`GitHub API ${response.status}`);
      const info = normalizeRelease(await response.json());
      applyReleaseInfo(info);
      try { localStorage.setItem(RELEASE_CACHE_KEY, JSON.stringify({ savedAt: Date.now(), info })); } catch {}
    } catch {
      applyReleaseInfo({ version: FALLBACK_VERSION, releaseUrl: RELEASE_URL, downloadUrl: DIRECT_DOWNLOAD_URL });
    }
  };

  const initMainEnhancements = () => {
    if (document.querySelector('.hero')) {
      normalizeMobileDownloadBar();
      initMobileDownloadBar();
    }
    if (document.querySelector('[data-release-download]')) loadReleaseInfo();
  };

  const init = () => {
    initSkipLink();
    initImageHints();
    initMobileNav();
    initMainEnhancements();
  };

  initStyles();
  initFavicon();
  init();
})();
