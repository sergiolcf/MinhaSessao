let ms_agendaVisao = "semana";
let ms_agendaReferencia = new Date();
ms_agendaReferencia.setHours(0, 0, 0, 0);

document.addEventListener("DOMContentLoaded", function () {
    const gradeSemanaEl = document.getElementById("agendaGradeSemana");
    const gradeMesEl = document.getElementById("agendaGradeMes");
    const cabecalhoMesEl = document.getElementById("agendaCabecalhoMes");
    const periodoLabelEl = document.getElementById("agendaPeriodoLabel");
    const btnVisaoSemana = document.getElementById("btnAgendaVisaoSemana");
    const btnVisaoMes = document.getElementById("btnAgendaVisaoMes");
    const btnAnterior = document.getElementById("btnAgendaAnterior");
    const btnProximo = document.getElementById("btnAgendaProximo");
    const btnHoje = document.getElementById("btnAgendaHoje");

    if (!gradeSemanaEl || !gradeMesEl) return;

    const diasSemanaAbrev = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"];
    const mesesNome = [
        "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    ];

    function formatarData2Digitos(numero) {
        return String(numero).padStart(2, "0");
    }

    function formatarDataIso(data) {
        return `${data.getFullYear()}-${formatarData2Digitos(data.getMonth() + 1)}-${formatarData2Digitos(data.getDate())}`;
    }

    function formatarDataChave(data) {
        return formatarDataIso(data);
    }

    function formatarDataCurta(data) {
        return `${formatarData2Digitos(data.getDate())}/${formatarData2Digitos(data.getMonth() + 1)}`;
    }

    function mesmaData(a, b) {
        return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
    }

    function adicionarDias(data, dias) {
        const nova = new Date(data);
        nova.setDate(nova.getDate() + dias);
        return nova;
    }

    function inicioDaSemana(data) {
        const nova = new Date(data);
        nova.setDate(nova.getDate() - nova.getDay());
        return nova;
    }

    // Grade mensal mostra semanas completas, então o intervalo real buscado no servidor
    // inclui os dias "de fora" do mês (padding) para preencher a primeira e a última linha
    function inicioGradeMes(data) {
        const primeiroDiaMes = new Date(data.getFullYear(), data.getMonth(), 1);
        return inicioDaSemana(primeiroDiaMes);
    }

    function fimGradeMesExclusivo(data) {
        const ultimoDiaMes = new Date(data.getFullYear(), data.getMonth() + 1, 0);
        const inicioUltimaSemana = inicioDaSemana(ultimoDiaMes);
        return adicionarDias(inicioUltimaSemana, 7);
    }

    // escaparHtml vem de wwwroot/js/sessao-utils.js (compartilhado)

    function classeChip(status) {
        if (status === "Agendada") return "ms-agenda-chip-agendada";
        if (status === "Realizada") return "ms-agenda-chip-realizada";
        return "ms-agenda-chip-cancelada";
    }

    function construirChip(sessao) {
        const botao = document.createElement("button");
        botao.type = "button";
        botao.className = `ms-agenda-chip ${classeChip(sessao.status)}`;
        botao.title = `${sessao.hora} - ${sessao.pacienteNome} (${sessao.status})`;
        botao.textContent = `${sessao.hora} ${sessao.pacienteNome}`;
        botao.dataset.sessaoId = sessao.id;
        botao.dataset.pacienteId = sessao.pacienteId;
        botao.dataset.pacienteNome = sessao.pacienteNome;
        botao.dataset.data = sessao.data;
        botao.dataset.hora = sessao.hora;
        botao.dataset.duracaoMinutos = sessao.duracaoMinutos;
        botao.dataset.status = sessao.status;
        return botao;
    }

    function agruparPorDia(sessoes) {
        const grupos = {};
        (sessoes || []).forEach(function (sessao) {
            const chave = sessao.dataHoraIso.slice(0, 10);
            if (!grupos[chave]) grupos[chave] = [];
            grupos[chave].push(sessao);
        });
        return grupos;
    }

    function renderizarSemana(referencia, sessoesPorDia) {
        gradeSemanaEl.innerHTML = "";
        const inicio = inicioDaSemana(referencia);
        const hoje = new Date();
        hoje.setHours(0, 0, 0, 0);

        for (let i = 0; i < 7; i++) {
            const dia = adicionarDias(inicio, i);
            const coluna = document.createElement("div");
            coluna.className = "ms-agenda-dia-semana" + (mesmaData(dia, hoje) ? " ms-agenda-dia-hoje" : "");
            coluna.dataset.data = formatarDataIso(dia);

            const cabecalho = document.createElement("div");
            cabecalho.className = "ms-agenda-dia-semana-header";
            cabecalho.innerHTML = `${diasSemanaAbrev[dia.getDay()]}<small>${formatarDataCurta(dia)}</small>`;
            coluna.appendChild(cabecalho);

            const sessoesDoDia = sessoesPorDia[formatarDataChave(dia)] || [];
            sessoesDoDia.forEach(function (sessao) {
                coluna.appendChild(construirChip(sessao));
            });

            gradeSemanaEl.appendChild(coluna);
        }
    }

    function renderizarMes(referencia, sessoesPorDia) {
        gradeMesEl.innerHTML = "";
        const inicio = inicioGradeMes(referencia);
        const fimExclusivo = fimGradeMesExclusivo(referencia);
        const totalDias = Math.round((fimExclusivo - inicio) / (1000 * 60 * 60 * 24));
        const hoje = new Date();
        hoje.setHours(0, 0, 0, 0);

        for (let i = 0; i < totalDias; i++) {
            const dia = adicionarDias(inicio, i);
            const foraDoMes = dia.getMonth() !== referencia.getMonth();

            const celula = document.createElement("div");
            celula.className = "ms-agenda-dia-mes"
                + (foraDoMes ? " ms-agenda-dia-mes-fora" : "")
                + (mesmaData(dia, hoje) ? " ms-agenda-dia-hoje" : "");
            celula.dataset.data = formatarDataIso(dia);

            const numero = document.createElement("div");
            numero.className = "ms-agenda-dia-mes-numero";
            numero.textContent = String(dia.getDate());
            celula.appendChild(numero);

            const sessoesDoDia = sessoesPorDia[formatarDataChave(dia)] || [];
            sessoesDoDia.forEach(function (sessao) {
                celula.appendChild(construirChip(sessao));
            });

            gradeMesEl.appendChild(celula);
        }
    }

    function atualizarPeriodoLabel() {
        if (!periodoLabelEl) return;

        if (ms_agendaVisao === "mes") {
            periodoLabelEl.textContent = `${mesesNome[ms_agendaReferencia.getMonth()]} de ${ms_agendaReferencia.getFullYear()}`;
            return;
        }

        const inicio = inicioDaSemana(ms_agendaReferencia);
        const fim = adicionarDias(inicio, 6);
        periodoLabelEl.textContent = `${formatarDataCurta(inicio)} - ${formatarDataCurta(fim)}/${fim.getFullYear()}`;
    }

    async function carregarAgenda() {
        atualizarPeriodoLabel();

        const inicio = ms_agendaVisao === "mes" ? inicioGradeMes(ms_agendaReferencia) : inicioDaSemana(ms_agendaReferencia);
        const fim = ms_agendaVisao === "mes" ? fimGradeMesExclusivo(ms_agendaReferencia) : adicionarDias(inicio, 7);

        try {
            const parametros = new URLSearchParams({ inicio: formatarDataIso(inicio), fim: formatarDataIso(fim) });
            const resposta = await fetch(`/Agenda/BuscarSessoesAgenda?${parametros.toString()}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) return;

            const sessoesPorDia = agruparPorDia(resultado.sessoes);

            if (ms_agendaVisao === "mes") {
                renderizarMes(ms_agendaReferencia, sessoesPorDia);
            } else {
                renderizarSemana(ms_agendaReferencia, sessoesPorDia);
            }
        } catch {
            // Mantém a grade atual em caso de falha de conexão
        }
    }

    function definirVisao(visao) {
        ms_agendaVisao = visao;
        if (btnVisaoSemana) btnVisaoSemana.classList.toggle("active", visao === "semana");
        if (btnVisaoMes) btnVisaoMes.classList.toggle("active", visao === "mes");
        gradeSemanaEl.classList.toggle("d-none", visao !== "semana");
        gradeMesEl.classList.toggle("d-none", visao !== "mes");
        if (cabecalhoMesEl) cabecalhoMesEl.classList.toggle("d-none", visao !== "mes");
        carregarAgenda();
    }

    if (btnVisaoSemana) btnVisaoSemana.addEventListener("click", () => definirVisao("semana"));
    if (btnVisaoMes) btnVisaoMes.addEventListener("click", () => definirVisao("mes"));

    if (btnAnterior) {
        btnAnterior.addEventListener("click", function () {
            ms_agendaReferencia = ms_agendaVisao === "mes"
                ? new Date(ms_agendaReferencia.getFullYear(), ms_agendaReferencia.getMonth() - 1, 1)
                : adicionarDias(ms_agendaReferencia, -7);
            carregarAgenda();
        });
    }

    if (btnProximo) {
        btnProximo.addEventListener("click", function () {
            ms_agendaReferencia = ms_agendaVisao === "mes"
                ? new Date(ms_agendaReferencia.getFullYear(), ms_agendaReferencia.getMonth() + 1, 1)
                : adicionarDias(ms_agendaReferencia, 7);
            carregarAgenda();
        });
    }

    if (btnHoje) {
        btnHoje.addEventListener("click", function () {
            ms_agendaReferencia = new Date();
            ms_agendaReferencia.setHours(0, 0, 0, 0);
            carregarAgenda();
        });
    }

    // ----- Modal: Detalhes da Sessão -----
    const modalEl = document.getElementById("modalDetalhesSessaoAgenda");
    if (modalEl) {
        const pacienteEl = document.getElementById("agendaDetalhePaciente");
        const dataHoraEl = document.getElementById("agendaDetalheDataHora");
        const duracaoEl = document.getElementById("agendaDetalheDuracao");
        const statusEl = document.getElementById("agendaDetalheStatus");
        const linkProntuarioEl = document.getElementById("agendaDetalheLinkProntuario");

        document.addEventListener("click", function (e) {
            const chip = e.target.closest(".ms-agenda-chip");
            if (!chip) return;

            if (pacienteEl) pacienteEl.textContent = chip.dataset.pacienteNome || "";
            if (dataHoraEl) dataHoraEl.textContent = `${chip.dataset.data} às ${chip.dataset.hora}`;
            if (duracaoEl) duracaoEl.textContent = `${chip.dataset.duracaoMinutos} min`;
            if (statusEl) {
                statusEl.innerHTML = `<span class="badge ${chip.dataset.status === "Agendada" ? "ms-badge-agendada" : chip.dataset.status === "Realizada" ? "ms-badge-realizada" : "ms-badge-cancelada"}">${escaparHtml(chip.dataset.status)}</span>`;
            }
            if (linkProntuarioEl) linkProntuarioEl.href = `/Pacientes/Detalhes/${chip.dataset.pacienteId}`;

            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        });
    }

    // Clicar num dia da grade (fora de um chip de sessão, que já abre a modal de detalhes) abre a
    // modal "Nova Sessão" (partial _ModalNovaSessao, a mesma de "Minhas Sessões") direto aqui na
    // Agenda, com a data já preenchida — sem sair da página. window.MsNovaSessao vem de nova-sessao.js,
    // carregado antes deste arquivo.
    function abrirNovaSessaoParaDia(e) {
        if (e.target.closest(".ms-agenda-chip")) return;
        const celula = e.target.closest("[data-data]");
        if (!celula || !celula.dataset.data || !window.MsNovaSessao) return;
        window.MsNovaSessao.abrirComData(celula.dataset.data);
    }

    gradeSemanaEl.addEventListener("click", abrirNovaSessaoParaDia);
    gradeMesEl.addEventListener("click", abrirNovaSessaoParaDia);

    carregarAgenda();
});
