document.addEventListener("DOMContentLoaded", function () {
    const modalCadastroEl = document.getElementById("modalCadastroPaciente");
    if (!modalCadastroEl) return;

    const tokenInput = modalCadastroEl.querySelector('input[name="__RequestVerificationToken"]');

    // Etapa 1: Verificar CPF
    const etapaVerificarCpfEl = document.getElementById("etapaVerificarCpf");
    const verificarCpfInput = document.getElementById("verificarCpfInput");
    const verificarCpfFeedbackErroEl = document.getElementById("verificarCpfFeedbackErro");
    const verificarCpfFeedbackErroMensagemEl = document.getElementById("verificarCpfFeedbackErroMensagem");
    const btnVerificarCpf = document.getElementById("btnVerificarCpf");
    const btnVerificarCpfSpinner = document.getElementById("btnVerificarCpfSpinner");
    const btnVerificarCpfTexto = document.getElementById("btnVerificarCpfTexto");

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
    const btnTrocarCpf = document.getElementById("btnTrocarCpf");
    const btnSalvar = document.getElementById("btnSalvarPaciente");
    const btnSalvarSpinner = document.getElementById("btnSalvarPacienteSpinner");
    const btnSalvarTexto = document.getElementById("btnSalvarPacienteTexto");

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

    function irParaEtapa(etapa) {
        etapaVerificarCpfEl.classList.toggle("d-none", etapa !== "verificar");
        etapaPacienteEncontradoEl.classList.toggle("d-none", etapa !== "encontrado");
        formCadastroPaciente.classList.toggle("d-none", etapa !== "cadastro");

        btnVerificarCpf.classList.toggle("d-none", etapa !== "verificar");
        btnVincularPaciente.classList.toggle("d-none", etapa !== "encontrado");
        btnSalvar.classList.toggle("d-none", etapa !== "cadastro");
    }

    function resetarModalParaEtapaInicial() {
        irParaEtapa("verificar");
        verificarCpfInput.value = "";
        pacienteEncontradoId = null;
        ocultarErroVerificar();
        ocultarErroCadastro();
        formCadastroPaciente.reset();
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

    // Etapa 1 -> verifica se já existe um paciente com esse CPF
    async function verificarCpf() {
        const cpf = verificarCpfInput.value.trim();
        ocultarErroVerificar();

        if (!cpf) {
            exibirErroVerificar("Informe o CPF do paciente.");
            return;
        }

        if (cpf.replace(/\D/g, "").length !== 11) {
            exibirErroVerificar("Informe um CPF válido (11 dígitos).");
            return;
        }

        definirCarregandoVerificar(true);

        try {
            const resposta = await fetch("/Pacientes/VerificarPacienteExistente?cpf=" + encodeURIComponent(cpf));

            if (!resposta.ok) {
                exibirErroVerificar("Ocorreu um erro inesperado no servidor. Tente novamente.");
                return;
            }

            const resultado = await resposta.json();

            if (resultado.existe) {
                pacienteEncontradoId = resultado.pacienteId;
                if (pacienteEncontradoIniciaisEl) pacienteEncontradoIniciaisEl.textContent = resultado.iniciais;
                if (pacienteEncontradoNomeEl) pacienteEncontradoNomeEl.textContent = resultado.nomeCompleto;
                irParaEtapa("encontrado");
            } else {
                if (pacienteCpfInput) pacienteCpfInput.value = cpf;
                irParaEtapa("cadastro");
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
