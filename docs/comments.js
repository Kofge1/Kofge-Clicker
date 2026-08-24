(() => {
  const root = document.querySelector('[data-comments-root]');
  if (!root) return;

  const config = window.KOFGE_COMMENTS_CONFIG || {};
  const apiBaseUrl = String(config.apiBaseUrl || '').replace(/\/$/, '');
  const turnstileSiteKey = String(config.turnstileSiteKey || '');
  const language = root.dataset.language === 'ru' ? 'ru' : 'en';
  const strings = language === 'ru'
    ? {
        unavailable: 'Отзывы пока настраиваются. Скоро здесь можно будет оставить комментарий без аккаунта GitHub.',
        loadError: 'Не удалось загрузить отзывы. Попробуйте обновить страницу немного позже.',
        empty: 'Пока нет опубликованных отзывов. Вы можете оставить первый.',
        oneComment: 'опубликованный отзыв', manyComments: 'опубликованных отзывов',
        sending: 'Отправляем…', submit: 'Отправить на проверку',
        sent: 'Спасибо! Отзыв отправлен и появится после проверки.',
        genericError: 'Не удалось отправить отзыв. Проверьте поля и попробуйте снова.',
        turnstileError: 'Подтвердите, что вы не робот, и попробуйте снова.',
        version: 'Версия', developerReply: 'Ответ разработчика'
      }
    : {
        unavailable: 'Reviews are being configured. Soon you will be able to comment here without a GitHub account.',
        loadError: 'Reviews could not be loaded. Please refresh the page a little later.',
        empty: 'There are no published reviews yet. You can leave the first one.',
        oneComment: 'published review', manyComments: 'published reviews',
        sending: 'Sending…', submit: 'Send for review',
        sent: 'Thank you! Your review was submitted and will appear after moderation.',
        genericError: 'Your review could not be submitted. Check the fields and try again.',
        turnstileError: 'Please complete the anti-spam check and try again.',
        version: 'Version', developerReply: 'Developer reply'
      };

  const list = root.querySelector('[data-comments-list]');
  const count = root.querySelector('[data-comments-count]');
  const form = root.querySelector('[data-comments-form]');
  const formStatus = root.querySelector('[data-comments-form-status]');
  const submitButton = root.querySelector("button[type='submit']");
  const turnstileHost = root.querySelector('[data-turnstile]');
  let turnstileWidgetId = null;
  let turnstileToken = '';
  let started = false;

  const setFormStatus = (message, kind = '') => {
    if (!formStatus) return;
    formStatus.textContent = message;
    formStatus.dataset.kind = kind;
  };

  const setUnavailable = () => {
    list.innerHTML = '';
    const message = document.createElement('div');
    message.className = 'comments-state';
    message.textContent = strings.unavailable;
    list.append(message);
    form.hidden = true;
    count.textContent = '';
  };

  const formatDate = (value) => new Intl.DateTimeFormat(language === 'ru' ? 'ru-RU' : 'en-US', {
    year: 'numeric', month: 'short', day: 'numeric'
  }).format(new Date(value));

  const renderComments = (comments) => {
    list.innerHTML = '';
    count.textContent = comments.length
      ? `${comments.length} ${comments.length === 1 ? strings.oneComment : strings.manyComments}`
      : '';

    if (!comments.length) {
      const empty = document.createElement('div');
      empty.className = 'comments-state';
      empty.textContent = strings.empty;
      list.append(empty);
      return;
    }

    for (const comment of comments) {
      const article = document.createElement('article');
      article.className = 'review-card';

      const header = document.createElement('div');
      header.className = 'review-card-head';
      const identity = document.createElement('div');
      identity.className = 'review-identity';
      const avatar = document.createElement('span');
      avatar.className = 'review-avatar';
      avatar.textContent = comment.name.trim().slice(0, 1).toUpperCase();
      avatar.setAttribute('aria-hidden', 'true');
      const author = document.createElement('strong');
      author.textContent = comment.name;
      identity.append(avatar, author);

      const meta = document.createElement('div');
      meta.className = 'review-meta';
      meta.textContent = formatDate(comment.createdAt);
      if (comment.appVersion) {
        const version = document.createElement('span');
        version.textContent = `${strings.version} ${comment.appVersion}`;
        meta.append(version);
      }

      const body = document.createElement('p');
      body.textContent = comment.message;
      header.append(identity, meta);
      article.append(header, body);

      if (comment.authorReply) {
        const reply = document.createElement('div');
        reply.className = 'developer-reply';
        const title = document.createElement('strong');
        title.textContent = strings.developerReply;
        const replyBody = document.createElement('p');
        replyBody.textContent = comment.authorReply;
        reply.append(title, replyBody);
        article.append(reply);
      }
      list.append(article);
    }
  };

  const loadComments = async () => {
    try {
      const response = await fetch(`${apiBaseUrl}/api/comments?language=${language}`, {
        headers: { Accept: 'application/json' }
      });
      if (!response.ok) throw new Error('Comments request failed');
      const payload = await response.json();
      renderComments(Array.isArray(payload.comments) ? payload.comments : []);
    } catch {
      list.innerHTML = '';
      const message = document.createElement('div');
      message.className = 'comments-state comments-state-error';
      message.textContent = strings.loadError;
      list.append(message);
    }
  };

  const loadTurnstile = () => {
    if (!turnstileSiteKey || !turnstileHost) return;

    const renderWidget = () => {
      if (!window.turnstile || turnstileWidgetId !== null) return;
      turnstileWidgetId = window.turnstile.render(turnstileHost, {
        sitekey: turnstileSiteKey,
        theme: 'dark',
        language,
        callback: (token) => { turnstileToken = token; setFormStatus(''); },
        'expired-callback': () => { turnstileToken = ''; },
        'error-callback': () => { turnstileToken = ''; setFormStatus(strings.turnstileError, 'error'); }
      });
    };

    if (window.turnstile) return renderWidget();
    if (document.querySelector('script[data-kofge-turnstile]')) return;

    window.kofgeTurnstileReady = renderWidget;
    const script = document.createElement('script');
    script.dataset.kofgeTurnstile = 'true';
    script.src = 'https://challenges.cloudflare.com/turnstile/v0/api.js?onload=kofgeTurnstileReady&render=explicit';
    script.async = true;
    script.defer = true;
    document.head.append(script);
  };

  const submitReview = async (event) => {
    event.preventDefault();
    if (!turnstileToken) {
      setFormStatus(strings.turnstileError, 'error');
      return;
    }

    const data = new FormData(form);
    const payload = {
      name: String(data.get('name') || '').trim(),
      message: String(data.get('message') || '').trim(),
      appVersion: String(data.get('appVersion') || '').trim(),
      language,
      website: String(data.get('website') || ''),
      turnstileToken
    };

    submitButton.disabled = true;
    submitButton.textContent = strings.sending;
    setFormStatus('');

    try {
      const response = await fetch(`${apiBaseUrl}/api/comments`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify(payload)
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.error || strings.genericError);

      form.reset();
      turnstileToken = '';
      if (window.turnstile && turnstileWidgetId !== null) window.turnstile.reset(turnstileWidgetId);
      setFormStatus(strings.sent, 'success');
    } catch (error) {
      setFormStatus(error.message || strings.genericError, 'error');
      if (window.turnstile && turnstileWidgetId !== null) {
        turnstileToken = '';
        window.turnstile.reset(turnstileWidgetId);
      }
    } finally {
      submitButton.disabled = false;
      submitButton.textContent = strings.submit;
    }
  };

  if (!apiBaseUrl || !turnstileSiteKey) {
    setUnavailable();
    return;
  }

  form.addEventListener('submit', submitReview);

  const start = () => {
    if (started) return;
    started = true;
    loadComments();
    loadTurnstile();
  };

  if ('IntersectionObserver' in window) {
    const observer = new IntersectionObserver((entries) => {
      if (!entries.some((entry) => entry.isIntersecting)) return;
      observer.disconnect();
      start();
    }, { rootMargin: '700px 0px' });
    observer.observe(root);
  } else {
    start();
  }
})();
