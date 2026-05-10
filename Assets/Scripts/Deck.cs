using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Deck : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERÊNCIAS
    // ─────────────────────────────────────────────
    [Header("Referências")]
    public GameObject cartaPrefab;
    public Transform posicaoDeck;

    // ─────────────────────────────────────────────
    // BARALHO
    // ─────────────────────────────────────────────
    private List<PokerCard> baralho = new List<PokerCard>();
    private int indiceAtual = 0;

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────
    void Awake()
    {
        CriarBaralho();
        Embaralhar();
    }

    // ─────────────────────────────────────────────
    // CRIAÇÃO E EMBARALHAMENTO
    // ─────────────────────────────────────────────
    void CriarBaralho()
    {
        baralho.Clear();

        foreach (PokerCard.Suit naipe in System.Enum.GetValues(typeof(PokerCard.Suit)))
        {
            foreach (PokerCard.Value valor in System.Enum.GetValues(typeof(PokerCard.Value)))
            {
                GameObject obj = Instantiate(cartaPrefab, posicaoDeck.position, cartaPrefab.transform.rotation);
                PokerCard carta = obj.GetComponent<PokerCard>();
                carta.naipe = naipe;
                carta.valor = valor;
                carta.VirarParaBaixo();
                baralho.Add(carta);
                obj.SetActive(false);
            }
        }
    }

    void Embaralhar()
    {
        indiceAtual = 0;

        for (int i = baralho.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            PokerCard temp = baralho[i];
            baralho[i] = baralho[j];
            baralho[j] = temp;
        }
    }

    // ─────────────────────────────────────────────
    // COMPRAR CARTA (usado pelo GameManager)
    // ─────────────────────────────────────────────
    public PokerCard ComprarCarta()
    {
        if (indiceAtual >= baralho.Count)
        {
            Debug.LogWarning("[Deck] Baralho esgotado!");
            return null;
        }
        return baralho[indiceAtual++];
    }

    // ─────────────────────────────────────────────
    // DISTRIBUIR CARTAS PARA OS JOGADORES
    // ─────────────────────────────────────────────
    public IEnumerator DistribuirCartasParaJogadores(
    List<GameManager.Jogador> jogadores, Transform[] slots)
{
    yield return new WaitForSeconds(0.5f);

    // Offset para separar as 2 cartas de cada jogador
    Vector3[] offsets = new Vector3[]
{
    new Vector3(-0.7f, 0f, 0.1f),  // carta 1: mais à esquerda
    new Vector3( 0.7f, 0f, 0.1f)   // carta 2: mais à direita
};

    for (int rodada = 0; rodada < 2; rodada++)
    {
        for (int i = 0; i < jogadores.Count; i++)
        {
            PokerCard carta = ComprarCarta();
            if (carta == null) yield break;

            jogadores[i].mao.Add(carta);
            carta.gameObject.SetActive(true);
            carta.transform.position = posicaoDeck.position;

            Vector3 destino = slots[i].position + offsets[rodada];
            carta.transform.DOMove(destino, 0.4f).SetEase(Ease.OutCubic);

            yield return new WaitForSeconds(0.25f);

            if (i == 0) carta.VirarParaCima();
        }

        yield return new WaitForSeconds(0.2f);
    }
}

    // ─────────────────────────────────────────────
    // RESETAR BARALHO (nova mão)
    // ─────────────────────────────────────────────
    public void ResetarBaralho()
    {
        // Desativa e reseta todas as cartas
        foreach (PokerCard carta in baralho)
        {
            carta.VirarParaBaixo();
            carta.gameObject.SetActive(false);
            carta.transform.position = posicaoDeck.position;
        }

        Embaralhar();
    }
}