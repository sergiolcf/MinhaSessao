namespace MinhaSessao.Models.ViewModels;

// Objetivo Terapêutico (com seus Combinados) em modo SOMENTE LEITURA — usado na tela "Meus Planos
// de Tratamento" do Painel do Paciente. Mesmo conteúdo que já aparece na aba "Plano de Tratamento"
// da Ficha do Paciente (lado do profissional), mas aqui não existe nenhuma ação de escrita (criar,
// mudar status, excluir objetivo ou combinado, marcar combinado como concluído).
public class ObjetivoTerapeuticoLeituraViewModel
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    // "EmAndamento", "Atingido", "Pausado" ou "Cancelado"
    public string Status { get; set; } = string.Empty;

    public DateTime DataCriacao { get; set; }

    public List<CombinadoLeituraViewModel> Combinados { get; set; } = new();

    public int TotalCombinados => Combinados.Count;

    public int CombinadosConcluidos => Combinados.Count(c => c.Concluido);

    public int PercentualConcluido => TotalCombinados == 0
        ? 0
        : (int)Math.Round(CombinadosConcluidos * 100.0 / TotalCombinados);
}

public class CombinadoLeituraViewModel
{
    public Guid Id { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public bool Concluido { get; set; }
}
