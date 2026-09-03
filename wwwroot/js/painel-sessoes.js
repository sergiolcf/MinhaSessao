document.addEventListener("DOMContentLoaded", function () {
    const modalEl = document.getElementById("modalDetalhesSessao");
    if (!modalEl) return;

    const carregandoEl = document.getElementById("detalhesSessaoCarregando");
    const conteudoEl = document.getElementById("detalhesSessaoConteudo");
    const erroEl = document.getElementById("detalhesSessaoErro");
    const dataEl = document.getElementById("detalhesSessaoData");
    const horaEl = document.getElementById("detalhesSessaoHora");
    const profissionalEl = document.getElementById("detalhesSessaoProfissional");
    const statusEl = document.getElementById("detalhesSessaoStatus");

    function definirEstadoModal(estado) {
        if (carregandoEl) carregandoEl.classList.toggle("d-none", estado !== "carregando");
        if (conteudoEl) conteudoEl.classList.toggle("d-none", estado !== "conteudo");
        if (erroEl) erroEl.classList.toggle("d-none", estado !== "erro");
    }

    async function abrirDetalhesSessao(sessaoId) {
        definirEstadoModal("carregando");
        bootstrap.Modal.getOrCreateInstance(modalEl).show();

        try {
            const resposta = await fetch(`/PainelPaciente/DetalhesSessao?id=${encodeURIComponent(sessaoId)}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) {
                definirEstadoModal("erro");
                return;
            }

            if (dataEl) dataEl.textContent = resultado.data;
            if (horaEl) horaEl.textContent = resultado.hora;
            if (profissionalEl) profissionalEl.textContent = resultado.profissionalNome;
            if (statusEl) statusEl.textContent = resultado.status;

            definirEstadoModal("conteudo");
        } catch (erro) {
            definirEstadoModal("erro");
        }
    }

    document.addEventListener("click", function (e) {
        const botaoDetalhes = e.target.closest(".btn-ver-detalhes-sessao");
        if (botaoDetalhes) {
            abrirDetalhesSessao(botaoDetalhes.dataset.sessaoId);
        }
    });
});
