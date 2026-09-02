document.addEventListener("DOMContentLoaded", function () {
    const STORAGE_KEY = "msSidebarCollapsed";
    const toggleBtn = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("dashSidebar");

    if (!toggleBtn || !sidebar) return;

    function aplicarEstado(colapsada) {
        sidebar.classList.toggle("sidebar-collapsed", colapsada);
        toggleBtn.setAttribute("aria-expanded", String(!colapsada));
    }

    // Restaura a preferência salva; sem preferência, inicia recolhida em telas pequenas
    const estadoSalvo = localStorage.getItem(STORAGE_KEY);
    const colapsadaInicial = estadoSalvo !== null ? estadoSalvo === "true" : (window.innerWidth > 0 && window.innerWidth <= 768);
    aplicarEstado(colapsadaInicial);

    toggleBtn.addEventListener("click", function () {
        const novoEstado = !sidebar.classList.contains("sidebar-collapsed");
        aplicarEstado(novoEstado);
        localStorage.setItem(STORAGE_KEY, String(novoEstado));
    });
});
