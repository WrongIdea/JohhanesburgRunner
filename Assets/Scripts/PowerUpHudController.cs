using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    /// <summary>Compact, safe-area power-up stack with icon, radial timer and seconds.</summary>
    public sealed class PowerUpHudController : MonoBehaviour
    {
        [SerializeField] PowerUpManager manager;
        [SerializeField] PowerUpType[] types;
        [SerializeField] GameObject[] roots;
        [SerializeField] Image[] fills;
        [SerializeField] TextMeshProUGUI[] seconds;
        int[] shownSeconds;

#if UNITY_EDITOR
        public void Configure(PowerUpManager source, Sprite[] icons, Sprite panelSprite)
        {
            manager = source;
            types = new[] { PowerUpType.TaxiMagnet, PowerUpType.JoziSneakers, PowerUpType.DroneBoost,
                PowerUpType.UbuntuMultiplier, PowerUpType.Hoverboard, PowerUpType.DoubleCoins, PowerUpType.UbuntuPulse };
            roots = new GameObject[types.Length]; fills = new Image[types.Length]; seconds = new TextMeshProUGUI[types.Length];
            shownSeconds = new int[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                GameObject tile = new GameObject(types[i] + "Indicator", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(transform, false); roots[i] = tile;
                RectTransform rt = (RectTransform)tile.transform; rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(32f, -152f - i * 112f); rt.sizeDelta = new Vector2(104f, 96f);
                Image bg = tile.GetComponent<Image>(); bg.sprite = panelSprite; bg.type = Image.Type.Sliced; bg.color = new Color(0.025f, .04f, .075f, .78f);

                Image ringBg = CreateImage(tile.transform, "RingBackground", panelSprite, new Color(1f,1f,1f,.16f)); StretchSquare(ringBg.rectTransform, 82f);
                Image ring = CreateImage(tile.transform, "Countdown", panelSprite, new Color(.25f,.78f,1f,.95f)); StretchSquare(ring.rectTransform, 82f);
                ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360; ring.fillOrigin = (int)Image.Origin360.Top; ring.fillClockwise = false; fills[i] = ring;
                Image icon = CreateImage(tile.transform, "Icon", i < icons.Length ? icons[i] : null, Color.white); StretchSquare(icon.rectTransform, 58f); icon.preserveAspect = true;
                TextMeshProUGUI time = new GameObject("Seconds", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                time.transform.SetParent(tile.transform, false); time.fontSize = 25; time.fontStyle = FontStyles.Bold; time.color = Color.white; time.alignment = TextAlignmentOptions.Center;
                RectTransform tr = time.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(1f,0f); tr.pivot = new Vector2(1f,0f); tr.anchoredPosition = new Vector2(-5f,4f); tr.sizeDelta = new Vector2(46f,32f); seconds[i] = time;
                tile.SetActive(false);
            }
        }
        static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>(); image.transform.SetParent(parent,false); image.sprite=sprite; image.color=color; return image;
        }
        static void StretchSquare(RectTransform rt,float size){rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(size,size);}
#endif

        void Awake() { if (shownSeconds == null || shownSeconds.Length != (types?.Length ?? 0)) shownSeconds = new int[types?.Length ?? 0]; }
        void Update()
        {
            if (manager == null || roots == null) return;
            for (int i=0;i<roots.Length;i++)
            {
                bool active=manager.IsActive(types[i]); GameObject root=roots[i];
                if(active && !root.activeSelf){root.SetActive(true);root.transform.localScale=Vector3.one;}
                if(!active){if(root.activeSelf)root.SetActive(false);continue;}
                float remain=manager.TimeRemaining(types[i]), duration=PowerUpManager.Duration(types[i]);
                fills[i].fillAmount=duration>0f?remain/duration:0f;
                int whole=Mathf.CeilToInt(remain);if(shownSeconds[i]!=whole){shownSeconds[i]=whole;seconds[i].text=whole+"s";}
                float pulse=remain<=3f?1f+.06f*(.5f+.5f*Mathf.Sin(Time.unscaledTime*7f)):1f;
                root.transform.localScale=Vector3.one*pulse;
            }
        }
    }
}
