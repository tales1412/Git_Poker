using UnityEngine;

public class Card : MonoBehaviour
{
    public enum Suit { Copas, Ouros, Paus, Espadas }
    public enum Value { Dois=2, Tres, Quatro, Cinco, Seis, Sete,
                        Oito, Nove, Dez, Valete, Dama, Rei, As }

    public Suit naipe;
    public Value valor;
    public bool viradaParaCima = false;

    private MeshRenderer meshRenderer;
    public Material materialFrente;
    public Material materialVerso;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void VirarParaCima()
    {
        viradaParaCima = true;
        meshRenderer.material = materialFrente;
    }

    public void VirarParaBaixo()
    {
        viradaParaCima = false;
        meshRenderer.material = materialVerso;
    }

    public override string ToString()
    {
        return $"{valor} de {naipe}";
    }
}