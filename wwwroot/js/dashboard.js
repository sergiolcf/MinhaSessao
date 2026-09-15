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

// Modal de Confirmação genérico: usado tanto pra confirmar ações destrutivas (window.confirmarAcao,
// ex.: excluir objetivo) quanto pra avisos que o usuário precisa mesmo ler (window.avisarUsuario,
// ex.: por que não é possível excluir um objetivo com sessões vinculadas) — os dois modos reaproveitam
// a mesma estrutura (#modalConfirmarAcao), só alternando os botões do rodapé.
document.addEventListener("DOMContentLoaded", function () {
    const modalConfirmarEl = document.getElementById("modalConfirmarAcao");
    const modalConfirmarMensagemEl = document.getElementById("modalConfirmarAcaoMensagem");
    const btnConfirmarAcaoCancelar = document.getElementById("modalConfirmarAcaoBtnCancelar");
    const btnConfirmarAcaoConfirmar = document.getElementById("btnConfirmarAcaoConfirmar");

    if (!modalConfirmarEl || !modalConfirmarMensagemEl || !btnConfirmarAcaoConfirmar) return;

    const modalConfirmarAcao = bootstrap.Modal.getOrCreateInstance(modalConfirmarEl);
    // Guarda o listener de clique da chamada anterior (de confirmarAcao() OU avisarUsuario(), que
    // reaproveitam o mesmo botão de ação) pra removê-lo antes de registrar um novo — evita empilhar
    // handlers quando as funções são chamadas várias vezes seguidas
    let aoClicarBotaoAcaoAnterior = null;

    // Garante que o modal comece no modo padrão de confirmação (2 botões, "Confirmar" em vermelho) —
    // chamado no início de confirmarAcao() pra desfazer um avisarUsuario() anterior que, senão,
    // deixaria o modal "preso" no modo aviso na próxima chamada de confirmarAcao()
    function restaurarModoConfirmacao() {
        if (btnConfirmarAcaoCancelar) btnConfirmarAcaoCancelar.classList.remove("d-none");
        btnConfirmarAcaoConfirmar.textContent = "Confirmar";
        btnConfirmarAcaoConfirmar.classList.remove("btn-ms-orange");
        btnConfirmarAcaoConfirmar.classList.add("btn-danger");
    }

    window.confirmarAcao = function (mensagem) {
        restaurarModoConfirmacao();
        modalConfirmarMensagemEl.textContent = mensagem;

        if (aoClicarBotaoAcaoAnterior) {
            btnConfirmarAcaoConfirmar.removeEventListener("click", aoClicarBotaoAcaoAnterior);
        }

        return new Promise(function (resolve) {
            let confirmado = false;

            function aoConfirmar() {
                confirmado = true;
                modalConfirmarAcao.hide();
            }

            // "hidden.bs.modal" dispara tanto ao confirmar (hide() acima) quanto ao cancelar/fechar
            // por X, ESC ou clique fora — cobrindo os dois casos com uma única resolução da Promise
            modalConfirmarEl.addEventListener("hidden.bs.modal", function aoFechar() {
                resolve(confirmado);
            }, { once: true });

            aoClicarBotaoAcaoAnterior = aoConfirmar;
            btnConfirmarAcaoConfirmar.addEventListener("click", aoConfirmar);

            modalConfirmarAcao.show();
        });
    };

    // Modo "aviso": mensagem informativa com um único botão ("Entendi") — pensado pra mensagens de
    // erro importantes que o usuário precisa mesmo ler, diferente do toast global (que passa
    // despercebido no canto da tela nesses casos)
    window.avisarUsuario = function (mensagem) {
        modalConfirmarMensagemEl.textContent = mensagem;

        if (btnConfirmarAcaoCancelar) btnConfirmarAcaoCancelar.classList.add("d-none");
        btnConfirmarAcaoConfirmar.textContent = "Entendi";
        btnConfirmarAcaoConfirmar.classList.remove("btn-danger");
        // Reaproveita a cor de destaque do projeto (laranja) em vez do azul padrão do Bootstrap —
        // aqui não é uma ação destrutiva, então não faz sentido manter o btn-danger
        btnConfirmarAcaoConfirmar.classList.add("btn-ms-orange");

        if (aoClicarBotaoAcaoAnterior) {
            btnConfirmarAcaoConfirmar.removeEventListener("click", aoClicarBotaoAcaoAnterior);
        }

        return new Promise(function (resolve) {
            function aoEntendi() {
                modalConfirmarAcao.hide();
            }

            // "hidden.bs.modal" dispara tanto ao clicar em "Entendi" (hide() acima) quanto ao fechar
            // por X, ESC ou clique fora — qualquer uma dessas formas resolve a Promise
            modalConfirmarEl.addEventListener("hidden.bs.modal", function aoFechar() {
                resolve();
            }, { once: true });

            aoClicarBotaoAcaoAnterior = aoEntendi;
            btnConfirmarAcaoConfirmar.addEventListener("click", aoEntendi);

            modalConfirmarAcao.show();
        });
    };
});

// Toast genérico do Dashboard: substitui window.alert nas telas do Dashboard (mesmo padrão de
// exibirToast() já usado em wwwroot/js/painel-configuracoes.js, mas com ids globais)
document.addEventListener("DOMContentLoaded", function () {
    const toastGlobalEl = document.getElementById("toastGlobal");
    const toastGlobalMensagemEl = document.getElementById("toastGlobalMensagem");
    const toastGlobal = toastGlobalEl ? bootstrap.Toast.getOrCreateInstance(toastGlobalEl, { delay: 4000 }) : null;

    window.exibirToastGlobal = function (mensagem, sucesso) {
        if (!toastGlobalEl || !toastGlobalMensagemEl || !toastGlobal) return;
        toastGlobalEl.classList.remove("text-bg-success", "text-bg-danger");
        toastGlobalEl.classList.add(sucesso ? "text-bg-success" : "text-bg-danger");
        toastGlobalMensagemEl.textContent = mensagem;
        toastGlobal.show();
    };
});
