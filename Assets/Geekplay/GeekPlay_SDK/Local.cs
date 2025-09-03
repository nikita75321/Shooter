using System; 
using System.Collections;
using System.Collections.Generic; 
using System.Net.Http; 
using System.Net.Http.Headers; 
using System.Text; 
using System.Threading.Tasks; 
using Newtonsoft.Json; 
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Local : MonoBehaviour
{
    int count;
    TextMeshProUGUI txt;

    [SerializeField] private TMP_FontAsset arFont;
    [SerializeField] private TMP_FontAsset jaFont;
    [SerializeField] private TMP_FontAsset hiFont;


    private string[] firstLines;
    private List<string> needLines = new List<string>();
    private static readonly HttpClient client = new HttpClient(); 

    private string[] firstLinesTR;
    private List<string> needLinesTR = new List<string>();

    private string[] firstLinesOTHER;
    private List<string> needLinesOTHER = new List<string>();

    string responseBodyOther;

    [InfoBox("Помните, что это платно и используйте кнопку Translate,\nкогда точно определились с ru текстом.", InfoMessageType.Warning)]
    [TabGroup("Обязательные тексты")]
    [InfoBox("Русский")]
    [SerializeField] private string ru;
    [TabGroup("Обязательные тексты")]
    [InfoBox("Английский")]
    [SerializeField] private string en;
    [TabGroup("Обязательные тексты")]
    [InfoBox("Турецкий")]
    [SerializeField] private string tr;

    [TabGroup("Тексты группы 2")]
    [InfoBox("Помните, что это платно и используйте кнопку Translate,\nкогда точно определились с ru текстом.", InfoMessageType.Warning)]
    [InfoBox("Арабский")]
    [ShowIf("local2")]
    [SerializeField] private string ar;
    [TabGroup("Тексты группы 2")]
    [InfoBox("Испанский")]
    [ShowIf("local2")]
    [SerializeField] private string es;
    [TabGroup("Тексты группы 2")]
    [InfoBox("Португальский")]
    [ShowIf("local2")]
    [SerializeField] private string pt;
    [TabGroup("Тексты группы 3")]
    [InfoBox("Помните, что это платно и используйте кнопку Translate,\nкогда точно определились с ru текстом.", InfoMessageType.Warning)]
    [InfoBox("Индонезийский")]
    [ShowIf("local3")]
    [SerializeField] private string id;
    [TabGroup("Тексты группы 3")]
    [InfoBox("Французский")]
    [ShowIf("local3")]
    [SerializeField] private string fr;
    [TabGroup("Тексты группы 3")]
    [InfoBox("Японский")]
    [ShowIf("local3")]
    [SerializeField] private string ja;
    [TabGroup("Тексты группы 2")]
    [InfoBox("Немецкий")]
    [ShowIf("local2")]
    [SerializeField] private string de;
    [TabGroup("Тексты группы 3")]
    [InfoBox("Хинди")]
    [ShowIf("local3")]
    [SerializeField] private string hi;

    [PropertySpace(SpaceBefore = 20)]
    [HorizontalGroup("G1", LabelWidth = 45, MarginLeft = 0.15f)]
    public bool local1 = true;
    [PropertySpace(SpaceBefore = 20)]
    [HorizontalGroup("G1")]
    public bool local2 = false;
    [PropertySpace(SpaceBefore = 20)]
    [HorizontalGroup("G1")]
    public bool local3 = false;

    void Start()
    {
        txt = GetComponent<TextMeshProUGUI>();
        switch (Geekplay.Instance.Language)
        {
            case "ru":
                txt.text = ru;
                break;
            case "en":
                txt.text = en;
                break;
            case "tr":
                txt.text = tr;
                break;
            case "ar":
                txt.text = ar;
                txt.font = arFont;
                if(gameObject.CompareTag("dd"))
                {
                    var vipTemp = "";
                    var moneyTemp = "";

                    var s = txt.text.Split(" ");
                    vipTemp = s[0] + s[1];
                    moneyTemp = s[2] +" "+ s[3];

                    txt.text = ReverseString(vipTemp) +" "+ moneyTemp;
                }
                else
                    txt.text = ReverseString(txt.text);
                break;
            case "es":
                txt.text = es;
                break;
            case "pt":
                txt.text = pt;
                break;
            case "id":
                txt.text = id;
                break;
            case "fr":
                txt.text = fr;
                break;
            case "ja":
                txt.font = jaFont;
                txt.text = ja;
                break;
            case "de":
                txt.text = de;
                break;
            case "hi":
                txt.font = hiFont;
                txt.text = hi;
                break;
        }
    }
    public string ReverseString(string s)
    {
        char[] arr = s.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }

    async Task PostEN() 
    { 
        string folderId = "b1getmtb1p1ho2akg51i";
        string targetLanguage = "en";
        var texts = txt.text.Split(" ");

        var body = new
        {
            targetLanguageCode = targetLanguage,
            texts = texts,
            folderId = folderId,
        };

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Key", "AQVNzJUv78qwMZq5EqH8gl4WhgVOMjbvozU6TD5T");

        var json = JsonConvert.SerializeObject(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("https://translate.api.cloud.yandex.net/translate/v2/translate", content);

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            WorkWithStringEN(responseBody);
        }
        else
        {
            Debug.Log($"Error: {response.StatusCode}");
        }
    } 
    async Task PostTR() 
    { 
        string folderId = "b1getmtb1p1ho2akg51i";
        string targetLanguage = "tr";
        var texts = txt.text.Split(" ");

        var body = new
        {
            targetLanguageCode = targetLanguage,
            texts = texts,
            folderId = folderId,
        };

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Key", "AQVNzJUv78qwMZq5EqH8gl4WhgVOMjbvozU6TD5T");

        var json = JsonConvert.SerializeObject(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("https://translate.api.cloud.yandex.net/translate/v2/translate", content);

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            WorkWithStringTR(responseBody);
        }
        else
        {
            Debug.Log($"Error: {response.StatusCode}");
        }
    } 

    async Task PostOther(string lang, UnityAction action) 
    { 
        string folderId = "b1getmtb1p1ho2akg51i";
        string targetLanguage = lang;
        var texts = txt.text.Split(" ");

        var body = new
        {
            targetLanguageCode = targetLanguage,
            texts = texts,
            folderId = folderId,
        };

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Key", "AQVNzJUv78qwMZq5EqH8gl4WhgVOMjbvozU6TD5T");

        var json = JsonConvert.SerializeObject(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("https://translate.api.cloud.yandex.net/translate/v2/translate", content);

        if (response.IsSuccessStatusCode)
        {
            responseBodyOther = await response.Content.ReadAsStringAsync();
            action.Invoke();
        }
        else
        {
            Debug.Log($"Error: {response.StatusCode}");
        }
    } 
    void WorkWithStringEN(string request)
    {
        en = "";
        needLines.Clear();
        firstLines = request.Split("\n");
        for (int i = 0; i < firstLines.Length; i++)
        {
            if ((i + 1) % 4 == 0 && i != 0)
                needLines.Add(firstLines[i]);
        }
        for (int i = 0; i < needLines.Count; i++)
        {
            if (!needLines[i].Contains("\""))
            {
                needLines.RemoveAt(i);
            }
        }
        for (int i = 0; i < needLines.Count; i++)
        {
            needLines[i] = needLines[i].Substring(needLines[i].IndexOf(":"));
        }
        for (int i = 0; i < needLines.Count; i++)
        {
            needLines[i] = needLines[i].Substring(3);
        }
        for (int i = 0; i < needLines.Count; i++)
        {
            needLines[i] = needLines[i].Remove(needLines[i].Length - 2);
        }
        for (int i = 0; i < needLines.Count; i++)
        {
            en += needLines[i];
            en += " ";
        }
    }

    void WorkWithStringTR(string request)
    {
        tr = "";
        needLinesTR.Clear();
        firstLinesTR = request.Split("\n");
        for (int i = 0; i < firstLinesTR.Length; i++)
        {
            if ((i + 1) % 4 == 0 && i != 0)
                needLinesTR.Add(firstLinesTR[i]);
        }
        for (int i = 0; i < needLinesTR.Count; i++)
        {
            if (!needLinesTR[i].Contains("\""))
            {
                needLinesTR.RemoveAt(i);
            }
        }
        for (int i = 0; i < needLinesTR.Count; i++)
        {
            needLinesTR[i] = needLinesTR[i].Substring(needLinesTR[i].IndexOf(":"));
        }
        for (int i = 0; i < needLinesTR.Count; i++)
        {
            needLinesTR[i] = needLinesTR[i].Substring(3);
        }
        for (int i = 0; i < needLinesTR.Count; i++)
        {
            needLinesTR[i] = needLinesTR[i].Remove(needLinesTR[i].Length - 2);
        }
        for (int i = 0; i < needLinesTR.Count; i++)
        {
            tr += needLinesTR[i];
            tr += " ";
        }
    }

    void WorkWithStringOther(string lang)
    {
        switch (lang)
        {
            case "ar":
                ar = "";
                break;
            case "es":
                es = "";
                break;
            case "pt":
                pt = "";
                break;
            case "de":
                de = "";
                break;

            case "id":
                id = "";
                break;
            case "fr":
                fr = "";
                break;
            case "ja":
                ja = "";
                break;
            case "hi":
                hi = "";
                break;
        }
        needLinesOTHER.Clear();
        firstLinesOTHER = responseBodyOther.Split("\n");
        for (int i = 0; i < firstLinesOTHER.Length; i++)
        {
            if ((i + 1) % 4 == 0 && i != 0)
                needLinesOTHER.Add(firstLinesOTHER[i]);
        }
        for (int i = 0; i < needLinesOTHER.Count; i++)
        {
            if (!needLinesOTHER[i].Contains("\""))
            {
                needLinesOTHER.RemoveAt(i);
            }
        }
        for (int i = 0; i < needLinesOTHER.Count; i++)
        {
            needLinesOTHER[i] = needLinesOTHER[i].Substring(needLinesOTHER[i].IndexOf(":"));
        }
        for (int i = 0; i < needLinesOTHER.Count; i++)
        {
            needLinesOTHER[i] = needLinesOTHER[i].Substring(3);
        }
        for (int i = 0; i < needLinesOTHER.Count; i++)
        {
            needLinesOTHER[i] = needLinesOTHER[i].Remove(needLinesOTHER[i].Length - 2);
        }
        for (int i = 0; i < needLinesOTHER.Count; i++)
        {
            switch (lang)
            {
                case "ar":
                    ar += needLinesOTHER[i];
                    ar += " ";
                    PostOther("es", () => WorkWithStringOther("es"));
                    break;
                case "es":
                    es += needLinesOTHER[i];
                    es += " ";
                    PostOther("pt", () => WorkWithStringOther("pt"));
                    break;
                case "pt":
                    pt += needLinesOTHER[i];
                    pt += " ";
                    PostOther("de", () => WorkWithStringOther("de"));
                    break;
                case "de":
                    de += needLinesOTHER[i];
                    de += " ";
                    if (local3)
                        PostOther("id", () => WorkWithStringOther("id"));
                    break;

                case "id":
                    id += needLinesOTHER[i];
                    id += " ";
                    PostOther("fr", () => WorkWithStringOther("fr"));
                    break;
                case "fr":
                    fr += needLinesOTHER[i];
                    fr += " ";
                    PostOther("ja", () => WorkWithStringOther("ja"));
                    break;
                case "ja":
                    ja += needLinesOTHER[i];
                    ja += " ";
                    PostOther("hi", () => WorkWithStringOther("hi"));
                    break;
                case "hi":
                    hi += needLinesOTHER[i];
                    hi += " ";
                    break;
            }
        }
    }

    [PropertySpace(SpaceBefore = 5)]
    [HorizontalGroup("Split", 0.5f, MarginLeft = 0.25f)]
    [Button(ButtonSizes.Large), GUIColor(0.5f,1f,1)]
    private void Translate()
    {
        //if (count >= 3)
        //{
        //    Debug.LogError("It not free! Stop translate please!");
        //    return;
        //}
        txt = GetComponent<TextMeshProUGUI>();
        ru = txt.text;
        if (local1)
        {
            PostEN();
            PostTR();
        }
        if (local2)
        {
            PostOther("ar", () => WorkWithStringOther("ar"));
        }
        else if (local3 && !local2)
        {
            PostOther("id", () => WorkWithStringOther("id"));
        }

        count += 1; 
    }
}
