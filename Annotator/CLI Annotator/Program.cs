using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml;
using System.Reflection;

class TextHanterare
{
    private HttpClient client;
    public TextHanterare()
    {
        client = new HttpClient();
        client.BaseAddress = new Uri("https://sv.wiktionary.org/");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    public string ParseWord(string ord)
    {
        string ordklass = "";
        HttpResponseMessage response = client.GetAsync($"/w/api.php?action=query&format=json&uselang=sv&prop=cirrusdoc&titles={ord}&redirects=1").Result;
        if (response.IsSuccessStatusCode)
        {
            var data = (JObject)JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
            var pageid = data["query"]["pages"].Last.Path.Substring(12);
            Console.WriteLine(pageid);
            if (pageid != "-1")
            {
                var headings = data["query"]["pages"][data["query"]["pages"].Last.Path.Substring(12)]["cirrusdoc"][0]["source"]["heading"].ToObject<List<string>>();
                ordklass = headings.SkipWhile(h => h != "Svenska").Skip(1).FirstOrDefault();
            }
            else
            {
                if (ord.Any(c => char.IsUpper(c)))
                {
                    ordklass = ParseWord(ord.ToLower());
                }
                else
                {
                    ordklass = "?";
                }
            }
        }

        return ordklass;
    }
    public string PipeList(List<string> lista)
    {
        string ord_piped = "";
        foreach (var ord in lista)
        {
            ord_piped += ord + "|";
        }
        return ord_piped.Remove(ord_piped.Length - 1);
    }
    public Dictionary<string, string> ParseWords(List<string> ord)
    {
        Dictionary<string, string> orddef = new Dictionary<string, string>();
        HttpResponseMessage response = client.GetAsync($"/w/api.php?action=query&format=json&uselang=sv&prop=cirrusdoc&titles={PipeList(ord)}&redirects=1").Result;
        if (response.IsSuccessStatusCode)
        {
            var data = (JObject)JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);

            var pages = data["query"]["pages"];

            List<string> failed = new List<string>();

            foreach (var page in pages)
            {
                var pageid = page.Path.Substring(12);
                var ordnamn = pages[pageid]["title"].Value<string>();

                if (pageid.StartsWith("-")) // fail
                {
                    failed.Add(ordnamn);
                }
                else
                {
                    orddef.Add(ordnamn, pages[pageid]["cirrusdoc"][0]["source"]["heading"].ToObject<string[]>().SkipWhile(h => h != "Svenska").Skip(1).FirstOrDefault());
                }
            }
            //ParseWords(failed).ToList().ForEach(x => orddef.Add(x.Key, x.Value));
        }
        return orddef;
    }

    public List<List<string>> ParseText(string text)
    {
        List<List<string>> lista = new List<List<string>>();
        string[] ord = text.Split(' ', '.', ',', '/', '(', ')');
        int i = 0;

        for (; i < ord.Length / 50; i++) // add clean
        {
            lista.Add(new List<string>());
            for (int j = 0; j < 50; j++)
            {
                lista[i].Add(ord[j].Trim());
            }
        }

        lista.Add(new List<string>());
        for (int k = 50 * i; k < ord.Length; k++)
        {
            lista[i].Add(ord[k].Trim());
        }
        return lista;
    }

}
class Klassifierare
{
    private HttpClient client;

    public Dictionary<string, string> Ordklass = new Dictionary<string, string>()
    {
        {"AB","Adverb" },
        {"DT","Determinerare, bestämningsord" },
        {"HA","Frågande/relativt adverb"},
        {"HD","Frågande/relativ bestämning"},
        {"HP","Frågande/relativt pronomen"},
        {"HS","Frågande/relativt possessivuttryck"},
        {"IE","Infinitivmärke"},
        {"IN","Interjektion"},
        {"JJ","Adjektiv"},
        {"KN","Konjunktion"},
        {"NN","Substantiv"},
        {"PC","Particip"},
        {"PL","Partikel"},
        {"PM","Egennamn"},
        {"PN","Pronomen"},
        {"PP","Preposition"},
        {"PS","Possessivuttryck"},
        {"RG","Räkneord: grundtal"},
        {"RO","Räkneord: ordningstal"},
        {"SN","Subjunktion"},
        {"UO","Utländskt ord"},
        {"VB","Verb"}
    };

