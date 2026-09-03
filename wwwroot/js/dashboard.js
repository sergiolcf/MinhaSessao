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

// Modal de Senha Temporária: usado tanto no cadastro de paciente quanto na regeneração de senha (Ficha do Paciente)
document.addEventListener("DOMContentLoaded", function () {
    const modalEl = document.getElementById("modalSenhaTemporaria");
    if (!modalEl) return;

    const inputSenha = document.getElementById("senhaTemporariaValor");
    const btnCopiar = document.getElementById("btnCopiarSenhaTemporaria");
    const mensagemCopiado = document.getElementById("senhaTemporariaCopiadoMsg");
    let callbackAoFechar = null;

    window.exibirSenhaTemporaria = function (senha, aoFechar) {
        callbackAoFechar = typeof aoFechar === "function" ? aoFechar : null;
        if (inputSenha) inputSenha.value = senha;
        if (mensagemCopiado) mensagemCopiado.classList.add("d-none");
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    };

    if (btnCopiar) {
        btnCopiar.addEventListener("click", async function () {
            if (!inputSenha) return;
            try {
                await navigator.clipboard.writeText(inputSenha.value);
            } catch {
                inputSenha.select();
                document.execCommand("copy");
            }
            if (mensagemCopiado) mensagemCopiado.classList.remove("d-none");
        });
    }

    modalEl.addEventListener("hidden.bs.modal", function () {
        if (inputSenha) inputSenha.value = "";
        if (mensagemCopiado) mensagemCopiado.classList.add("d-none");
        if (callbackAoFechar) {
            const callback = callbackAoFechar;
            callbackAoFechar = null;
            callback();
        }
    });
});
