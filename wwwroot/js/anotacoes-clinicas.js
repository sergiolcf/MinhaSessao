let ms_paginaAtualAnotacoesClinicas = 1;
let ms_totalPaginasAnotacoesClinicas = 1;
let ms_buscaAtualAnotacoesClinicas = "";
let ms_dataInicioAtualAnotacoesClinicas = "";
let ms_dataFimAtualAnotacoesClinicas = "";

document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("anotacoesClinicasContainer");
    const listaAnotacoes = document.getElementById("listaAnotacoesClinicas");
    const paginacaoEl = document.getElementById("paginacaoAnotacoesClinicas");
    const buscaTituloInput = document.getElementById("buscaAnotacaoClinicaTitulo");
    const dataInicioInput = document.getElementById("filtroAnotacaoClinicaDataInicio");
    const dataFimInput = document.getElementById("filtroAnotacaoClinicaDataFim");
    const btnLimparPeriodo = document.getElementById("btnLimparPeriodoAnotacaoClinica");

    if (!container || !listaAnotacoes) return;

    const pacienteId = container.dataset.pacienteId;
    ms_paginaAtualAnotacoesClinicas = parseInt(container.dataset.paginaAtual || "1", 10);
    ms_totalPaginasAnotacoesClinicas = parseInt(container.dataset.totalPaginas || "1", 10);

    // Escapa texto para uso seguro em conteúdo HTML (mesmo padrão de anotacoes.js)
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
        const temPeriodo = !!(ms_dataInicioAtualAnotacoesClinicas || ms_dataFimAtualAnotacoesClinicas);
        btnLimparPeriodo.classList.toggle("d-none", !temPeriodo);
    }

    // Mede cabeçalho + 4 linhas já renderizadas e aplica como max-height inline — mesmo raciocínio
    // de ajustarAlturaTabelaPacientes (pacientes.js): a altura da linha varia com os chips de
    // Objetivos Trabalhados, então um max-height fixo em CSS não bate certo aqui
    let ms_alturaTabelaAnotacoesClinicasCalculada = null;

    function ajustarAlturaTabelaAnotacoesClinicas() {
        const scrollEl = document.querySelector(".ms-dash-table-scroll-anotacoes-clinicas");
        if (!scrollEl) return;

        const thead = scrollEl.querySelector("thead");
        const primeiraLinha = scrollEl.querySelector("tbody tr[data-sessao-id]");

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

    // "Anotações Clínicas" não é a aba ativa por padrão (quem abre é "Anotações Confidenciais"), então
    // no DOMContentLoaded essa aba ainda está com display: none — getBoundingClientRect() retorna
    // altura 0 pra cabeçalho e linha, e o max-height calculado (0px) esconde a tabela mesmo depois de
    // o usuário clicar na aba. Recalcula de novo quando a aba Bootstrap é efetivamente exibida.
    const abaAnotacoesClinicas = document.getElementById("anotacoes-clinicas-tab");
    if (abaAnotacoesClinicas) {
        abaAnotacoesClinicas.addEventListener("shown.bs.tab", ajustarAlturaTabelaAnotacoesClinicas);
    }

    function criarLinhaAnotacao(anotacao) {
        const linha = document.createElement("tr");
        linha.dataset.sessaoId = anotacao.sessaoId;
        // sessaoUrl já vem do servidor com "?anotacaoId=..." (ver BuscarAnotacoesClinicas), pra que a
        // tela da Sessão abra com esta anotação específica já selecionada/destacada
        linha.dataset.sessaoUrl = anotacao.sessaoUrl;

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
                <a href="${anotacao.sessaoUrl}" class="ms-dash-row-link" title="Abrir sessão">
                    <i class="bi bi-chevron-right"></i>
                </a>
            </td>
        `;

        return linha;
    }

    function renderizarAnotacoes(anotacoes) {
        listaAnotacoes.innerHTML = "";

        if (!anotacoes || anotacoes.length === 0) {
            const filtroAtivo = ms_buscaAtualAnotacoesClinicas || ms_dataInicioAtualAnotacoesClinicas || ms_dataFimAtualAnotacoesClinicas;
            const mensagem = filtroAtivo
                ? "<h5>Nenhuma anotação encontrada</h5><p>Não há anotações clínicas com os filtros selecionados.</p>"
                : "<h5>Nenhuma anotação clínica registrada</h5><p>As anotações clínicas registradas em \"Minhas Sessões\" aparecerão aqui.</p>";

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
            const parametros = new URLSearchParams({
                pacienteId: pacienteId,
                pagina: String(pagina)
            });
            if (ms_buscaAtualAnotacoesClinicas) parametros.set("busca", ms_buscaAtualAnotacoesClinicas);
            if (ms_dataInicioAtualAnotacoesClinicas) parametros.set("dataInicio", ms_dataInicioAtualAnotacoesClinicas);
            if (ms_dataFimAtualAnotacoesClinicas) parametros.set("dataFim", ms_dataFimAtualAnotacoesClinicas);

            const resposta = await fetch(`/Pacientes/BuscarAnotacoesClinicas?${parametros.toString()}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) return;

            ms_paginaAtualAnotacoesClinicas = resultado.paginaAtual;
            ms_totalPaginasAnotacoesClinicas = resultado.totalPaginas;

            renderizarAnotacoes(resultado.anotacoes);
            renderizarPaginacao(resultado.paginaAtual, resultado.totalPaginas);
            ajustarAlturaTabelaAnotacoesClinicas();
        } catch (erro) {
            // Mantém a lista atual em caso de falha de conexão ao trocar de página/filtro
        }
    }

    if (buscaTituloInput) {
        const dispararBusca = debounce(function () {
            ms_buscaAtualAnotacoesClinicas = buscaTituloInput.value.trim();
            carregarPagina(1);
        }, 300);

        buscaTituloInput.addEventListener("input", dispararBusca);
    }

    if (dataInicioInput) {
        dataInicioInput.addEventListener("change", function () {
            ms_dataInicioAtualAnotacoesClinicas = dataInicioInput.value;
            atualizarBotaoLimparPeriodo();
            carregarPagina(1);
        });
    }

    if (dataFimInput) {
        dataFimInput.addEventListener("change", function () {
            ms_dataFimAtualAnotacoesClinicas = dataFimInput.value;
            atualizarBotaoLimparPeriodo();
            carregarPagina(1);
        });
    }

    if (btnLimparPeriodo) {
        btnLimparPeriodo.addEventListener("click", function () {
            ms_dataInicioAtualAnotacoesClinicas = "";
            ms_dataFimAtualAnotacoesClinicas = "";
            if (dataInicioInput) dataInicioInput.value = "";
            if (dataFimInput) dataFimInput.value = "";
            atualizarBotaoLimparPeriodo();
            carregarPagina(1);
        });
    }

    // Linha inteira leva pra tela da sessão, exceto cliques em links/botões (mesmo padrão de "Meus Pacientes")
    listaAnotacoes.addEventListener("click", function (e) {
        if (e.target.closest("a") || e.target.closest("button")) return;

        const linha = e.target.closest("tr[data-sessao-id]");
        if (!linha) return;

        window.location.href = linha.dataset.sessaoUrl;
    });

    if (paginacaoEl) {
        paginacaoEl.addEventListener("click", async function (e) {
            const botao = e.target.closest(".page-link");
            if (!botao) return;

            const pagina = parseInt(botao.dataset.pagina, 10);
            if (pagina === ms_paginaAtualAnotacoesClinicas) return;

            await carregarPagina(pagina);
        });
    }
});
