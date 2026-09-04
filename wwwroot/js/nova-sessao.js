// Lógica da modal "Nova Sessão" (+ popup "Procurar Paciente") — compartilhada entre a tela
// "Minhas Sessões" e a tela "Minha Agenda" via a partial Views/Shared/_ModalNovaSessao.cshtml.
// Depende dos helpers globais de wwwroot/js/sessao-utils.js (combinarDataHora, configurarSeletorHora,
// escaparHtml) — inclua esse script ANTES deste na página.
//
// Expõe window.MsNovaSessao.abrirComData(dataIso) para quem quiser abrir a modal já com uma data
// preenchida (ex.: clique num dia da grade da Agenda) sem precisar navegar para outra página.
document.addEventListener("DOMContentLoaded", function () {
    const modalNovaSessaoEl = document.getElementById("modalNovaSessao");
    if (!modalNovaSessaoEl) return;

    const toastEl = document.getElementById("toastNovaSessao");
    const toastMensagemEl = document.getElementById("toastNovaSessaoMensagem");
    const toast = toastEl ? bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 4000 }) : null;

    function exibirToast(mensagem, sucesso) {
        if (!toastEl || !toastMensagemEl || !toast) return;
        toastEl.classList.remove("text-bg-success", "text-bg-danger");
        toastEl.classList.add(sucesso ? "text-bg-success" : "text-bg-danger");
        toastMensagemEl.textContent = mensagem;
        toast.show();
    }

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
                // Recarrega a página atual (Minhas Sessões ou Minha Agenda) para refletir a sessão nova
                // nas estatísticas/listas — funciona igual nas duas telas, sem precisar navegar entre elas
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

    // Permite abrir a modal já com uma data preenchida sem sair da página atual — usado pelo clique
    // num dia da grade em wwwroot/js/agenda.js
    window.MsNovaSessao = {
        abrirComData(dataIso) {
            bootstrap.Modal.getOrCreateInstance(modalNovaSessaoEl).show();
            if (dataInputNova) dataInputNova.value = dataIso || "";
            atualizarDataHoraNova();
        }
    };
});
