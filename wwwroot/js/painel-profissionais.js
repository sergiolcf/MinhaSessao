document.addEventListener("DOMContentLoaded", function () {
    const listaTodosProfissionais = document.getElementById("listaTodosProfissionais");
    const buscaInput = document.getElementById("buscaTodosProfissionais");
    const modalEl = document.getElementById("modalDetalhesProfissional");

    if (!modalEl) return;

    const carregandoEl = document.getElementById("detalhesProfissionalCarregando");
    const conteudoEl = document.getElementById("detalhesProfissionalConteudo");
    const erroEl = document.getElementById("detalhesProfissionalErro");
    const fotoEl = document.getElementById("detalhesProfissionalFoto");
    const iniciaisEl = document.getElementById("detalhesProfissionalIniciais");
    const nomeEl = document.getElementById("detalhesProfissionalNome");
    const crpEl = document.getElementById("detalhesProfissionalCrp");
    const emailEl = document.getElementById("detalhesProfissionalEmail");
    const telefoneEl = document.getElementById("detalhesProfissionalTelefone");
    const apresentacaoEl = document.getElementById("detalhesProfissionalApresentacao");

    // Escapa texto para uso seguro em conteúdo HTML
    function escaparHtml(texto) {
        return (texto || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function criarLinhaProfissional(profissional) {
        const tr = document.createElement("tr");
        tr.dataset.profissionalId = profissional.id;

        const avatar = profissional.fotoUrl
            ? `<img src="${escaparHtml(profissional.fotoUrl)}" alt="Foto de ${escaparHtml(profissional.nomeCompleto)}" class="ms-avatar-iniciais" style="object-fit:cover;">`
            : `<span class="ms-avatar-iniciais">${escaparHtml(profissional.iniciais)}</span>`;

        const apresentacao = profissional.apresentacao && profissional.apresentacao.trim() !== ""
            ? escaparHtml(profissional.apresentacao)
            : "-";

        tr.innerHTML = `
            <td>
                <div class="ms-dash-paciente-link">
                    ${avatar}
                    <span>${escaparHtml(profissional.nomeCompleto)}</span>
                </div>
            </td>
            <td>${escaparHtml(profissional.registroCRP)}</td>
            <td class="ms-dash-table-subtext">${apresentacao}</td>
            <td class="text-end">
                <button type="button" class="btn btn-sm ms-dash-btn-ficha btn-ver-detalhes-profissional" data-profissional-id="${profissional.id}">
                    <i class="bi bi-eye"></i> Ver Detalhes
                </button>
            </td>
        `;

        return tr;
    }

    function renderizarTodosProfissionais(profissionais, termoBusca) {
        if (!listaTodosProfissionais) return;

        listaTodosProfissionais.innerHTML = "";

        if (!profissionais || profissionais.length === 0) {
            const mensagem = termoBusca
                ? `Nenhum profissional encontrado para "${escaparHtml(termoBusca)}".`
                : "Nenhum profissional cadastrado ainda.";
            listaTodosProfissionais.innerHTML = `<tr><td colspan="4" class="text-center ms-dash-table-subtext py-4">${mensagem}</td></tr>`;
            return;
        }

        profissionais.forEach(function (profissional) {
            listaTodosProfissionais.appendChild(criarLinhaProfissional(profissional));
        });
    }

    // Debounce simples: evita disparar uma requisição a cada milissegundo enquanto o usuário digita
    function debounce(fn, atrasoMs) {
        let temporizador;
        return function (...args) {
            clearTimeout(temporizador);
            temporizador = setTimeout(() => fn.apply(this, args), atrasoMs);
        };
    }

    async function buscarTodosProfissionais(termo) {
        try {
            const parametros = new URLSearchParams();
            if (termo) parametros.set("busca", termo);

            const resposta = await fetch(`/PainelPaciente/BuscarProfissionais?${parametros.toString()}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) return;

            renderizarTodosProfissionais(resultado.profissionais, termo);
        } catch (erro) {
            // Mantém a lista atual em caso de falha de conexão
        }
    }

    if (buscaInput) {
        const dispararBusca = debounce(function () {
            buscarTodosProfissionais(buscaInput.value.trim());
        }, 300);

        buscaInput.addEventListener("input", dispararBusca);
    }

    function definirEstadoModal(estado) {
        if (carregandoEl) carregandoEl.classList.toggle("d-none", estado !== "carregando");
        if (conteudoEl) conteudoEl.classList.toggle("d-none", estado !== "conteudo");
        if (erroEl) erroEl.classList.toggle("d-none", estado !== "erro");
    }

    async function abrirDetalhesProfissional(profissionalId) {
        definirEstadoModal("carregando");
        bootstrap.Modal.getOrCreateInstance(modalEl).show();

        try {
            const resposta = await fetch(`/PainelPaciente/DetalhesProfissional?id=${encodeURIComponent(profissionalId)}`);
            const resultado = await resposta.json();

            if (!resposta.ok || !resultado.success) {
                definirEstadoModal("erro");
                return;
            }

            if (nomeEl) nomeEl.textContent = resultado.nomeCompleto;
            if (crpEl) crpEl.textContent = resultado.registroCRP;
            if (emailEl) emailEl.textContent = resultado.email;
            if (telefoneEl) telefoneEl.textContent = resultado.telefone;
            if (apresentacaoEl) apresentacaoEl.textContent = resultado.apresentacao && resultado.apresentacao.trim() !== "" ? resultado.apresentacao : "Nenhuma informação adicional cadastrada.";

            if (resultado.fotoUrl) {
                if (fotoEl) {
                    fotoEl.src = resultado.fotoUrl;
                    fotoEl.alt = `Foto de ${resultado.nomeCompleto}`;
                    fotoEl.classList.remove("d-none");
                }
                if (iniciaisEl) iniciaisEl.classList.add("d-none");
            } else {
                if (fotoEl) fotoEl.classList.add("d-none");
                if (iniciaisEl) {
                    iniciaisEl.textContent = resultado.iniciais;
                    iniciaisEl.classList.remove("d-none");
                }
            }

            definirEstadoModal("conteudo");
        } catch (erro) {
            definirEstadoModal("erro");
        }
    }

    document.addEventListener("click", function (e) {
        const botaoDetalhes = e.target.closest(".btn-ver-detalhes-profissional");
        if (botaoDetalhes) {
            abrirDetalhesProfissional(botaoDetalhes.dataset.profissionalId);
            return;
        }

        // Clique em qualquer lugar da linha (fora de botões) também abre a modal — interface só informativa, sem Ligar/WhatsApp
        const linhaProfissional = e.target.closest("tr[data-profissional-id]");
        if (linhaProfissional && !e.target.closest("button")) {
            abrirDetalhesProfissional(linhaProfissional.dataset.profissionalId);
            return;
        }

        const botaoCopiar = e.target.closest(".btn-copiar-dado");
        if (botaoCopiar) {
            const alvoEl = document.getElementById(botaoCopiar.dataset.alvo);
            if (!alvoEl || !alvoEl.textContent) return;

            navigator.clipboard.writeText(alvoEl.textContent).then(function () {
                const icone = botaoCopiar.querySelector("i");
                if (!icone) return;
                icone.classList.remove("bi-clipboard");
                icone.classList.add("bi-clipboard-check");
                setTimeout(function () {
                    icone.classList.remove("bi-clipboard-check");
                    icone.classList.add("bi-clipboard");
                }, 1500);
            }).catch(function () {
                // Sem permissão de clipboard: sem feedback, o texto continua visível para copiar manualmente
            });
        }
    });
});
