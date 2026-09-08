// Relógio dinâmico e saudação
function atualizarRelogio() {
    const agora = new Date();
    const hora = agora.getHours();

    let icone;
    let corIcone;
    let texto;
    if (hora >= 6 && hora < 12) {
        icone = "bi-sun-fill";
        corIcone = "#F5A623";
        texto = "Bom dia";
    } else if (hora >= 12 && hora < 18) {
        icone = "bi-cloud-sun-fill";
        corIcone = "#5B9BD5";
        texto = "Boa tarde";
    } else {
        icone = "bi-moon-stars-fill";
        corIcone = "#6C63A6";
        texto = "Boa noite";
    }
    // Só o ícone ganha cor própria; o texto continua herdando o cinza suave de .ms-greeting
    const saudacao = '<span style="color:' + corIcone + '"><i class="bi ' + icone + '"></i></span> ' + texto;

    const horaFormatada = agora.toLocaleTimeString("pt-BR", {
        hour: "2-digit",
        minute: "2-digit"
    });

    // O pt-BR retorna a data toda em minúsculas ("quinta-feira, 3 de setembro"); capitaliza só a primeira letra
    const dataFormatada = agora.toLocaleDateString("pt-BR", {
        weekday: "long",
        day: "numeric",
        month: "long"
    });
    const dataCapitalizada = dataFormatada.charAt(0).toUpperCase() + dataFormatada.slice(1);

    const greetingEl = document.getElementById("ms-greeting");
    const dateEl = document.getElementById("ms-date");
    const clockEl = document.getElementById("ms-clock");
    if (greetingEl) greetingEl.innerHTML = saudacao;
    if (dateEl) dateEl.textContent = dataCapitalizada;
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
});
