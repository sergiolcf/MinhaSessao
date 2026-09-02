// Relógio dinâmico e saudação
function atualizarRelogio() {
    const agora = new Date();
    const hora = agora.getHours();

    let icone;
    let texto;
    if (hora >= 6 && hora < 12) {
        icone = "☀️";
        texto = "Bom dia";
    } else if (hora >= 12 && hora < 18) {
        icone = "🌤️";
        texto = "Boa tarde";
    } else {
        icone = "🌙";
        texto = "Boa noite";
    }
    const saudacao = icone + " " + texto;

    const horaFormatada = agora.toLocaleTimeString("pt-BR", {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit"
    });

    const greetingEl = document.getElementById("ms-greeting");
    const clockEl = document.getElementById("ms-clock");
    if (greetingEl) greetingEl.textContent = saudacao;
    if (clockEl) clockEl.textContent = horaFormatada;
}

// Frases inspiradoras sobre Paz e Saúde Mental
const frasesDePaz = [
    "Respire fundo. Este momento é só seu, em <span class=\"ms-highlight\">paz</span>.",
    "A <span class=\"ms-highlight\">paz</span> começa com um instante de <span class=\"ms-highlight\">silêncio</span> interior.",
    "Cuidar da <span class=\"ms-highlight\">mente</span> é um ato de coragem e amor-próprio.",
    "Você não precisa ter tudo resolvido para viver esta <span class=\"ms-highlight\">vida</span> em paz agora.",
    "Pequenos passos de <span class=\"ms-highlight\">autoconhecimento</span> constroem uma mente mais leve."
];

let indiceFrase = 0;

function alternarFrase() {
    const quoteEl = document.getElementById("ms-quote");
    if (!quoteEl) return;

    quoteEl.classList.add("ms-fade");

    setTimeout(() => {
        indiceFrase = (indiceFrase + 1) % frasesDePaz.length;
        quoteEl.innerHTML = frasesDePaz[indiceFrase];
        quoteEl.classList.remove("ms-fade");
    }, 800);
}

