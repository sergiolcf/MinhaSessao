// Escapa texto para uso seguro em conteúdo HTML (mesma implementação usada em sessoes.js/plano-tratamento.js)
function escaparHtml(texto) {
    return (texto || "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

document.addEventListener("DOMContentLoaded", function () {
    // Linha da tabela inteira leva pra ficha do paciente, exceto cliques em links/botões (ex.: "Ver ficha")
    const tbodyPacientes = document.getElementById("tbodyPacientes");
    if (tbodyPacientes) {
        tbodyPacientes.addEventListener("click", function (e) {
            if (e.target.closest("a") || e.target.closest("button")) return;

            const linha = e.target.closest("tr[data-paciente-id]");
            if (!linha) return;

            window.location.href = linha.dataset.pacienteUrl;
        });
    }

    // ----- Paginação (10 por página) + busca por nome/CPF de "Meus Pacientes" -----
    const cardPacientes = document.getElementById("cardPacientes");
    const paginacaoPacientes = document.getElementById("paginacaoPacientes");
    const filtroPacienteBuscaInput = document.getElementById("filtroPacienteBusca");

    if (tbodyPacientes && cardPacientes) {
        let ms_paginaPacientes = parseInt(cardPacientes.dataset.paginaAtual || "1", 10);
        let ms_totalPaginasPacientes = parseInt(cardPacientes.dataset.totalPaginas || "1", 10);
        // Último max-height calculado com sucesso — reaproveitado quando não há linha de dados pra medir
        // (ex.: resultado de busca vazio), pra não aplicar um valor errado nessa hora
        let ms_alturaTabelaPacientesCalculada = null;

        // Calcula o max-height do scroll (cabeçalho + 4 linhas) medindo a altura real já renderizada,
        // já que a coluna Contato empilha duas linhas (WhatsApp + e-mail) e isso varia com o conteúdo
        function ajustarAlturaTabelaPacientes() {
            const scrollEl = document.querySelector(".ms-dash-table-scroll-pacientes");
            if (!scrollEl) return;

            const thead = scrollEl.querySelector("thead");
            const primeiraLinha = scrollEl.querySelector("tbody tr[data-paciente-id]");

            if (!thead || !primeiraLinha) {
                if (ms_alturaTabelaPacientesCalculada) {
                    scrollEl.style.maxHeight = `${ms_alturaTabelaPacientesCalculada}px`;
                }
                return;
            }

            const alturaThead = thead.getBoundingClientRect().height;
            const alturaLinha = primeiraLinha.getBoundingClientRect().height;

            ms_alturaTabelaPacientesCalculada = alturaThead + alturaLinha * 4;
            scrollEl.style.maxHeight = `${ms_alturaTabelaPacientesCalculada}px`;
        }

        ajustarAlturaTabelaPacientes();

        // Recalcula ao redimensionar a janela (com debounce simples, sem exagerar)
        let ms_resizeTabelaPacientesDebounce = null;
        window.addEventListener("resize", function () {
            clearTimeout(ms_resizeTabelaPacientesDebounce);
            ms_resizeTabelaPacientesDebounce = setTimeout(ajustarAlturaTabelaPacientes, 200);
        });

        function construirLinhaPaciente(paciente) {
            const tr = document.createElement("tr");
            tr.dataset.pacienteId = paciente.id;
            tr.dataset.pacienteUrl = paciente.url;
            tr.innerHTML = `
                <td>
                    <a href="${paciente.url}" class="ms-dash-paciente-link">
                        <span class="ms-avatar-iniciais">${escaparHtml(paciente.iniciais)}</span>
                        <span>${escaparHtml(paciente.nomeCompleto)}</span>
                    </a>
                </td>
                <td>
                    <div class="ms-dash-contact-line"><i class="bi bi-whatsapp"></i> ${escaparHtml(paciente.telefone)}</div>
                    <div class="ms-dash-contact-line ms-dash-table-subtext"><i class="bi bi-envelope"></i> ${escaparHtml(paciente.email)}</div>
                </td>
                <td>
                    ${escaparHtml(paciente.dataNascimento)}
                    <div class="ms-dash-table-subtext">${paciente.idade} anos</div>
                </td>
                <td>
                    ${paciente.ativo
                        ? `<span class="badge ms-badge-ativo">Ativo</span>`
                        : `<span class="badge ms-badge-inativo">Inativo</span>`}
                </td>
                <td class="text-end">
                    <a href="${paciente.url}" class="ms-dash-row-link" title="Ver ficha">
                        <i class="bi bi-chevron-right"></i>
                    </a>
                </td>
            `;
            return tr;
        }

        function renderizarPacientes(pacientes, termoBusca) {
            tbodyPacientes.innerHTML = "";

            if (!pacientes || pacientes.length === 0) {
                const texto = termoBusca
                    ? `Nenhum paciente encontrado para "${escaparHtml(termoBusca)}".`
                    : "Nenhum paciente cadastrado ainda.";
                tbodyPacientes.innerHTML = `<tr><td colspan="5" class="text-center ms-dash-table-subtext py-4">${texto}</td></tr>`;
                return;
            }

            pacientes.forEach(function (paciente) {
                tbodyPacientes.appendChild(construirLinhaPaciente(paciente));
            });
        }

        function renderizarPaginacaoPacientes(paginaAtual, totalPaginas) {
            if (!paginacaoPacientes) return;
            paginacaoPacientes.innerHTML = "";

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
                paginacaoPacientes.appendChild(li);
            }
        }

        async function carregarPacientes(pagina) {
            const termo = filtroPacienteBuscaInput ? filtroPacienteBuscaInput.value.trim() : "";

            try {
                const parametros = new URLSearchParams({ pagina: String(pagina) });
                if (termo) parametros.set("termo", termo);

                const resposta = await fetch(`/Pacientes/BuscarPacientes?${parametros.toString()}`);
                const resultado = await resposta.json();

                if (!resposta.ok || !resultado.success) return;

                renderizarPacientes(resultado.pacientes, termo);
                renderizarPaginacaoPacientes(resultado.paginaAtual, resultado.totalPaginas);
                ajustarAlturaTabelaPacientes();
                ms_paginaPacientes = resultado.paginaAtual;
                ms_totalPaginasPacientes = resultado.totalPaginas;
            } catch (erro) {
                // Mantém a lista atual em caso de falha de conexão
            }
        }

        if (paginacaoPacientes) {
            paginacaoPacientes.addEventListener("click", function (e) {
                const botao = e.target.closest(".page-link");
                if (!botao) return;
                const pagina = parseInt(botao.dataset.pagina, 10);
                if (pagina === ms_paginaPacientes) return;
                carregarPacientes(pagina);
            });
        }

        if (filtroPacienteBuscaInput) {
            let filtroPacienteBuscaDebounce = null;

            filtroPacienteBuscaInput.addEventListener("input", function () {
                clearTimeout(filtroPacienteBuscaDebounce);
                // Toda nova busca reinicia na página 1 (o termo muda o total de resultados)
                filtroPacienteBuscaDebounce = setTimeout(function () {
                    carregarPacientes(1);
                }, 400);
            });
        }
    }

    const modalCadastroEl = document.getElementById("modalCadastroPaciente");
    if (!modalCadastroEl) return;

    const tokenInput = modalCadastroEl.querySelector('input[name="__RequestVerificationToken"]');

    // Etapa 0: Este paciente precisa de um responsável? (menor de idade, CARD-35)
    const etapaNecessitaResponsavelEl = document.getElementById("etapaNecessitaResponsavel");
    const btnResponsavelNao = document.getElementById("btnResponsavelNao");
    const btnResponsavelSim = document.getElementById("btnResponsavelSim");
    let necessitaResponsavel = false;

    // Etapa 1: Verificar CPF
    const etapaVerificarCpfEl = document.getElementById("etapaVerificarCpf");
    const verificarCpfInput = document.getElementById("verificarCpfInput");
    const verificarCpfPacienteOpcionalEl = document.getElementById("verificarCpfPacienteOpcional");
    const verificarCpfPacienteRequeridoEl = document.getElementById("verificarCpfPacienteRequerido");
    const verificarCpfResponsavelWrapperEl = document.getElementById("verificarCpfResponsavelWrapper");
    const verificarCpfResponsavelInput = document.getElementById("verificarCpfResponsavelInput");
    const verificarCpfFeedbackErroEl = document.getElementById("verificarCpfFeedbackErro");
    const verificarCpfFeedbackErroMensagemEl = document.getElementById("verificarCpfFeedbackErroMensagem");
    const btnVerificarCpf = document.getElementById("btnVerificarCpf");
    const btnVerificarCpfSpinner = document.getElementById("btnVerificarCpfSpinner");
    const btnVerificarCpfTexto = document.getElementById("btnVerificarCpfTexto");

    // Etapa 1b: Mais de um paciente encontrado com o mesmo Responsável
    const etapaEscolherPacienteEl = document.getElementById("etapaEscolherPaciente");
    const listaPacientesEncontradosEl = document.getElementById("listaPacientesEncontradosResponsavel");
    const btnNenhumDessesPacientes = document.getElementById("btnNenhumDessesPacientes");

    // Etapa 2: Paciente encontrado
    const etapaPacienteEncontradoEl = document.getElementById("etapaPacienteEncontrado");
    const pacienteEncontradoIniciaisEl = document.getElementById("pacienteEncontradoIniciais");
    const pacienteEncontradoNomeEl = document.getElementById("pacienteEncontradoNome");
    const btnVerificarOutroCpf = document.getElementById("btnVerificarOutroCpf");
    const btnVincularPaciente = document.getElementById("btnVincularPaciente");
    const btnVincularPacienteSpinner = document.getElementById("btnVincularPacienteSpinner");
    const btnVincularPacienteTexto = document.getElementById("btnVincularPacienteTexto");
    let pacienteEncontradoId = null;

    // Etapa 3: Cadastro completo
    const formCadastroPaciente = document.getElementById("formCadastroPaciente");
    const feedbackErroEl = document.getElementById("cadastroPacienteFeedbackErro");
    const feedbackErroMensagemEl = document.getElementById("cadastroPacienteFeedbackErroMensagem");
    const pacienteCpfInput = document.getElementById("PacienteCpf");
    const pacienteCpfOpcionalEl = document.getElementById("pacienteCpfOpcional");
    const pacienteCpfRequeridoEl = document.getElementById("pacienteCpfRequerido");
    const btnTrocarCpf = document.getElementById("btnTrocarCpf");
    const btnSalvar = document.getElementById("btnSalvarPaciente");
    const btnSalvarSpinner = document.getElementById("btnSalvarPacienteSpinner");
    const btnSalvarTexto = document.getElementById("btnSalvarPacienteTexto");
    const cadastroResponsavelFieldsEl = document.getElementById("cadastroResponsavelFields");
    const pacienteNecessitaResponsavelInput = document.getElementById("PacienteNecessitaResponsavel");
    const pacienteCpfResponsavelInput = document.getElementById("PacienteCpfResponsavel");

    // Aplica a máscara 000.000.000-00 a partir de um valor com ou sem pontuação
    function aplicarMascaraCpf(valor) {
        return valor
            .replace(/\D/g, "")
            .slice(0, 11)
            .replace(/(\d{3})(\d)/, "$1.$2")
            .replace(/(\d{3})(\d)/, "$1.$2")
            .replace(/(\d{3})(\d{1,2})$/, "$1-$2");
    }

    if (verificarCpfInput) {
        verificarCpfInput.addEventListener("input", function () {
            verificarCpfInput.value = aplicarMascaraCpf(verificarCpfInput.value);
        });
    }

    if (verificarCpfResponsavelInput) {
        verificarCpfResponsavelInput.addEventListener("input", function () {
            verificarCpfResponsavelInput.value = aplicarMascaraCpf(verificarCpfResponsavelInput.value);
        });
    }

    function irParaEtapa(etapa) {
        etapaNecessitaResponsavelEl.classList.toggle("d-none", etapa !== "responsavel");
        etapaVerificarCpfEl.classList.toggle("d-none", etapa !== "verificar");
        etapaEscolherPacienteEl.classList.toggle("d-none", etapa !== "escolha");
        etapaPacienteEncontradoEl.classList.toggle("d-none", etapa !== "encontrado");
        formCadastroPaciente.classList.toggle("d-none", etapa !== "cadastro");

        btnVerificarCpf.classList.toggle("d-none", etapa !== "verificar");
        btnVincularPaciente.classList.toggle("d-none", etapa !== "encontrado");
        btnSalvar.classList.toggle("d-none", etapa !== "cadastro");
    }

    // Mostra/oculta o campo "CPF do Responsável" e ajusta o rótulo do CPF do Paciente (opcional quando
    // necessitaResponsavel é true) na Etapa 1 — chamado ao trocar de resposta na Etapa 0 e ao reabrir a modal
    function atualizarCamposEtapaVerificar() {
        if (verificarCpfResponsavelWrapperEl) verificarCpfResponsavelWrapperEl.classList.toggle("d-none", !necessitaResponsavel);
        if (verificarCpfPacienteOpcionalEl) verificarCpfPacienteOpcionalEl.classList.toggle("d-none", !necessitaResponsavel);
        if (verificarCpfPacienteRequeridoEl) verificarCpfPacienteRequeridoEl.classList.toggle("d-none", necessitaResponsavel);
    }

    function resetarModalParaEtapaInicial() {
        necessitaResponsavel = false;
        // Toda reabertura volta pro estado neutro: nenhum dos dois botões começa destacado
        if (btnResponsavelNao) btnResponsavelNao.classList.remove("active");
        if (btnResponsavelSim) btnResponsavelSim.classList.remove("active");
        atualizarCamposEtapaVerificar();
        irParaEtapa("responsavel");
        verificarCpfInput.value = "";
        if (verificarCpfResponsavelInput) verificarCpfResponsavelInput.value = "";
        pacienteEncontradoId = null;
        ocultarErroVerificar();
        ocultarErroCadastro();
        formCadastroPaciente.reset();
    }

    // Os dois botões (Não/Sim) começam com o mesmo peso visual — nenhuma resposta é "a esperada" — e o
    // clicado passa a ficar destacado em laranja (.active), voltando o outro pro estado neutro
    function marcarBotaoResponsavelSelecionado(botaoSelecionado) {
        if (btnResponsavelNao) btnResponsavelNao.classList.toggle("active", botaoSelecionado === btnResponsavelNao);
        if (btnResponsavelSim) btnResponsavelSim.classList.toggle("active", botaoSelecionado === btnResponsavelSim);
    }

    if (btnResponsavelNao) {
        btnResponsavelNao.addEventListener("click", function () {
            necessitaResponsavel = false;
            marcarBotaoResponsavelSelecionado(btnResponsavelNao);
            atualizarCamposEtapaVerificar();
            irParaEtapa("verificar");
        });
    }

    if (btnResponsavelSim) {
        btnResponsavelSim.addEventListener("click", function () {
            necessitaResponsavel = true;
            marcarBotaoResponsavelSelecionado(btnResponsavelSim);
            atualizarCamposEtapaVerificar();
            irParaEtapa("verificar");
        });
    }

    // Leva a modal pra Etapa 3 (cadastro completo), já preenchendo/ocultando os campos de acordo com o
    // que foi apurado na Etapa 1 — reaproveitado tanto por "CPF não encontrado" quanto por
    // "Nenhum desses, cadastrar novo paciente" (Etapa 1b)
    function irParaCadastroCompleto(cpfPacienteValor, cpfResponsavelValor) {
        if (pacienteCpfInput) {
            pacienteCpfInput.value = cpfPacienteValor || "";
            // Só fica readonly quando o CPF do paciente já veio preenchido da etapa de verificação —
            // se ficou em branco (paciente menor sem CPF próprio), o profissional pode digitar aqui
            pacienteCpfInput.readOnly = !!cpfPacienteValor;
        }
        if (pacienteCpfOpcionalEl) pacienteCpfOpcionalEl.classList.toggle("d-none", !necessitaResponsavel);
        if (pacienteCpfRequeridoEl) pacienteCpfRequeridoEl.classList.toggle("d-none", necessitaResponsavel);
        if (btnTrocarCpf) btnTrocarCpf.classList.toggle("d-none", !cpfPacienteValor);

        if (cadastroResponsavelFieldsEl) cadastroResponsavelFieldsEl.classList.toggle("d-none", !necessitaResponsavel);
        if (pacienteNecessitaResponsavelInput) pacienteNecessitaResponsavelInput.value = necessitaResponsavel ? "true" : "false";

        if (necessitaResponsavel && pacienteCpfResponsavelInput) {
            pacienteCpfResponsavelInput.value = cpfResponsavelValor || "";
        }

        irParaEtapa("cadastro");
    }

    // Renderiza a lista de pacientes encontrados pelo CPF do Responsável (Etapa 1b) — clicar num deles
    // leva pra Etapa 2 (encontrado), reaproveitando o mesmo fluxo de vincular paciente já existente
    function renderizarListaPacientesEncontrados(pacientes) {
        if (!listaPacientesEncontradosEl) return;
        listaPacientesEncontradosEl.innerHTML = "";

        (pacientes || []).forEach(function (paciente) {
            const botao = document.createElement("button");
            botao.type = "button";
            botao.className = "btn btn-outline-secondary text-start d-flex align-items-center gap-2";
            botao.innerHTML = `<span class="ms-avatar-iniciais">${escaparHtml(paciente.iniciais)}</span> ${escaparHtml(paciente.nomeCompleto)}`;
            botao.addEventListener("click", function () {
                pacienteEncontradoId = paciente.id;
                if (pacienteEncontradoIniciaisEl) pacienteEncontradoIniciaisEl.textContent = paciente.iniciais;
                if (pacienteEncontradoNomeEl) pacienteEncontradoNomeEl.textContent = paciente.nomeCompleto;
                irParaEtapa("encontrado");
            });
            listaPacientesEncontradosEl.appendChild(botao);
        });
    }

    if (btnNenhumDessesPacientes) {
        btnNenhumDessesPacientes.addEventListener("click", function () {
            irParaCadastroCompleto("", verificarCpfResponsavelInput ? verificarCpfResponsavelInput.value.trim() : "");
        });
    }

    function definirCarregandoVerificar(carregando) {
        btnVerificarCpf.disabled = carregando;
        if (btnVerificarCpfSpinner) btnVerificarCpfSpinner.classList.toggle("d-none", !carregando);
        if (btnVerificarCpfTexto) btnVerificarCpfTexto.textContent = carregando ? "Verificando..." : "Verificar";
    }

    function exibirErroVerificar(mensagem) {
        if (!verificarCpfFeedbackErroEl || !verificarCpfFeedbackErroMensagemEl) return;
        verificarCpfFeedbackErroMensagemEl.textContent = mensagem;
        verificarCpfFeedbackErroEl.classList.remove("d-none");
    }

    function ocultarErroVerificar() {
        if (verificarCpfFeedbackErroEl) verificarCpfFeedbackErroEl.classList.add("d-none");
    }

    function definirCarregandoVincular(carregando) {
        btnVincularPaciente.disabled = carregando;
        if (btnVincularPacienteSpinner) btnVincularPacienteSpinner.classList.toggle("d-none", !carregando);
        if (btnVincularPacienteTexto) btnVincularPacienteTexto.textContent = carregando ? "Vinculando..." : "Vincular este paciente";
    }

    function definirCarregandoSalvar(carregando) {
        if (!btnSalvar) return;
        btnSalvar.disabled = carregando;
        if (btnSalvarSpinner) btnSalvarSpinner.classList.toggle("d-none", !carregando);
        if (btnSalvarTexto) btnSalvarTexto.textContent = carregando ? "Salvando..." : "Salvar Paciente";
    }

    function exibirErroCadastro(mensagem) {
        if (!feedbackErroEl || !feedbackErroMensagemEl) return;
        feedbackErroMensagemEl.textContent = mensagem;
        feedbackErroEl.classList.remove("d-none");
    }

    function ocultarErroCadastro() {
        if (feedbackErroEl) feedbackErroEl.classList.add("d-none");
    }

    // Toda vez que o modal abre, volta pro estado inicial (etapa "Verificar CPF")
    modalCadastroEl.addEventListener("show.bs.modal", resetarModalParaEtapaInicial);

    // Etapa 1 -> verifica se já existe um paciente com esse CPF (ou, quando necessitaResponsavel e o
    // CPF do paciente ficou em branco, com o CPF do Responsável — CARD-35)
    async function verificarCpf() {
        ocultarErroVerificar();

        const cpfPaciente = verificarCpfInput.value.trim();
        const cpfResponsavel = necessitaResponsavel && verificarCpfResponsavelInput ? verificarCpfResponsavelInput.value.trim() : "";

        // O CPF do paciente tem prioridade se preenchido; senão, busca pelo do responsável
        const usarResponsavel = necessitaResponsavel && !cpfPaciente;
        const cpfParaBuscar = usarResponsavel ? cpfResponsavel : cpfPaciente;

        if (!cpfParaBuscar) {
            exibirErroVerificar(necessitaResponsavel ? "Informe o CPF do paciente ou do responsável." : "Informe o CPF do paciente.");
            return;
        }

        if (cpfParaBuscar.replace(/\D/g, "").length !== 11) {
            exibirErroVerificar(usarResponsavel ? "Informe um CPF do responsável válido (11 dígitos)." : "Informe um CPF válido (11 dígitos).");
            return;
        }

        definirCarregandoVerificar(true);

        try {
            const parametros = new URLSearchParams();
            if (usarResponsavel) {
                parametros.set("cpfResponsavel", cpfResponsavel);
            } else {
                parametros.set("cpf", cpfPaciente);
            }

            const resposta = await fetch("/Pacientes/VerificarPacienteExistente?" + parametros.toString());

            if (!resposta.ok) {
                exibirErroVerificar("Ocorreu um erro inesperado no servidor. Tente novamente.");
                return;
            }

            const resultado = await resposta.json();

            if (!resultado.cpfValido) {
                exibirErroVerificar(usarResponsavel ? "CPF do responsável inválido. Verifique os números digitados." : "CPF inválido. Verifique os números digitados.");
                return;
            }

            if (resultado.multiplos) {
                renderizarListaPacientesEncontrados(resultado.pacientes);
                irParaEtapa("escolha");
            } else if (resultado.existe) {
                pacienteEncontradoId = resultado.pacienteId;
                if (pacienteEncontradoIniciaisEl) pacienteEncontradoIniciaisEl.textContent = resultado.iniciais;
                if (pacienteEncontradoNomeEl) pacienteEncontradoNomeEl.textContent = resultado.nomeCompleto;
                irParaEtapa("encontrado");
            } else {
                irParaCadastroCompleto(cpfPaciente, cpfResponsavel);
            }
        } catch (erro) {
            exibirErroVerificar("Erro de conexão. Verifique sua internet e tente novamente.");
        } finally {
            definirCarregandoVerificar(false);
        }
    }

    if (btnVerificarCpf) {
        btnVerificarCpf.addEventListener("click", verificarCpf);
    }

    if (verificarCpfInput) {
        verificarCpfInput.addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                verificarCpf();
            }
        });
    }

    // Etapa 2 -> "Não é esse paciente? Verificar outro CPF" volta pra etapa 1, limpando o CPF
    if (btnVerificarOutroCpf) {
        btnVerificarOutroCpf.addEventListener("click", function () {
            verificarCpfInput.value = "";
            if (verificarCpfResponsavelInput) verificarCpfResponsavelInput.value = "";
            pacienteEncontradoId = null;
            irParaEtapa("verificar");
        });
    }

    // Etapa 2 -> vincula o paciente já existente ao profissional logado
    if (btnVincularPaciente) {
        btnVincularPaciente.addEventListener("click", async function () {
            if (!pacienteEncontradoId) return;

            definirCarregandoVincular(true);

            const formData = new FormData();
            formData.append("pacienteId", pacienteEncontradoId);
            if (tokenInput) formData.append("__RequestVerificationToken", tokenInput.value);

            try {
                const resposta = await fetch("/Pacientes/Vincular", {
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
                    modalCadastroEl.addEventListener("hidden.bs.modal", function aoFechar() {
                        modalCadastroEl.removeEventListener("hidden.bs.modal", aoFechar);
                        window.alert(resultado.message || "Paciente vinculado com sucesso!");
                        window.location.reload();
                    });
                    bootstrap.Modal.getOrCreateInstance(modalCadastroEl).hide();
                } else {
                    window.alert(resultado.message || "Não foi possível vincular o paciente. Tente novamente.");
                }
            } catch (erro) {
                window.alert("Erro de conexão. Verifique sua internet e tente novamente.");
            } finally {
                definirCarregandoVincular(false);
            }
        });
    }

    // Etapa 3 -> "trocar CPF" volta pra etapa 1, mantendo o valor já digitado para edição
    if (btnTrocarCpf) {
        btnTrocarCpf.addEventListener("click", function () {
            ocultarErroCadastro();
            irParaEtapa("verificar");
        });
    }

    // Etapa 3 -> cadastro completo do paciente (paciente novo)
    if (formCadastroPaciente) {
        formCadastroPaciente.addEventListener("submit", async function (e) {
            e.preventDefault();
            ocultarErroCadastro();
            definirCarregandoSalvar(true);

            const formData = new FormData(formCadastroPaciente);
            if (tokenInput) formData.append("__RequestVerificationToken", tokenInput.value);

            try {
                const resposta = await fetch("/Pacientes/Criar", {
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
                    // Sucesso: fecha a modal de cadastro e exibe a modal de senha temporária do paciente novo.
                    modalCadastroEl.addEventListener("hidden.bs.modal", function aoFecharCadastro() {
                        modalCadastroEl.removeEventListener("hidden.bs.modal", aoFecharCadastro);
                        if (resultado.senhaTemporaria && typeof window.exibirSenhaTemporaria === "function") {
                            window.exibirSenhaTemporaria(resultado.senhaTemporaria, function () {
                                window.location.reload();
                            });
                        } else {
                            window.location.reload();
                        }
                    });
                    bootstrap.Modal.getOrCreateInstance(modalCadastroEl).hide();
                } else {
                    exibirErroCadastro(resultado.message || "Não foi possível concluir o cadastro.");
                    definirCarregandoSalvar(false);
                }
            } catch (erro) {
                exibirErroCadastro("Erro de conexão. Verifique sua internet e tente novamente.");
                definirCarregandoSalvar(false);
            }
        });
    }
});
