(() => {
  const cards = Array.from(document.querySelectorAll('.gallery-card'));
  // Keep the ordinary image links usable without native dialog support.
  if (!cards.length || typeof HTMLDialogElement === 'undefined'
    || typeof HTMLDialogElement.prototype.showModal !== 'function') return;

  const isRu = document.documentElement.lang === 'ru';
  const labels = isRu
    ? { close: 'Закрыть', previous: 'Предыдущий скриншот', next: 'Следующий скриншот', zoom: 'Увеличить', fit: 'Вписать', hint: '← → — листать · Esc — закрыть' }
    : { close: 'Close', previous: 'Previous screenshot', next: 'Next screenshot', zoom: 'Zoom in', fit: 'Fit image', hint: '← → to browse · Esc to close' };
  const dialog = document.createElement('dialog');
  dialog.className = 'gallery-dialog';
  dialog.setAttribute('aria-labelledby', 'gallery-dialog-caption');
  dialog.innerHTML = '<div class="gallery-dialog-toolbar"><span class="gallery-dialog-count" aria-live="polite"></span><div class="gallery-dialog-actions"></div></div><div class="gallery-dialog-stage"><img class="gallery-dialog-image" alt=""></div><p id="gallery-dialog-caption" class="gallery-dialog-caption" aria-live="polite"></p><p class="gallery-dialog-hint"></p>';
  const actions = dialog.querySelector('.gallery-dialog-actions');
  const stage = dialog.querySelector('.gallery-dialog-stage');
  stage.setAttribute('role', 'region');
  stage.setAttribute('aria-label', isRu ? 'Просмотр скриншота' : 'Screenshot viewer');
  const image = dialog.querySelector('.gallery-dialog-image');
  const caption = dialog.querySelector('.gallery-dialog-caption');
  const counter = dialog.querySelector('.gallery-dialog-count');
  dialog.querySelector('.gallery-dialog-hint').textContent = labels.hint;
  const makeButton = (text, label) => {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'gallery-dialog-button';
    button.textContent = text;
    button.setAttribute('aria-label', label);
    actions.append(button);
    return button;
  };
  const previous = makeButton('←', labels.previous);
  const next = makeButton('→', labels.next);
  const zoom = makeButton(labels.zoom, labels.zoom);
  const close = makeButton('×', labels.close);
  close.autofocus = true;
  document.body.append(dialog);
  let current = 0;
  let opener = null;

  const setZoom = (enabled) => {
    stage.classList.toggle('is-zoomed', enabled);
    zoom.setAttribute('aria-pressed', String(enabled));
    zoom.textContent = enabled ? labels.fit : labels.zoom;
    zoom.setAttribute('aria-label', zoom.textContent);
    stage.scrollTop = 0;
    stage.scrollLeft = 0;
    stage.tabIndex = enabled ? 0 : -1;
    if (enabled) stage.focus();
  };
  const showImage = (index) => {
    current = (index + cards.length) % cards.length;
    const card = cards[current];
    setZoom(false);
    image.src = card.href;
    image.alt = card.querySelector('img')?.alt || '';
    caption.textContent = card.querySelector('strong')?.textContent || image.alt;
    counter.textContent = `${current + 1} / ${cards.length}`;
  };
  cards.forEach((card, index) => {
    card.setAttribute('aria-haspopup', 'dialog');
    card.addEventListener('click', (event) => {
      if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
      showImage(index);
      // Only suppress navigation after the enhanced viewer has opened.
      try { dialog.showModal(); } catch { return; }
      event.preventDefault();
      opener = card;
      document.body.classList.add('gallery-dialog-open');
    });
  });
  previous.addEventListener('click', () => showImage(current - 1));
  next.addEventListener('click', () => showImage(current + 1));
  zoom.addEventListener('click', () => setZoom(zoom.getAttribute('aria-pressed') !== 'true'));
  close.addEventListener('click', () => dialog.close());
  dialog.addEventListener('keydown', (event) => {
    // Preserve arrow-key scrolling while viewing the full-size image.
    if (stage.classList.contains('is-zoomed')) return;
    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
      event.preventDefault();
      showImage(current + (event.key === 'ArrowRight' ? 1 : -1));
    }
  });
  dialog.addEventListener('click', (event) => {
    if (event.target !== dialog) return;
    const rect = dialog.getBoundingClientRect();
    if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) dialog.close();
  });
  dialog.addEventListener('close', () => {
    document.body.classList.remove('gallery-dialog-open');
    setZoom(false);
    opener?.focus({ preventScroll: true });
  });
})();
