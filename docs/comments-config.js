window.KOFGE_COMMENTS_CONFIG = Object.freeze({
  apiBaseUrl: "https://kofge-clicker-comments.kofge-clicker-comments-worker.workers.dev",
  turnstileSiteKey: "0x4AAAAAAEVq96P5LpuiFY7F"
});

(() => {
  const SUPPORT = Object.freeze({
    donationAlerts: "https://www.donationalerts.com/r/kofge",
    erc20: "0x5701793453c1d73a527af74f9b615717052c4738",
    trc20: "TMTvgkSzEARmZ81HG2SE7nRf2KbC63tcBJ"
  });

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
                <a class="btn btn-secondary" href="${SUPPORT.donationAlerts}" target="_blank" rel="noopener">DonationAlerts</a>
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
                <a class="btn btn-secondary" href="${SUPPORT.donationAlerts}" target="_blank" rel="noopener">DonationAlerts</a>
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

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", addSiteLinksAndSupport, { once: true });
  } else {
    addSiteLinksAndSupport();
  }
})();
