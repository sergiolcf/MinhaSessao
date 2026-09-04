// Utilitários de Data/Hora e escaping compartilhados por sessoes.js, nova-sessao.js e agenda.js.
// Ficam em escopo global (fora de qualquer DOMContentLoaded) justamente para serem visíveis nos
// outros arquivos, que são carregados como scripts separados na mesma página.

// Campo Data usa o input nativo type="date" (fecha o próprio popup ao escolher o dia).
// Campo Hora é um seletor próprio (texto somente-leitura + painel com dois <select> e botão "Selecionar")
// porque o time picker nativo do Chrome/Edge não fecha sozinho enquanto hora e minuto não são escolhidos.
function combinarDataHora(dataInput, horaInput, hiddenInput) {
    if (!dataInput || !horaInput || !hiddenInput) return;
    const data = dataInput.value;
    const hora = horaInput.value;
    hiddenInput.value = data && hora ? `${data}T${hora}` : "";
}

function separarDataHora(iso, dataInput, horaInput, hiddenInput, horaTextoInput) {
    if (hiddenInput) hiddenInput.value = iso || "";
    const [data, hora] = (iso || "").split("T");
    if (dataInput) dataInput.value = data || "";
    if (horaInput) horaInput.value = hora || "";
    if (horaTextoInput) horaTextoInput.value = hora || "";
}

function preencherOpcoesHoraMinuto(selectHora, selectMinuto) {
    if (selectHora && selectHora.options.length === 0) {
        for (let h = 0; h < 24; h++) {
            const opcao = document.createElement("option");
            opcao.value = String(h).padStart(2, "0");
            opcao.textContent = String(h).padStart(2, "0");
            selectHora.appendChild(opcao);
        }
    }
    if (selectMinuto && selectMinuto.options.length === 0) {
        for (let m = 0; m < 60; m++) {
            const opcao = document.createElement("option");
            opcao.value = String(m).padStart(2, "0");
            opcao.textContent = String(m).padStart(2, "0");
            selectMinuto.appendChild(opcao);
        }
    }
}

// Monta o seletor de hora custom: abre/fecha um painel próprio (não é o time picker nativo do navegador)
// e só aplica o valor quando o botão "Selecionar" é clicado.
function configurarSeletorHora({ textoInput, painelEl, selectHora, selectMinuto, btnSelecionar, hiddenInput, aoSelecionar }) {
    if (!textoInput || !painelEl || !selectHora || !selectMinuto || !btnSelecionar || !hiddenInput) return null;

    preencherOpcoesHoraMinuto(selectHora, selectMinuto);

    function abrirPainel() {
        const [horaAtual, minutoAtual] = (hiddenInput.value || "").split(":");
        selectHora.value = horaAtual || "00";
        selectMinuto.value = minutoAtual || "00";
        painelEl.classList.remove("d-none");
    }

    function fecharPainel() {
        painelEl.classList.add("d-none");
    }

    textoInput.addEventListener("click", function () {
        if (painelEl.classList.contains("d-none")) abrirPainel(); else fecharPainel();
    });

    btnSelecionar.addEventListener("click", function () {
        const valor = `${selectHora.value}:${selectMinuto.value}`;
        hiddenInput.value = valor;
        textoInput.value = valor;
        fecharPainel();
        if (aoSelecionar) aoSelecionar();
    });

    document.addEventListener("click", function (e) {
        if (!textoInput.contains(e.target) && !painelEl.contains(e.target)) fecharPainel();
    });

    return {
        definirValor(valor) {
            hiddenInput.value = valor || "";
            textoInput.value = valor || "";
        }
    };
}

function escaparHtml(texto) {
    return (texto || "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}
