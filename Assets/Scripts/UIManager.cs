using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static UIManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // HUD TOPO
    // ─────────────────────────────────────────────
    [Header("HUD Topo")]
    public TextMeshProUGUI txtFase;
    public TextMeshProUGUI txtPote;
    public TextMeshProUGUI txtApostaAtual;
    public TextMeshProUGUI txtBlinds;

    // ─────────────────────────────────────────────
    // AVATARES DOS JOGADORES NA MESA
    // ─────────────────────────────────────────────
    [Header("Avatares (ordem: Você, IA1..IA5)")]
    public TextMeshProUGUI[] txtNomesJogadores;    // 6
    public TextMeshProUGUI[] txtFichasJogadores;   // 6
    public GameObject[]      indicadoresVez;        // 6 — borda dourada ativa
    public GameObject[]      indicadoresFold;       // 6 — overlay de fold

    // ─────────────────────────────────────────────
    // HISTÓRICO
    // ─────────────────────────────────────────────
    [Header("Histórico de Ações")]
    public Transform   painelHistorico;
    public GameObject  prefabLinhaHistorico;
    private readonly Queue<GameObject> _linhas = new Queue<GameObject>();
    private const int MAX_LINHAS = 8;

    // ─────────────────────────────────────────────
    // BARRA INFERIOR
    // ─────────────────────────────────────────────
    [Header("Barra de Ações")]
    public GameObject      painelAcoes;
    public Button          btnFold;
    public Button          btnCheck;
    public Button          btnCall;
    public Button          btnRaise;
    public TextMeshProUGUI txtLabelCall;     // "Call 40"
    public TextMeshProUGUI txtSubCall;       // "-40 fichas"
    public TextMeshProUGUI txtSubRaise;      // "mín. 60"
    public TextMeshProUGUI txtFichasJogador; // fichas do jogador (canto direito)
    public TextMeshProUGUI txtTimer;

    // ─────────────────────────────────────────────
    // PAINEL DE RAISE (teclado numérico)
    // ─────────────────────────────────────────────
    [Header("Painel Raise")]
    public GameObject      painelRaise;
    public TextMeshProUGUI txtValorRaise;
    public TextMeshProUGUI txtMinRaise;
    public TextMeshProUGUI txtMaxRaise;
    public Button[]        botoesNumericos;   // 10 botões: índice = dígito
    public Button          btnApagar;
    public Button          btnAllIn;
    public Button          btnCancelarRaise;
    public Button          btnConfirmarRaise;

    private string _inputRaise = "";
    private int    _minRaise;
    private int    _maxRaise;

    // ─────────────────────────────────────────────
    // SHOWDOWN
    // ─────────────────────────────────────────────
    [Header("Showdown")]
    public GameObject      painelShowdown;
    public TextMeshProUGUI txtResultado;
    public Button          btnProximaMao;

    // ─────────────────────────────────────────────
    // TEMPORIZADOR
    // ─────────────────────────────────────────────
    private Coroutine _corTimer;
    private const float TEMPO_LIMITE = 30f;

    // Cores estilo cassino
    private static readonly Color CorDourado  = new Color(0.788f, 0.659f, 0.298f);
    private static readonly Color CorUrgente  = new Color(0.886f, 0.290f, 0.290f);
    private static readonly Color CorMuted    = new Color(0.533f, 0.533f, 0.533f);

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ConfigurarBotoes();
        AssinarEventos();
        EsconderPaineis();
        InicializarNomes();
    }

    // ─────────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────────
    void ConfigurarBotoes()
    {
        btnFold.onClick.AddListener(OnFold);
        btnCheck.onClick.AddListener(OnCheck);
        btnCall.onClick.AddListener(OnCall);
        btnRaise.onClick.AddListener(AbrirRaise);

        btnApagar.onClick.AddListener(ApagarDigito);
        btnAllIn.onClick.AddListener(ClicarAllIn);
        btnCancelarRaise.onClick.AddListener(FecharRaise);
        btnConfirmarRaise.onClick.AddListener(ConfirmarRaise);

        for (int i = 0; i < botoesNumericos.Length; i++)
        {
            int digito = i;
            botoesNumericos[i].onClick.AddListener(() => DigitarNumero(digito));
        }

        btnProximaMao.onClick.AddListener(() => painelShowdown.SetActive(false));
    }

    void AssinarEventos()
    {
        var gm = GameManager.Instance;
        gm.OnEstadoMudou       += AoMudarEstado;
        gm.OnPoteAtualizado    += AoPoteAtualizado;
        gm.OnFichasAtualizadas += AoFichasAtualizadas;
        gm.OnVezDoJogador      += AoVezDoJogador;
        gm.OnAcaoRealizada     += AoAcaoRealizada;
        gm.OnShowdown          += AoShowdown;
    }

    void EsconderPaineis()
    {
        painelAcoes.SetActive(false);
        painelRaise.SetActive(false);
        painelShowdown.SetActive(false);
    }

    void InicializarNomes()
    {
        if (txtNomesJogadores == null) return;
        string[] nomes = { "Você", "IA 1", "IA 2", "IA 3", "IA 4", "IA 5" };
        for (int i = 0; i < txtNomesJogadores.Length && i < nomes.Length; i++)
            if (txtNomesJogadores[i] != null)
                txtNomesJogadores[i].text = nomes[i];
    }

    // ─────────────────────────────────────────────
    // EVENTOS DO GAMEMANAGER
    // ─────────────────────────────────────────────
    void AoMudarEstado(GameManager.GameState estado)
    {
        if (txtFase != null)
            txtFase.text = estado switch
            {
                GameManager.GameState.PreFlop      => "Pré-Flop",
                GameManager.GameState.Flop         => "Flop",
                GameManager.GameState.Turn         => "Turn",
                GameManager.GameState.River        => "River",
                GameManager.GameState.Showdown     => "Showdown",
                GameManager.GameState.Distribuindo => "Distribuindo...",
                _ => ""
            };

        AdicionarHistorico($"── {txtFase?.text} ──", CorDourado);

        // Limpa indicadores de fold a cada nova mão
        if (estado == GameManager.GameState.Distribuindo)
            LimparMesa();
    }

    void AoPoteAtualizado(int pote, int apostaAtual)
    {
        if (txtPote       != null) txtPote.text       = $"Pote: {pote}";
        if (txtApostaAtual != null) txtApostaAtual.text = $"Aposta: {apostaAtual}";
    }

    void AoFichasAtualizadas(int indice, int fichas)
    {
        if (txtFichasJogadores != null && indice < txtFichasJogadores.Length)
            if (txtFichasJogadores[indice] != null)
                txtFichasJogadores[indice].text = $"{fichas}";

        if (indice == 0 && txtFichasJogador != null)
            txtFichasJogador.text = $"{fichas} fichas";
    }

    void AoVezDoJogador(int indice)
    {
        // Atualiza indicadores de vez
        for (int i = 0; i < indicadoresVez?.Length; i++)
            if (indicadoresVez[i] != null)
                indicadoresVez[i].SetActive(i == indice);

        if (indice == 0)
        {
            AtualizarBotoes();
            painelAcoes.SetActive(true);
            IniciarTimer();
        }
        else
        {
            painelAcoes.SetActive(false);
            PararTimer();
        }
    }

    void AoAcaoRealizada(int indice, string acao)
    {
        string nome  = indice == 0 ? "Você" : $"IA {indice}";
        Color  cor   = indice == 0 ? CorDourado : CorMuted;
        AdicionarHistorico($"{nome}: {acao}", cor);

        if (acao == "Fold" && indicadoresFold != null && indice < indicadoresFold.Length)
            if (indicadoresFold[indice] != null)
                indicadoresFold[indice].SetActive(true);

        if (indice == 0)
        {
            painelAcoes.SetActive(false);
            painelRaise.SetActive(false);
            PararTimer();
        }
    }

    void AoShowdown(List<int> vencedores)
    {
        painelAcoes.SetActive(false);
        painelRaise.SetActive(false);
        PararTimer();

        string nomes = string.Join(" e ", vencedores.ConvertAll(
            i => i == 0 ? "Você" : $"IA {i}"));

        if (txtResultado != null)
            txtResultado.text = vencedores.Contains(0)
                ? "Você venceu!"
                : $"Vencedor: {nomes}";

        painelShowdown.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // ATUALIZAR BOTÕES CONFORME O ESTADO
    // ─────────────────────────────────────────────
    void AtualizarBotoes()
    {
        var gm          = GameManager.Instance;
        var jogadores   = gm.GetJogadores();
        int apostaAtual = gm.GetApostaAtual();
        int fichas      = jogadores[0].fichas;
        int apostado    = jogadores[0].apostaRodada;
        int diferenca   = apostaAtual - apostado;

        _minRaise = diferenca + gm.bigBlind;
        _maxRaise = fichas;

        // Check
        btnCheck.interactable = diferenca == 0;

        // Call
        btnCall.interactable = diferenca > 0 && fichas > 0;
        if (txtLabelCall != null)
            txtLabelCall.text = diferenca > 0 ? $"Call {diferenca}" : "Call";
        if (txtSubCall != null)
            txtSubCall.text = diferenca > 0 ? $"-{Mathf.Min(diferenca, fichas)} fichas" : "";

        // Raise
        btnRaise.interactable = fichas > diferenca;
        if (txtSubRaise != null)
            txtSubRaise.text = $"mín. {_minRaise}";
    }

    // ─────────────────────────────────────────────
    // AÇÕES DOS BOTÕES PRINCIPAIS
    // ─────────────────────────────────────────────
    void OnFold()  => GameManager.Instance.AcaoHumano("Fold");
    void OnCheck() => GameManager.Instance.AcaoHumano("Check");
    void OnCall()  => GameManager.Instance.AcaoHumano("Call");

    // ─────────────────────────────────────────────
    // PAINEL RAISE — TECLADO NUMÉRICO
    // ─────────────────────────────────────────────
    void AbrirRaise()
    {
        _inputRaise = "";
        AtualizarDisplayRaise();
        if (txtMinRaise != null) txtMinRaise.text = $"Mín: {_minRaise}";
        if (txtMaxRaise != null) txtMaxRaise.text = $"Máx: {_maxRaise}";
        painelRaise.SetActive(true);
        painelAcoes.SetActive(false);
    }

    void FecharRaise()
    {
        painelRaise.SetActive(false);
        painelAcoes.SetActive(true);
    }

    void DigitarNumero(int digito)
    {
        if (_inputRaise.Length >= 6) return;
        if (_inputRaise == "0") _inputRaise = "";
        _inputRaise += digito.ToString();
        AtualizarDisplayRaise();
    }

    void ApagarDigito()
    {
        if (_inputRaise.Length == 0) return;
        _inputRaise = _inputRaise[..^1];
        AtualizarDisplayRaise();
    }

    void ClicarAllIn()
    {
        _inputRaise = _maxRaise.ToString();
        AtualizarDisplayRaise();
    }

    void AtualizarDisplayRaise()
    {
        if (txtValorRaise == null) return;
        int valor = _inputRaise.Length > 0 && int.TryParse(_inputRaise, out int v) ? v : 0;
        txtValorRaise.text = valor.ToString();

        // Feedback visual se fora dos limites
        bool valido = valor >= _minRaise && valor <= _maxRaise;
        txtValorRaise.color = valido ? CorDourado : CorUrgente;

        if (btnConfirmarRaise != null)
            btnConfirmarRaise.interactable = valido;
    }

    void ConfirmarRaise()
    {
        if (!int.TryParse(_inputRaise, out int valor)) return;
        valor = Mathf.Clamp(valor, _minRaise, _maxRaise);
        FecharRaise();
        GameManager.Instance.AcaoHumano("Raise", valor);
    }

    // ─────────────────────────────────────────────
    // TEMPORIZADOR
    // ─────────────────────────────────────────────
    void IniciarTimer()
    {
        PararTimer();
        _corTimer = StartCoroutine(CorTimer());
    }

    void PararTimer()
    {
        if (_corTimer != null) { StopCoroutine(_corTimer); _corTimer = null; }
        if (txtTimer != null)  txtTimer.text = "";
    }

    IEnumerator CorTimer()
    {
        float t = TEMPO_LIMITE;
        while (t > 0f)
        {
            if (txtTimer != null)
            {
                txtTimer.text  = Mathf.CeilToInt(t).ToString();
                txtTimer.color = t <= 10f ? CorUrgente : CorDourado;
            }
            t -= Time.deltaTime;
            yield return null;
        }

        // Tempo esgotado — fold automático
        GameManager.Instance.AcaoHumano("Fold");
    }

    // ─────────────────────────────────────────────
    // HISTÓRICO
    // ─────────────────────────────────────────────
    void AdicionarHistorico(string mensagem, Color cor)
    {
        if (prefabLinhaHistorico == null || painelHistorico == null) return;

        GameObject linha = Instantiate(prefabLinhaHistorico, painelHistorico);
        var tmp = linha.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) { tmp.text = mensagem; tmp.color = cor; }

        _linhas.Enqueue(linha);
        if (_linhas.Count > MAX_LINHAS)
            Destroy(_linhas.Dequeue());
    }

    // ─────────────────────────────────────────────
    // LIMPAR MESA (nova mão)
    // ─────────────────────────────────────────────
    void LimparMesa()
    {
        if (indicadoresFold != null)
            foreach (var obj in indicadoresFold)
                if (obj != null) obj.SetActive(false);

        if (indicadoresVez != null)
            foreach (var obj in indicadoresVez)
                if (obj != null) obj.SetActive(false);
    }
}