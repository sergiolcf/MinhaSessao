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
});
