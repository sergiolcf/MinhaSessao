// Tela "Planos de Tratamento": mesma lógica de CRUD de wwwroot/js/plano-tratamento.js (aba "Plano de
// Tratamento" da Ficha do Paciente), adaptada pra uma lista cross-paciente com filtros de
// Paciente/Período/Status em vez de um pacienteId fixo vindo da URL.
(function () {
    let ms_combinadosNovoObjetivo = [];
    let ms_filtroPacienteId = "";
    let ms_filtroDataInicio = "";
    let ms_filtroDataFim = "";
    let ms_filtroStatus = "todos";

    document.addEventListener("DOMContentLoaded", function () {
        const objetivosContainer = document.getElementById("objetivosContainer");
        const listaObjetivos = document.getElementById("listaObjetivos");
        const form = document.getElementById("formNovoObjetivo");
        const modalEl = document.getElementById("modalNovoObjetivo");
        const objetivoPacienteSelect = document.getElementById("objetivoPacienteId");
        const objetivoTituloInput = document.getElementById("objetivoTitulo");
        const objetivoDescricaoInput = document.getElementById("objetivoDescricao");
        const novoCombinadoInput = document.getElementById("novoCombinadoInput");
        const btnAdicionarCombinado = document.getElementById("btnAdicionarCombinado");
        const listaCombinadosForm = document.getElementById("listaCombinadosForm");
        const feedbackErroEl = document.getElementById("objetivoFeedbackErro");
        const feedbackErroMensagemEl = document.getElementById("objetivoFeedbackErroMensagem");
        const btnSalvar = document.getElementById("btnSalvarObjetivo");
        const btnSalvarSpinner = document.getElementById("btnSalvarObjetivoSpinner");
        const btnSalvarTexto = document.getElementById("btnSalvarObjetivoTexto");

        // Toolbar de filtros
        const filtroPacienteSelect = document.getElementById("filtroObjetivoPaciente");
        const filtroDataInicioInput = document.getElementById("filtroObjetivoDataInicio");
        const filtroDataFimInput = document.getElementById("filtroObjetivoDataFim");
        const btnLimparPeriodo = document.getElementById("btnLimparPeriodoObjetivo");
        const filtroStatusSelect = document.getElementById("filtroObjetivoStatus");

        // Modal "Visualizar Sessão" (somente leitura), aberto ao clicar na data de um item do Histórico de Sessões
        const modalVisualizarSessaoEl = document.getElementById("modalVisualizarSessao");
        const modalVisualizarSessaoCodigoEl = document.getElementById("modalVisualizarSessaoCodigo");
        const modalVisualizarSessaoDataEl = document.getElementById("modalVisualizarSessaoData");
        const modalVisualizarSessaoDuracaoEl = document.getElementById("modalVisualizarSessaoDuracao");
        const modalVisualizarSessaoStatusEl = document.getElementById("modalVisualizarSessaoStatus");
        const modalVisualizarSessaoObjetivosWrapperEl = document.getElementById("modalVisualizarSessaoObjetivosWrapper");
        const modalVisualizarSessaoObjetivosEl = document.getElementById("modalVisualizarSessaoObjetivos");
        const modalVisualizarSessaoAnotacoesEl = document.getElementById("modalVisualizarSessaoAnotacoes");

        if (!objetivosContainer || !listaObjetivos || !form) return;

        const rotulosStatus = {
            EmAndamento: "Em Andamento",
            Atingido: "Atingido",
            Pausado: "Pausado",
            Cancelado: "Cancelado"
        };

        const classesBadgeStatus = {
            EmAndamento: "bg-info",
            Atingido: "bg-success",
            Pausado: "bg-warning text-dark",
            Cancelado: "bg-secondary"
        };

        // Escapa texto para uso seguro tanto em conteúdo HTML quanto em valores de atributos (aspas incluídas)
        function escaparHtml(texto) {
            return (texto || "")
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;")
                .replace(/'/g, "&#39;");
        }

        function obterToken() {
            return document.querySelector('#formNovoObjetivo input[name="__RequestVerificationToken"]')?.value;
        }

        function definirCarregando(carregando) {
            if (!btnSalvar) return;
            btnSalvar.disabled = carregando;
            if (btnSalvarSpinner) btnSalvarSpinner.classList.toggle("d-none", !carregando);
            if (btnSalvarTexto) btnSalvarTexto.textContent = carregando ? "Salvando..." : "Salvar Objetivo";
        }

        function ocultarErro() {
            if (feedbackErroEl) feedbackErroEl.classList.add("d-none");
        }

        function exibirErro(mensagem) {
            if (!feedbackErroEl || !feedbackErroMensagemEl) return;
            feedbackErroMensagemEl.textContent = mensagem;
            feedbackErroEl.classList.remove("d-none");
        }

        // Renderiza a lista (em memória) de combinados já adicionados dentro do modal "Novo Objetivo"
        function renderizarCombinadosForm() {
            if (!listaCombinadosForm) return;

            listaCombinadosForm.innerHTML = ms_combinadosNovoObjetivo
                .map(function (descricao, indice) {
                    return `
                        <li>
                            <span>${escaparHtml(descricao)}</span>
                            <button type="button" class="ms-combinado-remover" data-indice="${indice}" title="Remover">
                                <i class="bi bi-x-lg"></i>
                            </button>
                        </li>
                    `;
                })
                .join("");
        }

        function adicionarLinhaCombinado() {
            if (!novoCombinadoInput) return;

            const descricao = novoCombinadoInput.value.trim();
            if (!descricao) return;

            ms_combinadosNovoObjetivo.push(descricao);
            novoCombinadoInput.value = "";
            renderizarCombinadosForm();
            novoCombinadoInput.focus();
        }

        function resetarModalNovoObjetivo() {
            ocultarErro();
            form.reset();
            ms_combinadosNovoObjetivo = [];
            renderizarCombinadosForm();
        }

        if (modalEl) {
            modalEl.addEventListener("show.bs.modal", resetarModalNovoObjetivo);
        }

        if (btnAdicionarCombinado) {
            btnAdicionarCombinado.addEventListener("click", adicionarLinhaCombinado);
        }

        if (novoCombinadoInput) {
            novoCombinadoInput.addEventListener("keydown", function (e) {
                if (e.key === "Enter") {
                    e.preventDefault();
                    adicionarLinhaCombinado();
                }
            });
        }

        if (listaCombinadosForm) {
            listaCombinadosForm.addEventListener("click", function (e) {
                const botaoRemover = e.target.closest(".ms-combinado-remover");
                if (!botaoRemover) return;

                const indice = parseInt(botaoRemover.dataset.indice, 10);
                ms_combinadosNovoObjetivo.splice(indice, 1);
                renderizarCombinadosForm();
            });
        }

        function criarCardObjetivo(objetivo) {
            const card = document.createElement("div");
            card.className = "ms-dash-card ms-objetivo-card";
            card.dataset.objetivoId = objetivo.id;

            const badgeClasse = classesBadgeStatus[objetivo.status] || "bg-secondary";
            const rotuloStatus = rotulosStatus[objetivo.status] || objetivo.status;
            const percentual = objetivo.totalCombinados > 0
                ? Math.round((objetivo.combinadosConcluidos / objetivo.totalCombinados) * 100)
                : 0;

            // Itens do dropdown de status (badge clicável no cabeçalho do card) — o item que
            // corresponde ao status atual ganha um ícone de check, já que não há mais um <select>
            // separado mostrando esse valor (evita duplicidade de exibição do mesmo status)
            const opcoesStatusDropdown = Object.keys(rotulosStatus)
                .map(function (valor) {
                    const ehStatusAtual = valor === objetivo.status;
                    return `
                        <li>
                            <button class="dropdown-item" type="button" data-objetivo-id="${objetivo.id}" data-status="${valor}">
                                ${ehStatusAtual ? '<i class="bi bi-check-lg"></i> ' : ""}${rotulosStatus[valor]}
                            </button>
                        </li>
                    `;
                })
                .join("");

            const itensCombinados = (objetivo.combinados || [])
                .map(function (combinado) {
                    const idCheckbox = `combinado_${combinado.id}`;
                    return `
                        <li class="ms-combinado-item">
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" id="${idCheckbox}" data-combinado-id="${combinado.id}" ${combinado.concluido ? "checked" : ""}>
                                <label class="form-check-label${combinado.concluido ? " ms-combinado-concluido" : ""}" for="${idCheckbox}">${escaparHtml(combinado.descricao)}</label>
                            </div>
                        </li>
                    `;
                })
                .join("");

            const totalSessoesVinculadas = objetivo.totalSessoesVinculadas || 0;
            const idHistorico = `historicoSessoesObjetivo_${objetivo.id}`;

            const historicoHtml = totalSessoesVinculadas === 0
                ? `<p class="ms-objetivo-historico-vazio">Nenhuma sessão registrada ainda</p>`
                : `
                    <button type="button" class="ms-objetivo-historico-toggle" data-bs-toggle="collapse" data-bs-target="#${idHistorico}">
                        <i class="bi bi-clock-history"></i> Histórico de Sessões (${totalSessoesVinculadas})
                    </button>
                    <div class="collapse ms-objetivo-historico-collapse" id="${idHistorico}" data-objetivo-id="${objetivo.id}" data-carregado="false" data-pagina-atual="0" data-total-paginas="1">
                        <ul class="ms-objetivo-historico-lista"></ul>
                        <button type="button" class="btn btn-sm btn-link ms-objetivo-historico-carregar-mais d-none">Carregar mais</button>
                    </div>
                `;

            // Sem nenhum combinado ainda, a barra de progresso não tem o que mostrar — omitida pra
            // não sobrar um bloco vazio/redundante no card
            const progressoHtml = objetivo.totalCombinados > 0
                ? `
                    <div class="ms-objetivo-progresso">
                        <div class="d-flex justify-content-between ms-objetivo-progresso-label">
                            <span>Progresso</span>
                            <span>${objetivo.combinadosConcluidos} de ${objetivo.totalCombinados} combinados</span>
                        </div>
                        <div class="progress" role="progressbar" aria-valuenow="${percentual}" aria-valuemin="0" aria-valuemax="100">
                            <div class="progress-bar" style="width: ${percentual}%"></div>
                        </div>
                    </div>
                `
                : "";

            card.innerHTML = `
                <div class="ms-objetivo-card-header">
                    <div>
                        <a href="${objetivo.pacienteUrl}" class="ms-objetivo-card-paciente">
                            <i class="bi bi-person"></i> ${escaparHtml(objetivo.pacienteNome)}
                        </a>
                        <h6 class="ms-objetivo-titulo">${escaparHtml(objetivo.titulo)}</h6>
                    </div>
                    <div class="dropdown ms-objetivo-status-dropdown">
                        <button type="button" class="badge ${badgeClasse} dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false" data-objetivo-id="${objetivo.id}" title="Alterar status">
                            ${rotuloStatus}
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end">
                            ${opcoesStatusDropdown}
                        </ul>
                    </div>
                </div>
                ${objetivo.descricao ? `<p class="ms-objetivo-descricao">${escaparHtml(objetivo.descricao)}</p>` : ""}
                ${progressoHtml}
                <ul class="ms-combinado-lista">
                    ${itensCombinados}
                </ul>
                <div class="ms-objetivo-historico">
                    ${historicoHtml}
                </div>
                <div class="ms-objetivo-card-footer">
                    <button type="button" class="btn btn-sm btn-outline-danger ms-objetivo-excluir" data-objetivo-id="${objetivo.id}" title="Excluir objetivo">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            `;

            return card;
        }

        function renderizarObjetivos(objetivos) {
            listaObjetivos.innerHTML = "";

            if (!objetivos || objetivos.length === 0) {
                const filtroAtivo = ms_filtroPacienteId || ms_filtroDataInicio || ms_filtroDataFim || ms_filtroStatus !== "todos";
                const mensagem = filtroAtivo
                    ? `<h5>Nenhum objetivo encontrado</h5><p>Não há objetivos terapêuticos com os filtros selecionados.</p>`
                    : `<h5>Nenhum objetivo encontrado</h5><p>Ajuste os filtros acima ou clique em "Novo Objetivo" para registrar o primeiro.</p>`;

                listaObjetivos.innerHTML = `
                    <div class="ms-dash-empty-state" id="objetivosEmptyState">
                        <i class="bi bi-bullseye"></i>
                        ${mensagem}
                    </div>
                `;
                return;
            }

            objetivos.forEach(function (objetivo) {
                listaObjetivos.appendChild(criarCardObjetivo(objetivo));
            });
        }

        async function carregarObjetivos() {
            try {
                const parametros = new URLSearchParams({ status: ms_filtroStatus });
                if (ms_filtroPacienteId) parametros.set("pacienteId", ms_filtroPacienteId);
                if (ms_filtroDataInicio) parametros.set("dataInicio", ms_filtroDataInicio);
                if (ms_filtroDataFim) parametros.set("dataFim", ms_filtroDataFim);

                const resposta = await fetch(`/PlanosTratamento/Buscar?${parametros.toString()}`);
                const resultado = await resposta.json();

                if (!resposta.ok || !resultado.success) return;

                renderizarObjetivos(resultado.objetivos);
            } catch (erro) {
                // Mantém o estado atual em caso de falha de conexão
            }
        }

        function atualizarBotaoLimparPeriodo() {
            if (!btnLimparPeriodo) return;
            btnLimparPeriodo.classList.toggle("d-none", !(ms_filtroDataInicio || ms_filtroDataFim));
        }

        if (filtroPacienteSelect) {
            filtroPacienteSelect.addEventListener("change", function () {
                ms_filtroPacienteId = filtroPacienteSelect.value;
                carregarObjetivos();
            });
        }

        if (filtroDataInicioInput) {
            filtroDataInicioInput.addEventListener("change", function () {
                ms_filtroDataInicio = filtroDataInicioInput.value;
                atualizarBotaoLimparPeriodo();
                carregarObjetivos();
            });
        }

        if (filtroDataFimInput) {
            filtroDataFimInput.addEventListener("change", function () {
                ms_filtroDataFim = filtroDataFimInput.value;
                atualizarBotaoLimparPeriodo();
                carregarObjetivos();
            });
        }

        if (btnLimparPeriodo) {
            btnLimparPeriodo.addEventListener("click", function () {
                ms_filtroDataInicio = "";
                ms_filtroDataFim = "";
                if (filtroDataInicioInput) filtroDataInicioInput.value = "";
                if (filtroDataFimInput) filtroDataFimInput.value = "";
                atualizarBotaoLimparPeriodo();
                carregarObjetivos();
            });
        }

        if (filtroStatusSelect) {
            filtroStatusSelect.addEventListener("change", function () {
                ms_filtroStatus = filtroStatusSelect.value;
                carregarObjetivos();
            });
        }

        async function salvarObjetivo() {
            ocultarErro();
            definirCarregando(true);

            try {
                // Envia via FormData (mesmo padrão de model binding + antiforgery do resto do projeto);
                // campos repetidos "Combinados" fazem o binder do MVC montar a List<string> do ViewModel
                const formData = new FormData();
                formData.append("PacienteId", objetivoPacienteSelect.value);
                formData.append("Titulo", objetivoTituloInput.value.trim());
                formData.append("Descricao", objetivoDescricaoInput.value.trim());
                ms_combinadosNovoObjetivo.forEach(function (descricao) {
                    formData.append("Combinados", descricao);
                });
                const token = obterToken();
                if (token) formData.append("__RequestVerificationToken", token);

                const resposta = await fetch("/Pacientes/SalvarObjetivo", {
                    method: "POST",
                    body: formData
                });

                let resultado;
                try {
                    resultado = await resposta.json();
                } catch {
                    resultado = { success: false, message: "Ocorreu um erro inesperado no servidor. Tente novamente." };
                }

                if (resposta.ok && resultado.success) {
                    bootstrap.Modal.getOrCreateInstance(modalEl).hide();
                    resetarModalNovoObjetivo();
                    await carregarObjetivos();
                } else {
                    exibirErro(resultado.message || "Não foi possível salvar o objetivo.");
                }
            } catch (erro) {
                exibirErro("Erro de conexão. Verifique sua internet e tente novamente.");
            } finally {
                definirCarregando(false);
            }
        }

        async function alternarCombinado(id, checkboxEl) {
            const formData = new FormData();
            formData.append("id", id);
            const token = obterToken();
            if (token) formData.append("__RequestVerificationToken", token);

            try {
                const resposta = await fetch("/Pacientes/AlternarCombinado", {
                    method: "POST",
                    body: formData
                });

                const resultado = await resposta.json();

                if (resposta.ok && resultado.success) {
                    const label = checkboxEl.closest(".form-check")?.querySelector("label");
                    if (label) label.classList.toggle("ms-combinado-concluido", resultado.concluido);

                    // Progresso do card recalculado a partir do backend (fonte da verdade)
                    await carregarObjetivos();
                } else {
                    checkboxEl.checked = !checkboxEl.checked;
                    await avisarUsuario(resultado.message || "Não foi possível atualizar o combinado.");
                }
            } catch (erro) {
                checkboxEl.checked = !checkboxEl.checked;
                await avisarUsuario("Erro de conexão. Verifique sua internet e tente novamente.");
            }
        }

        // O status agora é alterado pelo dropdown do próprio badge no cabeçalho do card, não mais
        // por um <select> à parte — por isso não recebe mais um elemento de referência: qualquer
        // atualização (sucesso ou erro) já recarrega o card inteiro via carregarObjetivos()
        async function atualizarStatusObjetivo(id, status) {
            const formData = new FormData();
            formData.append("id", id);
            formData.append("status", status);
            const token = obterToken();
            if (token) formData.append("__RequestVerificationToken", token);

            try {
                const resposta = await fetch("/Pacientes/AtualizarStatusObjetivo", {
                    method: "POST",
                    body: formData
                });

                const resultado = await resposta.json();

                if (resposta.ok && resultado.success) {
                    await carregarObjetivos();
                } else {
                    await avisarUsuario(resultado.message || "Não foi possível atualizar o status.");
                    await carregarObjetivos();
                }
            } catch (erro) {
                await avisarUsuario("Erro de conexão. Verifique sua internet e tente novamente.");
            }
        }

        async function carregarHistoricoSessoes(collapseEl, pagina) {
            const objetivoId = collapseEl.dataset.objetivoId;
            const listaEl = collapseEl.querySelector(".ms-objetivo-historico-lista");
            const btnCarregarMais = collapseEl.querySelector(".ms-objetivo-historico-carregar-mais");

            try {
                const parametros = new URLSearchParams({ objetivoId, pagina });
                const resposta = await fetch(`/Pacientes/ListarSessoesDoObjetivo?${parametros.toString()}`);
                const resultado = await resposta.json();

                if (!resposta.ok || !resultado.success) return;

                const itensHtml = (resultado.sessoes || [])
                    .map(function (sessao) {
                        return `
                            <li class="ms-objetivo-historico-item">
                                <button type="button" class="ms-objetivo-historico-data-link" data-sessao-id="${sessao.sessaoId}">${escaparHtml(sessao.codigo)} &mdash; ${escaparHtml(sessao.dataHora)}</button>
                                ${sessao.observacao ? `<span class="ms-objetivo-historico-observacao">${escaparHtml(sessao.observacao)}</span>` : ""}
                            </li>
                        `;
                    })
                    .join("");

                if (pagina === 1) {
                    listaEl.innerHTML = itensHtml;
                } else {
                    listaEl.insertAdjacentHTML("beforeend", itensHtml);
                }

                collapseEl.dataset.carregado = "true";
                collapseEl.dataset.paginaAtual = String(resultado.paginaAtual);
                collapseEl.dataset.totalPaginas = String(resultado.totalPaginas);

                if (btnCarregarMais) {
                    btnCarregarMais.classList.toggle("d-none", resultado.paginaAtual >= resultado.totalPaginas);
                }
            } catch (erro) {
                // Mantém o estado atual em caso de falha de conexão
            }
        }

        const classesBadgeStatusSessao = {
            Agendada: "ms-badge-agendada",
            Realizada: "ms-badge-realizada",
            Cancelada: "ms-badge-cancelada"
        };

        // Busca os dados da sessão (mesmo endpoint usado pelo modal de edição em Minhas Sessões) e
        // exibe num popup somente leitura, sem nenhum campo editável nem botão de salvar
        async function abrirModalVisualizarSessao(sessaoId) {
            if (!modalVisualizarSessaoEl) return;

            try {
                const resposta = await fetch(`/Sessoes/ObterSessao?id=${encodeURIComponent(sessaoId)}`);
                const dados = await resposta.json();

                if (!resposta.ok || !dados.success) {
                    await avisarUsuario(dados.message || "Não foi possível carregar os detalhes da sessão.");
                    return;
                }

                if (modalVisualizarSessaoCodigoEl) {
                    modalVisualizarSessaoCodigoEl.textContent = dados.codigo || "-";
                }

                const [dataParte, horaParte] = (dados.dataHoraIso || "").split("T");
                const [ano, mes, dia] = (dataParte || "").split("-");
                if (modalVisualizarSessaoDataEl) {
                    modalVisualizarSessaoDataEl.textContent = dia && mes && ano ? `${dia}/${mes}/${ano} às ${horaParte}` : "-";
                }
                if (modalVisualizarSessaoDuracaoEl) {
                    modalVisualizarSessaoDuracaoEl.textContent = `${dados.duracaoMinutos} min`;
                }
                if (modalVisualizarSessaoStatusEl) {
                    modalVisualizarSessaoStatusEl.className = `badge ${classesBadgeStatusSessao[dados.status] || "ms-badge-cancelada"}`;
                    modalVisualizarSessaoStatusEl.textContent = dados.status || "-";
                }

                const objetivosVinculados = dados.objetivosVinculados || [];
                if (modalVisualizarSessaoObjetivosWrapperEl) {
                    modalVisualizarSessaoObjetivosWrapperEl.classList.toggle("d-none", objetivosVinculados.length === 0);
                }
                if (modalVisualizarSessaoObjetivosEl) {
                    modalVisualizarSessaoObjetivosEl.innerHTML = objetivosVinculados
                        .map(function (objetivo) {
                            const icone = objetivo.observacao
                                ? `<i class="bi bi-info-circle" title="${escaparHtml(objetivo.observacao)}"></i>`
                                : "";
                            return `<span class="ms-objetivo-chip">${escaparHtml(objetivo.titulo)}${icone}</span>`;
                        })
                        .join("");
                }

                if (modalVisualizarSessaoAnotacoesEl) {
                    var anotacoes = dados.anotacoes || [];
                    modalVisualizarSessaoAnotacoesEl.innerHTML = anotacoes.length === 0
                        ? '<p class="mb-0">Nenhuma anotação registrada.</p>'
                        : anotacoes.map(function (anotacao) {
                            return '<div><div class="fw-semibold">' + escaparHtml(anotacao.titulo) + '</div><p class="mb-0">' + escaparHtml(anotacao.conteudo) + '</p></div>';
                        }).join("");
                }

                bootstrap.Modal.getOrCreateInstance(modalVisualizarSessaoEl).show();
            } catch (erro) {
                await avisarUsuario("Erro de conexão. Verifique sua internet e tente novamente.");
            }
        }

        async function excluirObjetivo(id) {
            if (!(await confirmarAcao("Tem certeza que deseja excluir este objetivo? Os combinados dele também serão removidos."))) return;

            const formData = new FormData();
            formData.append("id", id);
            const token = obterToken();
            if (token) formData.append("__RequestVerificationToken", token);

            try {
                const resposta = await fetch("/Pacientes/ExcluirObjetivo", {
                    method: "POST",
                    body: formData
                });

                const resultado = await resposta.json();

                if (resposta.ok && resultado.success) {
                    await carregarObjetivos();
                } else {
                    await avisarUsuario(resultado.message || "Não foi possível excluir o objetivo.");
                }
            } catch (erro) {
                await avisarUsuario("Erro de conexão. Verifique sua internet e tente novamente.");
            }
        }

        form.addEventListener("submit", function (e) {
            e.preventDefault();

            if (!objetivoPacienteSelect.value) {
                exibirErro("Selecione o paciente.");
                return;
            }
            if (!objetivoTituloInput.value.trim()) {
                exibirErro("Informe o título do objetivo.");
                return;
            }
            salvarObjetivo();
        });

        listaObjetivos.addEventListener("change", function (e) {
            const checkbox = e.target.closest('input[type="checkbox"][data-combinado-id]');
            if (checkbox) {
                alternarCombinado(checkbox.dataset.combinadoId, checkbox);
            }
        });

        listaObjetivos.addEventListener("click", function (e) {
            const botaoExcluir = e.target.closest(".ms-objetivo-excluir");
            if (botaoExcluir) {
                excluirObjetivo(botaoExcluir.dataset.objetivoId);
                return;
            }

            // Item do dropdown de status (badge clicável no cabeçalho do card)
            const botaoStatus = e.target.closest(".ms-objetivo-status-dropdown .dropdown-item[data-objetivo-id]");
            if (botaoStatus) {
                atualizarStatusObjetivo(botaoStatus.dataset.objetivoId, botaoStatus.dataset.status);
                return;
            }

            const botaoCarregarMais = e.target.closest(".ms-objetivo-historico-carregar-mais");
            if (botaoCarregarMais) {
                const collapseEl = botaoCarregarMais.closest(".ms-objetivo-historico-collapse");
                const proximaPagina = parseInt(collapseEl.dataset.paginaAtual, 10) + 1;
                carregarHistoricoSessoes(collapseEl, proximaPagina);
                return;
            }

            const botaoData = e.target.closest(".ms-objetivo-historico-data-link");
            if (botaoData) {
                abrirModalVisualizarSessao(botaoData.dataset.sessaoId);
            }
        });

        listaObjetivos.addEventListener("show.bs.collapse", function (e) {
            const collapseEl = e.target;
            if (!collapseEl.classList.contains("ms-objetivo-historico-collapse")) return;
            if (collapseEl.dataset.carregado === "true") return;

            carregarHistoricoSessoes(collapseEl, 1);
        });

        renderizarCombinadosForm();
        carregarObjetivos();
    });
})();
