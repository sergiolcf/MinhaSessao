document.addEventListener("DOMContentLoaded", function () {
    const formCadastroPaciente = document.getElementById("formCadastroPaciente");
    if (!formCadastroPaciente) return;

    const feedbackErroEl = document.getElementById("cadastroPacienteFeedbackErro");
    const feedbackErroMensagemEl = document.getElementById("cadastroPacienteFeedbackErroMensagem");
    const btnSalvar = document.getElementById("btnSalvarPaciente");
    const btnSalvarSpinner = document.getElementById("btnSalvarPacienteSpinner");
    const btnSalvarTexto = document.getElementById("btnSalvarPacienteTexto");

    function definirCarregando(carregando) {
        if (!btnSalvar) return;
        btnSalvar.disabled = carregando;
        if (btnSalvarSpinner) {
            btnSalvarSpinner.classList.toggle("d-none", !carregando);
        }
        if (btnSalvarTexto) {
            btnSalvarTexto.textContent = carregando ? "Salvando..." : "Salvar Paciente";
        }
    }

    function exibirErro(mensagem) {
        if (!feedbackErroEl || !feedbackErroMensagemEl) return;
        feedbackErroMensagemEl.textContent = mensagem;
        feedbackErroEl.classList.remove("d-none");
    }

    function ocultarFeedback() {
        if (feedbackErroEl) feedbackErroEl.classList.add("d-none");
    }

    formCadastroPaciente.addEventListener("submit", async function (e) {
        e.preventDefault();
        ocultarFeedback();
        definirCarregando(true);

        const formData = new FormData(formCadastroPaciente);

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
                // Sucesso: fecha a modal de cadastro e exibe a senha temporária gerada para o paciente
                const modalCadastroEl = document.getElementById("modalCadastroPaciente");
                if (modalCadastroEl) {
                    modalCadastroEl.addEventListener("hidden.bs.modal", function aoFecharCadastro() {
                        modalCadastroEl.removeEventListener("hidden.bs.modal", aoFecharCadastro);
                        formCadastroPaciente.reset();
                        ocultarFeedback();
                        if (resultado.senhaTemporaria && typeof window.exibirSenhaTemporaria === "function") {
                            window.exibirSenhaTemporaria(resultado.senhaTemporaria, function () {
                                window.location.reload();
                            });
                        } else {
                            window.location.reload();
                        }
                    });
                    bootstrap.Modal.getOrCreateInstance(modalCadastroEl).hide();
                }
            } else {
                exibirErro(resultado.message || "Não foi possível concluir o cadastro.");
                definirCarregando(false);
            }
        } catch (erro) {
            exibirErro("Erro de conexão. Verifique sua internet e tente novamente.");
            definirCarregando(false);
        }
    });
});
