document.addEventListener("DOMContentLoaded", function () {
    const btnGerarSenha = document.getElementById("btnGerarNovaSenhaPaciente");
    if (!btnGerarSenha) return;

    const spinner = document.getElementById("btnGerarNovaSenhaPacienteSpinner");
    const icone = document.getElementById("btnGerarNovaSenhaPacienteIcone");
    const texto = document.getElementById("btnGerarNovaSenhaPacienteTexto");

    function definirCarregando(carregando) {
        btnGerarSenha.disabled = carregando;
        if (spinner) spinner.classList.toggle("d-none", !carregando);
        if (icone) icone.classList.toggle("d-none", carregando);
        if (texto) texto.textContent = carregando ? "Gerando..." : "Gerar nova senha";
    }

    btnGerarSenha.addEventListener("click", async function () {
        if (!window.confirm("Isso vai invalidar a senha atual do paciente. Deseja gerar uma nova senha temporária?")) return;

        definirCarregando(true);

        const token = document.querySelector('#tokenGerarNovaSenhaPaciente input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        formData.append("id", btnGerarSenha.dataset.pacienteId);
        if (token) formData.append("__RequestVerificationToken", token);

        try {
            const resposta = await fetch("/Pacientes/GerarNovaSenha", {
                method: "POST",
                body: formData
            });

            const resultado = await resposta.json();

            if (resposta.ok && resultado.success && typeof window.exibirSenhaTemporaria === "function") {
                window.exibirSenhaTemporaria(resultado.senhaTemporaria);
            } else {
                window.alert(resultado.message || "Não foi possível gerar a nova senha. Tente novamente.");
            }
        } catch {
            window.alert("Erro de conexão. Verifique sua internet e tente novamente.");
        } finally {
            definirCarregando(false);
        }
    });
});
