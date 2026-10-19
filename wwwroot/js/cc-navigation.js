(() => {
    const menu = document.getElementById("cc-discover");
    if (!menu) return;
    const summary = menu.querySelector("summary");
    document.addEventListener("click", event => { if (!menu.contains(event.target)) menu.open = false; });
    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && menu.open) { menu.open = false; summary.focus(); }
    });
    menu.addEventListener("focusout", event => {
        if (!menu.contains(event.relatedTarget)) menu.open = false;
    });
})();
