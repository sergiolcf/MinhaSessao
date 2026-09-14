// Lógica da tela dedicada de conteúdo clínico da sessão (Views/Sessoes/Sessao.cshtml): CRUD de
// Anotações Clínicas / Evolução — painel de composição à esquerda (título + objetivos trabalhados
// + conteúdo, tudo salvo junto num único clique) + lista de anotações já registradas à direita.
// Data/Hora, Duração e Status continuam exclusivos do modal "Editar Sessão" (Minhas Sessões).
function escaparHtml(texto) {
    return (texto || "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

document.addEventListener("DOMContentLoaded", function () {
    const composeEl = document.getElementById("anotacaoSessaoCompose");
    const listaEl = document.getElementById("anotacaoSessaoLista");
    if (!composeEl || !listaEl) return;

    const toastEl = document.getElementById("toastSessaoDetalhe");
    const toastMensagemEl = document.getElementById("toastSessaoDetalheMensagem");
    const toast = toastEl ? bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 4000 }) : null;

    function exibirToast(mensagem, sucesso) {
        if (!toastEl || !toastMensagemEl || !toast) return;
        toastEl.classList.remove("text-bg-success", "text-bg-danger");
        toastEl.classList.add(sucesso ? "text-bg-success" : "text-bg-danger");
        toastMensagemEl.textContent = mensagem;
        toast.show();
    }

    const sessaoIdInput = document.getElementById("AnotacaoSessaoSessaoId");
    const sessaoId = sessaoIdInput ? sessaoIdInput.value : "";
    const tokenInput = document.querySelector('#tokenAnotacaoSessao input[name="__RequestVerificationToken"]');

    let objetivosAtivos = [];
    const objetivosAtivosDataEl = document.getElementById("objetivosAtivosData");
    if (objetivosAtivosDataEl) {
        try {
            objetivosAtivos = JSON.parse(objetivosAtivosDataEl.textContent) || [];
        } catch {
            objetivosAtivos = [];
        }
    }

    let anotacoes = [];
    let composing = false;
    let editId = null;
    let draftTitulo = "";
    let draftConteudo = "";
    let draftObjetivosIds = [];
    // Estado "visualização": guarda o id da anotação mostrada em modo somente leitura no painel da
    // esquerda (ver renderVisualizacao) — separado de "composing"/"editId" porque não é um rascunho
    // editável, é só o conteúdo já salvo sendo exibido. Por isso temRascunho() (abaixo) ignora esse
    // estado: trocar de anotação visualizada, ou começar uma nova, não precisa de aviso de descarte.
    let visualizandoId = null;

    function temRascunho() {
        return composing && (draftTitulo.trim() !== "" || draftConteudo.trim() !== "");
    }

    // O botão "Criar anotação" agora é fixo no cabeçalho do card (fora de composeEl), não dentro
    // do estado vazio — precisa só ficar desabilitado enquanto já existe uma composição em andamento,
    // pra não abrir duas ao mesmo tempo
    function atualizarEstadoBotaoFixo() {
        const btnFixo = document.getElementById("btnCriarAnotacaoSessaoFixo");
        if (btnFixo) btnFixo.disabled = composing;
    }

    function renderIdle() {
        visualizandoId = null;
        composeEl.innerHTML = `
            <div class="ms-anotacao-sessao-vazio">
                <i class="bi bi-journal-plus"></i>
                <div>Nenhuma anotação sendo criada no momento</div>
            </div>
        `;
        atualizarEstadoBotaoFixo();
    }

    // Mostra o conteúdo já registrado de uma anotação em modo somente leitura, sem cair direto no
    // formulário de edição — usado quando a tela chega via "?anotacaoId=..." (clique numa anotação na
    // aba "Anotações Clínicas" da Ficha do Paciente), pra abrir o conteúdo de cara sem exigir um
    // clique extra, mas também sem risco de o profissional editar o texto sem querer. O botão
    // "Editar" reaproveita o fluxo real de edição (abrirEdicao).
    function renderVisualizacao(anotacao) {
        visualizandoId = anotacao.id;

        const chipsObjetivos = (anotacao.objetivos || []).map(function (objetivo) {
            return `<span class="ms-objetivo-chip">${escaparHtml(objetivo.titulo)}</span>`;
        }).join("");

        // Escapa antes de trocar as quebras de linha por <br>, senão um "\n" dentro de um trecho
        // malicioso do conteúdo poderia reabrir uma brecha de HTML injection
        const conteudoHtml = escaparHtml(anotacao.conteudo).replace(/\n/g, "<br>");

        composeEl.innerHTML = `
            <div class="ms-anotacao-sessao-compose-titulo">${escaparHtml(anotacao.titulo)}</div>
            ${chipsObjetivos ? `<div class="ms-objetivo-chips-wrapper mb-2">${chipsObjetivos}</div>` : ""}
            <div class="mb-2 flex-grow-1" style="white-space: pre-wrap;">${conteudoHtml}</div>
            <div class="d-flex justify-content-end">
                <button type="button" class="btn btn-ms-orange btn-sm" id="btnEditarAnotacaoSessaoVisualizada">Editar</button>
            </div>
        `;

        document.getElementById("btnEditarAnotacaoSessaoVisualizada").addEventListener("click", function () {
            abrirEdicao(anotacao.id);
        });

        atualizarEstadoBotaoFixo();
    }

    // Entra em modo de composição de uma anotação nova — reaproveitada tanto pelo clique direto no
    // botão fixo quanto pela opção "Descartar e editar" do aviso de rascunho não salvo
    function iniciarNovaAnotacao(forcar) {
        if (!forcar && temRascunho()) {
            pedirDescarte(function () { iniciarNovaAnotacao(true); });
            return;
        }

        composing = true;
        editId = null;
        draftTitulo = "";
        draftConteudo = "";
        draftObjetivosIds = [];
        renderCompose();
    }

    function renderObjetivosHtml() {
        if (objetivosAtivos.length === 0) {
            return '<p class="ms-dash-table-subtext mb-2">Nenhum objetivo em andamento para este paciente.</p>';
        }

        return objetivosAtivos.map(function (objetivo) {
            const marcado = draftObjetivosIds.indexOf(objetivo.id) !== -1;
            return `
                <div class="form-check mb-1">
                    <input class="form-check-input ms-objetivo-sessao-checkbox" type="checkbox" value="${objetivo.id}" id="anotacaoObjetivoChk_${objetivo.id}" ${marcado ? "checked" : ""}>
                    <label class="form-check-label" for="anotacaoObjetivoChk_${objetivo.id}">${escaparHtml(objetivo.titulo)}</label>
                </div>
            `;
        }).join("");
    }

    function renderCompose(avisoHtml) {
        visualizandoId = null;
        composeEl.innerHTML = (avisoHtml || "") + `
            <div class="ms-anotacao-sessao-compose-titulo">${editId ? "Editando anotação" : "Nova anotação"}</div>
            <input type="text" class="form-control form-control-sm mb-2" id="anotacaoSessaoTituloInput" placeholder="Título" maxlength="150">
            <div class="mb-2">
                <label class="form-label mb-1" style="font-size:0.8rem; font-weight:500;">Objetivos Trabalhados</label>
                <div id="anotacaoSessaoObjetivosContainer">${renderObjetivosHtml()}</div>
            </div>
            <textarea class="form-control mb-2 flex-grow-1" id="anotacaoSessaoConteudoInput" placeholder="Registre a evolução clínica desta sessão..." style="min-height:90px; resize:none;"></textarea>
            <div class="d-flex justify-content-end gap-2">
                <button type="button" class="btn btn-outline-secondary btn-sm" id="btnCancelarAnotacaoSessao">Cancelar</button>
                <button type="button" class="btn btn-ms-orange btn-sm" id="btnSalvarAnotacaoSessao">
                    <span class="spinner-border spinner-border-sm d-none" role="status" aria-hidden="true" id="btnSalvarAnotacaoSessaoSpinner"></span>
                    <span id="btnSalvarAnotacaoSessaoTexto">Salvar</span>
                </button>
            </div>
        `;

        const tituloInput = document.getElementById("anotacaoSessaoTituloInput");
        const conteudoInput = document.getElementById("anotacaoSessaoConteudoInput");
        const objetivosContainer = document.getElementById("anotacaoSessaoObjetivosContainer");
        tituloInput.value = draftTitulo;
        conteudoInput.value = draftConteudo;
        tituloInput.addEventListener("input", function () { draftTitulo = tituloInput.value; });
        conteudoInput.addEventListener("input", function () { draftConteudo = conteudoInput.value; });

        if (objetivosContainer) {
            objetivosContainer.addEventListener("change", function (e) {
                if (!e.target.classList.contains("ms-objetivo-sessao-checkbox")) return;
                const id = e.target.value;
                if (e.target.checked) {
                    if (draftObjetivosIds.indexOf(id) === -1) draftObjetivosIds.push(id);
                } else {
                    draftObjetivosIds = draftObjetivosIds.filter(function (existente) { return existente !== id; });
                }
            });
        }

        document.getElementById("btnCancelarAnotacaoSessao").addEventListener("click", function () {
            composing = false;
            editId = null;
            draftTitulo = "";
            draftConteudo = "";
            draftObjetivosIds = [];
            renderIdle();
        });

        document.getElementById("btnSalvarAnotacaoSessao").addEventListener("click", function () {
            salvarAnotacao(tituloInput, conteudoInput);
        });

        atualizarEstadoBotaoFixo();
    }

    // Aviso exibido quando o profissional tenta abrir outra composição (editar uma anotação da
    // lista, ou clicar em "Criar anotação" de novo) enquanto já tem um rascunho não salvo — evita
    // sobrescrever silenciosamente. aoDescartar é chamado se ele confirmar a troca.
    function pedirDescarte(aoDescartar) {
        const aviso = `
            <div class="ms-anotacao-sessao-aviso">
                Você tem uma anotação em andamento. Editar outra vai descartar o que não foi salvo.
                <div class="ms-anotacao-sessao-aviso-acoes">
                    <button type="button" class="btn btn-outline-secondary btn-sm" id="btnManterRascunhoAnotacaoSessao">Continuar aqui</button>
                    <button type="button" class="btn btn-ms-orange btn-sm" id="btnDescartarAnotacaoSessao">Descartar e editar</button>
                </div>
            </div>
        `;
        renderCompose(aviso);
        document.getElementById("btnManterRascunhoAnotacaoSessao").addEventListener("click", function () { renderCompose(); });
        document.getElementById("btnDescartarAnotacaoSessao").addEventListener("click", aoDescartar);
    }

    function abrirEdicao(id, forcar) {
        if (!forcar && temRascunho() && editId !== id) {
            pedirDescarte(function () { abrirEdicao(id, true); });
            return;
        }

        const anotacao = anotacoes.find(function (a) { return a.id === id; });
        if (!anotacao) return;

        composing = true;
        editId = id;
        draftTitulo = anotacao.titulo;
        draftConteudo = anotacao.conteudo;
        draftObjetivosIds = (anotacao.objetivos || []).map(function (o) { return o.objetivoTerapeuticoId; });
        renderCompose();
    }

    // Botão "Visualizar" de cada item da lista — reaproveita renderVisualizacao (mesma usada ao
    // chegar via "?anotacaoId=..."). Visualizar não é destrutivo, mas ainda assim pede confirmação
    // se houver um rascunho não salvo em andamento, pra não perder o que a pessoa estava digitando
    function abrirVisualizacao(id, forcar) {
        if (!forcar && temRascunho()) {
            pedirDescarte(function () { abrirVisualizacao(id, true); });
            return;
        }

        const anotacao = anotacoes.find(function (a) { return a.id === id; });
        if (!anotacao) return;

        renderVisualizacao(anotacao);
    }

    async function salvarAnotacao(tituloInput, conteudoInput) {
        const titulo = tituloInput.value.trim();
        const conteudo = conteudoInput.value.trim();

        if (titulo === "") {
            tituloInput.classList.add("is-invalid");
            return;
        }
        tituloInput.classList.remove("is-invalid");

        const btnSalvar = document.getElementById("btnSalvarAnotacaoSessao");
        const btnSalvarSpinner = document.getElementById("btnSalvarAnotacaoSessaoSpinner");
        const btnSalvarTexto = document.getElementById("btnSalvarAnotacaoSessaoTexto");
        if (btnSalvar) btnSalvar.disabled = true;
        if (btnSalvarSpinner) btnSalvarSpinner.classList.remove("d-none");
        if (btnSalvarTexto) btnSalvarTexto.textContent = "Salvando...";

        const formData = new FormData();
        if (tokenInput) formData.append("__RequestVerificationToken", tokenInput.value);
        formData.append("Titulo", titulo);
        formData.append("Conteudo", conteudo);

        draftObjetivosIds.forEach(function (objetivoId, indice) {
            formData.append(`Objetivos[${indice}].ObjetivoTerapeuticoId`, objetivoId);
            formData.append(`Objetivos[${indice}].Observacao`, "");
        });

        const editandoId = editId;
        const url = editandoId ? "/Sessoes/AtualizarAnotacaoSessao" : "/Sessoes/SalvarAnotacaoSessao";
        if (editandoId) {
            formData.append("Id", editandoId);
        } else {
            formData.append("SessaoId", sessaoId);
        }

        try {
            const resposta = await fetch(url, { method: "POST", body: formData });
            let resultado;
            try {
                resultado = await resposta.json();
            } catch {
                resultado = { success: false, message: "Ocorreu um erro inesperado no servidor. Tente novamente." };
            }

            if (resultado.success) {
                exibirToast(resultado.message || "Anotação salva com sucesso!", true);
                composing = false;
                editId = null;
                draftTitulo = "";
                draftConteudo = "";
                draftObjetivosIds = [];
                renderIdle();
                await carregarAnotacoes();
            } else {
                exibirToast(resultado.message || "Não foi possível salvar a anotação.", false);
                if (btnSalvar) btnSalvar.disabled = false;
                if (btnSalvarSpinner) btnSalvarSpinner.classList.add("d-none");
                if (btnSalvarTexto) btnSalvarTexto.textContent = "Salvar";
            }
        } catch {
            exibirToast("Erro de conexão. Verifique sua internet e tente novamente.", false);
            if (btnSalvar) btnSalvar.disabled = false;
            if (btnSalvarSpinner) btnSalvarSpinner.classList.add("d-none");
            if (btnSalvarTexto) btnSalvarTexto.textContent = "Salvar";
        }
    }

    async function excluirAnotacao(id) {
        if (!window.confirm("Tem certeza que deseja excluir esta anotação?")) return;

        const formData = new FormData();
        if (tokenInput) formData.append("__RequestVerificationToken", tokenInput.value);
        formData.append("id", id);

        try {
            const resposta = await fetch("/Sessoes/ExcluirAnotacaoSessao", { method: "POST", body: formData });
            const resultado = await resposta.json();

            exibirToast(resultado.message || (resultado.success ? "Anotação removida com sucesso!" : "Não foi possível remover a anotação."), !!resultado.success);

            if (resultado.success) {
                if (editId === id) {
                    composing = false;
                    editId = null;
                    draftTitulo = "";
                    draftConteudo = "";
                    draftObjetivosIds = [];
                    renderIdle();
                } else if (visualizandoId === id) {
                    // A anotação que estava sendo visualizada foi excluída — volta pro estado ocioso
                    renderIdle();
                }
                await carregarAnotacoes();
            }
        } catch {
            exibirToast("Erro de conexão. Verifique sua internet e tente novamente.", false);
        }
    }

    function renderizarLista() {
        if (anotacoes.length === 0) {
            listaEl.innerHTML = '<p class="ms-dash-table-subtext mb-0">Nenhuma anotação registrada ainda.</p>';
            return;
        }

        listaEl.innerHTML = anotacoes.map(function (anotacao) {
            return `
                <div class="ms-anotacao-sessao-item" data-anotacao-id="${anotacao.id}">
                    <div class="ms-anotacao-sessao-item-info">
                        <div class="ms-anotacao-sessao-item-titulo">${escaparHtml(anotacao.titulo)}</div>
                        <div class="ms-anotacao-sessao-item-data">${escaparHtml(anotacao.dataRegistro)}</div>
                    </div>
                    <div class="d-flex gap-1">
                        <button type="button" class="ms-dash-row-link" data-visualizar-anotacao="${anotacao.id}" title="Visualizar">
                            <i class="bi bi-eye"></i>
                        </button>
                        <button type="button" class="ms-dash-row-link" data-editar-anotacao="${anotacao.id}" title="Editar">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                        <button type="button" class="ms-dash-row-link" data-excluir-anotacao="${anotacao.id}" title="Excluir">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </div>
            `;
        }).join("");
    }

    // Se a URL trouxer "?anotacaoId=...", vindo do clique numa anotação na aba "Anotações Clínicas"
    // da Ficha do Paciente, abre direto o conteúdo dela no painel da esquerda (renderVisualizacao) e
    // reforça visualmente qual é ela na lista da direita (scroll + destaque temporário) — tudo isso
    // só na primeira vez que a lista é carregada (carregarAnotacoes também é reusado após salvar/excluir)
    const anotacaoIdParaDestacar = new URLSearchParams(window.location.search).get("anotacaoId");
    let ms_anotacaoJaDestacada = false;

    function abrirAnotacaoDaUrlSeNecessario() {
        if (!anotacaoIdParaDestacar || ms_anotacaoJaDestacada) return;

        const item = listaEl.querySelector(`[data-anotacao-id="${anotacaoIdParaDestacar}"]`);
        const anotacao = anotacoes.find(function (a) { return a.id === anotacaoIdParaDestacar; });
        if (!item || !anotacao) return;

        ms_anotacaoJaDestacada = true;

        item.scrollIntoView({ behavior: "smooth", block: "center" });
        item.classList.add("ms-anotacao-destacada");
        setTimeout(function () {
            item.classList.remove("ms-anotacao-destacada");
        }, 2500);

        renderVisualizacao(anotacao);
    }

    listaEl.addEventListener("click", function (e) {
        const botaoVisualizar = e.target.closest("[data-visualizar-anotacao]");
        if (botaoVisualizar) {
            abrirVisualizacao(botaoVisualizar.getAttribute("data-visualizar-anotacao"));
            return;
        }

        const botaoEditar = e.target.closest("[data-editar-anotacao]");
        if (botaoEditar) {
            abrirEdicao(botaoEditar.getAttribute("data-editar-anotacao"));
            return;
        }

        const botaoExcluir = e.target.closest("[data-excluir-anotacao]");
        if (botaoExcluir) {
            excluirAnotacao(botaoExcluir.getAttribute("data-excluir-anotacao"));
        }
    });

    async function carregarAnotacoes() {
        try {
            const resposta = await fetch(`/Sessoes/ListarAnotacoesSessao?sessaoId=${encodeURIComponent(sessaoId)}`);
            const resultado = await resposta.json();

            if (!resultado.success) {
                exibirToast(resultado.message || "Não foi possível carregar as anotações.", false);
                return;
            }

            anotacoes = resultado.anotacoes || [];
            renderizarLista();
            abrirAnotacaoDaUrlSeNecessario();
        } catch {
            exibirToast("Erro de conexão. Verifique sua internet e tente novamente.", false);
        }
    }

    const btnCriarFixo = document.getElementById("btnCriarAnotacaoSessaoFixo");
    if (btnCriarFixo) {
        btnCriarFixo.addEventListener("click", function () {
            iniciarNovaAnotacao();
        });
    }

    renderIdle();
    carregarAnotacoes();
});
