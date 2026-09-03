document.addEventListener("DOMContentLoaded", function () {
    const toastEl = document.getElementById("toastConfiguracoes");
    const toastMensagemEl = document.getElementById("toastConfiguracoesMensagem");
    const toast = toastEl ? bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 4000 }) : null;

    function exibirToast(mensagem, sucesso) {
        if (!toastEl || !toastMensagemEl || !toast) return;
        toastEl.classList.remove("text-bg-success", "text-bg-danger");
        toastEl.classList.add(sucesso ? "text-bg-success" : "text-bg-danger");
        toastMensagemEl.textContent = mensagem;
        toast.show();
    }

    function definirCarregando(botao, spinner, texto, carregando, textoCarregando, textoPadrao) {
        if (!botao) return;
        botao.disabled = carregando;
        if (spinner) spinner.classList.toggle("d-none", !carregando);
        if (texto) texto.textContent = carregando ? textoCarregando : textoPadrao;
    }

    // Aba "Meus Dados"
    const formDados = document.getElementById("formDadosPaciente");
    const btnSalvarDados = document.getElementById("btnSalvarDados");
    const btnSalvarDadosSpinner = document.getElementById("btnSalvarDadosSpinner");
    const btnSalvarDadosTexto = document.getElementById("btnSalvarDadosTexto");
    const btnEditarDados = document.getElementById("btnEditarDados");
    const btnCancelarDados = document.getElementById("btnCancelarDados");
    const camposEditaveis = [
        document.getElementById("dadosNomeCompleto"),
        document.getElementById("dadosEmail"),
        document.getElementById("dadosTelefone")
    ];
    const valoresOriginais = camposEditaveis.map(campo => campo ? campo.value : "");

    function definirModoEdicao(editando) {
        camposEditaveis.forEach(campo => {
            if (campo) campo.readOnly = !editando;
        });
        if (btnEditarDados) btnEditarDados.classList.toggle("d-none", editando);
        if (btnCancelarDados) btnCancelarDados.classList.toggle("d-none", !editando);
        if (btnSalvarDados) btnSalvarDados.classList.toggle("d-none", !editando);
    }

    if (btnEditarDados) {
        btnEditarDados.addEventListener("click", function () {
            definirModoEdicao(true);
            if (camposEditaveis[0]) camposEditaveis[0].focus();
        });
    }

    if (btnCancelarDados) {
        btnCancelarDados.addEventListener("click", function () {
            camposEditaveis.forEach((campo, i) => {
                if (campo) campo.value = valoresOriginais[i];
            });
            definirModoEdicao(false);
        });
    }

    if (formDados) {
        formDados.addEventListener("submit", async function (e) {
            e.preventDefault();
            definirCarregando(btnSalvarDados, btnSalvarDadosSpinner, btnSalvarDadosTexto, true, "Salvando...", "Salvar Alterações");

            const formData = new FormData(formDados);

            try {
                const resposta = await fetch("/PainelPaciente/AtualizarDados", {
                    method: "POST",
                    body: formData
                });

                let resultado;
                try {
                    resultado = await resposta.json();
                } catch {
                    resultado = { success: false, message: "Ocorreu um erro inesperado no servidor. Tente novamente." };
                }

                const sucessoDados = !!(resposta.ok && resultado.success);
                exibirToast(resultado.message || (sucessoDados ? "Dados atualizados com sucesso!" : "Não foi possível salvar os dados."), sucessoDados);

                if (sucessoDados) {
                    camposEditaveis.forEach((campo, i) => {
                        if (campo) valoresOriginais[i] = campo.value;
                    });
                    definirModoEdicao(false);
                }
            } catch (erro) {
                exibirToast("Erro de conexão. Verifique sua internet e tente novamente.", false);
            } finally {
                definirCarregando(btnSalvarDados, btnSalvarDadosSpinner, btnSalvarDadosTexto, false, "Salvando...", "Salvar Alterações");
            }
        });
    }

    // Aba "Segurança"
    const formSenha = document.getElementById("formSenhaPaciente");
    const btnAlterarSenha = document.getElementById("btnAlterarSenha");
    const btnAlterarSenhaSpinner = document.getElementById("btnAlterarSenhaSpinner");
    const btnAlterarSenhaTexto = document.getElementById("btnAlterarSenhaTexto");

    if (formSenha) {
        formSenha.addEventListener("submit", async function (e) {
            e.preventDefault();
            definirCarregando(btnAlterarSenha, btnAlterarSenhaSpinner, btnAlterarSenhaTexto, true, "Alterando...", "Alterar Senha");

            const formData = new FormData(formSenha);

            try {
                const resposta = await fetch("/PainelPaciente/AlterarSenha", {
                    method: "POST",
                    body: formData
                });

                let resultado;
                try {
                    resultado = await resposta.json();
                } catch {
                    resultado = { success: false, message: "Ocorreu um erro inesperado no servidor. Tente novamente." };
                }

                const sucesso = !!(resposta.ok && resultado.success);
                exibirToast(resultado.message || (sucesso ? "Senha alterada com sucesso!" : "Não foi possível alterar a senha."), sucesso);

                if (sucesso) {
                    formSenha.reset();
                }
            } catch (erro) {
                exibirToast("Erro de conexão. Verifique sua internet e tente novamente.", false);
            } finally {
                definirCarregando(btnAlterarSenha, btnAlterarSenhaSpinner, btnAlterarSenhaTexto, false, "Alterando...", "Alterar Senha");
            }
        });
    }
});
