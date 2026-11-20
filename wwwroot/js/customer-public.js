(() => {
    "use strict";
    const menuButton = document.querySelector(".bw-public .menu-toggle");
    const navigation = document.querySelector(".bw-public .site-nav");
    function closeMenu() {
        navigation?.classList.remove("is-open");
        menuButton?.setAttribute("aria-expanded", "false");
        menuButton?.setAttribute("aria-label", "Open navigation");
    }
    menuButton?.addEventListener("click", () => {
        const open = menuButton.getAttribute("aria-expanded") !== "true";
        navigation?.classList.toggle("is-open", open);
        menuButton.setAttribute("aria-expanded", String(open));
        menuButton.setAttribute("aria-label", open ? "Close navigation" : "Open navigation");
    });
    navigation?.addEventListener("click", event => {
        if (event.target.closest("a")) closeMenu();
    });
    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && menuButton?.getAttribute("aria-expanded") === "true") {
            closeMenu();
            menuButton.focus();
        }
    });
    const tabs = Array.from(document.querySelectorAll('.bw-public [role="tab"]'));
    function selectTab(selected) {
        tabs.forEach(tab => {
            const active = tab === selected;
            tab.setAttribute("aria-selected", String(active));
            tab.tabIndex = active ? 0 : -1;
            document.getElementById(tab.getAttribute("aria-controls")).hidden = !active;
        });
    }
    tabs.forEach((tab, index) => {
        tab.addEventListener("click", () => selectTab(tab));
        tab.addEventListener("keydown", event => {
            let next;
            if (event.key === "ArrowRight") next = (index + 1) % tabs.length;
            if (event.key === "ArrowLeft") next = (index + tabs.length - 1) % tabs.length;
            if (event.key === "Home") next = 0;
            if (event.key === "End") next = tabs.length - 1;
            if (next === undefined) return;
            event.preventDefault();
            selectTab(tabs[next]);
            tabs[next].focus();
        });
    });

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    if (reducedMotion.matches || !("IntersectionObserver" in window)) return;

    const revealTargets = document.querySelectorAll(
        ".bw-public .section-heading, .bw-public .step, " +
        ".bw-public .feature-intro, .bw-public .feature-list article, " +
        ".bw-public .faq-section > div, .bw-public .closing-card"
    );
    function reveal(element) {
        element.classList.remove("public-reveal-pending");
        element.classList.add("public-reveal-visible");
        observer.unobserve(element);
    }
    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (entry.isIntersecting) reveal(entry.target);
        });
    }, { threshold: 0.08 });

    revealTargets.forEach(element => {
        // Above-the-fold content stays visible even before observation runs.
        if (element.getBoundingClientRect().top < window.innerHeight) return;
        element.classList.add("public-reveal-pending");
        observer.observe(element);
    });
    document.addEventListener("focusin", event => {
        const element = event.target.closest(".public-reveal-pending");
        if (element) reveal(element);
    });
    reducedMotion.addEventListener("change", event => {
        if (!event.matches) return;
        observer.disconnect();
        revealTargets.forEach(element => {
            element.classList.remove("public-reveal-pending", "public-reveal-visible");
        });
    });
})();
