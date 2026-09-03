let ms_paginaAtualAnotacoes = 1;
let ms_totalPaginasAnotacoes = 1;
let ms_buscaAtualAnotacoes = "";
let ms_ordemAtualAnotacoes = "recente";

document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("formAnotacaoConfidencial");
    const anotacoesContainer = document.getElementById("anotacoesContainer");
    const listaAnotacoes = document.getElementById("listaAnotacoes");
    const paginacaoEl = document.getElementById("paginacaoAnotacoes");
    const buscaTituloInput = document.getElementById("buscaAnotacaoTitulo");
    const sugestoesTituloEl = document.getElementById("sugestoesTituloAnotacao");
    const ordenacaoSelect = document.getElementById("ordenacaoAnotacoes");
    const modalEl = document.getElementById("modalNovaAnotacao");
    const modalTituloEl = document.getElementById("modalNovaAnotacaoTitulo");
    const anotacaoIdInput = document.getElementById("anotacaoId");
    const anotacaoTituloInput = document.getElementById("anotacaoTitulo");
    const anotacaoConteudoInput = document.getElementById("anotacaoConteudo");
    const feedbackErroEl = document.getElementById("anotacaoFeedbackErro");
    const feedbackErroMensagemEl = document.getElementById("anotacaoFeedbackErroMensagem");
    const btnSalvar = document.getElementById("btnSalvarAnotacao");
    const btnSalvarSpinner = document.getElementById("btnSalvarAnotacaoSpinner");
    const btnSalvarTexto = document.getElementById("btnSalvarAnotacaoTexto");

    if (!anotacoesContainer || !listaAnotacoes || !form) return;

    const pacienteId = anotacoesContainer.dataset.pacienteId;
    ms_paginaAtualAnotacoes = parseInt(anotacoesContainer.dataset.paginaAtual || "1", 10);
    ms_totalPaginasAnotacoes = parseInt(anotacoesContainer.dataset.totalPaginas || "1", 10);

    function definirCarregando(carregando) {
        if (!btnSalvar) return;
        btnSalvar.disabled = carregando;
        if (btnSalvarSpinner) btnSalvarSpinner.classList.toggle("d-none", !carregando);
        if (btnSalvarTexto) {
            const modoEdicao = !!anotacaoIdInput.value;
            btnSalvarTexto.textContent = carregando ? "Salvando..." : (modoEdicao ? "Salvar Alterações" : "Salvar Anotação");
        }
    }

    function ocultarErro() {
        if (feedbackErroEl) feedbackErroEl.classList.add("d-none");
    }

    function exibirErro(mensagem) {
        if (!feedbackErroEl || !feedbackErroMensagemEl) return;
        feedbackErroMensagemEl.textContent = mensagem;
        feedbackErroEl.classList.remove("d-none");
    }

    function resetarModalParaCriacao() {
        ocultarErro();
        form.reset();
        anotacaoIdInput.value = "";
        if (modalTituloEl) modalTituloEl.textContent = "Nova Anotação Confidencial";
        if (btnSalvarTexto) btnSalvarTexto.textContent = "Salvar Anotação";
    }

    // Função global usada pelos botões de edição de cada card da timeline
    window.abrirModalEdicao = function (id, titulo, conteudo) {
        ocultarErro();
        anotacaoIdInput.value = id;
        anotacaoTituloInput.value = titulo || "";
        anotacaoConteudoInput.value = conteudo || "";
        if (modalTituloEl) modalTituloEl.textContent = "Editar Anotação Confidencial";
        if (btnSalvarTexto) btnSalvarTexto.textContent = "Salvar Alterações";
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    };

    // Sempre que a modal é aberta pelo botão "+ Nova Anotação" (sem passar por abrirModalEdicao), garante o modo de criação
    if (modalEl) {
        modalEl.addEventListener("show.bs.modal", function (e) {
            const gatilho = e.relatedTarget;
            if (gatilho && gatilho.hasAttribute("data-bs-toggle")) {
                resetarModalParaCriacao();
            }
        });
    }

    // Escapa texto para uso seguro tanto em conteúdo HTML quanto em valores de atributos (aspas incluídas)
    function escaparHtml(texto) {
        return (texto || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function criarCardAnotacao(anotacao) {
        const item = document.createElement("div");
        item.className = "ms-timeline-item";
        item.dataset.anotacaoId = anotacao.id;

        const titulo = anotacao.titulo && anotacao.titulo.trim() !== "" ? anotacao.titulo : "Anotação";

        item.innerHTML = `
            <span class="ms-timeline-marker"></span>
            <div class="ms-timeline-content">
                <div class="ms-timeline-header">
                    <strong class="ms-timeline-title">${escaparHtml(titulo)}</strong>
                    <div class="ms-timeline-actions">
                        <span class="ms-timeline-date">${escaparHtml(anotacao.dataRegistro)}</span>
                        <button type="button" class="ms-timeline-edit" data-anotacao-id="${anotacao.id}" data-titulo="${escaparHtml(anotacao.titulo || "")}" data-conteudo="${escaparHtml(anotacao.conteudo)}" title="Editar anotação">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                        <button type="button" class="ms-timeline-delete" data-anotacao-id="${anotacao.id}" title="Excluir anotação">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </div>
                <p class="ms-timeline-body">${escaparHtml(anotacao.conteudo)}</p>
            </div>
        `;

        return item;
    }

    function renderizarAnotacoes(anotacoes) {
        listaAnotacoes.innerHTML = "";

        if (!anotacoes || anotacoes.length === 0) {
            const mensagem = ms_buscaAtualAnotacoes
                ? `<h5>Nenhuma anotação encontrada</h5><p>Não há anotações com título contendo "${escaparHtml(ms_buscaAtualAnotacoes)}".</p>`
                : `<h5>Nenhuma anotação registrada</h5><p>Clique em "Nova Anotação" para registrar a primeira anotação confidencial.</p>`;

            listaAnotacoes.innerHTML = `
                <div class="ms-dash-empty-state" id="anotacoesEmptyState">
                    <i class="bi bi-journal-lock"></i>
                    ${mensagem}
                </div>
            `;
            return;
        }

        anotacoes.forEach(function (anotacao) {
            listaAnotacoes.appendChild(criarCardAnotacao(anotacao));
        });
    }

    function renderizarPaginacao(paginaAtual, totalPaginas) {
        if (!paginacaoEl) return;

        paginacaoEl.innerHTML = "";

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
            paginacaoEl.appendChild(li);
        }
    }

    async function carregarPagina(pagina) {
        try {
            const parametros = new URLSearchParams({
                pacienteId: pacienteId,
                pagina: String(pagina),
                ordem: ms_ordemAtualAnotacoes
            });
            if (ms_buscaAtualAnotacoes) parametros.set("busca", ms_buscaAtualAnotacoes);

            const resposta = await fetch(`/Pacientes/BuscarAnotacoes?${parametros.toString()}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) return;

            ms_paginaAtualAnotacoes = resultado.paginaAtual;
            ms_totalPaginasAnotacoes = resultado.totalPaginas;

            renderizarAnotacoes(resultado.anotacoes);
            renderizarPaginacao(resultado.paginaAtual, resultado.totalPaginas);
        } catch (erro) {
            // Mantém a lista atual em caso de falha de conexão ao trocar de página
        }
    }

    // Debounce simples: evita disparar uma requisição a cada milissegundo enquanto o usuário digita
    function debounce(fn, atrasoMs) {
        let temporizador;
        return function (...args) {
            clearTimeout(temporizador);
            temporizador = setTimeout(() => fn.apply(this, args), atrasoMs);
        };
    }

    async function atualizarSugestoesTitulo(termo) {
        if (!sugestoesTituloEl) return;

        if (!termo) {
            sugestoesTituloEl.innerHTML = "";
            return;
        }

        try {
            const parametros = new URLSearchParams({ pacienteId, termo });
            const resposta = await fetch(`/Pacientes/SugerirTitulosAnotacao?${parametros.toString()}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) return;

            sugestoesTituloEl.innerHTML = resultado.titulos
                .map(titulo => `<option value="${escaparHtml(titulo)}"></option>`)
                .join("");
        } catch (erro) {
            // Sem sugestões em caso de falha de conexão; a busca principal continua funcionando
        }
    }

    if (buscaTituloInput) {
        const dispararBusca = debounce(function () {
            ms_buscaAtualAnotacoes = buscaTituloInput.value.trim();
            carregarPagina(1);
        }, 300);

        const dispararSugestoes = debounce(function () {
            atualizarSugestoesTitulo(buscaTituloInput.value.trim());
        }, 150);

        buscaTituloInput.addEventListener("input", function () {
            dispararBusca();
            dispararSugestoes();
        });
    }

    if (ordenacaoSelect) {
        ordenacaoSelect.addEventListener("change", function () {
            ms_ordemAtualAnotacoes = ordenacaoSelect.value;
            carregarPagina(1);
        });
    }

    form.addEventListener("submit", async function (e) {
        e.preventDefault();
        ocultarErro();
        definirCarregando(true);

        const modoEdicao = !!anotacaoIdInput.value;
        const url = modoEdicao ? "/Pacientes/AtualizarAnotacao" : "/Pacientes/SalvarAnotacao";
        const formData = new FormData(form);

        try {
            const resposta = await fetch(url, {
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
                resetarModalParaCriacao();

                if (modoEdicao) {
                    // Atualiza o card em tela sem precisar refazer a busca (a ordenação não muda)
                    const item = listaAnotacoes.querySelector(`[data-anotacao-id="${resultado.anotacao.id}"]`);
                    if (item) {
                        item.replaceWith(criarCardAnotacao(resultado.anotacao));
                    } else {
                        await carregarPagina(ms_paginaAtualAnotacoes);
                    }
                } else {
                    // Nova anotação sempre aparece primeiro: volta para a página 1
                    await carregarPagina(1);
                }
            } else {
                exibirErro(resultado.message || "Não foi possível salvar a anotação.");
            }
        } catch (erro) {
            exibirErro("Erro de conexão. Verifique sua internet e tente novamente.");
        } finally {
            definirCarregando(false);
        }
    });

    listaAnotacoes.addEventListener("click", async function (e) {
        const botaoEditar = e.target.closest(".ms-timeline-edit");
        if (botaoEditar) {
            window.abrirModalEdicao(botaoEditar.dataset.anotacaoId, botaoEditar.dataset.titulo, botaoEditar.dataset.conteudo);
            return;
        }

        const botaoExcluir = e.target.closest(".ms-timeline-delete");
        if (botaoExcluir) {
            if (!window.confirm("Tem certeza que deseja excluir esta anotação?")) return;

            const anotacaoId = botaoExcluir.dataset.anotacaoId;
            const token = document.querySelector('#formAnotacaoConfidencial input[name="__RequestVerificationToken"]')?.value;

            const formData = new FormData();
            formData.append("id", anotacaoId);
            if (token) formData.append("__RequestVerificationToken", token);

            try {
                const resposta = await fetch("/Pacientes/ExcluirAnotacao", {
                    method: "POST",
                    body: formData
                });

                const resultado = await resposta.json();

                if (resposta.ok && resultado.success) {
                    // Recarrega a página atual; se ficou vazia e não é a primeira, volta uma página
                    const paginaAlvo = ms_paginaAtualAnotacoes;
                    await carregarPagina(paginaAlvo);

                    if (!listaAnotacoes.querySelector(".ms-timeline-item") && paginaAlvo > 1) {
                        await carregarPagina(paginaAlvo - 1);
                    }
                } else {
                    window.alert(resultado.message || "Não foi possível excluir a anotação.");
                }
            } catch (erro) {
                window.alert("Erro de conexão. Verifique sua internet e tente novamente.");
            }
        }
    });

    if (paginacaoEl) {
        paginacaoEl.addEventListener("click", async function (e) {
            const botao = e.target.closest(".page-link");
            if (!botao) return;

            const pagina = parseInt(botao.dataset.pagina, 10);
            if (pagina === ms_paginaAtualAnotacoes) return;

            await carregarPagina(pagina);
        });
    }
});
