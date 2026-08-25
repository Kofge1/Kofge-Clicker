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
  const CONTACT_EMAIL = "kofge.dev@gmail.com";

  const addCustomDevelopmentStyles = () => {
    if (document.querySelector("[data-custom-development-styles]")) return;

    const style = document.createElement("style");
    style.dataset.customDevelopmentStyles = "true";
    style.textContent = `
      .custom-development-section { position: relative; }
      .custom-development-options {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 14px;
      }
      .custom-option {
        position: relative;
        display: flex;
        flex-direction: column;
        min-width: 0;
        overflow: hidden;
      }
      .custom-option::after {
        content: "";
        position: absolute;
        width: 240px;
        height: 240px;
        right: -135px;
        top: -145px;
        border-radius: 50%;
        background: rgba(102, 167, 255, .065);
        pointer-events: none;
      }
      .custom-option > * { position: relative; z-index: 1; }
      .custom-option-private::after { background: rgba(167, 139, 250, .07); }
      .custom-option-icon {
        display: grid;
        width: 44px;
        height: 44px;
        place-items: center;
        margin-bottom: 14px;
        border: 1px solid rgba(126,184,255,.2);
        border-radius: 14px;
        background: rgba(102,167,255,.065);
        font-size: 19px;
      }
      .custom-option-private .custom-option-icon {
        border-color: rgba(196,181,253,.2);
        background: rgba(167,139,250,.065);
      }
      .custom-option h3 { margin: 0 0 8px; font-size: 20px; }
      .custom-option > p { margin-top: 0; }
      .custom-option-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 7px;
        margin: 8px 0 18px;
      }
      .custom-option-tags span {
        padding: 6px 9px;
        border: 1px solid rgba(255,255,255,.1);
        border-radius: 999px;
        color: #d7e4f7;
        background: rgba(255,255,255,.025);
        font-size: 11px;
        font-weight: 750;
      }
      .custom-option .actions { margin-top: auto; }
      .custom-option-note {
        margin: 11px 0 0 !important;
        color: var(--muted);
        font-size: 12px;
        line-height: 1.45;
      }
      .custom-development-bottom {
        display: grid;
        grid-template-columns: minmax(0, 1.25fr) minmax(280px, .75fr);
        gap: 14px;
        margin-top: 14px;
      }
      .custom-development-steps {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 10px;
        margin-top: 16px;
      }
      .custom-development-step {
        min-width: 0;
        padding: 14px;
        border: 1px solid rgba(255,255,255,.09);
        border-radius: 15px;
        background: rgba(8,11,16,.32);
      }
      .custom-development-number {
        display: grid;
        width: 28px;
        height: 28px;
        place-items: center;
        margin-bottom: 10px;
        border: 1px solid rgba(126,184,255,.22);
        border-radius: 9px;
        color: #d9eaff;
        background: rgba(102,167,255,.08);
        font-size: 11px;
        font-weight: 900;
      }
      .custom-development-step strong { display: block; margin-bottom: 5px; font-size: 13px; }
      .custom-development-step span { color: var(--muted); font-size: 12px; line-height: 1.45; }
      .custom-contact {
        margin-top: 16px;
        padding-top: 14px;
        border-top: 1px solid rgba(255,255,255,.08);
        color: var(--muted);
        font-size: 13px;
      }
      .custom-contact a { color: #cfe1ff; font-weight: 750; text-decoration: none; }
      .custom-contact a:hover { color: var(--text); }
      .supporters-card { display: flex; flex-direction: column; min-height: 100%; }
      .supporters-empty {
        display: grid;
        place-items: center;
        min-height: 128px;
        margin: 14px 0;
        padding: 20px 15px;
        border: 1px dashed rgba(255,255,255,.12);
        border-radius: 16px;
        text-align: center;
        background: rgba(255,255,255,.018);
      }
      .supporters-empty-icon {
        display: grid;
        width: 42px;
        height: 42px;
        place-items: center;
        margin: 0 auto 9px;
        border: 1px solid rgba(251,113,133,.18);
        border-radius: 14px;
        background: rgba(251,113,133,.055);
        font-size: 18px;
      }
      .supporters-empty strong { display: block; margin-bottom: 5px; font-size: 14px; }
      .supporters-empty span,
      .supporters-footnote { color: var(--muted); font-size: 12px; line-height: 1.45; }
      .supporters-footnote { margin-top: auto; }

      @media (max-width: 900px) {
        .custom-development-options,
        .custom-development-bottom { grid-template-columns: 1fr; }
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
        .custom-option .btn { width: 100%; }
      }
    `;
    document.head.appendChild(style);
  };

  const addCustomDevelopmentSection = () => {
    const faq = document.querySelector("#faq");
    if (!faq) return;

    document.querySelector("[data-custom-development-section]")?.remove();

    const isRu = document.documentElement.lang === "ru";
    const section = document.createElement("section");
    section.id = "custom-development";
    section.className = "custom-development-section";
    section.dataset.customDevelopmentSection = "true";

    section.innerHTML = isRu
      ? `
        <div class="container">
          <div class="section-head">
            <div class="section-kicker">Разработка на заказ</div>
            <h2>Два способа получить нужную вам функцию</h2>
            <p>Выберите приватную сборку только для себя или профинансируйте функцию для основной бесплатной версии Kofge-Clicker. Перед началом работы мы отдельно согласуем возможность реализации, стоимость и сроки.</p>
          </div>

          <div class="custom-development-options">
            <article class="panel custom-option custom-option-private">
              <div class="custom-option-icon" aria-hidden="true">🔒</div>
              <div class="section-kicker">Private Custom Build</div>
              <h3>Функция только для вас</h3>
              <p>Отдельная сборка Kofge-Clicker с функционалом под вашу задачу. Такая доработка не публикуется в обычных GitHub Releases и не становится автоматически доступна другим пользователям.</p>
              <div class="custom-option-tags"><span>Отдельный EXE</span><span>Не публикуется в Releases</span><span>Приватная передача</span></div>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-primary" href="${CUSTOM_REQUEST_URL}">Заказать приватную сборку</a>
              </div>
              <p class="custom-option-note">Поддержка приватной модификации в будущих версиях Kofge-Clicker при необходимости оценивается отдельно.</p>
            </article>

            <article class="panel custom-option">
              <div class="custom-option-icon" aria-hidden="true">♥</div>
              <div class="section-kicker">Sponsored Public Feature</div>
              <h3>Профинансировать функцию для всех</h3>
              <p>Вы оплачиваете приоритетную разработку функции для основного Kofge-Clicker. После реализации она входит в обычный бесплатный релиз и становится доступна всем пользователям.</p>
              <div class="custom-option-tags"><span>Приоритетная разработка</span><span>Обычный Release</span><span>Бесплатно для всех</span></div>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-secondary" href="${CUSTOM_REQUEST_URL}">Спонсировать функцию</a>
              </div>
              <p class="custom-option-note">Публичная функция принимается в проект, если она подходит общей концепции Kofge-Clicker и может безопасно поддерживаться дальше.</p>
            </article>
          </div>

          <div class="custom-development-bottom">
            <div class="panel">
              <div class="section-kicker">Как проходит заказ</div>
              <div class="custom-development-steps">
                <div class="custom-development-step"><div class="custom-development-number">1</div><strong>Оставьте заявку</strong><span>Выберите тип разработки и подробно опишите желаемое поведение.</span></div>
                <div class="custom-development-step"><div class="custom-development-number">2</div><strong>Получите оценку</strong><span>Я проверю задачу и до начала работы согласую с вами стоимость и сроки.</span></div>
                <div class="custom-development-step"><div class="custom-development-number">3</div><strong>Разработка и передача</strong><span>Приватная сборка передаётся лично заказчику, публичная функция выходит в обычном релизе.</span></div>
              </div>
              <div class="custom-contact">После рассмотрения заявки приватное обсуждение деталей, стоимости и оплаты продолжается по email: <a href="mailto:${CONTACT_EMAIL}">${CONTACT_EMAIL}</a>.</div>
            </div>

            <aside class="panel supporters-card" id="supporters">
              <div class="section-kicker">Supporters</div>
              <h3>Поддержавшие разработку</h3>
              <p>После выполненного заказа можно по желанию оставить публичный ник, выбрать Anonymous или отказаться от упоминания.</p>
              <div class="supporters-empty"><div><div class="supporters-empty-icon">♥</div><strong>Здесь появятся первые supporters</strong><span>Для приватных заказов можно указать только ник без описания заказанной функции.</span></div></div>
              <p class="supporters-footnote">Имя и детали заказа публикуются только с разрешения заказчика.</p>
            </aside>
          </div>
        </div>`
      : `
        <div class="container">
          <div class="section-head">
            <div class="section-kicker">Custom development</div>
            <h2>Two ways to get the feature you need</h2>
            <p>Choose a private build made only for you, or sponsor a feature for the main free Kofge-Clicker release. Feasibility, price and timing are agreed before any paid work begins.</p>
          </div>

          <div class="custom-development-options">
            <article class="panel custom-option custom-option-private">
              <div class="custom-option-icon" aria-hidden="true">🔒</div>
              <div class="section-kicker">Private Custom Build</div>
              <h3>A feature only for you</h3>
              <p>A separate Kofge-Clicker build tailored to your workflow. The custom functionality is not published in normal GitHub Releases and is not automatically made available to other users.</p>
              <div class="custom-option-tags"><span>Separate EXE</span><span>Not published in Releases</span><span>Private delivery</span></div>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-primary" href="${CUSTOM_REQUEST_URL}">Request a private build</a>
              </div>
              <p class="custom-option-note">Maintenance or porting of a private modification to future Kofge-Clicker versions can be estimated separately when needed.</p>
            </article>

            <article class="panel custom-option">
              <div class="custom-option-icon" aria-hidden="true">♥</div>
              <div class="section-kicker">Sponsored Public Feature</div>
              <h3>Sponsor a feature for everyone</h3>
              <p>You fund priority development for the main Kofge-Clicker project. Once completed, the feature is included in the normal free release and becomes available to all users.</p>
              <div class="custom-option-tags"><span>Priority development</span><span>Normal Release</span><span>Free for everyone</span></div>
              <div class="actions" style="justify-content:flex-start">
                <a class="btn btn-secondary" href="${CUSTOM_REQUEST_URL}">Sponsor a public feature</a>
              </div>
              <p class="custom-option-note">A public feature is accepted when it fits Kofge-Clicker's overall direction and can be maintained safely in future releases.</p>
            </article>
          </div>

          <div class="custom-development-bottom">
            <div class="panel">
              <div class="section-kicker">How it works</div>
              <div class="custom-development-steps">
                <div class="custom-development-step"><div class="custom-development-number">1</div><strong>Submit a request</strong><span>Choose the development type and describe the behavior you want in detail.</span></div>
                <div class="custom-development-step"><div class="custom-development-number">2</div><strong>Get an estimate</strong><span>I review the request and agree on price and timing with you before work begins.</span></div>
                <div class="custom-development-step"><div class="custom-development-number">3</div><strong>Build & delivery</strong><span>A private build is delivered to the requester; a sponsored feature ships in the normal public release.</span></div>
              </div>
              <div class="custom-contact">After the request is reviewed, private discussion of details, pricing and payment continues by email: <a href="mailto:${CONTACT_EMAIL}">${CONTACT_EMAIL}</a>.</div>
            </div>

            <aside class="panel supporters-card" id="supporters">
              <div class="section-kicker">Supporters</div>
              <h3>Development supporters</h3>
              <p>After completed work, you can optionally use a public nickname, choose Anonymous, or decline public credit.</p>
              <div class="supporters-empty"><div><div class="supporters-empty-icon">♥</div><strong>The first supporters will appear here</strong><span>For private requests, a nickname can be listed without revealing what was commissioned.</span></div></div>
              <p class="supporters-footnote">Names and request details are published only with the requester's permission.</p>
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
