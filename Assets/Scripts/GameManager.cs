using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// GameManager.cs
/// Orquestra o fluxo completo de uma mão de Texas Hold'em
/// para 6 jogadores (1 humano + 5 IAs).
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // ESTADO DO JOGO
    // ─────────────────────────────────────────────
    public enum GameState
    {
        Idle,
        Distribuindo,
        PreFlop,
        Flop,
        Turn,
        River,
        Showdown
    }

    public GameState estadoAtual { get; private set; } = GameState.Idle;

    // ─────────────────────────────────────────────
    // REFERÊNCIAS
    // ─────────────────────────────────────────────
    [Header("Referências")]
    public Deck deck;                          // script Deck.cs já existente
    public Transform[] slotsComunitarios;      // 5 posições do centro da mesa
    public Transform[] slotsJogadores;         // 6 posições ao redor da mesa

    // ─────────────────────────────────────────────
    // JOGADORES
    // ─────────────────────────────────────────────
    [System.Serializable]
    public class Jogador
    {
        public string nome;
        public bool isHumano;
        public int fichas;
        public int apostaRodada;   // quanto apostou nesta ronda
        public bool foldou;
        public bool allIn;
        public List<PokerCard> mao = new List<PokerCard>();
    }

    [Header("Jogadores")]
    public int fichasIniciais = 1000;

    private List<Jogador> jogadores = new List<Jogador>();
    private int indiceDealerButton = 0;   // quem é o dealer esta mão
    private int indiceJogadorAtual = 0;   // de quem é a vez

    // ─────────────────────────────────────────────
    // APOSTAS
    // ─────────────────────────────────────────────
    [Header("Blinds")]
    public int smallBlind = 10;
    public int bigBlind   = 20;

    private int pote = 0;
    private int apostaAtual = 0;          // maior aposta da rodada

    // ─────────────────────────────────────────────
    // CARTAS COMUNITÁRIAS
    // ─────────────────────────────────────────────
    private List<PokerCard> cartasComunitarias = new List<PokerCard>();

    // ─────────────────────────────────────────────
    // EVENTOS (UI assina estes eventos)
    // ─────────────────────────────────────────────
    public System.Action<GameState>      OnEstadoMudou;
    public System.Action<int, int>       OnPoteAtualizado;      // (pote, apostaAtual)
    public System.Action<int, int>       OnFichasAtualizadas;   // (indiceJogador, fichas)
    public System.Action<int>            OnVezDoJogador;        // indice do jogador
    public System.Action<int, string>    OnAcaoRealizada;       // (indice, "Fold/Call/Raise")
    public System.Action<List<int>>      OnShowdown;            // indices dos vencedores

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

   IEnumerator Start()
{
    yield return null; // aguarda Awake() de todos os objetos
    InicializarJogadores();
    yield return StartCoroutine(IniciarMao());
}

    // ─────────────────────────────────────────────
    // INICIALIZAÇÃO
    // ─────────────────────────────────────────────
    void InicializarJogadores()
    {
        jogadores.Clear();

        for (int i = 0; i < 6; i++)
        {
            jogadores.Add(new Jogador
            {
                nome         = i == 0 ? "Você" : $"IA {i}",
                isHumano     = i == 0,
                fichas       = fichasIniciais,
                apostaRodada = 0,
                foldou       = false,
                allIn        = false
            });
        }
    }

    // ─────────────────────────────────────────────
    // FLUXO PRINCIPAL DA MÃO
    // ─────────────────────────────────────────────
    IEnumerator IniciarMao()
    {
        ResetarMao();
        MudarEstado(GameState.Distribuindo);

        // Distribui cartas via Deck (já existente)
        yield return StartCoroutine(deck.DistribuirCartasParaJogadores(jogadores, slotsJogadores));

        // Posta blinds
        PostarBlinds();

        yield return StartCoroutine(RodadaDeApostas(GameState.PreFlop));

        if (JogadoresAtivos() > 1)
        {
            yield return StartCoroutine(RevelarCartas(0, 3)); // Flop
            yield return StartCoroutine(RodadaDeApostas(GameState.Flop));
        }

        if (JogadoresAtivos() > 1)
        {
            yield return StartCoroutine(RevelarCartas(3, 1)); // Turn
            yield return StartCoroutine(RodadaDeApostas(GameState.Turn));
        }

        if (JogadoresAtivos() > 1)
        {
            yield return StartCoroutine(RevelarCartas(4, 1)); // River
            yield return StartCoroutine(RodadaDeApostas(GameState.River));
        }

        yield return StartCoroutine(RealizarShowdown());
    }

    // ─────────────────────────────────────────────
    // BLINDS
    // ─────────────────────────────────────────────
    void PostarBlinds()
    {
        int iSB = (indiceDealerButton + 1) % 6;
        int iBB = (indiceDealerButton + 2) % 6;

        Apostar(iSB, smallBlind);
        Apostar(iBB, bigBlind);
        apostaAtual = bigBlind;

        // Primeiro a agir no pré-flop é UTG (posição 3)
        indiceJogadorAtual = (indiceDealerButton + 3) % 6;
    }

    // ─────────────────────────────────────────────
    // RODADA DE APOSTAS
    // ─────────────────────────────────────────────
    IEnumerator RodadaDeApostas(GameState estado)
    {
        MudarEstado(estado);
        ResetarApostasRodada();

        int jogadoresParaAgir = JogadoresAtivos();
        int contadorSemAcao   = 0;

        while (contadorSemAcao < jogadoresParaAgir)
        {
            Jogador atual = jogadores[indiceJogadorAtual];

            if (!atual.foldou && !atual.allIn)
            {
                OnVezDoJogador?.Invoke(indiceJogadorAtual);

                if (atual.isHumano)
                    yield return StartCoroutine(EsperarAcaoHumano());
                else
                    yield return StartCoroutine(ExecutarAcaoIA(indiceJogadorAtual));

                contadorSemAcao = 0; // reinicia contador se alguém agiu
            }
            else
            {
                contadorSemAcao++;
            }

            // Se restou 1 jogador ativo, encerra
            if (JogadoresAtivos() == 1) yield break;

            // Verifica se todos igualaram a aposta
            if (TodosIgualaram()) yield break;

            indiceJogadorAtual = (indiceJogadorAtual + 1) % 6;
        }
    }

    // ─────────────────────────────────────────────
    // AÇÃO DO HUMANO (UI chama AcaoHumano())
    // ─────────────────────────────────────────────
    private bool aguardandoHumano = false;

    IEnumerator EsperarAcaoHumano()
    {
        aguardandoHumano = true;
        yield return new WaitUntil(() => !aguardandoHumano);
    }

    /// <summary>
    /// Chamado pelos botões da UI (Check/Call/Raise/Fold).
    /// </summary>
    public void AcaoHumano(string acao, int valorRaise = 0)
    {
        if (!aguardandoHumano) return;

        ExecutarAcao(0, acao, valorRaise);
        aguardandoHumano = false;
    }

    // ─────────────────────────────────────────────
    // AÇÃO DA IA (simples por enquanto)
    // ─────────────────────────────────────────────
    IEnumerator ExecutarAcaoIA(int indice)
    {
        // Delay humanizado (será expandido pelo AIDecision.cs)
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        Jogador ia = jogadores[indice];
        int diferenca = apostaAtual - ia.apostaRodada;

        // Lógica básica temporária (será substituída pelo AIDecision.cs)
        float chance = Random.value;

        if (diferenca == 0)
        {
            // Pode dar check ou apostar
            if (chance < 0.6f) ExecutarAcao(indice, "Check");
            else                ExecutarAcao(indice, "Raise", bigBlind * 2);
        }
        else
        {
            if (chance < 0.3f)       ExecutarAcao(indice, "Fold");
            else if (chance < 0.75f) ExecutarAcao(indice, "Call");
            else                     ExecutarAcao(indice, "Raise", diferenca + bigBlind);
        }
    }

    // ─────────────────────────────────────────────
    // EXECUTAR AÇÃO
    // ─────────────────────────────────────────────
    void ExecutarAcao(int indice, string acao, int valorExtra = 0)
    {
        Jogador j = jogadores[indice];
        int diferenca = apostaAtual - j.apostaRodada;

        switch (acao)
        {
            case "Fold":
                j.foldou = true;
                break;

            case "Check":
                // só válido se diferenca == 0
                break;

            case "Call":
                Apostar(indice, Mathf.Min(diferenca, j.fichas));
                break;

            case "Raise":
                int total = diferenca + valorExtra;
                Apostar(indice, Mathf.Min(total, j.fichas));
                apostaAtual = j.apostaRodada;
                break;
        }

        OnAcaoRealizada?.Invoke(indice, acao);
    }

    // ─────────────────────────────────────────────
    // REVELAR CARTAS COMUNITÁRIAS
    // ─────────────────────────────────────────────
    IEnumerator RevelarCartas(int inicio, int quantidade)
    {
        for (int i = inicio; i < inicio + quantidade; i++)
        {
            PokerCard carta = deck.ComprarCarta();
            cartasComunitarias.Add(carta);
            carta.gameObject.SetActive(true);
            carta.transform.position = deck.posicaoDeck.position;
            carta.transform.DOMove(slotsComunitarios[i].position, 0.4f)
                 .SetEase(DG.Tweening.Ease.OutCubic);
            yield return new WaitForSeconds(0.35f);
            carta.VirarParaCima();
            yield return new WaitForSeconds(0.2f);
        }
    }

    // ─────────────────────────────────────────────
    // SHOWDOWN
    // ─────────────────────────────────────────────
    IEnumerator RealizarShowdown()
    {
        MudarEstado(GameState.Showdown);

        // Revela mãos dos ativos
        foreach (Jogador j in jogadores)
            if (!j.foldou)
                foreach (PokerCard c in j.mao)
                    c.VirarParaCima();

        yield return new WaitForSeconds(1f);

        // Avaliação de mãos (HandEvaluator.cs — próximo passo)
        List<int> vencedores = DeterminarVencedores();
        DistribuirPote(vencedores);

        OnShowdown?.Invoke(vencedores);

        yield return new WaitForSeconds(3f);

        // Próxima mão
        indiceDealerButton = (indiceDealerButton + 1) % 6;
        StartCoroutine(IniciarMao());
    }

    // ─────────────────────────────────────────────
    // PLACEHOLDER — será substituído por HandEvaluator.cs
    // ─────────────────────────────────────────────
    List<int> DeterminarVencedores()
    {
        List<int> ativos = new List<int>();
        for (int i = 0; i < jogadores.Count; i++)
            if (!jogadores[i].foldou) ativos.Add(i);

        // Por enquanto escolhe aleatório entre os ativos
        int sorteado = ativos[Random.Range(0, ativos.Count)];
        return new List<int> { sorteado };
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────
    void Apostar(int indice, int valor)
    {
        Jogador j = jogadores[indice];
        valor = Mathf.Min(valor, j.fichas);
        j.fichas       -= valor;
        j.apostaRodada += valor;
        pote           += valor;

        if (j.fichas == 0) j.allIn = true;

        OnPoteAtualizado?.Invoke(pote, apostaAtual);
        OnFichasAtualizadas?.Invoke(indice, j.fichas);
    }

    void ResetarMao()
    {
        pote        = 0;
        apostaAtual = 0;
        cartasComunitarias.Clear();
         deck.ResetarBaralho();

        foreach (Jogador j in jogadores)
        {
            j.apostaRodada = 0;
            j.foldou       = false;
            j.allIn        = false;
            j.mao.Clear();
        }
    }

    void ResetarApostasRodada()
    {
        foreach (Jogador j in jogadores)
            j.apostaRodada = 0;
        apostaAtual = 0;
    }

    void MudarEstado(GameState novoEstado)
    {
        estadoAtual = novoEstado;
        OnEstadoMudou?.Invoke(novoEstado);
        Debug.Log($"[GameManager] Estado: {novoEstado}");
    }

    int JogadoresAtivos()
    {
        int count = 0;
        foreach (Jogador j in jogadores)
            if (!j.foldou) count++;
        return count;
    }

    bool TodosIgualaram()
    {
        foreach (Jogador j in jogadores)
            if (!j.foldou && !j.allIn && j.apostaRodada != apostaAtual)
                return false;
        return true;
    }

    void DistribuirPote(List<int> vencedores)
    {
        int parte = pote / vencedores.Count;
        foreach (int i in vencedores)
        {
            jogadores[i].fichas += parte;
            OnFichasAtualizadas?.Invoke(i, jogadores[i].fichas);
        }
        pote = 0;
    }

    // Acessores públicos para a UI
    public List<Jogador> GetJogadores() => jogadores;
    public int GetPote()                => pote;
    public int GetApostaAtual()         => apostaAtual;
    public int GetIndiceAtual()         => indiceJogadorAtual;
}