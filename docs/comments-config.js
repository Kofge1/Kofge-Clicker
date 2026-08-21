window.KOFGE_COMMENTS_CONFIG = Object.freeze({
  apiBaseUrl: "https://kofge-clicker-comments.kofge-clicker-comments-worker.workers.dev",
  turnstileSiteKey: "0x4AAAAAAEVq96P5LpuiFY7F"
});

(() => {
  const addGuideLinks = () => {
    const isRu = document.documentElement.lang === "ru";
    const guideHref = isRu ? "./guide/" : "./guide/";
    const guideText = isRu ? "Руководство" : "User Guide";

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

    const footer = document.querySelector(".footer-links");
    if (footer && !footer.querySelector("[data-guide-link]")) {
      const guideLink = document.createElement("a");
      guideLink.href = guideHref;
      guideLink.textContent = guideText;
      guideLink.dataset.guideLink = "true";
      footer.insertBefore(guideLink, footer.firstElementChild);
    }
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", addGuideLinks, { once: true });
  } else {
    addGuideLinks();
  }
})();