    public Dictionary<string, string> Genus = new Dictionary<string, string>
    {
        {"UTR","Utrum"},
        {"NEU","Neutrum"},
        {"MAS","Maskulinum"},
        {"UTR/NEU","Underspecificerat"}
    };

    public Dictionary<string, string> Numerus = new Dictionary<string, string>
    {
        {"SIN","Singularis"},
        {"PLU","Pluralis"},
        {"SIN/PLU","Underspecificerat"}
    };

    public Dictionary<string, string> Bestämdhet = new Dictionary<string, string>
    {
        {"IND","Obestämd"},
        {"DEF","Bestämd"},
        {"IND/DEF","Underspecificerat"}
    };

    public Dictionary<string, string> Substantivform = new Dictionary<string, string>
    {
        {"NOM","Nominativ"},
        {"GEN","Genitiv"},
        {"SMS","Sammansättning"},
        {"-", "Ospecificerat"}
    };

    public Dictionary<string, string> Komparation = new Dictionary<string, string>
    {
        {"POS","Positiv"},
        {"KOM","Komparativ"},
        {"SUV","Superlativ"}
    };

    public Dictionary<string, string> Satsdel = new Dictionary<string, string>
    {
        {"SUB","Subjekt"},
        {"OBJ","Objekt"},
        {"SUB/OBJ","Underspecificerat"}
    };

    public Dictionary<string, string> Verbform = new Dictionary<string, string>
    {
        {"PRS","Presens"},
        {"PRT","Preteritum (imperfekt)"},
        {"INF","Infinitiv"},
        {"SUP","Supinum"},
        {"IMP","Imperativ"},
        {"AKT","Aktiv diates"},
        {"SFO","S-form (passivum, deponens)"},
        {"KON","Konjunktiv"},
        {"PRF","Perfekt particip"}
    };

