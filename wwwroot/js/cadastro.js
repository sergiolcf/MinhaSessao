// Adaptadores do jQuery Validate Unobtrusive para os ValidationAttribute customizados
// (MaiorDeIdadeAttribute/CpfValidoAttribute) — precisam ser registrados antes do unobtrusive
// escanear o formulário, por isso ficam fora do DOMContentLoaded, direto no carregamento do script.
if (window.jQuery && $.validator) {
    $.validator.addMethod("maiordeidade", function (value, element, idadeMinima) {
        if (!value) {
            return true; // [Required] cuida da ausência de valor
        }

        var nascimento = new Date(value);
        if (isNaN(nascimento.getTime())) {
            return true;
        }

        var hoje = new Date();
        var idade = hoje.getFullYear() - nascimento.getUTCFullYear();
        var aniversarioJaFezEsteAno =
            hoje.getMonth() > nascimento.getUTCMonth() ||
            (hoje.getMonth() === nascimento.getUTCMonth() && hoje.getDate() >= nascimento.getUTCDate());

        if (!aniversarioJaFezEsteAno) {
            idade--;
        }

        return idade >= parseInt(idadeMinima, 10);
    });

    $.validator.unobtrusive.adapters.add("maiordeidade", ["idademinima"], function (options) {
        options.rules.maiordeidade = options.params.idademinima;
        options.messages.maiordeidade = options.message;
    });

    $.validator.addMethod("cpfvalido", function (value) {
        if (!value) {
            return true; // [Required] cuida da ausência de valor
        }

        var digitos = value.replace(/\D/g, "");

        if (digitos.length !== 11 || /^(\d)\1{10}$/.test(digitos)) {
            return false;
        }

        function calcularDigitoVerificador(base) {
            var soma = 0;
            var peso = base.length + 1;
            for (var i = 0; i < base.length; i++) {
                soma += parseInt(base.charAt(i), 10) * peso;
                peso--;
            }
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        var primeiroDv = calcularDigitoVerificador(digitos.substring(0, 9));
        if (primeiroDv !== parseInt(digitos.charAt(9), 10)) {
            return false;
        }

        var segundoDv = calcularDigitoVerificador(digitos.substring(0, 10));
        return segundoDv === parseInt(digitos.charAt(10), 10);
    });

    $.validator.unobtrusive.adapters.addBool("cpfvalido");
}

// Reformata o valor de um campo mascarado; usado tanto ao digitar quanto em cenários em que o
// navegador preenche o campo sozinho (autopreenchimento) sem disparar um evento "input" "de verdade"
function aplicarMascara(elemento, formatar) {
    if (!elemento) {
        return;
    }

    var reformatar = function () {
        var novoValor = formatar(elemento.value);
        if (novoValor !== elemento.value) {
            elemento.value = novoValor;
        }
    };

    ["input", "blur", "change"].forEach(function (evento) {
        elemento.addEventListener(evento, reformatar);
    });

    // Cobre o caso do navegador já ter preenchido o campo antes do script rodar
    if (elemento.value) {
        reformatar();
    }
}

function formatarCpf(valor) {
    var digitos = valor.replace(/\D/g, "").slice(0, 11);

    if (digitos.length > 9) {
        digitos = digitos.replace(/^(\d{3})(\d{3})(\d{3})(\d{1,2})$/, "$1.$2.$3-$4");
    } else if (digitos.length > 6) {
        digitos = digitos.replace(/^(\d{3})(\d{3})(\d{1,3})$/, "$1.$2.$3");
    } else if (digitos.length > 3) {
        digitos = digitos.replace(/^(\d{3})(\d{1,3})$/, "$1.$2");
    }

    return digitos;
}

function formatarTelefone(valor) {
    var digitos = valor.replace(/\D/g, "").slice(0, 11);

    if (digitos.length > 10) {
        digitos = digitos.replace(/^(\d{2})(\d{5})(\d{0,4})$/, "($1) $2-$3");
    } else if (digitos.length > 6) {
        digitos = digitos.replace(/^(\d{2})(\d{4})(\d{0,4})$/, "($1) $2-$3");
    } else if (digitos.length > 2) {
        digitos = digitos.replace(/^(\d{2})(\d{0,5})$/, "($1) $2");
    } else if (digitos.length > 0) {
        digitos = digitos.replace(/^(\d{0,2})$/, "($1");
    }

    return digitos.trimEnd();
}

// Máscara de CRP (00/000000) no cadastro de Profissional
function formatarCrp(valor) {
    var digitos = valor.replace(/\D/g, "").slice(0, 8);

    if (digitos.length > 2) {
        digitos = digitos.replace(/^(\d{2})(\d{1,6})$/, "$1/$2");
    }

    return digitos;
}

// Checagem imediata (client-side) de tamanho/extensão da Foto de Perfil do Profissional — o servidor
// já valida via ArquivoValidoAttribute, mas sem isso o erro só apareceria depois de um POST completo,
// igual acontecia com MaiorDeIdadeAttribute/CpfValidoAttribute antes de ganharem IClientModelValidator.
function configurarValidacaoFoto() {
    var TAMANHO_MAXIMO_BYTES = 2 * 1024 * 1024;
    var EXTENSOES_PERMITIDAS = [".jpg", ".jpeg", ".png"];

    var fotoEl = document.querySelector("#tab-profissional #Foto");
    var mensagemEl = document.querySelector("#tab-profissional [data-valmsg-for='Foto']");
    if (!fotoEl) {
        return;
    }

    fotoEl.addEventListener("change", function () {
        var arquivo = fotoEl.files && fotoEl.files[0];
        if (!arquivo) {
            return;
        }

        var extensao = "." + arquivo.name.split(".").pop().toLowerCase();
        var erro = null;

        if (EXTENSOES_PERMITIDAS.indexOf(extensao) === -1) {
            erro = "Extensões permitidas: " + EXTENSOES_PERMITIDAS.join(", ") + ".";
        } else if (arquivo.size > TAMANHO_MAXIMO_BYTES) {
            erro = "O arquivo deve ter no máximo " + (TAMANHO_MAXIMO_BYTES / (1024 * 1024)) + "MB.";
        }

        if (erro) {
            fotoEl.value = "";
            if (mensagemEl) {
                mensagemEl.textContent = erro;
                mensagemEl.classList.add("field-validation-error");
            }
        } else if (mensagemEl) {
            mensagemEl.textContent = "";
            mensagemEl.classList.remove("field-validation-error");
        }
    });
}

// Botão de mostrar/ocultar senha (.ms-toggle-senha), reaproveitado pelos campos Senha/ConfirmarSenha
// tanto do Cadastro de Paciente quanto do Profissional — alterna type="password"/"text" do input
// imediatamente anterior ao botão e troca o ícone bi-eye/bi-eye-slash.
function configurarToggleSenha() {
    document.querySelectorAll(".ms-toggle-senha").forEach(function (botao) {
        botao.addEventListener("click", function () {
            var input = botao.previousElementSibling;
            if (!input) {
                return;
            }

            var estaMostrando = input.type === "text";
            input.type = estaMostrando ? "password" : "text";
            botao.setAttribute("aria-label", estaMostrando ? "Mostrar senha" : "Ocultar senha");

            var icone = botao.querySelector("i");
            if (icone) {
                icone.classList.toggle("bi-eye", estaMostrando);
                icone.classList.toggle("bi-eye-slash", !estaMostrando);
            }
        });
    });
}

document.addEventListener("DOMContentLoaded", function () {
    configurarToggleSenha();

    // Contador de caracteres da apresentação do profissional
    const apresentacaoEl = document.getElementById("Apresentacao");
    const apresentacaoCounterEl = document.getElementById("apresentacaoCounter");
    if (apresentacaoEl && apresentacaoCounterEl) {
        apresentacaoCounterEl.textContent = apresentacaoEl.value.length + "/500";
        apresentacaoEl.addEventListener("input", function () {
            apresentacaoCounterEl.textContent = apresentacaoEl.value.length + "/500";
        });
    }

    // Máscaras de CPF (000.000.000-00) e Telefone (fixo/celular dinâmico) no cadastro de Paciente.
    // Escopadas a #tab-paciente porque o id "Telefone" também existe no formulário do Profissional
    // (mesma página, aba "Sou Profissional") — document.getElementById pegaria sempre o primeiro
    // elemento com esse id no documento, deixando um dos dois campos sem máscara.
    aplicarMascara(document.querySelector("#tab-paciente #Cpf"), formatarCpf);
    aplicarMascara(document.querySelector("#tab-paciente #Telefone"), formatarTelefone);

    // Máscaras de CRP (00/000000) e Telefone (fixo/celular dinâmico) no cadastro de Profissional
    aplicarMascara(document.querySelector("#tab-profissional #RegistroCRP"), formatarCrp);
    aplicarMascara(document.querySelector("#tab-profissional #Telefone"), formatarTelefone);

    configurarValidacaoFoto();
});
