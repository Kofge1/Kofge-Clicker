window.KOFGE_COMMENTS_CONFIG = Object.freeze({
  apiBaseUrl: "https://kofge-clicker-comments.kofge-clicker-comments-worker.workers.dev",
  turnstileSiteKey: "0x4AAAAAAEVq96P5LpuiFY7F"
});

(() => {
  const SUPPORT = Object.freeze({
    boosty: "https://boosty.to/kofge/donate",
    erc20: "0x5701793453c1d73a527af74f9b615717052c4738",
    trc20: "TMTvgkSzEARmZ81HG2SE7nRf2KbC63tcBJ"
  });
  const DIRECT_DOWNLOAD_URL = "https://github.com/Kofge1/Kofge-Clicker/releases/latest/download/Kofge-Clicker.exe";
  const DIRECT_DOWNLOAD_SELECTOR = [
    ".hero .actions .btn-primary",
    ".cta .actions .btn-primary",
    "[data-mobile-download-bar] .btn-primary",
    "[data-release-download]"
  ].join(", ");

  const addSiteLinksAndSupport = () => {
    const isRu = document.documentElement.lang === "ru";
    const guideHref = "./guide/";
    const guideText = isRu ? "Руководство" : "User Guide";
    const supportText = isRu ? "Поддержать" : "Support";

    const nav = document.querySelector(".nav-links");
    if (nav && !nav.querySelector("[data-guide-link]")) {
      const guideLink = document.createElement("a");
      guideLink.href = guideHref;
      guideLink.textContent = guideText;
      guideLink.dataset.guideLink = "true";
      const githubLink = Array.from(nav.querySelectorAll("a")).find((link) =>
        link.href.includes("github.com/Kofge1/Kofge-Clicker")
      );
      nav.insertBefore(guideLink, githubLink || nav.lastElementChild);
    }

    const hero = document.querySelector(".hero");
    if (hero && !document.querySelector("[data-help-support-section]")) {
      const section = document.createElement("section");
      section.dataset.helpSupportSection = "true";
      section.innerHTML = isRu
        ? `
          <div class="container split">
            <div class="panel">
              <div class="section-kicker">Помощь</div>
              <h2>Руководство Kofge-Clicker</h2>
              <p>Не знаете, что делает настройка, как назначить хоткей, настроить профиль или целевое окно? В полном руководстве собраны быстрый старт, все вкладки, примеры настроек и решение проблем.</p>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-primary" href="${guideHref}">Открыть руководство</a>
              </div>
            </div>
            <div class="panel" id="support">
              <div class="section-kicker">Добровольная поддержка</div>
              <h2>Поддержать Kofge-Clicker</h2>
              <p>Kofge-Clicker остаётся полностью бесплатным и open source. Если проект оказался полезен, вы можете добровольно поддержать дальнейшую разработку. Донат ничего не разблокирует и не меняет доступ к функциям.</p>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-secondary" href="${SUPPORT.boosty}" target="_blank" rel="noopener">Boosty</a>
              </div>
              <details style="margin-top:16px">
                <summary>USDT</summary>
                <p><strong>ERC20</strong></p>
                <div class="code" style="overflow-wrap:anywhere">${SUPPORT.erc20}</div>
                <p><strong>TRC20</strong></p>
                <div class="code" style="overflow-wrap:anywhere">${SUPPORT.trc20}</div>
                <p style="color:var(--muted);font-size:14px">Перед переводом обязательно проверьте выбранную сеть.</p>
              </details>
            </div>
          </div>`
        : `
          <div class="container split">
            <div class="panel">
              <div class="section-kicker">Help</div>
              <h2>Kofge-Clicker User Guide</h2>
              <p>Need help with a setting, hotkey, profile or target window? The complete guide covers quick start, every tab, example configurations and troubleshooting.</p>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-primary" href="${guideHref}">Open User Guide</a>
              </div>
            </div>
            <div class="panel" id="support">
              <div class="section-kicker">Optional support</div>
              <h2>Support Kofge-Clicker</h2>
              <p>Kofge-Clicker remains completely free and open source. If the project is useful to you, you can optionally support continued development. Donations do not unlock features or change access.</p>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-secondary" href="${SUPPORT.boosty}" target="_blank" rel="noopener">Boosty</a>
              </div>
              <details style="margin-top:16px">
                <summary>USDT</summary>
                <p><strong>ERC20</strong></p>
                <div class="code" style="overflow-wrap:anywhere">${SUPPORT.erc20}</div>
                <p><strong>TRC20</strong></p>
                <div class="code" style="overflow-wrap:anywhere">${SUPPORT.trc20}</div>
                <p style="color:var(--muted);font-size:14px">Please double-check the selected network before sending.</p>
              </details>
            </div>
          </div>`;
      hero.insertAdjacentElement("afterend", section);
    }

    const footer = document.querySelector(".footer-links");
    if (footer) {
      if (!footer.querySelector("[data-guide-link]")) {
        const guideLink = document.createElement("a");
        guideLink.href = guideHref;
        guideLink.textContent = guideText;
        guideLink.dataset.guideLink = "true";
        footer.insertBefore(guideLink, footer.firstElementChild);
      }
      if (!footer.querySelector("[data-support-link]")) {
        const supportLink = document.createElement("a");
        supportLink.href = "#support";
        supportLink.textContent = supportText;
        supportLink.dataset.supportLink = "true";
        footer.insertBefore(supportLink, footer.firstElementChild);
      }
    }
  };

  const setLocalizedHero = () => {
    const image = document.querySelector(".hero-shot img");
    if (!image) return;

    const isRu = document.documentElement.lang === "ru";
    const source = isRu
      ? "../assets/kofge-clicker-hero-ru.png"
      : "./assets/kofge-clicker-hero-en.png";

    image.src = source;
    image.alt = isRu
      ? "Главное окно Kofge-Clicker v0.19.5"
      : "Kofge-Clicker v0.19.5 main interface in English";
    image.style.visibility = "visible";
  };

  const setLocalizedGallery = () => {
    const cards = Array.from(document.querySelectorAll(".gallery-grid .gallery-card"));
    if (cards.length < 4) return;

    const isRu = document.documentElement.lang === "ru";
    const prefix = isRu ? "../assets/" : "./assets/";
    const lang = isRu ? "ru" : "en";
    const items = [
      { file: `gallery-patterns-${lang}.png`, enAlt: "Kofge-Clicker click patterns", ruAlt: "Настройки паттернов кликов Kofge-Clicker" },
      { file: `gallery-hotkeys-${lang}.png`, enAlt: "Kofge-Clicker hotkey settings", ruAlt: "Настройки горячих клавиш Kofge-Clicker" },
      { file: `gallery-profiles-${lang}.png`, enAlt: "Kofge-Clicker profiles", ruAlt: "Профили Kofge-Clicker" },
      { file: `gallery-targeting-${lang}.png`, enAlt: "Kofge-Clicker window targeting and options", ruAlt: "Привязка к окну и параметры Kofge-Clicker" }
    ];

    cards.slice(0, 4).forEach((card, index) => {
      const image = card.querySelector("img");
      const item = items[index];
      const source = `${prefix}${item.file}`;
      card.href = source;
      if (image) {
        image.src = source;
        image.alt = isRu ? item.ruAlt : item.enAlt;
        image.decoding = "async";
        image.fetchPriority = "low";
      }
    });
  };

  const applyDirectDownloadLinks = () => {
    document.querySelectorAll(DIRECT_DOWNLOAD_SELECTOR).forEach((link) => {
      if (link instanceof HTMLAnchorElement) link.href = DIRECT_DOWNLOAD_URL;
    });
  };

  const simplifyDownloadFlow = () => {
    const downloadSection = document.querySelector("#download");
    if (downloadSection) {
      downloadSection.hidden = true;
      downloadSection.setAttribute("aria-hidden", "true");
    }

    applyDirectDownloadLinks();
    window.setTimeout(applyDirectDownloadLinks, 0);
    window.setTimeout(applyDirectDownloadLinks, 250);
    window.addEventListener("load", applyDirectDownloadLinks, { once: true });

    const forceLatestExe = (event) => {
      const link = event.target.closest?.(DIRECT_DOWNLOAD_SELECTOR);
      if (link instanceof HTMLAnchorElement) link.href = DIRECT_DOWNLOAD_URL;
    };
    document.addEventListener("pointerdown", forceLatestExe, true);
    document.addEventListener("click", forceLatestExe, true);
  };

  const getHashTarget = () => {
    if (!window.location.hash) return null;
    let id = window.location.hash.slice(1);
    try { id = decodeURIComponent(id); } catch { /* Keep raw hash. */ }
    return document.getElementById(id);
  };

  const alignHashTarget = () => {
    const target = getHashTarget();
    if (!target) return;
    target.scrollIntoView({ block: "start", behavior: "auto" });
  };

  const restoreHashTarget = () => {
    if (!window.location.hash) return;

    requestAnimationFrame(() => requestAnimationFrame(alignHashTarget));
    [60, 180, 450, 900].forEach((delay) => window.setTimeout(alignHashTarget, delay));

    if (document.readyState !== "complete") {
      window.addEventListener("load", () => {
        alignHashTarget();
        window.setTimeout(alignHashTarget, 120);
      }, { once: true });
    }
  };

  setLocalizedHero();
  setLocalizedGallery();
  addSiteLinksAndSupport();
  simplifyDownloadFlow();
  restoreHashTarget();
  window.addEventListener("hashchange", restoreHashTarget);
})();