    public Dictionary<string, string> Övrigt = new Dictionary<string, string>
    {
        {"AN","Förkortning"},
        {"MAD","Meningsskiljande interpunktion"},
        {"MID","Interpunktion"},
        {"PAD","Interpunktion" }
    };
    private void SetDef(Ord ord, string input)
    {
        foreach (var ms in input.Split('.', '+'))
        {
            if (Ordklass.ContainsKey(ms))
            {
                ord.Ordklass = Ordklass[ms];
            }
            else if (Genus.ContainsKey(ms))
            {
                ord.Genus = Genus[ms];
            }
            else if (Numerus.ContainsKey(ms))
            {
                ord.Numerus = Numerus[ms];
            }
            else if (Bestämdhet.ContainsKey(ms))
            {
                ord.Bestämdhet = Bestämdhet[ms];
            }
            else if (Substantivform.ContainsKey(ms))
            {
                ord.SubstantivForm = Substantivform[ms];
            }
            else if (Komparation.ContainsKey(ms))
            {
                ord.Komparation = Komparation[ms];
            }
            else if (Satsdel.ContainsKey(ms))
            {
                ord.Satsdel = Satsdel[ms];
            }
            else if (Verbform.ContainsKey(ms))
            {
                ord.Verbform = Verbform[ms];
            }
            else if (Övrigt.ContainsKey(ms))
            {
                ord.Övrigt = Övrigt[ms];
            }
        }
    }
    public Klassifierare()
    {
        client = new HttpClient();
        client.BaseAddress = new Uri("https://ws.spraakbanken.gu.se/");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
    }
    public Ord DefineraOrd(string ord)
    {
        Ord nyord = new Ord();
        HttpResponseMessage response = client.GetAsync($"/ws/sparv/v2/?text={ord}" + "&settings={\"positional_attributes\":{\"lexical_attributes\":[\"pos\",\"msd\",\"lemma\"]}}").Result;
        if (response.IsSuccessStatusCode)
        {
            var xml = new XmlDocument();
            xml.LoadXml(response.Content.ReadAsStringAsync().Result);
            var data = (JObject)JsonConvert.DeserializeObject(JsonConvert.SerializeXmlNode(xml));
            var word = data["result"]["corpus"]["text"]["paragraph"]["sentence"]["w"];
            SetDef(nyord, word["@msd"].Value<string>());
            nyord.Term = ord;
            nyord.Grundform = word["@lemma"].Value<string>();
        }
        return nyord;
    }
    public List<Ord> AnnoteraMening(string mening)
    {
        List<Ord> ord = new List<Ord>();
        HttpResponseMessage response = client.GetAsync($"/ws/sparv/v2/?text={mening}" + "&settings={\"positional_attributes\":{\"lexical_attributes\":[\"pos\",\"msd\",\"lemma\"],\"dependency_attributes\":[\"ref\",\"dephead\",\"deprel\"]}}").Result;
        if (response.IsSuccessStatusCode)
        {
            var xml = new XmlDocument();
            xml.LoadXml(response.Content.ReadAsStringAsync().Result);
            var data = (JObject)JsonConvert.DeserializeObject(JsonConvert.SerializeXmlNode(xml));
            var sentences = data["result"]["corpus"]["text"]["paragraph"]["sentence"];
            if (sentences.Count() > 2)
            {
                foreach (var sentence in sentences)
                {
                    foreach (var word in sentence["w"])
                    {
                        Ord nyord = new Ord();
                        nyord.Term = word["#text"].Value<string>();
                        SetDef(nyord, word["@msd"].Value<string>());
                        nyord.Grundform = word["@lemma"].Value<string>();
                        ord.Add(nyord);
                    }
                }
            }
            else
            {

                if (sentences["w"].Type.ToString() == "Array")
                {
                    foreach (var word in sentences["w"])
                    {
                        Ord nyord = new Ord();
                        nyord.Term = word["#text"].Value<string>();
                        SetDef(nyord, word["@msd"].Value<string>());
                        nyord.Grundform = word["@lemma"].Value<string>();
                        ord.Add(nyord);
                    }
                }
                else
                {
                    Ord nyord = new Ord();
                    var word = sentences["w"];
                    nyord.Term = word["#text"].Value<string>();
                    SetDef(nyord, word["@msd"].Value<string>());
                    nyord.Grundform = word["@lemma"].Value<string>();
                    ord.Add(nyord);
                }
            }

        }
        return ord;
    }
}
class Ord
{
    public string Term { get; set; }
    public string Ordklass { get; set; }
    public string Grundform { get; set; }
    public string Genus { get; set; }
    public string Numerus { get; set; }
    public string Bestämdhet { get; set; }
    public string SubstantivForm { get; set; }
    public string Komparation { get; set; }
    public string Satsdel { get; set; }
    public string Verbform { get; set; }
    public string Övrigt { get; set; }
}
class Program
{

    static void Main(string[] args)
    {
        Klassifierare kls = new Klassifierare();
        string input = "";
        while (input != "!")
        {
            Console.Write("Mening/Ord: ");
            input = Console.ReadLine();
            Console.Write("\n");
            if (input.Contains(" "))
            {
                Console.WriteLine($"\"{input}\" => {{\n");
            }
            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var word in kls.AnnoteraMening(input))
            {
                Console.Write("    " + word.Term + " => {\n");
                foreach (PropertyInfo prop in word.GetType().GetProperties())
                {
                    if (!string.IsNullOrEmpty((string)prop.GetValue(word)) && (string)prop.GetValue(word) != "|")
                    {
                        Console.Write("      " + prop.Name + " = " + prop.GetValue(word) + "\n");
                    }
                }
                Console.WriteLine("    }\n");
            }
            if (input.Contains(" "))
            {
                Console.WriteLine("}\n");
            }
            watch.Stop();
            Console.WriteLine("Query Time: " + watch.Elapsed + "\n");
        }
    }
}
