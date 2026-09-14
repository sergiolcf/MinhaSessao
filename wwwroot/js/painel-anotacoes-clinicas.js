let ms_paginaAtualAnotacoesClinicasPaciente = 1;
let ms_totalPaginasAnotacoesClinicasPaciente = 1;
let ms_buscaAtualAnotacoesClinicasPaciente = "";
let ms_dataInicioAtualAnotacoesClinicasPaciente = "";
let ms_dataFimAtualAnotacoesClinicasPaciente = "";

document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("anotacoesClinicasContainer");
    const listaAnotacoes = document.getElementById("listaAnotacoesClinicas");
    const paginacaoEl = document.getElementById("paginacaoAnotacoesClinicas");
    const buscaTituloInput = document.getElementById("buscaAnotacaoClinicaTituloPaciente");
    const dataInicioInput = document.getElementById("filtroAnotacaoClinicaDataInicio");
    const dataFimInput = document.getElementById("filtroAnotacaoClinicaDataFim");
    const btnLimparPeriodo = document.getElementById("btnLimparPeriodoAnotacaoClinica");

    if (!container || !listaAnotacoes) return;

    ms_paginaAtualAnotacoesClinicasPaciente = parseInt(container.dataset.paginaAtual || "1", 10);
    ms_totalPaginasAnotacoesClinicasPaciente = parseInt(container.dataset.totalPaginas || "1", 10);

    // Escapa texto para uso seguro em conteúdo HTML (mesmo padrão de anotacoes-clinicas.js)
    function escaparHtml(texto) {
        return (texto || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function debounce(fn, atrasoMs) {
        let temporizador;
        return function (...args) {
            clearTimeout(temporizador);
            temporizador = setTimeout(() => fn.apply(this, args), atrasoMs);
        };
    }

    function atualizarBotaoLimparPeriodo() {
        if (!btnLimparPeriodo) return;
        const temPeriodo = !!(ms_dataInicioAtualAnotacoesClinicasPaciente || ms_dataFimAtualAnotacoesClinicasPaciente);
        btnLimparPeriodo.classList.toggle("d-none", !temPeriodo);
    }

    // Mede cabeçalho + 4 linhas já renderizadas e aplica como max-height inline — mesmo raciocínio
    // de ajustarAlturaTabelaAnotacoesClinicas (anotacoes-clinicas.js, lado do profissional): a altura
    // da linha varia com os chips de Objetivos Trabalhados, então um max-height fixo em CSS não bate
    // certo aqui.
    let ms_alturaTabelaAnotacoesClinicasCalculada = null;

    function ajustarAlturaTabelaAnotacoesClinicas() {
        const scrollEl = document.querySelector(".ms-dash-table-scroll-anotacoes-clinicas");
        if (!scrollEl) return;

        const thead = scrollEl.querySelector("thead");
        const primeiraLinha = scrollEl.querySelector("tbody tr[data-anotacao-id]");

        if (!thead || !primeiraLinha) {
            if (ms_alturaTabelaAnotacoesClinicasCalculada) {
                scrollEl.style.maxHeight = `${ms_alturaTabelaAnotacoesClinicasCalculada}px`;
            }
            return;
        }

        const alturaThead = thead.getBoundingClientRect().height;
        const alturaLinha = primeiraLinha.getBoundingClientRect().height;

        ms_alturaTabelaAnotacoesClinicasCalculada = alturaThead + alturaLinha * 4;
        scrollEl.style.maxHeight = `${ms_alturaTabelaAnotacoesClinicasCalculada}px`;
    }

    ajustarAlturaTabelaAnotacoesClinicas();

    let ms_resizeTabelaAnotacoesClinicasDebounce = null;
    window.addEventListener("resize", function () {
        clearTimeout(ms_resizeTabelaAnotacoesClinicasDebounce);
        ms_resizeTabelaAnotacoesClinicasDebounce = setTimeout(ajustarAlturaTabelaAnotacoesClinicas, 200);
    });

    // "Anotações Clínicas" não é a aba ativa por padrão (quem abre é "Agendadas"), então no
    // DOMContentLoaded essa aba ainda está com display: none — getBoundingClientRect() retorna
    // altura 0 pra cabeçalho e linha, e o max-height calculado (0px) esconde a tabela mesmo depois de
    // o paciente clicar na aba. Recalcula de novo quando a aba Bootstrap é efetivamente exibida
    // (mesmo bug/correção já aplicados do lado do profissional, ver anotacoes-clinicas.js).
    const abaAnotacoesClinicas = document.getElementById("anotacoes-clinicas-tab");
    if (abaAnotacoesClinicas) {
        abaAnotacoesClinicas.addEventListener("shown.bs.tab", ajustarAlturaTabelaAnotacoesClinicas);
    }

    function criarLinhaAnotacao(anotacao) {
        const linha = document.createElement("tr");
        linha.dataset.anotacaoId = anotacao.id;
        // Título/conteúdo/data ficam guardados na própria linha — o clique reaproveita esses dados
        // pra preencher o modal de visualização, sem precisar de uma segunda chamada ao servidor
        linha.dataset.titulo = anotacao.titulo || "";
        linha.dataset.conteudo = anotacao.conteudo;
        linha.dataset.dataRegistro = anotacao.dataRegistro;

        const chipsObjetivos = (anotacao.objetivos || [])
            .map(titulo => `<span class="ms-objetivo-chip">${escaparHtml(titulo)}</span>`)
            .join("");

        linha.innerHTML = `
            <td>
                ${escaparHtml(anotacao.titulo)}
                ${chipsObjetivos ? `<div class="ms-objetivo-chips-wrapper mt-1">${chipsObjetivos}</div>` : ""}
            </td>
            <td>
                <span class="ms-badge-codigo">${escaparHtml(anotacao.sessaoCodigo)}</span>
                <div class="ms-dash-table-subtext">${escaparHtml(anotacao.sessaoDataHora)}</div>
            </td>
            <td>${escaparHtml(anotacao.dataRegistro)}</td>
            <td class="text-end">
                <button type="button" class="ms-dash-row-link" title="Visualizar">
                    <i class="bi bi-eye"></i>
                </button>
            </td>
        `;

        return linha;
    }

    function renderizarAnotacoes(anotacoes) {
        listaAnotacoes.innerHTML = "";

        if (!anotacoes || anotacoes.length === 0) {
            const filtroAtivo = ms_buscaAtualAnotacoesClinicasPaciente || ms_dataInicioAtualAnotacoesClinicasPaciente || ms_dataFimAtualAnotacoesClinicasPaciente;
            const mensagem = filtroAtivo
                ? "<h5>Nenhuma anotação encontrada</h5><p>Não há anotações clínicas com os filtros selecionados.</p>"
                : "<h5>Nenhuma anotação clínica registrada</h5><p>As anotações clínicas registradas pelos seus profissionais em \"Minhas Sessões\" aparecerão aqui.</p>";

            listaAnotacoes.innerHTML = `
                <tr id="anotacoesClinicasEmptyRow">
                    <td colspan="4">
                        <div class="ms-dash-empty-state">
                            <i class="bi bi-journal-text"></i>
                            ${mensagem}
                        </div>
                    </td>
                </tr>
            `;
            return;
        }

        anotacoes.forEach(function (anotacao) {
            listaAnotacoes.appendChild(criarLinhaAnotacao(anotacao));
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
            const parametros = new URLSearchParams({ pagina: String(pagina) });
            if (ms_buscaAtualAnotacoesClinicasPaciente) parametros.set("busca", ms_buscaAtualAnotacoesClinicasPaciente);
            if (ms_dataInicioAtualAnotacoesClinicasPaciente) parametros.set("dataInicio", ms_dataInicioAtualAnotacoesClinicasPaciente);
            if (ms_dataFimAtualAnotacoesClinicasPaciente) parametros.set("dataFim", ms_dataFimAtualAnotacoesClinicasPaciente);

            // Sem pacienteId na query string — a action usa sempre o paciente logado (User.ObterPacienteId())
            const resposta = await fetch(`/PainelPaciente/BuscarAnotacoesClinicas?${parametros.toString()}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) return;

            ms_paginaAtualAnotacoesClinicasPaciente = resultado.paginaAtual;
            ms_totalPaginasAnotacoesClinicasPaciente = resultado.totalPaginas;

            renderizarAnotacoes(resultado.anotacoes);
            renderizarPaginacao(resultado.paginaAtual, resultado.totalPaginas);
            ajustarAlturaTabelaAnotacoesClinicas();
        } catch (erro) {
            // Mantém a lista atual em caso de falha de conexão ao trocar de página/filtro
        }
    }

    if (buscaTituloInput) {
        const dispararBusca = debounce(function () {
            ms_buscaAtualAnotacoesClinicasPaciente = buscaTituloInput.value.trim();
            carregarPagina(1);
        }, 300);

        buscaTituloInput.addEventListener("input", dispararBusca);
    }

    if (dataInicioInput) {
        dataInicioInput.addEventListener("change", function () {
            ms_dataInicioAtualAnotacoesClinicasPaciente = dataInicioInput.value;
            atualizarBotaoLimparPeriodo();
            carregarPagina(1);
        });
    }

    if (dataFimInput) {
        dataFimInput.addEventListener("change", function () {
            ms_dataFimAtualAnotacoesClinicasPaciente = dataFimInput.value;
            atualizarBotaoLimparPeriodo();
            carregarPagina(1);
        });
    }

    if (btnLimparPeriodo) {
        btnLimparPeriodo.addEventListener("click", function () {
            ms_dataInicioAtualAnotacoesClinicasPaciente = "";
            ms_dataFimAtualAnotacoesClinicasPaciente = "";
            if (dataInicioInput) dataInicioInput.value = "";
            if (dataFimInput) dataFimInput.value = "";
            atualizarBotaoLimparPeriodo();
            carregarPagina(1);
        });
    }

    // Abre a modal "Visualizar Anotação Clínica" (somente leitura) com os dados já guardados na
    // própria linha — sem nenhuma ação de editar/excluir, já que o paciente não tem esse acesso
    function abrirModalVisualizacao(linha) {
        const modalEl = document.getElementById("modalVisualizarAnotacaoClinica");
        if (!modalEl) return;

        const tituloEl = document.getElementById("modalVisualizarAnotacaoClinicaTitulo");
        const dataEl = document.getElementById("modalVisualizarAnotacaoClinicaData");
        const conteudoEl = document.getElementById("modalVisualizarAnotacaoClinicaConteudo");
        const objetivosWrapperEl = document.getElementById("modalVisualizarAnotacaoClinicaObjetivosWrapper");
        const objetivosEl = document.getElementById("modalVisualizarAnotacaoClinicaObjetivos");

        if (tituloEl) tituloEl.textContent = linha.dataset.titulo || "Anotação";
        if (dataEl) dataEl.textContent = linha.dataset.dataRegistro;

        // Reaproveita os chips de Objetivos Trabalhados já renderizados na própria linha (coluna
        // Título), em vez de serializar a lista de objetivos numa segunda estrutura de dados
        const chipsWrapper = linha.querySelector(".ms-objetivo-chips-wrapper");
        if (objetivosEl) objetivosEl.innerHTML = chipsWrapper ? chipsWrapper.innerHTML : "";
        if (objetivosWrapperEl) objetivosWrapperEl.classList.toggle("d-none", !chipsWrapper);

        // Escapa antes de trocar "\n" por "<br>", senão uma quebra de linha dentro do conteúdo
        // poderia reabrir uma brecha de HTML injection (mesmo cuidado de renderVisualizacao em
        // sessao-detalhe.js)
        if (conteudoEl) conteudoEl.innerHTML = escaparHtml(linha.dataset.conteudo).replace(/\n/g, "<br>");

        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    // A única ação de cada linha é "Visualizar" (somente leitura) — clicar em qualquer ponto dela,
    // inclusive no ícone de olho, abre a mesma modal
    listaAnotacoes.addEventListener("click", function (e) {
        const linha = e.target.closest("tr[data-anotacao-id]");
        if (!linha) return;

        abrirModalVisualizacao(linha);
    });

    if (paginacaoEl) {
        paginacaoEl.addEventListener("click", async function (e) {
            const botao = e.target.closest(".page-link");
            if (!botao) return;

            const pagina = parseInt(botao.dataset.pagina, 10);
            if (pagina === ms_paginaAtualAnotacoesClinicasPaciente) return;

            await carregarPagina(pagina);
        });
    }
});