document.addEventListener("DOMContentLoaded", function () {
    // Relógio
    atualizarRelogio();
    setInterval(atualizarRelogio, 1000);

    // Frase inicial
    const quoteEl = document.getElementById("ms-quote");
    if (quoteEl) {
        quoteEl.innerHTML = frasesDePaz[0];
    }
    setInterval(alternarFrase, 9000);

    // Alternância entre Modal 1 (Login) e Modal 2 (Seleção de Perfil)
    const loginModalEl = document.getElementById("loginModal");
    const registerTypeModalEl = document.getElementById("registerTypeModal");
    const linkCadastreSe = document.getElementById("linkCadastreSe");

    if (loginModalEl && registerTypeModalEl && linkCadastreSe) {
        const loginModal = bootstrap.Modal.getOrCreateInstance(loginModalEl);
        const registerTypeModal = bootstrap.Modal.getOrCreateInstance(registerTypeModalEl);

        linkCadastreSe.addEventListener("click", function (e) {
            e.preventDefault();
            loginModalEl.addEventListener("hidden.bs.modal", function abrirModal2() {
                registerTypeModal.show();
                loginModalEl.removeEventListener("hidden.bs.modal", abrirModal2);
            });
            loginModal.hide();
        });
    }

    // Formulário de login (placeholder)
    const loginForm = document.getElementById("loginForm");
    if (loginForm) {
        loginForm.addEventListener("submit", function (e) {
            e.preventDefault();
        });
    }

    // Alternância entre Modal 2 (Seleção de Perfil) e Modal 3 (Cadastro do Profissional)
    const modalProfissionalEl = document.getElementById("modalCadastroProfissional");
    const btnPerfilProfissional = document.querySelector('.ms-profile-btn[data-perfil="profissional"]');

    if (registerTypeModalEl && modalProfissionalEl && btnPerfilProfissional) {
        const registerTypeModalInstance = bootstrap.Modal.getOrCreateInstance(registerTypeModalEl);
        const modalProfissional = bootstrap.Modal.getOrCreateInstance(modalProfissionalEl);

        btnPerfilProfissional.addEventListener("click", function () {
            registerTypeModalEl.addEventListener("hidden.bs.modal", function abrirModalProfissional() {
                modalProfissional.show();
                registerTypeModalEl.removeEventListener("hidden.bs.modal", abrirModalProfissional);
            });
            registerTypeModalInstance.hide();
        });
    }

    // Contador de caracteres da apresentação do profissional
    const apresentacaoEl = document.getElementById("Apresentacao");
    const apresentacaoCounterEl = document.getElementById("apresentacaoCounter");
    if (apresentacaoEl && apresentacaoCounterEl) {
        apresentacaoEl.addEventListener("input", function () {
            apresentacaoCounterEl.textContent = apresentacaoEl.value.length + "/500";
        });
    }

    // Formulário de cadastro do profissional (envio para o Controller via Fetch)
    const formCadastroProfissional = document.getElementById("formCadastroProfissional");
    const feedbackEl = document.getElementById("cadastroProfissionalFeedback");
    const feedbackMensagemEl = document.getElementById("cadastroProfissionalFeedbackMensagem");
    const modalSucessoEl = document.getElementById("modalSucessoCadastro");
    const btnCriarConta = document.getElementById("btnCriarConta");
    const btnCriarContaSpinner = document.getElementById("btnCriarContaSpinner");
    const btnCriarContaTexto = document.getElementById("btnCriarContaTexto");

    function definirCarregando(carregando) {
        if (!btnCriarConta) return;
        btnCriarConta.disabled = carregando;
        if (btnCriarContaSpinner) {
            btnCriarContaSpinner.classList.toggle("d-none", !carregando);
        }
        if (btnCriarContaTexto) {
            btnCriarContaTexto.textContent = carregando ? "Enviando..." : "Criar Conta";
        }
    }

    function exibirErro(mensagem) {
        if (!feedbackEl || !feedbackMensagemEl) return;
        feedbackMensagemEl.textContent = mensagem;
        feedbackEl.classList.remove("d-none");
    }

    function ocultarErro() {
        if (!feedbackEl) return;
        feedbackEl.classList.add("d-none");
    }

    if (formCadastroProfissional) {
        formCadastroProfissional.addEventListener("submit", async function (e) {
            e.preventDefault();
            ocultarErro();
            definirCarregando(true);

            const formData = new FormData(formCadastroProfissional);

            try {
                const resposta = await fetch("/Profissional/Criar", {
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
                    // Sucesso: limpa o formulário e alterna para a modal de confirmação
                    formCadastroProfissional.reset();
                    if (apresentacaoCounterEl) {
                        apresentacaoCounterEl.textContent = "0/500";
                    }

                    if (modalProfissionalEl && modalSucessoEl) {
                        modalProfissionalEl.addEventListener("hidden.bs.modal", function abrirModalSucesso() {
                            bootstrap.Modal.getOrCreateInstance(modalSucessoEl).show();
                            modalProfissionalEl.removeEventListener("hidden.bs.modal", abrirModalSucesso);
                        });
                        bootstrap.Modal.getOrCreateInstance(modalProfissionalEl).hide();
                    }
                } else {
                    // Erro: mantém a modal aberta com os dados preenchidos e mostra o motivo
                    const primeiroErroValidacao = resultado.errors
                        ? Object.values(resultado.errors)[0]?.[0]
                        : null;
                    exibirErro(resultado.message || primeiroErroValidacao || "Não foi possível concluir o cadastro.");
                }
            } catch (erro) {
                exibirErro("Erro de conexão. Verifique sua internet e tente novamente.");
            } finally {
                definirCarregando(false);
            }
        });
    }
});
