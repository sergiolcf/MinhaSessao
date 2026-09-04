let ms_filtroPacienteSessoes = "";
let ms_paginaAgendadas = 1;
let ms_totalPaginasAgendadas = 1;
let ms_paginaHistorico = 1;
let ms_totalPaginasHistorico = 1;
let ms_historicoCarregado = false;

document.addEventListener("DOMContentLoaded", function () {
    const toastEl = document.getElementById("toastSessoes");
    const toastMensagemEl = document.getElementById("toastSessoesMensagem");
    const toast = toastEl ? bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 4000 }) : null;

    function exibirToast(mensagem, sucesso) {
        if (!toastEl || !toastMensagemEl || !toast) return;
        toastEl.classList.remove("text-bg-success", "text-bg-danger");
        toastEl.classList.add(sucesso ? "text-bg-success" : "text-bg-danger");
        toastMensagemEl.textContent = mensagem;
        toast.show();
    }

    const tbodyAgendadas = document.getElementById("tbodySessoesAgendadas");
    const tbodyHistorico = document.getElementById("tbodySessoesHistorico");
    const paginacaoAgendadas = document.getElementById("paginacaoSessoesAgendadas");
    const paginacaoHistorico = document.getElementById("paginacaoSessoesHistorico");
    const filtroPacienteInput = document.getElementById("filtroPacienteSessoesInput");
    const filtroPacienteSugestoesEl = document.getElementById("sugestoesFiltroPacienteSessoes");
    const btnLimparFiltroPaciente = document.getElementById("btnLimparFiltroPacienteSessoes");

    if (!tbodyAgendadas || !tbodyHistorico) return;

    const cardAgendadas = document.getElementById("cardSessoesAgendadas");
    ms_paginaAgendadas = parseInt(cardAgendadas?.dataset.paginaAtual || "1", 10);
    ms_totalPaginasAgendadas = parseInt(cardAgendadas?.dataset.totalPaginas || "1", 10);

    function classeBadge(status) {
        if (status === "Agendada") return "ms-badge-agendada";
        if (status === "Realizada") return "ms-badge-realizada";
        return "ms-badge-cancelada";
    }

    // combinarDataHora/separarDataHora/preencherOpcoesHoraMinuto/configurarSeletorHora/escaparHtml
    // vêm de wwwroot/js/sessao-utils.js (compartilhado com nova-sessao.js e agenda.js)

    function construirLinhaSessao(sessao) {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>
                ${escaparHtml(sessao.data)}
                <div class="ms-dash-table-subtext">${escaparHtml(sessao.hora)}</div>
            </td>
            <td>${escaparHtml(sessao.pacienteNome)}</td>
            <td>${sessao.duracaoMinutos} min</td>
            <td><span class="badge ${classeBadge(sessao.status)}">${escaparHtml(sessao.status)}</span></td>
            <td class="text-end">
                <div class="d-inline-flex gap-2">
                    <button type="button" class="btn btn-sm ms-dash-btn-ficha btn-editar-sessao"
                            data-sessao-id="${sessao.id}"
                            data-sessao-data="${sessao.dataHoraIso}"
                            data-sessao-duracao="${sessao.duracaoMinutos}"
                            data-sessao-status="${sessao.status}">
                        <i class="bi bi-pencil-square"></i> Status/Editar
                    </button>
                    <a href="/Pacientes/Detalhes/${sessao.pacienteId}" class="btn btn-sm ms-dash-btn-ficha">
                        <i class="bi bi-folder2-open"></i> Prontuário
                    </a>
                </div>
            </td>
        `;
        return tr;
    }

    function renderizarLista(tbody, sessoes, aba) {
        tbody.innerHTML = "";

        if (!sessoes || sessoes.length === 0) {
            const icone = aba === "historico" ? "bi-clock-history" : "bi-calendar-week";
            const titulo = aba === "historico" ? "Nenhum histórico ainda" : "Nenhuma sessão agendada";
            const texto = ms_filtroPacienteSessoes
                ? "Nenhuma sessão encontrada para o paciente selecionado."
                : (aba === "historico" ? "As sessões realizadas ou canceladas aparecerão aqui." : "Clique em \"Nova Sessão\" para agendar um atendimento.");

            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td colspan="5">
                    <div class="ms-dash-empty-state">
                        <i class="bi ${icone}"></i>
                        <h5>${titulo}</h5>
                        <p>${texto}</p>
                    </div>
                </td>
            `;
            tbody.appendChild(tr);
            return;
        }

        sessoes.forEach(function (sessao) {
            tbody.appendChild(construirLinhaSessao(sessao));
        });
    }

    function renderizarPaginacao(elemento, paginaAtual, totalPaginas) {
        if (!elemento) return;
        elemento.innerHTML = "";

        if (totalPaginas <= 1) return;

        for (let pagina = 1; pagina <= totalPaginas; pagina++) {
            const li = document.createElement("li");
            li.className = "page-item" + (pagina === paginaAtual ? " active" : "");

            const botao = document.createElement("button");
            botao.type = "button";
            botao.className = "page-link";
            botao.dataset.pagina = String(pagina);
            botao.textContent = String(pagina);

            li.appendChild(botao);
            elemento.appendChild(li);
        }
    }

    async function carregarSessoes(aba, pagina) {
        const tbody = aba === "historico" ? tbodyHistorico : tbodyAgendadas;
        const paginacaoEl = aba === "historico" ? paginacaoHistorico : paginacaoAgendadas;

        try {
            const parametros = new URLSearchParams({ aba, pagina: String(pagina) });
            if (ms_filtroPacienteSessoes) parametros.set("pacienteId", ms_filtroPacienteSessoes);

            const resposta = await fetch(`/Sessoes/BuscarSessoes?${parametros.toString()}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) return;

            renderizarLista(tbody, resultado.sessoes, aba);
            renderizarPaginacao(paginacaoEl, resultado.paginaAtual, resultado.totalPaginas);

            if (aba === "historico") {
                ms_paginaHistorico = resultado.paginaAtual;
                ms_totalPaginasHistorico = resultado.totalPaginas;
                ms_historicoCarregado = true;
            } else {
                ms_paginaAgendadas = resultado.paginaAtual;
                ms_totalPaginasAgendadas = resultado.totalPaginas;
            }
        } catch {
            // Mantém a lista atual em caso de falha de conexão
        }
    }

    function ocultarSugestoesFiltroPaciente() {
        if (!filtroPacienteSugestoesEl) return;
        filtroPacienteSugestoesEl.innerHTML = "";
        filtroPacienteSugestoesEl.classList.add("d-none");
    }

    function selecionarPacienteFiltro(id, nomeCompleto) {
        ms_filtroPacienteSessoes = id;
        if (filtroPacienteInput) filtroPacienteInput.value = nomeCompleto;
        if (btnLimparFiltroPaciente) btnLimparFiltroPaciente.classList.remove("d-none");
        ocultarSugestoesFiltroPaciente();
        carregarSessoes("agendadas", 1);
        carregarSessoes("historico", 1);
    }

    function limparFiltroPaciente() {
        ms_filtroPacienteSessoes = "";
        if (filtroPacienteInput) filtroPacienteInput.value = "";
        if (btnLimparFiltroPaciente) btnLimparFiltroPaciente.classList.add("d-none");
        ocultarSugestoesFiltroPaciente();
        carregarSessoes("agendadas", 1);
        carregarSessoes("historico", 1);
    }

    async function atualizarSugestoesFiltroPaciente(termo) {
        if (!filtroPacienteSugestoesEl) return;

        if (!termo) {
            ocultarSugestoesFiltroPaciente();
            return;
        }

        try {
            const resposta = await fetch(`/Sessoes/BuscarPacientesFiltro?termo=${encodeURIComponent(termo)}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) return;

            if (resultado.pacientes.length === 0) {
                filtroPacienteSugestoesEl.innerHTML = `<div class="ms-filtro-paciente-sugestao text-muted">Nenhum paciente encontrado</div>`;
                filtroPacienteSugestoesEl.classList.remove("d-none");
                return;
            }

            filtroPacienteSugestoesEl.innerHTML = resultado.pacientes.map(function (paciente) {
                return `
                    <button type="button" class="ms-filtro-paciente-sugestao" data-paciente-id="${paciente.id}" data-paciente-nome="${escaparHtml(paciente.nomeCompleto)}">
                        <span class="ms-filtro-paciente-sugestao-nome">${escaparHtml(paciente.nomeCompleto)}</span>
                        <span class="ms-filtro-paciente-sugestao-cpf">${escaparHtml(paciente.cpfFormatado)}</span>
                    </button>
                `;
            }).join("");
            filtroPacienteSugestoesEl.classList.remove("d-none");
        } catch {
            // Mantém a lista atual de sugestões em caso de falha de conexão
        }
    }

    if (filtroPacienteInput) {
        let filtroPacienteDebounce = null;

        filtroPacienteInput.addEventListener("input", function () {
            if (ms_filtroPacienteSessoes) {
                // Qualquer edição no texto invalida a seleção anterior até uma nova sugestão ser escolhida
                ms_filtroPacienteSessoes = "";
                if (btnLimparFiltroPaciente) btnLimparFiltroPaciente.classList.add("d-none");
                carregarSessoes("agendadas", 1);
                carregarSessoes("historico", 1);
            }

            const termo = filtroPacienteInput.value.trim();
            clearTimeout(filtroPacienteDebounce);
            filtroPacienteDebounce = setTimeout(function () {
                atualizarSugestoesFiltroPaciente(termo);
            }, 300);
        });

        filtroPacienteInput.addEventListener("focus", function () {
            const termo = filtroPacienteInput.value.trim();
            if (termo && !ms_filtroPacienteSessoes) atualizarSugestoesFiltroPaciente(termo);
        });

        document.addEventListener("click", function (e) {
            if (!filtroPacienteInput.contains(e.target) && !filtroPacienteSugestoesEl.contains(e.target)) {
                ocultarSugestoesFiltroPaciente();
            }
        });
    }

    if (filtroPacienteSugestoesEl) {
        filtroPacienteSugestoesEl.addEventListener("click", function (e) {
            const botao = e.target.closest(".ms-filtro-paciente-sugestao[data-paciente-id]");
            if (!botao) return;
            selecionarPacienteFiltro(botao.dataset.pacienteId, botao.dataset.pacienteNome);
        });
    }

    if (btnLimparFiltroPaciente) {
        btnLimparFiltroPaciente.addEventListener("click", limparFiltroPaciente);
    }

    if (paginacaoAgendadas) {
        paginacaoAgendadas.addEventListener("click", function (e) {
            const botao = e.target.closest(".page-link");
            if (!botao) return;
            const pagina = parseInt(botao.dataset.pagina, 10);
            if (pagina === ms_paginaAgendadas) return;
            carregarSessoes("agendadas", pagina);
        });
    }

    if (paginacaoHistorico) {
        paginacaoHistorico.addEventListener("click", function (e) {
            const botao = e.target.closest(".page-link");
            if (!botao) return;
            const pagina = parseInt(botao.dataset.pagina, 10);
            if (pagina === ms_paginaHistorico) return;
            carregarSessoes("historico", pagina);
        });
    }

    // Carrega o histórico só na primeira vez que a aba é aberta (a de agendadas já vem renderizada pelo servidor)
    const historicoTabBtn = document.getElementById("historico-tab");
    if (historicoTabBtn) {
        historicoTabBtn.addEventListener("shown.bs.tab", function () {
            if (!ms_historicoCarregado) {
                carregarSessoes("historico", 1);
            }
        });
    }

    // ----- Modal: Editar Sessão -----
    const modalEditarSessaoEl = document.getElementById("modalEditarSessao");
    if (modalEditarSessaoEl) {
        const formEditarSessao = document.getElementById("formEditarSessao");
        const tokenInputEditar = modalEditarSessaoEl.querySelector('input[name="__RequestVerificationToken"]');
        const idInput = document.getElementById("EditarSessaoId");
        const dataHoraInput = document.getElementById("EditarSessaoDataHora");
        const dataInputEditar = document.getElementById("EditarSessaoData");
        const horaInputEditar = document.getElementById("EditarSessaoHora");
        const duracaoInput = document.getElementById("EditarSessaoDuracaoMinutos");
        const statusSelect = document.getElementById("EditarSessaoStatus");
        const anotacoesClinicasInput = document.getElementById("EditarSessaoAnotacoesClinicas");
        const feedbackErroEl = document.getElementById("editarSessaoFeedbackErro");
        const feedbackErroMensagemEl = document.getElementById("editarSessaoFeedbackErroMensagem");
        const btnSalvar = document.getElementById("btnSalvarEditarSessao");
        const btnSalvarSpinner = document.getElementById("btnSalvarEditarSessaoSpinner");
        const btnSalvarTexto = document.getElementById("btnSalvarEditarSessaoTexto");

        function ocultarErro() {
            if (feedbackErroEl) feedbackErroEl.classList.add("d-none");
        }

        function exibirErro(mensagem) {
            if (!feedbackErroEl || !feedbackErroMensagemEl) return;
            feedbackErroMensagemEl.textContent = mensagem;
            feedbackErroEl.classList.remove("d-none");
        }

        function definirCarregando(carregando) {
            if (!btnSalvar) return;
            btnSalvar.disabled = carregando;
            if (btnSalvarSpinner) btnSalvarSpinner.classList.toggle("d-none", !carregando);
            if (btnSalvarTexto) btnSalvarTexto.textContent = carregando ? "Salvando..." : "Salvar Alterações";
        }

        const horaTextoEditar = document.getElementById("EditarSessaoHoraTexto");
        const objetivosContainer = document.getElementById("EditarSessaoObjetivosContainer");

        // Renderiza a lista de objetivos em andamento do paciente, marcando/preenchendo os que já estão
        // vinculados a esta sessão (checkbox + campo de observação escondido até o objetivo ser marcado)
        function renderizarObjetivosSessao(objetivosAtivos, objetivosVinculados) {
            if (!objetivosContainer) return;

            objetivosContainer.innerHTML = "";

            if (!objetivosAtivos || objetivosAtivos.length === 0) {
                objetivosContainer.innerHTML = `<p class="ms-dash-table-subtext mb-0">Nenhum objetivo em andamento para este paciente.</p>`;
                return;
            }

            const vinculados = {};
            (objetivosVinculados || []).forEach(function (v) {
                vinculados[v.objetivoTerapeuticoId] = v.observacao || "";
            });

            objetivosAtivos.forEach(function (objetivo) {
                const marcado = Object.prototype.hasOwnProperty.call(vinculados, objetivo.id);
                const observacao = vinculados[objetivo.id] || "";

                const wrapper = document.createElement("div");
                wrapper.className = "form-check mb-2";
                wrapper.innerHTML = `
                    <input class="form-check-input ms-objetivo-sessao-checkbox" type="checkbox" value="${objetivo.id}" id="objetivoSessaoChk_${objetivo.id}" ${marcado ? "checked" : ""}>
                    <label class="form-check-label" for="objetivoSessaoChk_${objetivo.id}">${escaparHtml(objetivo.titulo)}</label>
                    <div class="mt-1 ms-objetivo-sessao-observacao-wrapper ${marcado ? "" : "d-none"}">
                        <input type="text" class="form-control form-control-sm ms-objetivo-sessao-observacao" placeholder="Observação (opcional)" value="${escaparHtml(observacao)}">
                    </div>
                `;
                objetivosContainer.appendChild(wrapper);
            });
        }

        if (objetivosContainer) {
            objetivosContainer.addEventListener("change", function (e) {
                if (!e.target.classList.contains("ms-objetivo-sessao-checkbox")) return;
                const wrapper = e.target.closest(".form-check").querySelector(".ms-objetivo-sessao-observacao-wrapper");
                if (wrapper) wrapper.classList.toggle("d-none", !e.target.checked);
            });
        }

        function preencherModalEdicao(dados) {
            ocultarErro();
            definirCarregando(false);
            idInput.value = dados.id || "";
            separarDataHora(dados.dataHoraIso, dataInputEditar, horaInputEditar, dataHoraInput, horaTextoEditar);
            duracaoInput.value = dados.duracaoMinutos || "";
            statusSelect.value = dados.status || "Agendada";
            if (anotacoesClinicasInput) anotacoesClinicasInput.value = dados.anotacoesClinicas || "";
            renderizarObjetivosSessao(dados.objetivosAtivos, dados.objetivosVinculados);
        }

        async function abrirModalEdicao(sessaoId) {
            try {
                const resposta = await fetch(`/Sessoes/ObterSessao?id=${encodeURIComponent(sessaoId)}`);
                const resultado = await resposta.json();

                if (!resultado.success) {
                    exibirToast(resultado.message || "Sessão não encontrada.", false);
                    return;
                }

                preencherModalEdicao(resultado);
                bootstrap.Modal.getOrCreateInstance(modalEditarSessaoEl).show();
            } catch {
                exibirToast("Erro de conexão. Verifique sua internet e tente novamente.", false);
            }
        }

        function atualizarDataHoraEditar() {
            combinarDataHora(dataInputEditar, horaInputEditar, dataHoraInput);
        }

        if (dataInputEditar) {
            dataInputEditar.addEventListener("change", atualizarDataHoraEditar);
        }

        configurarSeletorHora({
            textoInput: horaTextoEditar,
            painelEl: document.getElementById("EditarSessaoHoraPainel"),
            selectHora: document.getElementById("EditarSessaoHoraSelectH"),
            selectMinuto: document.getElementById("EditarSessaoHoraSelectM"),
            btnSelecionar: document.getElementById("btnSelecionarEditarSessaoHora"),
            hiddenInput: horaInputEditar,
            aoSelecionar: atualizarDataHoraEditar
        });

        document.addEventListener("click", function (e) {
            const botao = e.target.closest(".btn-editar-sessao");
            if (!botao) return;

            abrirModalEdicao(botao.dataset.sessaoId);
        });

        formEditarSessao.addEventListener("submit", async function (e) {
            e.preventDefault();
            ocultarErro();
            combinarDataHora(dataInputEditar, horaInputEditar, dataHoraInput);
            definirCarregando(true);

            const formData = new FormData(formEditarSessao);
            if (tokenInputEditar) formData.append("__RequestVerificationToken", tokenInputEditar.value);

            if (objetivosContainer) {
                const checkboxesMarcados = objetivosContainer.querySelectorAll(".ms-objetivo-sessao-checkbox:checked");
                checkboxesMarcados.forEach(function (checkbox, indice) {
                    const observacaoInput = checkbox.closest(".form-check").querySelector(".ms-objetivo-sessao-observacao");
                    formData.append(`Objetivos[${indice}].ObjetivoTerapeuticoId`, checkbox.value);
                    formData.append(`Objetivos[${indice}].Observacao`, observacaoInput ? observacaoInput.value : "");
                });
            }

            try {
                const resposta = await fetch("/Sessoes/Atualizar", { method: "POST", body: formData });
                let resultado;
                try {
                    resultado = await resposta.json();
                } catch {
                    resultado = { success: false, message: "Ocorreu um erro inesperado no servidor. Tente novamente." };
                }

                if (resultado.success) {
                    exibirToast(resultado.message || "Sessão atualizada com sucesso!", true);
                    modalEditarSessaoEl.addEventListener("hidden.bs.modal", function aoFecharRecarregar() {
                        modalEditarSessaoEl.removeEventListener("hidden.bs.modal", aoFecharRecarregar);
                        // Remove o parâmetro abrirSessaoId antes de recarregar, senão o modal reabriria sozinho
                        const url = new URL(window.location.href);
                        url.searchParams.delete("abrirSessaoId");
                        window.location.href = url.toString();
                    });
                    bootstrap.Modal.getOrCreateInstance(modalEditarSessaoEl).hide();
                } else {
                    exibirErro(resultado.message || "Não foi possível atualizar a sessão.");
                    definirCarregando(false);
                }
            } catch {
                exibirErro("Erro de conexão. Verifique sua internet e tente novamente.");
                definirCarregando(false);
            }
        });

        // Se a página foi aberta a partir do botão "Abrir" na Ficha do Paciente (?abrirSessaoId=...),
        // busca os dados da sessão específica e já abre a modal de edição preenchida
        const parametrosUrl = new URLSearchParams(window.location.search);
        const abrirSessaoId = parametrosUrl.get("abrirSessaoId");
        if (abrirSessaoId) {
            abrirModalEdicao(abrirSessaoId);
        }
    }
});
