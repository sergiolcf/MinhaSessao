document.addEventListener("DOMContentLoaded", function () {
    // Contador de caracteres da apresentação do profissional
    const apresentacaoEl = document.getElementById("Apresentacao");
    const apresentacaoCounterEl = document.getElementById("apresentacaoCounter");
    if (apresentacaoEl && apresentacaoCounterEl) {
        apresentacaoCounterEl.textContent = apresentacaoEl.value.length + "/500";
        apresentacaoEl.addEventListener("input", function () {
            apresentacaoCounterEl.textContent = apresentacaoEl.value.length + "/500";
        });
    }
});
