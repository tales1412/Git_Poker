using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Deck : MonoBehaviour
{
    public GameObject cartaPrefab;
    public Transform[] posicoesJogador;  // 2 slots para o jogador
    public Transform[] posicoesIA;       // 2 slots para a IA
    public Transform posicaoDeck;        // ponto de origem das cartas

    private List<Card> baralho = new List<Card>();

    void Start()
    {
        CriarBaralho();
        Embaralhar();
        StartCoroutine(DistribuirCartas());
    }

    void CriarBaralho()
    {
        foreach (Card.Suit naipe in System.Enum.GetValues(typeof(Card.Suit)))
        {
            foreach (Card.Value valor in System.Enum.GetValues(typeof(Card.Value)))
            {
                GameObject obj = Instantiate(cartaPrefab, posicaoDeck.position, cartaPrefab.transform.rotation);
                Card carta = obj.GetComponent<Card>();
                carta.naipe = naipe;
                carta.valor = valor;
                carta.VirarParaBaixo();
                baralho.Add(carta);
                obj.SetActive(false); // esconde até distribuir
            }
        }
    }

    void Embaralhar()
    {
        for (int i = baralho.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = baralho[i];
            baralho[i] = baralho[j];
            baralho[j] = temp;
        }
    }

    System.Collections.IEnumerator DistribuirCartas()
    {
        yield return new WaitForSeconds(0.5f);

        int index = 0;

        // Distribui 2 cartas para o jogador
        foreach (Transform slot in posicoesJogador)
        {
            Card carta = baralho[index++];
            carta.gameObject.SetActive(true);
            carta.transform.position = posicaoDeck.position;

            // Anima a carta indo até o slot
            carta.transform.DOMove(slot.position, 0.4f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.3f);
            carta.VirarParaCima();
        }

        yield return new WaitForSeconds(0.3f);

        // Distribui 2 cartas para a IA (viradas para baixo)
        foreach (Transform slot in posicoesIA)
        {
            Card carta = baralho[index++];
            carta.gameObject.SetActive(true);
            carta.transform.position = posicaoDeck.position;

            carta.transform.DOMove(slot.position, 0.4f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.3f);
        }
    }
}