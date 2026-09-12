let ms_filtroPacienteSessoes = "";
let ms_paginaAgendadas = 1;
let ms_totalPaginasAgendadas = 1;
let ms_paginaHistorico = 1;
let ms_totalPaginasHistorico = 1;
let ms_historicoCarregado = false;
let ms_paginaEmAndamento = 1;
let ms_totalPaginasEmAndamento = 1;
let ms_emAndamentoCarregado = false;

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
    const tbodyEmAndamento = document.getElementById("tbodySessoesEmAndamento");
    const paginacaoAgendadas = document.getElementById("paginacaoSessoesAgendadas");
    const paginacaoHistorico = document.getElementById("paginacaoSessoesHistorico");
    const paginacaoEmAndamento = document.getElementById("paginacaoSessoesEmAndamento");
    const filtroPacienteInput = document.getElementById("filtroPacienteSessoesInput");
    const filtroPacienteSugestoesEl = document.getElementById("sugestoesFiltroPacienteSessoes");
    const btnLimparFiltroPaciente = document.getElementById("btnLimparFiltroPacienteSessoes");

    if (!tbodyAgendadas || !tbodyHistorico) return;

    const cardAgendadas = document.getElementById("cardSessoesAgendadas");
    ms_paginaAgendadas = parseInt(cardAgendadas?.dataset.paginaAtual || "1", 10);
    ms_totalPaginasAgendadas = parseInt(cardAgendadas?.dataset.totalPaginas || "1", 10);

    function classeBadge(status) {
        if (status === "Agendada") return "ms-badge-agendada";
        if (status === "EmAndamento") return "ms-badge-em-andamento";
        if (status === "Realizada") return "ms-badge-realizada";
        return "ms-badge-cancelada";
    }

    // Enum.ToString() não tem espaço ("EmAndamento") — só esse valor precisa de um texto de exibição próprio
    function textoStatus(status) {
        return status === "EmAndamento" ? "Em Andamento" : status;
    }

    // combinarDataHora/separarDataHora/preencherOpcoesHoraMinuto/configurarSeletorHora/escaparHtml
    // vêm de wwwroot/js/sessao-utils.js (compartilhado com nova-sessao.js e agenda.js)

    function construirLinhaSessao(sessao) {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>
                <span class="ms-dash-paciente-link">
                    <span class="ms-avatar-iniciais">${escaparHtml(sessao.iniciais)}</span>
                    <span>${escaparHtml(sessao.pacienteNome)}</span>
                </span>
            </td>
            <td>
                ${escaparHtml(sessao.data)}
                <div class="ms-dash-table-hora"><i class="bi bi-clock"></i> ${escaparHtml(sessao.hora)}</div>
            </td>
            <td>${sessao.duracaoMinutos} min</td>
            <td>
                <span class="ms-badge-codigo">${escaparHtml(sessao.codigo)}</span>
            </td>
            <td><span class="badge ${classeBadge(sessao.status)}">${escaparHtml(textoStatus(sessao.status))}</span></td>
            <td class="text-end">
                <div class="d-inline-flex gap-1">
                    <button type="button" class="ms-dash-row-link btn-editar-sessao"
                            data-sessao-id="${sessao.id}"
                            data-sessao-data="${sessao.dataHoraIso}"
                            data-sessao-duracao="${sessao.duracaoMinutos}"
                            data-sessao-status="${sessao.status}"
                            title="Status/Editar">
                        <i class="bi bi-pencil-square"></i>
                    </button>
                    <a href="/Sessoes/Sessao/${sessao.id}" class="ms-dash-row-link" title="Anotações Clínicas / Objetivos">
                        <i class="bi bi-journal-medical"></i>
                    </a>
                    <a href="/Pacientes/Detalhes/${sessao.pacienteId}" class="ms-dash-row-link" title="Prontuário">
                        <i class="bi bi-folder2-open"></i>
                    </a>
                </div>
            </td>
        `;
        return tr;
    }

    function renderizarLista(tbody, sessoes, aba) {
        tbody.innerHTML = "";

        if (!sessoes || sessoes.length === 0) {
            const icone = aba === "historico" ? "bi-clock-history" : (aba === "em_andamento" ? "bi-broadcast" : "bi-calendar-week");
            const titulo = aba === "historico" ? "Nenhum histórico ainda" : (aba === "em_andamento" ? "Nenhuma sessão em andamento no momento" : "Nenhuma sessão agendada");
            const texto = ms_filtroPacienteSessoes
                ? "Nenhuma sessão encontrada para o paciente selecionado."
                : (aba === "historico"
                    ? "As sessões realizadas ou canceladas aparecerão aqui."
                    : (aba === "em_andamento"
                        ? "Inicie uma sessão pelo card \"Próxima Sessão\" para acompanhá-la aqui."
                        : "Clique em \"Nova Sessão\" para agendar um atendimento."));

            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td colspan="6">
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

    function tbodyDaAba(aba) {
        if (aba === "historico") return tbodyHistorico;
        if (aba === "em_andamento") return tbodyEmAndamento;
        return tbodyAgendadas;
    }

    function paginacaoDaAba(aba) {
        if (aba === "historico") return paginacaoHistorico;
        if (aba === "em_andamento") return paginacaoEmAndamento;
        return paginacaoAgendadas;
    }

    async function carregarSessoes(aba, pagina) {
        const tbody = tbodyDaAba(aba);
        const paginacaoEl = paginacaoDaAba(aba);

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
            } else if (aba === "em_andamento") {
                ms_paginaEmAndamento = resultado.paginaAtual;
                ms_totalPaginasEmAndamento = resultado.totalPaginas;
                ms_emAndamentoCarregado = true;
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
        carregarSessoes("em_andamento", 1);
    }

    function limparFiltroPaciente() {
        ms_filtroPacienteSessoes = "";
        if (filtroPacienteInput) filtroPacienteInput.value = "";
        if (btnLimparFiltroPaciente) btnLimparFiltroPaciente.classList.add("d-none");
        ocultarSugestoesFiltroPaciente();
        carregarSessoes("agendadas", 1);
        carregarSessoes("historico", 1);
        carregarSessoes("em_andamento", 1);
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
                carregarSessoes("em_andamento", 1);
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

    if (paginacaoEmAndamento) {
        paginacaoEmAndamento.addEventListener("click", function (e) {
            const botao = e.target.closest(".page-link");
            if (!botao) return;
            const pagina = parseInt(botao.dataset.pagina, 10);
            if (pagina === ms_paginaEmAndamento) return;
            carregarSessoes("em_andamento", pagina);
        });
    }

    // Carrega o histórico/em andamento só na primeira vez que a aba é aberta (a de agendadas já vem renderizada pelo servidor)
    const historicoTabBtn = document.getElementById("historico-tab");
    if (historicoTabBtn) {
        historicoTabBtn.addEventListener("shown.bs.tab", function () {
            if (!ms_historicoCarregado) {
                carregarSessoes("historico", 1);
            }
        });
    }

    const emAndamentoTabBtn = document.getElementById("em-andamento-tab");
    if (emAndamentoTabBtn) {
        emAndamentoTabBtn.addEventListener("shown.bs.tab", function () {
            if (!ms_emAndamentoCarregado) {
                carregarSessoes("em_andamento", 1);
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

        function preencherModalEdicao(dados) {
            ocultarErro();
            definirCarregando(false);
            idInput.value = dados.id || "";
            separarDataHora(dados.dataHoraIso, dataInputEditar, horaInputEditar, dataHoraInput, horaTextoEditar);
            duracaoInput.value = dados.duracaoMinutos || "";
            statusSelect.value = dados.status || "Agendada";
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
                        window.location.reload();
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

        // ----- Card "Próxima Sessão": confirmação de início de atendimento -----
        const cardProximaSessao = document.getElementById("cardProximaSessao");
        const modalConfirmarIniciarSessaoEl = document.getElementById("modalConfirmarIniciarSessao");
        const btnIniciarProximaSessao = document.getElementById("btnIniciarProximaSessao");
        const btnIniciarProximaSessaoSpinner = document.getElementById("btnIniciarProximaSessaoSpinner");
        const btnIniciarProximaSessaoTexto = document.getElementById("btnIniciarProximaSessaoTexto");
        const btnEditarProximaSessao = document.getElementById("btnEditarProximaSessao");

        // Só liga a interação quando o card realmente tem uma sessão (data-sessao-id vem vazio quando não há)
        if (cardProximaSessao && modalConfirmarIniciarSessaoEl && cardProximaSessao.dataset.sessaoId) {
            const modalConfirmarIniciarSessao = bootstrap.Modal.getOrCreateInstance(modalConfirmarIniciarSessaoEl);

            function definirCarregandoIniciar(carregando) {
                if (!btnIniciarProximaSessao) return;
                btnIniciarProximaSessao.disabled = carregando;
                if (btnIniciarProximaSessaoSpinner) btnIniciarProximaSessaoSpinner.classList.toggle("d-none", !carregando);
                if (btnIniciarProximaSessaoTexto) btnIniciarProximaSessaoTexto.textContent = carregando ? "Iniciando..." : "Iniciar";
            }

            cardProximaSessao.addEventListener("click", function () {
                modalConfirmarIniciarSessao.show();
            });

            if (btnEditarProximaSessao) {
                btnEditarProximaSessao.addEventListener("click", function () {
                    const sessaoId = cardProximaSessao.dataset.sessaoId;
                    modalConfirmarIniciarSessaoEl.addEventListener("hidden.bs.modal", function aoFecharAbrirEdicao() {
                        modalConfirmarIniciarSessaoEl.removeEventListener("hidden.bs.modal", aoFecharAbrirEdicao);
                        abrirModalEdicao(sessaoId);
                    });
                    modalConfirmarIniciarSessao.hide();
                });
            }

            if (btnIniciarProximaSessao) {
                btnIniciarProximaSessao.addEventListener("click", async function () {
                    const sessaoId = cardProximaSessao.dataset.sessaoId;
                    definirCarregandoIniciar(true);

                    const formData = new FormData();
                    formData.append("id", sessaoId);
                    if (tokenInputEditar) formData.append("__RequestVerificationToken", tokenInputEditar.value);

                    try {
                        const resposta = await fetch("/Sessoes/IniciarSessao", { method: "POST", body: formData });
                        let resultado;
                        try {
                            resultado = await resposta.json();
                        } catch {
                            resultado = { success: false, message: "Ocorreu um erro inesperado no servidor. Tente novamente." };
                        }

                        if (resultado.success) {
                            // Recarrega a página já na aba "Em Andamento" — os cards de estatística e a
                            // "Próxima Sessão" dependem de uma consulta nova ao servidor pra recalcular
                            const url = new URL(window.location.href);
                            url.searchParams.set("abaAtiva", "em_andamento");
                            window.location.href = url.toString();
                        } else {
                            definirCarregandoIniciar(false);
                            modalConfirmarIniciarSessao.hide();
                            exibirToast(resultado.message || "Não foi possível iniciar a sessão.", false);
                        }
                    } catch {
                        definirCarregandoIniciar(false);
                        exibirToast("Erro de conexão. Verifique sua internet e tente novamente.", false);
                    }
                });
            }
        }

        // Se a URL pede pra abrir direto na aba "Em Andamento" (ex.: logo após iniciar uma sessão), ativa a aba
        const abaAtivaParam = parametrosUrl.get("abaAtiva");
        if (abaAtivaParam === "em_andamento" && emAndamentoTabBtn) {
            bootstrap.Tab.getOrCreateInstance(emAndamentoTabBtn).show();
            const url = new URL(window.location.href);
            url.searchParams.delete("abaAtiva");
            window.history.replaceState({}, "", url.toString());
        }
    }
});
