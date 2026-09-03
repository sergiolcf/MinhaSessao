document.addEventListener("DOMContentLoaded", function () {
    const toastEl = document.getElementById("toastConfiguracoesProfissional");
    const toastMensagemEl = document.getElementById("toastConfiguracoesProfissionalMensagem");
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

    async function enviarFormulario(form, url, textoCarregando, textoPadrao, botao, spinner, texto, aoSucesso) {
        definirCarregando(botao, spinner, texto, true, textoCarregando, textoPadrao);

        const formData = new FormData(form);

        try {
            const resposta = await fetch(url, {
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
            exibirToast(resultado.message || (sucesso ? "Alterações salvas com sucesso!" : "Não foi possível salvar as alterações."), sucesso);

            if (sucesso && aoSucesso) aoSucesso();
        } catch (erro) {
            exibirToast("Erro de conexão. Verifique sua internet e tente novamente.", false);
        } finally {
            definirCarregando(botao, spinner, texto, false, textoCarregando, textoPadrao);
        }
    }

    // Aba "Perfil Profissional" (modo leitura / edição)
    const formPerfil = document.getElementById("formPerfilProfissional");
    const btnEditarPerfil = document.getElementById("btnEditarPerfil");
    const btnCancelarPerfil = document.getElementById("btnCancelarPerfil");
    const btnSalvarPerfil = document.getElementById("btnSalvarPerfil");
    // E-mail fica de fora: é o login de acesso e nunca entra em modo edição
    const camposEditaveisPerfil = [
        document.getElementById("perfilNomeCompleto"),
        document.getElementById("perfilRegistroCRP"),
        document.getElementById("perfilTelefone"),
        document.getElementById("perfilAbordagem"),
        document.getElementById("perfilApresentacao")
    ];
    const valoresOriginaisPerfil = camposEditaveisPerfil.map(campo => campo ? campo.value : "");

    function definirModoEdicaoPerfil(editando) {
        camposEditaveisPerfil.forEach(campo => {
            if (campo) campo.readOnly = !editando;
        });
        if (btnEditarPerfil) btnEditarPerfil.classList.toggle("d-none", editando);
        if (btnCancelarPerfil) btnCancelarPerfil.classList.toggle("d-none", !editando);
        if (btnSalvarPerfil) btnSalvarPerfil.classList.toggle("d-none", !editando);
    }

    if (btnEditarPerfil) {
        btnEditarPerfil.addEventListener("click", function () {
            definirModoEdicaoPerfil(true);
            if (camposEditaveisPerfil[0]) camposEditaveisPerfil[0].focus();
        });
    }

    if (btnCancelarPerfil) {
        btnCancelarPerfil.addEventListener("click", function () {
            camposEditaveisPerfil.forEach((campo, i) => {
                if (campo) campo.value = valoresOriginaisPerfil[i];
            });
            definirModoEdicaoPerfil(false);
        });
    }

    if (formPerfil) {
        formPerfil.addEventListener("submit", function (e) {
            e.preventDefault();
            enviarFormulario(
                formPerfil,
                "/Dashboard/AtualizarPerfil",
                "Salvando...",
                "Salvar Alterações",
                btnSalvarPerfil,
                document.getElementById("btnSalvarPerfilSpinner"),
                document.getElementById("btnSalvarPerfilTexto"),
                function () {
                    camposEditaveisPerfil.forEach((campo, i) => {
                        if (campo) valoresOriginaisPerfil[i] = campo.value;
                    });
                    definirModoEdicaoPerfil(false);
                }
            );
        });
    }

    // Aba "Preferências da Clínica"
    const formPreferencias = document.getElementById("formPreferenciasProfissional");
    if (formPreferencias) {
        formPreferencias.addEventListener("submit", function (e) {
            e.preventDefault();
            enviarFormulario(
                formPreferencias,
                "/Dashboard/AtualizarPreferencias",
                "Salvando...",
                "Salvar Alterações",
                document.getElementById("btnSalvarPreferencias"),
                document.getElementById("btnSalvarPreferenciasSpinner"),
                document.getElementById("btnSalvarPreferenciasTexto")
            );
        });
    }

    // Aba "Segurança"
    const formSenha = document.getElementById("formSenhaProfissional");
    if (formSenha) {
        formSenha.addEventListener("submit", function (e) {
            e.preventDefault();
            enviarFormulario(
                formSenha,
                "/Dashboard/AlterarSenha",
                "Alterando...",
                "Alterar Senha",
                document.getElementById("btnAlterarSenhaProfissional"),
                document.getElementById("btnAlterarSenhaProfissionalSpinner"),
                document.getElementById("btnAlterarSenhaProfissionalTexto"),
                function () { formSenha.reset(); }
            );
        });
    }
});
