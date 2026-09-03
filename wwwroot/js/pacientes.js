document.addEventListener("DOMContentLoaded", function () {
    const formCadastroPaciente = document.getElementById("formCadastroPaciente");
    if (!formCadastroPaciente) return;

    const feedbackErroEl = document.getElementById("cadastroPacienteFeedbackErro");
    const feedbackErroMensagemEl = document.getElementById("cadastroPacienteFeedbackErroMensagem");
    const feedbackSucessoEl = document.getElementById("cadastroPacienteFeedbackSucesso");
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
        if (feedbackSucessoEl) feedbackSucessoEl.classList.add("d-none");
        if (!feedbackErroEl || !feedbackErroMensagemEl) return;
        feedbackErroMensagemEl.textContent = mensagem;
        feedbackErroEl.classList.remove("d-none");
    }

    function ocultarFeedback() {
        if (feedbackErroEl) feedbackErroEl.classList.add("d-none");
        if (feedbackSucessoEl) feedbackSucessoEl.classList.add("d-none");
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
                // Sucesso: mostra o alerta verde e recarrega a página para atualizar a tabela
                if (feedbackErroEl) feedbackErroEl.classList.add("d-none");
                if (feedbackSucessoEl) feedbackSucessoEl.classList.remove("d-none");

                setTimeout(function () {
                    window.location.reload();
                }, 1200);
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