(() => {
  const CUSTOM_REQUEST_URL = "https://github.com/Kofge1/Kofge-Clicker/issues/new?template=custom-development.yml";

  const addCustomDevelopmentStyles = () => {
    if (document.querySelector("[data-custom-development-styles]")) return;

    const style = document.createElement("style");
    style.dataset.customDevelopmentStyles = "true";
    style.textContent = `
      .custom-development-section { position: relative; }
      .custom-development-grid {
        display: grid;
        grid-template-columns: minmax(0, 1.45fr) minmax(280px, .55fr);
        gap: 14px;
        align-items: stretch;
      }
      .custom-development-main {
        position: relative;
        overflow: hidden;
      }
      .custom-development-main::after {
        content: "";
        position: absolute;
        width: 280px;
        height: 280px;
        right: -150px;
        top: -150px;
        border-radius: 50%;
        background: rgba(102, 167, 255, .075);
        pointer-events: none;
      }
      .custom-development-main > * { position: relative; z-index: 1; }
      .custom-development-badges {
        display: flex;
        flex-wrap: wrap;
        gap: 7px;
        margin: 16px 0 20px;
      }
      .custom-development-badges span {
        padding: 6px 9px;
        border: 1px solid rgba(126, 184, 255, .17);
        border-radius: 999px;
        color: #cfe1ff;
        background: rgba(102, 167, 255, .055);
        font-size: 11px;
        font-weight: 750;
      }
      .custom-development-steps {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 10px;
        margin: 18px 0 20px;
      }
      .custom-development-step {
        min-width: 0;
        padding: 14px;
        border: 1px solid rgba(255,255,255,.09);
        border-radius: 15px;
        background: rgba(8, 11, 16, .32);
      }
      .custom-development-step strong {
        display: block;
        margin-bottom: 5px;
        color: var(--text);
        font-size: 13px;
      }
      .custom-development-step span {
        color: var(--muted);
        font-size: 12px;
        line-height: 1.45;
      }
      .custom-development-number {
        display: grid;
        width: 28px;
        height: 28px;
        place-items: center;
        margin-bottom: 10px;
        border: 1px solid rgba(126, 184, 255, .22);
        border-radius: 9px;
        color: #d9eaff;
        background: rgba(102, 167, 255, .08);
        font-size: 11px;
        font-weight: 900;
      }
      .custom-development-note {
        margin: 10px 0 0;
        color: var(--muted);
        font-size: 12px;
      }
      .supporters-card {
        display: flex;
        flex-direction: column;
        min-height: 100%;
      }
      .supporters-empty {
        display: grid;
        place-items: center;
        min-height: 150px;
        margin: 18px 0;
        padding: 22px 16px;
        border: 1px dashed rgba(255,255,255,.12);
        border-radius: 16px;
        text-align: center;
        background: rgba(255,255,255,.018);
      }
      .supporters-empty-icon {
        display: grid;
        width: 44px;
        height: 44px;
        place-items: center;
        margin-bottom: 10px;
        border: 1px solid rgba(251, 113, 133, .18);
        border-radius: 14px;
        background: rgba(251, 113, 133, .055);
        font-size: 19px;
      }
      .supporters-empty strong { display: block; margin-bottom: 5px; font-size: 14px; }
      .supporters-empty span { color: var(--muted); font-size: 12px; line-height: 1.45; }
      .supporters-footnote { margin-top: auto; color: var(--muted); font-size: 12px; }

      @media (max-width: 900px) {
        .custom-development-grid { grid-template-columns: 1fr; }
      }
      @media (max-width: 680px) {
        .custom-development-steps { grid-template-columns: 1fr; }
        .custom-development-step {
          display: grid;
          grid-template-columns: auto 1fr;
          column-gap: 11px;
          align-items: start;
        }
        .custom-development-number { grid-row: 1 / span 2; margin: 0; }
        .custom-development-step span { grid-column: 2; }
      }
    `;
    document.head.appendChild(style);
  };

  const addCustomDevelopmentSection = () => {
    const faq = document.querySelector("#faq");
    if (!faq || document.querySelector("[data-custom-development-section]")) return;

    const isRu = document.documentElement.lang === "ru";
    const section = document.createElement("section");
    section.id = "custom-development";
    section.className = "custom-development-section";
    section.dataset.customDevelopmentSection = "true";

    section.innerHTML = isRu
      ? `
        <div class="container">
          <div class="section-head">
            <div class="section-kicker">Индивидуальная разработка</div>
            <h2>Нужна функция, которой ещё нет в Kofge-Clicker?</h2>
            <p>Можно заказать приоритетную разработку конкретной функции или доработки. Сам Kofge-Clicker при этом остаётся бесплатным и open source — вы оплачиваете работу над нужной вам идеей, а не доступ к Premium.</p>
          </div>
          <div class="custom-development-grid">
            <div class="panel custom-development-main">
              <div class="custom-development-badges">
                <span>Без Premium</span>
                <span>Цена согласуется заранее</span>
                <span>Запрос ни к чему не обязывает</span>
              </div>
              <div class="custom-development-steps">
                <div class="custom-development-step">
                  <div class="custom-development-number">1</div>
                  <strong>Опишите идею</strong>
                  <span>Что нужно добавить, как это должно работать и для чего вам это нужно.</span>
                </div>
                <div class="custom-development-step">
                  <div class="custom-development-number">2</div>
                  <strong>Получите оценку</strong>
                  <span>Я проверю возможность реализации, объём работы и заранее назову стоимость.</span>
                </div>
                <div class="custom-development-step">
                  <div class="custom-development-number">3</div>
                  <strong>Разработка и релиз</strong>
                  <span>После согласования функция реализуется и тестируется. Если она подходит проекту, её можно включить в общий бесплатный релиз.</span>
                </div>
              </div>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-primary" href="${CUSTOM_REQUEST_URL}">Предложить платную доработку</a>
              </div>
              <p class="custom-development-note">Форма запроса находится на GitHub. Отправка заявки бесплатна и не означает согласие на оплату или начало работы.</p>
            </div>

            <aside class="panel supporters-card" id="supporters">
              <div class="section-kicker">Supporters</div>
              <h3>Поддержавшие разработку</h3>
              <p>После выполненного заказа можно по желанию оставить свой ник рядом с функцией, которую вы помогли добавить.</p>
              <div class="supporters-empty">
                <div>
                  <div class="supporters-empty-icon">♥</div>
                  <strong>Здесь появятся первые supporters</strong>
                  <span>Можно выбрать публичный ник, Anonymous или вообще отказаться от упоминания.</span>
                </div>
              </div>
              <p class="supporters-footnote">Имя публикуется только с разрешения заказчика.</p>
            </aside>
          </div>
        </div>`
      : `
        <div class="container">
          <div class="section-head">
            <div class="section-kicker">Custom development</div>
            <h2>Need a feature Kofge-Clicker does not have yet?</h2>
            <p>You can sponsor priority development of a specific feature or improvement. Kofge-Clicker remains free and open source — you pay for work on the idea you need, not for access to a Premium tier.</p>
          </div>
          <div class="custom-development-grid">
            <div class="panel custom-development-main">
              <div class="custom-development-badges">
                <span>No Premium tier</span>
                <span>Price agreed in advance</span>
                <span>No-obligation request</span>
              </div>
              <div class="custom-development-steps">
                <div class="custom-development-step">
                  <div class="custom-development-number">1</div>
                  <strong>Describe the idea</strong>
                  <span>Tell me what you want added, how it should work and what you need it for.</span>
                </div>
                <div class="custom-development-step">
                  <div class="custom-development-number">2</div>
                  <strong>Get an estimate</strong>
                  <span>I will review feasibility, scope and give you a price before any work begins.</span>
                </div>
                <div class="custom-development-step">
                  <div class="custom-development-number">3</div>
                  <strong>Development & release</strong>
                  <span>After agreement, the feature is built and tested. If it fits the project, it can be included in the public free release.</span>
                </div>
              </div>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-primary" href="${CUSTOM_REQUEST_URL}">Request custom development</a>
              </div>
              <p class="custom-development-note">The request form is hosted on GitHub. Submitting a request is free and does not commit you to payment or start any work.</p>
            </div>

            <aside class="panel supporters-card" id="supporters">
              <div class="section-kicker">Supporters</div>
              <h3>Development supporters</h3>
              <p>After a completed request, you can optionally have your nickname credited next to the feature you helped make possible.</p>
              <div class="supporters-empty">
                <div>
                  <div class="supporters-empty-icon">♥</div>
                  <strong>The first supporters will appear here</strong>
                  <span>Choose a public nickname, Anonymous, or no public credit at all.</span>
                </div>
              </div>
              <p class="supporters-footnote">A name is published only with the requester's permission.</p>
            </aside>
          </div>
        </div>`;

    faq.insertAdjacentElement("beforebegin", section);

    const footer = document.querySelector(".footer-links");
    if (footer && !footer.querySelector("[data-custom-development-link]")) {
      const link = document.createElement("a");
      link.href = "#custom-development";
      link.textContent = isRu ? "Доработки" : "Custom development";
      link.dataset.customDevelopmentLink = "true";
      const reviewsLink = Array.from(footer.querySelectorAll("a")).find((item) => item.getAttribute("href") === "#reviews");
      footer.insertBefore(link, reviewsLink || footer.firstElementChild);
    }
  };

  addCustomDevelopmentStyles();
  addCustomDevelopmentSection();
})();
