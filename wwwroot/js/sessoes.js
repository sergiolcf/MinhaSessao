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

    // ----- Modal: Nova Sessão -----
    const modalNovaSessaoEl = document.getElementById("modalNovaSessao");
    if (modalNovaSessaoEl) {
        const formNovaSessao = document.getElementById("formNovaSessao");
        const tokenInputNova = modalNovaSessaoEl.querySelector('input[name="__RequestVerificationToken"]');
        const feedbackErroEl = document.getElementById("novaSessaoFeedbackErro");
        const feedbackErroMensagemEl = document.getElementById("novaSessaoFeedbackErroMensagem");
        const btnSalvar = document.getElementById("btnSalvarNovaSessao");
        const btnSalvarSpinner = document.getElementById("btnSalvarNovaSessaoSpinner");
        const btnSalvarTexto = document.getElementById("btnSalvarNovaSessaoTexto");

        function ocultarErro() {
            if (feedbackErroEl) feedbackErroEl.classList.add("d-none");
        }

        function exibirErro(mensagem) {
            if (!feedbackErroEl || !feedbackErroMensagemEl) return;
            feedbackErroMensagemEl.textContent = mensagem;
            feedbackErroEl.classList.remove("d-none");
        }

        const dataInputNova = document.getElementById("NovaSessaoData");
        const horaInputNova = document.getElementById("NovaSessaoHora");
        const dataHoraHiddenNova = document.getElementById("NovaSessaoDataHora");

        function atualizarDataHoraNova() {
            combinarDataHora(dataInputNova, horaInputNova, dataHoraHiddenNova);
        }

        if (dataInputNova) {
            dataInputNova.addEventListener("change", atualizarDataHoraNova);
        }

        const seletorHoraNova = configurarSeletorHora({
            textoInput: document.getElementById("NovaSessaoHoraTexto"),
            painelEl: document.getElementById("NovaSessaoHoraPainel"),
            selectHora: document.getElementById("NovaSessaoHoraSelectH"),
            selectMinuto: document.getElementById("NovaSessaoHoraSelectM"),
            btnSelecionar: document.getElementById("btnSelecionarNovaSessaoHora"),
            hiddenInput: horaInputNova,
            aoSelecionar: atualizarDataHoraNova
        });

        function definirCarregando(carregando) {
            if (!btnSalvar) return;
            btnSalvar.disabled = carregando;
            if (btnSalvarSpinner) btnSalvarSpinner.classList.toggle("d-none", !carregando);
            if (btnSalvarTexto) btnSalvarTexto.textContent = carregando ? "Agendando..." : "Agendar Sessão";
        }

        // ----- Lookup de Paciente (campo de busca + sugestões + popup "Procurar") -----
        const pacienteIdInput = document.getElementById("NovaSessaoPacienteId");
        const pacienteBuscaInput = document.getElementById("NovaSessaoPacienteBusca");
        const pacienteSugestoesEl = document.getElementById("sugestoesNovaSessaoPaciente");
        const btnLimparPaciente = document.getElementById("btnLimparNovaSessaoPaciente");
        const btnProcurarPaciente = document.getElementById("btnProcurarNovaSessaoPaciente");
        const modalProcurarPacienteEl = document.getElementById("modalProcurarPacienteSessao");
        const buscaModalProcurarInput = document.getElementById("buscaModalProcurarPaciente");
        const listaModalProcurarEl = document.getElementById("listaModalProcurarPaciente");
        const listaModalProcurarVaziaEl = document.getElementById("listaModalProcurarPacienteVazia");

        function ocultarSugestoesPaciente() {
            if (!pacienteSugestoesEl) return;
            pacienteSugestoesEl.innerHTML = "";
            pacienteSugestoesEl.classList.add("d-none");
        }

        function selecionarPaciente(id, nomeCompleto) {
            if (pacienteIdInput) pacienteIdInput.value = id;
            if (pacienteBuscaInput) pacienteBuscaInput.value = nomeCompleto;
            if (btnLimparPaciente) btnLimparPaciente.classList.remove("d-none");
            ocultarSugestoesPaciente();
        }

        function limparPacienteSelecionado() {
            if (pacienteIdInput) pacienteIdInput.value = "";
            if (pacienteBuscaInput) pacienteBuscaInput.value = "";
            if (btnLimparPaciente) btnLimparPaciente.classList.add("d-none");
            ocultarSugestoesPaciente();
        }

        async function atualizarSugestoesPaciente(termo) {
            if (!pacienteSugestoesEl) return;

            if (!termo) {
                ocultarSugestoesPaciente();
                return;
            }

            try {
                const resposta = await fetch(`/Sessoes/BuscarPacientesFiltro?termo=${encodeURIComponent(termo)}`);
                const resultado = await resposta.json();

                if (!resposta.ok || !resultado.success) return;

                if (resultado.pacientes.length === 0) {
                    pacienteSugestoesEl.innerHTML = `<div class="ms-filtro-paciente-sugestao text-muted">Nenhum paciente encontrado</div>`;
                    pacienteSugestoesEl.classList.remove("d-none");
                    return;
                }

                pacienteSugestoesEl.innerHTML = resultado.pacientes.map(function (paciente) {
                    return `
                        <button type="button" class="ms-filtro-paciente-sugestao" data-paciente-id="${paciente.id}" data-paciente-nome="${escaparHtml(paciente.nomeCompleto)}">
                            <span class="ms-filtro-paciente-sugestao-nome">${escaparHtml(paciente.nomeCompleto)}</span>
                            <span class="ms-filtro-paciente-sugestao-cpf">${escaparHtml(paciente.cpfFormatado)}</span>
                        </button>
                    `;
                }).join("");
                pacienteSugestoesEl.classList.remove("d-none");
            } catch {
                // Mantém as sugestões atuais em caso de falha de conexão
            }
        }

        if (pacienteBuscaInput) {
            let pacienteDebounce = null;

            pacienteBuscaInput.addEventListener("input", function () {
                if (pacienteIdInput && pacienteIdInput.value) {
                    pacienteIdInput.value = "";
                    if (btnLimparPaciente) btnLimparPaciente.classList.add("d-none");
                }

                const termo = pacienteBuscaInput.value.trim();
                clearTimeout(pacienteDebounce);
                pacienteDebounce = setTimeout(function () {
                    atualizarSugestoesPaciente(termo);
                }, 300);
            });

            pacienteBuscaInput.addEventListener("focus", function () {
                const termo = pacienteBuscaInput.value.trim();
                if (termo && !(pacienteIdInput && pacienteIdInput.value)) atualizarSugestoesPaciente(termo);
            });

            document.addEventListener("click", function (e) {
                if (!pacienteBuscaInput.contains(e.target) && pacienteSugestoesEl && !pacienteSugestoesEl.contains(e.target)) {
                    ocultarSugestoesPaciente();
                }
            });
        }

        if (pacienteSugestoesEl) {
            pacienteSugestoesEl.addEventListener("click", function (e) {
                const botao = e.target.closest(".ms-filtro-paciente-sugestao[data-paciente-id]");
                if (!botao) return;
                selecionarPaciente(botao.dataset.pacienteId, botao.dataset.pacienteNome);
            });
        }

        if (btnLimparPaciente) {
            btnLimparPaciente.addEventListener("click", limparPacienteSelecionado);
        }

        if (btnProcurarPaciente && modalProcurarPacienteEl) {
            const filtrarListaModalProcurar = function (termo) {
                if (!listaModalProcurarEl) return;
                const termoNome = termo.trim().toLowerCase();
                const termoCpf = termo.replace(/\D/g, "");
                let algumVisivel = false;

                listaModalProcurarEl.querySelectorAll(".ms-lookup-paciente-item").forEach(function (item) {
                    const nome = (item.dataset.pacienteNome || "").toLowerCase();
                    const cpf = item.dataset.pacienteCpfNormalizado || "";
                    const combina = !termoNome && !termoCpf ? true : (nome.includes(termoNome) || (termoCpf && cpf.includes(termoCpf)));
                    item.classList.toggle("d-none", !combina);
                    if (combina) algumVisivel = true;
                });

                if (listaModalProcurarVaziaEl) listaModalProcurarVaziaEl.classList.toggle("d-none", algumVisivel);
                listaModalProcurarEl.classList.toggle("d-none", !algumVisivel);
            };

            btnProcurarPaciente.addEventListener("click", function () {
                bootstrap.Modal.getOrCreateInstance(modalProcurarPacienteEl).show();
            });

            modalProcurarPacienteEl.addEventListener("show.bs.modal", function () {
                if (buscaModalProcurarInput) buscaModalProcurarInput.value = "";
                filtrarListaModalProcurar("");
            });

            modalProcurarPacienteEl.addEventListener("shown.bs.modal", function () {
                if (buscaModalProcurarInput) buscaModalProcurarInput.focus();
            });

            if (buscaModalProcurarInput) {
                buscaModalProcurarInput.addEventListener("input", function () {
                    filtrarListaModalProcurar(buscaModalProcurarInput.value);
                });
            }

            if (listaModalProcurarEl) {
                listaModalProcurarEl.addEventListener("click", function (e) {
                    const botao = e.target.closest(".ms-lookup-paciente-item[data-paciente-id]");
                    if (!botao) return;
                    selecionarPaciente(botao.dataset.pacienteId, botao.dataset.pacienteNome);
                    bootstrap.Modal.getOrCreateInstance(modalProcurarPacienteEl).hide();
                });
            }
        }

        modalNovaSessaoEl.addEventListener("show.bs.modal", function () {
            ocultarErro();
            formNovaSessao.reset();
            limparPacienteSelecionado();
            if (dataHoraHiddenNova) dataHoraHiddenNova.value = "";
            if (seletorHoraNova) seletorHoraNova.definirValor("");
        });

        formNovaSessao.addEventListener("submit", async function (e) {
            e.preventDefault();
            ocultarErro();
            combinarDataHora(dataInputNova, horaInputNova, dataHoraHiddenNova);
            definirCarregando(true);

            const formData = new FormData(formNovaSessao);
            if (tokenInputNova) formData.append("__RequestVerificationToken", tokenInputNova.value);

            try {
                const resposta = await fetch("/Sessoes/Criar", { method: "POST", body: formData });
                let resultado;
                try {
                    resultado = await resposta.json();
                } catch {
                    resultado = { success: false, message: "Ocorreu um erro inesperado no servidor. Tente novamente." };
                }

                if (resultado.success) {
                    bootstrap.Modal.getOrCreateInstance(modalNovaSessaoEl).hide();
                    exibirToast(resultado.message || "Sessão agendada com sucesso!", true);
                    window.location.reload();
                } else {
                    exibirErro(resultado.message || "Não foi possível agendar a sessão.");
                    definirCarregando(false);
                }
            } catch {
                exibirErro("Erro de conexão. Verifique sua internet e tente novamente.");
                definirCarregando(false);
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

        function preencherModalEdicao(id, dataHoraIso, duracaoMinutos, status) {
            ocultarErro();
            definirCarregando(false);
            idInput.value = id || "";
            separarDataHora(dataHoraIso, dataInputEditar, horaInputEditar, dataHoraInput, horaTextoEditar);
            duracaoInput.value = duracaoMinutos || "";
            statusSelect.value = status || "Agendada";
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

            preencherModalEdicao(botao.dataset.sessaoId, botao.dataset.sessaoData, botao.dataset.sessaoDuracao, botao.dataset.sessaoStatus);
            bootstrap.Modal.getOrCreateInstance(modalEditarSessaoEl).show();
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
                    bootstrap.Modal.getOrCreateInstance(modalEditarSessaoEl).hide();
                    exibirToast(resultado.message || "Sessão atualizada com sucesso!", true);
                    window.location.reload();
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
            fetch(`/Sessoes/ObterSessao?id=${encodeURIComponent(abrirSessaoId)}`)
                .then(resposta => resposta.json())
                .then(resultado => {
                    if (resultado.success) {
                        preencherModalEdicao(resultado.id, resultado.dataHoraIso, resultado.duracaoMinutos, resultado.status);
                        bootstrap.Modal.getOrCreateInstance(modalEditarSessaoEl).show();
                    }
                })
                .catch(() => {});
        }
    }
});
