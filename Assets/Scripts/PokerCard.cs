using UnityEngine;

public class PokerCard : MonoBehaviour
{
    public enum Suit  { Clubs, Diamonds, Hearts, Spades }
    public enum Value { Two=2, Three, Four, Five, Six, Seven,
                        Eight, Nine, Ten, J, Q, K, A }

    public Suit   naipe;
    public Value  valor;
    public bool   viradaParaCima = false;
    public string estilo = "Classic";

    public Material materialVerso;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    // ─────────────────────────────────────────────
    // VIRAR PARA CIMA
    // ─────────────────────────────────────────────
    public void VirarParaCima()
    {
        viradaParaCima = true;

        Sprite spriteValor = Resources.Load<Sprite>(
            $"uVegas/Images/Cards/{estilo}/{GetValorString()}");
        Sprite spriteNaipe = Resources.Load<Sprite>(
            $"uVegas/Images/Cards/{estilo}/{naipe}");

        if (spriteValor == null)
        {
            Debug.LogWarning($"[PokerCard] Valor não encontrado: {GetValorString()}");
            return;
        }
        if (spriteNaipe == null)
        {
            Debug.LogWarning($"[PokerCard] Naipe não encontrado: {naipe}");
            return;
        }

        Texture2D texValor = DuplicarTextura(spriteValor.texture);
        Texture2D texNaipe = DuplicarTextura(spriteNaipe.texture);

        Texture2D cartaFinal = MontarCarta(texValor, texNaipe);
        meshRenderer.material = CriarMaterial(cartaFinal);
    }

    // ─────────────────────────────────────────────
    // VIRAR PARA BAIXO
    // ─────────────────────────────────────────────
    public void VirarParaBaixo()
    {
        viradaParaCima = false;
        meshRenderer.material = materialVerso;
    }

    // ─────────────────────────────────────────────
    // DUPLICAR TEXTURA (sem precisar de Read/Write)
    // ─────────────────────────────────────────────
    Texture2D DuplicarTextura(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(source.width, source.height,
            TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    // ─────────────────────────────────────────────
    // MONTAR CARTA
    // ─────────────────────────────────────────────
Texture2D MontarCarta(Texture2D texValor, Texture2D texNaipe)
{
    int w = 350, h = 500;

    RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
    RenderTexture.active = rt;
    GL.Clear(true, true, Color.white);

    Color cor = (naipe == Suit.Hearts || naipe == Suit.Diamonds)
        ? new Color(0.85f, 0.1f, 0.1f)
        : new Color(0.05f, 0.05f, 0.05f);

    // Valor ocupa a carta inteira (já tem os dois cantos no sprite)
    DesenharTextura(texValor, new Rect(0, 0, w, h), cor, false);

    // Naipe central por cima
    DesenharTextura(texNaipe, new Rect(w/2f - 300, h/2f - 300, 600, 600), cor, false);

    Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
    result.ReadPixels(new Rect(0, 0, w, h), 0, 0);
    result.Apply();

    RenderTexture.active = null;
    RenderTexture.ReleaseTemporary(rt);
    return result;
}

    // ─────────────────────────────────────────────
    // DESENHAR TEXTURA NA RENDER TEXTURE
    // ─────────────────────────────────────────────
    void DesenharTextura(Texture2D tex, Rect rect, Color color, bool invertido)
    {
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, 350, 500, 0);

        Rect uv = invertido
            ? new Rect(1, 1, -1, -1)
            : new Rect(0, 0,  1,  1);

        Graphics.DrawTexture(rect, tex, uv, 0, 0, 0, 0, color);

        GL.PopMatrix();
    }

    // ─────────────────────────────────────────────
    // CRIAR MATERIAL
    // ─────────────────────────────────────────────
    Material CriarMaterial(Texture2D tex)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.mainTexture = tex;
        return mat;
    }

    // ─────────────────────────────────────────────
    // HELPER
    // ─────────────────────────────────────────────
    string GetValorString()
    {
        switch (valor)
        {
            case Value.Two:   return "2";
            case Value.Three: return "3";
            case Value.Four:  return "4";
            case Value.Five:  return "5";
            case Value.Six:   return "6";
            case Value.Seven: return "7";
            case Value.Eight: return "8";
            case Value.Nine:  return "9";
            case Value.Ten:   return "10";
            case Value.J:     return "J";
            case Value.Q:     return "Q";
            case Value.K:     return "K";
            case Value.A:     return "A";
            default:          return "";
        }
    }

    public override string ToString() => $"{valor} de {naipe}";
}