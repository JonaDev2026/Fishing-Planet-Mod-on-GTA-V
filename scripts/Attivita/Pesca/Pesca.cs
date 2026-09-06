// ============================================================
//  PESCA - versione pulita, si costruisce un pezzo alla volta
//  PASSO 1: il quaderno dei pesci (239 specie da pesci.txt)
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

public class Pesca : Script
{
    const string MY_DIR = "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V Enhanced\\scripts\\Attivita\\Pesca";
    const string TRAINER_DIR = "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V Enhanced\\scripts\\Trainer";

    // ---------- LA LINGUA ----------
    // Come Bus, Camionista e Fuzer: si legge 900= dal config.ini del
    // trainer. 0 = inglese, 1 = italiano.
    int lang = 1;

    void LeggiLingua()
    {
        try
        {
            string f = Path.Combine(TRAINER_DIR, "config.ini");
            if (!File.Exists(f)) return;
            string[] rows = File.ReadAllLines(f);
            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string r = rows[i].Trim();
                if (!r.StartsWith("900=")) continue;
                int val;
                if (int.TryParse(r.Substring(4).Trim(), out val)) lang = val;
            }
        }
        catch { }
    }

    string L(string en, string it)
    {
        return (lang == 1) ? it : en;
    }

    int ultimaLingua = 0;

    // i nomi italiani dei pesci, da pesci_it.txt
    Dictionary<string, string> nomiIt = new Dictionary<string, string>();

    void CaricaNomiIt()
    {
        nomiIt.Clear();
        string[] r = LeggiRighe("pesci_it.txt");
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 2) continue;
            string en = c[0].Trim(), it = c[1].Trim();
            if (en.Length == 0 || it.Length == 0) continue;
            if (!nomiIt.ContainsKey(en)) nomiIt.Add(en, it);
        }
    }

    // il nome di un pesce nella lingua scelta
    string NomeIt(string en)
    {
        if (lang != 1 || en == null) return en;
        string v;
        if (nomiIt.TryGetValue(en.Trim(), out v)) return v;
        return en;
    }

    // LE ESCHE IN ITALIANO, da esche_it.txt e colori_it.txt.
    // Le liste vere restano in inglese - sono i nomi del wiki e servono
    // per far tornare i conti - qui c'e' solo come si leggono.
    Dictionary<string, string> escheIt = new Dictionary<string, string>();
    Dictionary<string, string> coloriIt = new Dictionary<string, string>();

    void CaricaTabella(string file, Dictionary<string, string> d)
    {
        d.Clear();
        string[] r = LeggiRighe(file);
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 2) continue;
            string en = c[0].Trim(), it = c[1].Trim();
            if (en.Length == 0 || it.Length == 0) continue;
            if (!d.ContainsKey(en)) d.Add(en, it);
        }
    }

    // LA FORMA DI OGNI PESCE, da pesci_modello.txt.
    // a_c_fish ha tre corpi diversi: se il gioco ne pesca uno a caso, un
    // persico ti esce lungo come una trota. Qui ogni specie ha il suo.
    Dictionary<string, int> formaPesce = new Dictionary<string, int>();
    Dictionary<string, string> modelloPesce = new Dictionary<string, string>();

    void CaricaForme()
    {
        formaPesce.Clear();
        modelloPesce.Clear();
        string[] r = LeggiRighe("pesci_modello.txt");
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 3) continue;
            string en = c[0].Trim();
            if (en.Length == 0 || formaPesce.ContainsKey(en)) continue;
            modelloPesce.Add(en, c[1].Trim());
            formaPesce.Add(en, Numero(c[2].Trim()));
        }
    }

    int FormaDi(string nome)
    {
        int v;
        if (nome != null && formaPesce.TryGetValue(nome.Trim(), out v)) return v;
        return 0;
    }

    string ModelloDi(string nome)
    {
        string v;
        if (nome != null && modelloPesce.TryGetValue(nome.Trim(), out v)
            && v.Length > 0) return v;
        return "a_c_fish";
    }

    void CaricaEscheIt()
    {
        CaricaTabella("esche_it.txt", escheIt);
        CaricaTabella("colori_it.txt", coloriIt);
    }

    // il nome di un'esca nella lingua scelta
    string EscaIt(string en)
    {
        if (lang != 1 || en == null || en.Length == 0) return en;
        string v;
        if (escheIt.TryGetValue(en.Trim(), out v)) return v;
        return en;
    }

    // il colore di un'artificiale nella lingua scelta
    string ColoreIt(string en)
    {
        if (lang != 1 || en == null || en.Length == 0) return en;
        string v;
        if (coloriIt.TryGetValue(en.Trim(), out v)) return v;
        return en;
    }

    // piu' esche di fila, tradotte una per una
    string EscheIt(string elenco)
    {
        if (lang != 1 || elenco == null || elenco.Length == 0) return elenco;
        string[] pz = elenco.Split(',');
        string t = "";
        int i;
        for (i = 0; i < pz.Length; i++)
        {
            if (t.Length > 0) t += ", ";
            t += EscaIt(pz[i].Trim());
        }
        return t;
    }

    // ---------- i pesci letti da pesci.txt ----------
    class Specie
    {
        public string Nome;
        public string Img;
        public float KgC, KgT, KgU;          // comune / trofeo / unico  (pesi veri)
        public int PrC, PrT, PrU;            // prezzi veri
        public int Livello, Denti;
        public string Amo, Famiglia;
        public int[] Esche;                  // esche naturali
        public int[] Art;                    // esche artificiali
        public string[] Zone;
        public string Quando;                // notte/alba_tramonto/giorno/sempre
        public int Rarita;                   // 1 comunissimo ... 5 rarissimo
        public int Pred;                     // 1 = la sua pagina elenca artificiali
        public float TMin, TMax, TOtt;       // gradi dell'acqua (temperature_pesci.txt), -1 = non noti
    }
    List<Specie> pesci = new List<Specie>();

    List<string> esche = new List<string>();      // i nomi veri del wiki
    List<string> escheTipo = new List<string>();  // naturale / artificiale

    // ---------- le acque della mappa ----------
    // ============================================================
    //  LE AREE DI PESCA
    //  Non piu' dieci zone decise a tavolino sui nomi di GTA, ma le aree
    //  vere registrate andandoci sopra (acque.txt). Un posto appartiene
    //  all'area del punto registrato piu' vicino.
    //  Il GRUPPO e' l'acqua a cui l'area appartiene: la licenza si paga
    //  per gruppo, cosi' con "Alamo Sea" peschi in tutti i suoi tratti.
    // ============================================================
    List<string> arNome = new List<string>();
    List<string> arTipo = new List<string>();
    List<string> arGruppo = new List<string>();
    List<string> arCodice = new List<string>();   // codice del gruppo
    List<string> arFile = new List<string>();     // q_<n>.txt del quaderno
    List<float> arCx = new List<float>();
    List<float> arCy = new List<float>();
    List<float> arCz = new List<float>();
    List<List<string>> arZoneGta = new List<List<string>>();
    // IL PUNTO D'ACCESSO: dove si arriva davvero, segnato a mano.
    // Il centro geometrico puo' cadere su uno scoglio o in mezzo all'acqua:
    // il segnaposto e il blip vanno qui, se c'e'.
    // il livello che serve per pescare in quest'area, e l'acqua vera che
    // rappresenta: da aree_livello.txt
    List<int> arLiv = new List<int>();
    List<string> arAcqua = new List<string>();
    List<float> arAx = new List<float>();
    List<float> arAy = new List<float>();
    List<bool> arAcc = new List<bool>();
    // tutti i punti, con l'indice dell'area a cui appartengono
    List<float> apX = new List<float>();
    List<float> apY = new List<float>();
    List<int> apA = new List<int>();

    const float RAGGIO_AREA = 400f;   // oltre questo non sei in nessuna area

    static string Codicino(string t)
    {
        string r = "";
        int i;
        for (i = 0; i < t.Length && r.Length < 14; i++)
        {
            char c = char.ToLower(t[i]);
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) r += c;
        }
        if (r.Length == 0) r = "zona";
        return r;
    }

    void CaricaAree()
    {
        arNome.Clear(); arTipo.Clear(); arGruppo.Clear(); arCodice.Clear();
        arFile.Clear(); arCx.Clear(); arCy.Clear(); arCz.Clear(); arZoneGta.Clear();
        arAx.Clear(); arAy.Clear(); arAcc.Clear();
        arLiv.Clear(); arAcqua.Clear();
        apX.Clear(); apY.Clear(); apA.Clear();
        List<float> sx = new List<float>(), sy = new List<float>(), sz = new List<float>();
        List<int> sn = new List<int>();

        string[] r = LeggiRighe("acque.txt");
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 6) continue;
            string nome = c[0].Trim();
            int a = arNome.IndexOf(nome);
            if (a < 0)
            {
                a = arNome.Count;
                arNome.Add(nome);
                arTipo.Add(c[1].Trim());
                string gr = (c.Length > 6 && c[6].Trim().Length > 0) ? c[6].Trim() : nome;
                arGruppo.Add(gr);
                arCodice.Add(Codicino(gr));
                arFile.Add("q_" + Codicino(nome) + ".txt");
                arZoneGta.Add(new List<string>());
                sx.Add(0f); sy.Add(0f); sz.Add(0f); sn.Add(0);
                arCx.Add(0f); arCy.Add(0f); arCz.Add(0f);
                arAx.Add(0f); arAy.Add(0f); arAcc.Add(false);
                arLiv.Add(1); arAcqua.Add("");
            }
            float x = Decimale(c[2]), y = Decimale(c[3]), z = Decimale(c[4]);
            apX.Add(x); apY.Add(y); apA.Add(a);
            sx[a] = sx[a] + x; sy[a] = sy[a] + y; sz[a] = sz[a] + z; sn[a] = sn[a] + 1;
            string zg = c[5].Trim().ToUpper();
            if (zg.Length > 0 && !arZoneGta[a].Contains(zg)) arZoneGta[a].Add(zg);
        }
        for (i = 0; i < arNome.Count; i++)
        {
            if (sn[i] <= 0) continue;
            arCx[i] = sx[i] / sn[i];
            arCy[i] = sy[i] / sn[i];
            arCz[i] = sz[i] / sn[i];
        }
        // due aree con lo stesso file rovinerebbero il quaderno
        for (i = 0; i < arFile.Count; i++)
        {
            int k, n = 1;
            for (k = 0; k < i; k++)
                if (arFile[k] == arFile[i])
                { n++; arFile[i] = arFile[i].Replace(".txt", n + ".txt"); k = -1; }
        }
    }

    void CaricaLivelliAree()
    {
        string[] r = LeggiRighe("aree_livello.txt");
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 3) continue;
            int a = arNome.IndexOf(c[0].Trim());
            if (a < 0) continue;
            arAcqua[a] = c[1].Trim();
            int lv = Numero(c[2]);
            if (lv < 1) lv = 1;
            arLiv[a] = lv;
            // quarta colonna: la taglia massima che esce qui (1 comune,
            // 2 trofeo, 3 unico); senza, tutto
            int tm = (c.Length > 3) ? Numero(c[3]) : 3;
            if (tm < 1 || tm > 3) tm = 3;
            arTagliaMax[a] = tm;
        }
    }

    Dictionary<int, int> arTagliaMax = new Dictionary<int, int>();

    int TagliaMaxArea(int a)
    {
        int v;
        if (arTagliaMax.TryGetValue(a, out v)) return v;
        return 3;
    }

    int LivelloArea(int lu)
    {
        if (lu < 0 || lu >= arLiv.Count) return 1;
        return arLiv[lu];
    }

    void CaricaAccessi()
    {
        string[] r = LeggiRighe("accessi.txt");
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 3) continue;
            int a = arNome.IndexOf(c[0].Trim());
            if (a < 0) continue;
            arAx[a] = Decimale(c[1]); arAy[a] = Decimale(c[2]); arAcc[a] = true;
        }
    }

    void SalvaAccessi()
    {
        List<string> v = new List<string>();
        v.Add("# DOVE SI ARRIVA in ogni area, segnato andandoci.");
        v.Add("# area|x|y");
        v.Add("# Il segnaposto e il blip puntano qui invece che al centro");
        v.Add("# geometrico, che puo' cadere su uno scoglio o in mezzo all'acqua.");
        int i;
        for (i = 0; i < arNome.Count; i++)
        {
            if (!arAcc[i]) continue;
            v.Add(arNome[i] + "|"
                  + arAx[i].ToString("0.0", CultureInfo.InvariantCulture) + "|"
                  + arAy[i].ToString("0.0", CultureInfo.InvariantCulture));
        }
        try { File.WriteAllLines(Path.Combine(MY_DIR, "accessi.txt"), v.ToArray()); }
        catch { }
    }

    // dove puntare per quest'area: l'accesso se c'e', se no il centro
    float PuntoX(int a)
    {
        if (a < 0 || a >= arNome.Count) return 0f;
        return arAcc[a] ? arAx[a] : arCx[a];
    }

    float PuntoY(int a)
    {
        if (a < 0 || a >= arNome.Count) return 0f;
        return arAcc[a] ? arAy[a] : arCy[a];
    }

    string NomeLuogo(int lu)
    {
        if (lu < 0 || lu >= arNome.Count) return "";
        return arNome[lu];
    }

    string TipoLuogo(int lu)
    {
        if (lu < 0 || lu >= arTipo.Count) return "";
        return arTipo[lu];
    }

    string FileLuogo(int lu)
    {
        if (lu < 0 || lu >= arFile.Count) return "studio_voci.txt";
        return arFile[lu];
    }

    // il codice del GRUPPO: e' quello che si compra con la licenza
    string CodiceLuogo(int lu)
    {
        if (lu < 0 || lu >= arCodice.Count) return "";
        return arCodice[lu];
    }

    // la prima area di quel gruppo
    int IndiceLuogo(string zona)
    {
        if (zona == null || zona.Length == 0) return -1;
        int i;
        for (i = 0; i < arCodice.Count; i++) if (arCodice[i] == zona) return i;
        return -1;
    }

    string NomeGruppo(string zona)
    {
        int i;
        for (i = 0; i < arCodice.Count; i++) if (arCodice[i] == zona) return arGruppo[i];
        return "";
    }

    // ============================================================
    //  I PUNTI CALDI
    //  Dentro un'acqua i pesci non stanno sparsi uguali: stanno dove c'e'
    //  l'erba, la buca, la corrente che gira. Ogni area ha due punti a
    //  specie - li' quel pesce abbocca molto piu' che altrove - e un punto
    //  profondo, dove vengono piu' trofei ed esemplari unici.
    //  Si misurano sull'ESCA, non su di te: lanci da riva e peschi trenta
    //  metri piu' in la'.
    // ============================================================
    List<float> pcX = new List<float>();
    List<float> pcY = new List<float>();
    List<float> pcR = new List<float>();
    List<string> pcSpecie = new List<string>();
    List<float> pcBonus = new List<float>();

    void CaricaPuntiCaldi()
    {
        pcX.Clear(); pcY.Clear(); pcR.Clear(); pcSpecie.Clear(); pcBonus.Clear();
        string[] r = LeggiRighe("punti_caldi.txt");
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 6) continue;
            pcX.Add(Decimale(c[1]));
            pcY.Add(Decimale(c[2]));
            float rr = Decimale(c[3]);
            if (rr < 5f) rr = 30f;
            pcR.Add(rr);
            pcSpecie.Add(c[4].Trim());
            float b = Decimale(c[5]);
            if (b < 1f) b = 1f;
            pcBonus.Add(b);
        }
    }

    // dove sta l'esca adesso: da dove hai lanciato, nella direzione in cui
    // guardavi, per i metri di lenza che sono fuori
    float escaX = 0f, escaY = 0f;
    bool escaInAcqua = false;
    bool escaATerra = false;     // il lancio e' finito sul prato

    // dove guardava quando ha lanciato, e di quanto ha spostato la canna
    float dirBase = 0f;
    float scartoCanna = 0f;
    int ultimoGiroCanna = 0;

    // L'ESCA STA DOVE E' CADUTA.
    // Prima si ricavava dalla direzione in cui guardava il pescatore:
    // bastava girare la canna e l'esca girava con lui, tutta la lenza
    // faceva l'arco. In acqua non succede: il piombo sta dove sta, e
    // muovendo la canna lo trascini appena. Adesso l'esca ha una sua
    // direzione - "escaDir" - che si segna quando lanci e che la canna
    // sposta solo di un pelo.
    float escaDir = 0f;
    // dove guardava quando ha lanciato, e di quanti centimetri la canna
    // ha trascinato l'esca di lato da allora
    float escaBase = 0f;
    float escaScarto = 0f;

    void AggiornaEsca(Ped p, float metri)
    {
        try
        {
            double rad = escaDir * Math.PI / 180.0;
            float fx = -(float)Math.Sin(rad);
            float fy = (float)Math.Cos(rad);
            GTA.Math.Vector3 o = p.Position;
            escaX = o.X + fx * metri;
            escaY = o.Y + fy * metri;
            escaInAcqua = true;
        }
        catch { escaInAcqua = false; }
    }

    // il punto caldo in cui sta l'esca, -1 se in nessuno
    int CaldoDellEsca()
    {
        if (!escaInAcqua) return -1;
        int i;
        for (i = 0; i < pcX.Count; i++)
        {
            float dx = pcX[i] - escaX, dy = pcY[i] - escaY;
            if (dx * dx + dy * dy <= pcR[i] * pcR[i]) return i;
        }
        return -1;
    }

    // I PESCI DI OGNI AREA, da pesci_aree.txt.
    // Non piu' i codici zona di GTA: quelli davano gli stessi settanta
    // pesci a tutto il lago. Adesso ogni tratto ha la sua lista, presa
    // dalle acque vere di Fishing Planet che quel tratto rappresenta.
    List<List<string>> arPesci = new List<List<string>>();

    void CaricaPesciAree()
    {
        arPesci.Clear();
        int i;
        for (i = 0; i < arNome.Count; i++) arPesci.Add(new List<string>());
        string[] r = LeggiRighe("pesci_aree.txt");
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            int b = l.IndexOf('|');
            if (b < 1) continue;
            int a = arNome.IndexOf(l.Substring(0, b).Trim());
            if (a < 0) continue;
            string[] nn = l.Substring(b + 1).Split(';');
            int k;
            for (k = 0; k < nn.Length; k++)
            {
                string x = nn[k].Trim();
                if (x.Length > 0 && !arPesci[a].Contains(x)) arPesci[a].Add(x);
            }
        }
    }

    // ============================================================
    //  A CHE LIVELLO SI PRENDE UN PESCE
    //  Non e' una scelta nostra: viene dal negozio. Per ogni livello si
    //  guarda il pezzo migliore che a quel livello puoi comprare - lenza,
    //  canna, frizione del mulinello, nassa - e si vede da quale livello
    //  in poi reggono tutte e quattro il peso di quel pesce. Prima di
    //  quel livello o ti spezza la lenza o in nassa non ci sta.
    // ============================================================
    static float NumeroPiuAlto(string t)
    {
        if (t == null) return 0f;
        float best = 0f, cur = 0f;
        bool dentro = false;
        int i;
        string n = "";
        for (i = 0; i <= t.Length; i++)
        {
            char c = (i < t.Length) ? t[i] : ' ';
            if ((c >= '0' && c <= '9') || c == '.') { n += c; dentro = true; }
            else
            {
                if (dentro)
                {
                    if (float.TryParse(n, NumberStyles.Float,
                                       CultureInfo.InvariantCulture, out cur))
                        if (cur > best) best = cur;
                    n = ""; dentro = false;
                }
            }
        }
        return best;
    }

    float[] topLenza, topCanna, topFriz, topNassa;

    void CalcolaProgressione()
    {
        topLenza = new float[101]; topCanna = new float[101];
        topFriz = new float[101]; topNassa = new float[101];
        int lv, i;
        for (lv = 1; lv <= 100; lv++)
        {
            float a = 0f, b = 0f, c = 0f, d = 0f;
            for (i = 0; i < lenze.Count; i++)
                if (lenze[i].LivWiki <= lv && lenze[i].Kg > a) a = lenze[i].Kg;
            for (i = 0; i < canne.Count; i++)
                if (canne[i].LivWiki <= lv)
                {
                    float x = NumeroPiuAlto(canne[i].LenzaKg);
                    if (x > b) b = x;
                }
            for (i = 0; i < mulinelli.Count; i++)
                if (mulinelli[i].LivWiki <= lv && mulinelli[i].Frizione > c)
                    c = mulinelli[i].Frizione;
            for (i = 0; i < nasse.Count; i++)
                if (nasse[i].LivWiki <= lv && nasse[i].KgPesce > d) d = nasse[i].KgPesce;
            topLenza[lv] = a; topCanna[lv] = b; topFriz[lv] = c; topNassa[lv] = d;
        }
    }

    int LivelloDelPesce(Specie s)
    {
        if (topLenza == null) CalcolaProgressione();
        float k = s.KgC;
        if (k <= 0f) k = 0.3f;
        int lv;
        for (lv = 1; lv <= 100; lv++)
            if (topLenza[lv] >= k && topCanna[lv] >= k
                && topNassa[lv] >= k && topFriz[lv] >= k * 0.7f) return lv;
        return 100;
    }

    // IL LIVELLO CONSIGLIATO DI UN'AREA.
    // Non e' una regola, e' un consiglio: il livello a cui la nassa che
    // puoi comprare regge il pesce di mezzo di quel posto. Sotto quel
    // livello ci peschi lo stesso, ma meta' di quello che prendi lo devi
    // ributtare perche' non ci sta.
    int LivelloConsigliato(int lu)
    {
        if (lu < 0 || lu >= arPesci.Count) return 1;
        List<float> pp = new List<float>();
        int i;
        for (i = 0; i < pesci.Count; i++)
            if (arPesci[lu].Contains(pesci[i].Nome)) pp.Add(pesci[i].KgC);
        if (pp.Count == 0) return 1;
        pp.Sort();
        float mediano = pp[pp.Count / 2];
        // il primo livello in cui una nassa regge un pesce cosi'
        int best = 100;
        for (i = 0; i < nasse.Count; i++)
        {
            if (nasse[i].KgPesce < mediano) continue;
            if (nasse[i].LivWiki < best) best = nasse[i].LivWiki;
        }
        if (best > 100) best = 100;
        if (best < 1) best = 1;
        return best;
    }

    // quanti pesci di quest'area ti stanno gia' nella nassa che hai
    int PesciAllaTuaPortata(int lu)
    {
        if (lu < 0 || lu >= arPesci.Count) return 0;
        float max = KgPesceMax();
        if (max <= 0f) max = 1f;
        int i, q = 0;
        for (i = 0; i < pesci.Count; i++)
            if (arPesci[lu].Contains(pesci[i].Nome) && pesci[i].KgC <= max) q++;
        return q;
    }

    // il livello piu' basso e piu' alto fra i pesci di un'area
    void LivelliArea(int lu, out int basso, out int alto)
    {
        basso = 100; alto = 1;
        if (lu < 0 || lu >= arPesci.Count) { basso = 1; return; }
        int i;
        for (i = 0; i < pesci.Count; i++)
        {
            if (!arPesci[lu].Contains(pesci[i].Nome)) continue;
            int l = LivelloDelPesce(pesci[i]);
            if (l < basso) basso = l;
            if (l > alto) alto = l;
        }
        if (basso > alto) basso = alto;
    }

    // LA SCHEDA DI UNA ZONA: i suoi pesci raggruppati per livello.
    // I livelli non li decidiamo qui: sono quelli del negozio, cioe' da
    // quando lenza, canna, frizione e nassa reggono quel pesce.
    int zonaQui = -1;   // dove sei, calcolato una volta sola da ScriviZone

    // IL NOME DEL FILE DEL BANNER: "Riva nord-est di Alamo" ->
    // "riva_nord_est_di_alamo.png". Tutto minuscolo, e quello che non e'
    // una lettera o un numero diventa un trattino basso.
    static string SlugArea(string nome)
    {
        string t = SoloAscii(nome).ToLower();
        string o = "";
        bool ultimoTratto = true;
        int i;
        for (i = 0; i < t.Length; i++)
        {
            char c = t[i];
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            { o += c; ultimoTratto = false; }
            else if (!ultimoTratto) { o += '_'; ultimoTratto = true; }
        }
        while (o.Length > 0 && o[o.Length - 1] == '_') o = o.Substring(0, o.Length - 1);
        return o;
    }

    void ScriviUnaZona(int a)
    {
        List<string> v = new List<string>();
        v.Add("titolo_finestra|" + SoloAscii(arNome[a]).ToUpper());
        // il banner del posto, come ce l'hanno i tornei
        string insZ = ImgOk("img\\zone\\" + SlugArea(arNome[a]) + ".png");
        if (insZ.Length > 0) v.Add("insegna|" + insZ);

        int quanti = (a < arPesci.Count) ? arPesci[a].Count : 0;
        bool aperta = (livelloPescatore >= LivelloArea(a));

        v.Add("testo|" + L(arNome[a] + ", " + arTipo[a] + ". " + quanti
                           + " species. Opens at level " + LivelloArea(a) + ".",
                           arNome[a] + ", " + arTipo[a] + ". " + quanti
                           + " specie. Apre al livello " + LivelloArea(a) + "."));
        if (!aperta)
            v.Add("testo|" + L("Closed for you: you are level " + livelloPescatore + ".",
                               "Per te e' chiusa: sei al livello "
                               + livelloPescatore + "."));

        // l'elenco dei pesci e basta: un livello solo per scheda, quello
        // della zona. Mettere anche il livello di attrezzatura di ogni
        // pesce faceva solo confusione.
        string el = "";
        int i;
        for (i = 0; i < pesci.Count; i++)
        {
            if (a >= arPesci.Count || !arPesci[a].Contains(pesci[i].Nome)) continue;
            string nn = NomeIt(pesci[i].Nome);
            el = (el.Length > 0) ? (el + ", " + nn) : nn;
        }
        if (el.Length > 0)
        {
            v.Add("testo|- " + L("What lives here", "Chi ci vive"));
            v.Add("testo|" + el + ".");
        }

        // se ci sei gia' non ha senso mandarti il segnaposto
        if (zonaQui == a)
            v.Add(L("You are here", "Sei qui") + "|niente|||250,175,205");
        else
            v.Add(L("Get to the spot", "Raggiungi il posto")
                  + "|gps_zona " + a + "|||130,225,180");
        ScriviVoci("z_zona_" + a + ".txt", v);
    }

    void ScriviZone()
    {
        List<string> v = new List<string>();
        v.Add("titolo_finestra|" + L("FISHING SPOTS", "ZONE DI PESCA"));
        v.Add("nota|" + L("Press to open the spot.", "Premi per aprire la zona."));

        // in ordine: prima quelle che si esauriscono presto, poi quelle
        // che ti portano avanti fino ai livelli alti
        List<int> ord = new List<int>();
        int i, k;
        for (i = 0; i < arNome.Count; i++) ord.Add(i);
        for (i = 1; i < ord.Count; i++)
        {
            int t = ord[i];
            int l1 = LivelloArea(t);
            k = i - 1;
            while (k >= 0)
            {
                int l2 = LivelloArea(ord[k]);
                if (l2 > l1) { ord[k + 1] = ord[k]; k--; }
                else break;
            }
            ord[k + 1] = t;
        }
        int qui = LuogoQui();
        zonaQui = qui;
        for (i = 0; i < ord.Count; i++)
        {
            int a = ord[i];
            int b1, a1; LivelliArea(a, out b1, out a1);
            int qs = (a < arPesci.Count) ? arPesci[a].Count : 0;
            // il livello va scritto attaccato, "Liv.1-20": se ci metti gli
            // spazi il trainer stacca solo "Liv.1" e il resto resta orfano
            string d = L("Lv.", "Liv.") + LivelloArea(a)
                     + "   " + qs + " " + L("species", "specie")
                     + "   " + arTipo[a];
            if (livelloPescatore < LivelloArea(a))
                d += "   " + L("closed", "chiusa");
            // "sei qui" sta accanto al nome, non nella fascia
            string et = arNome[a];
            if (a == qui) et += "   " + L("you are here", "sei qui");
            // OGNI RIGA PORTA IL BANNER DEL SUO POSTO.
            // Il riquadro grande in cima e' l'immagine della riga scelta:
            // col logo dell'associazione su tutte le righe scorrevi la
            // lista e in cima non cambiava mai niente. Cosi' invece
            // scendendo vedi il banner di ogni acqua.
            string imgZ = BannerArea(a);
            if (imgZ.Length == 0) imgZ = Banner();
            v.Add("sottofile|" + et
                  + "|z_zona_" + a + ".txt||" + imgZ + "|" + d);
            ScriviUnaZona(a);
        }
        ScriviVoci("zone_voci.txt", v);
    }

    bool PesceQui(Specie s, int lu)
    {
        if (lu < 0 || lu >= arPesci.Count) return true;
        if (arPesci[lu].Count == 0) return false;
        return arPesci[lu].Contains(s.Nome);
    }

    // ---------- le lenze lette da lenze.txt (dati veri del wiki) ----------
    class Lenza
    {
        public int Id;
        public string Tipo, Marca, Prodotto, Mm, Img;
        public float Kg;
        public int Metri, Prezzo, LivWiki;
    }
    List<Lenza> lenze = new List<Lenza>();

    static readonly string[] TIPO_COD = new string[] { "mono", "fluoro", "braid", "mare" };
    static readonly string[] TIPO_NOME = new string[] {
        "Monofilo", "Fluorocarbon", "Trecciato", "Lenze da mare" };
    static readonly string[] TIPO_NOTA = new string[] {
        "Forza media, si vede poco, elastica: la lenza di tutti i giorni.",
        "Quasi invisibile in acqua e resistentissima all'abrasione: per i pesci diffidenti.",
        "Fortissima e per niente elastica, ma i pesci la vedono bene.",
        "Le piu' potenti, fatte per il mare e per i pesci grossi." };

    // ---------- canne e mulinelli (dati veri del wiki) ----------
    class Canna
    {
        public int Id;
        public string Tipo, Marca, Modello, Lunghezza, Esca, LenzaKg, Potenza, Img;
        public int LivWiki, Prezzo;
    }
    class Mulinello
    {
        public int Id;
        public string Tipo, Marca, Serie, Misura, Rapporto, Recupero, Capacita, Img;
        public float Frizione;
        public int LivWiki, Prezzo;
    }
    List<Canna> canne = new List<Canna>();
    List<Mulinello> mulinelli = new List<Mulinello>();

    static readonly string[] CANNA_COD = new string[] {
        "spinning", "casting", "fondo", "feeder", "match",
        "carpa", "telescopica", "mare", "spod" };
    static readonly string[] CANNA_NOME = new string[] {
        "Spinning", "Casting", "Da fondo", "Feeder", "Match",
        "Da carpa", "Telescopiche", "Da mare", "Spod" };

    static readonly string[] MUL_COD = new string[] { "spinning", "casting", "mare" };
    static readonly string[] MUL_NOME = new string[] {
        "Da spinning", "Da casting", "Da mare" };

    // ---------- ami, jig head, rig, leader e piombi ----------
    class Terminale
    {
        public int Id;
        public string Cat, Marca, Modello, Misura, Mm, Kg, Pezzi, Img, Grammi;
        public string Forma;                 // il disegno grande dell'amo
        public int Prezzo, LivWiki;
    }
    List<Terminale> terminali = new List<Terminale>();

    static readonly string[] TERM_COD = new string[] { "amo", "jig", "rig", "leader", "piombo" };
    static readonly string[] TERM_NOME = new string[] {
        "Ami", "Testine piombate", "Rig montati", "Leader", "Piombi" };
    static readonly string[] TERM_NOTA = new string[] {
        "Misure da #16 a #1, poi da #1/0 a #18/0.",
        "Amo e piombo in un pezzo solo, per le esche artificiali morbide.",
        "Montature gia' pronte: Carolina, Texas e Three-way.",
        "Il pezzo di filo prima dell'amo. Quello in titanio serve per i pesci coi denti.",
        "Per portare l'esca sul fondo e lanciare piu' lontano." };

    // I prezzi sul wiki sono in crediti di Fishing Planet, gonfiati perche'
    // quel gioco ci vende sopra la moneta a pagamento. Qui li divido per
    // avere cifre da GTA. Cambia solo questo numero per rifare i conti.
    const int CAMBIO = 10;

    static int Dollari(int crediti)
    {
        int v = crediti / CAMBIO;
        if (v < 1 && crediti > 0) v = 1;
        return v;
    }

    // ---------- le esche in vendita ----------
    class EscaShop
    {
        public int Id;
        public string Cat, Nome, Quantita, Peso, Amo, Pesci, Img;
        public int Prezzo, LivWiki;
    }
    List<EscaShop> escheShop = new List<EscaShop>();

    static readonly string[] ESCA_COD = new string[] {
        "comuni", "vermi", "fresche", "boilies", "mare" };
    static readonly string[] ESCA_NOME = new string[] {
        "Comuni", "Vermi e insetti", "Fresche", "Boilies e pellet", "Da mare" };
    static readonly string[] ESCA_NOTA = new string[] {
        "Pane, formaggio, mais: quello che si trova in cucina.",
        "Lombrichi, camole, larve: l'esca che funziona quasi sempre.",
        "Pesciolini, gamberi, pezzi di pesce: per i predatori.",
        "Palline e pellet aromatizzati: la pesca alla carpa.",
        "Sardine, granchi, vermi di mare: per il mare." };

    // ---------- cassette e borse ----------
    class Cassetta
    {
        public int Id;
        public string Nome, Materiale, Attrezzi, Lenze, Mulinelli, Img;
        public int Prezzo, LivWiki;
    }
    List<Cassetta> cassette = new List<Cassetta>();

    // le borse portacanne (Rod Cases): dati veri dal wiki
    class Portacanne
    {
        public int Id;
        public string Nome, Materiale, Img;
        public int Canne, Mulinelli, Lenze, Prezzo, LivWiki;
    }
    List<Portacanne> portacanne = new List<Portacanne>();

    // nasse (keepnet) e fili (stringer)
    class Nassa
    {
        public int Id;
        public string Tipo, Nome, Taglia, Materiale, Img;
        public float KgPesce, KgTotale;
        public int Prezzo, LivWiki;
    }
    List<Nassa> nasse = new List<Nassa>();

    // galleggianti: dati veri dal wiki (Classic Bobbers, Wagglers, Sliders)
    class Galleggiante
    {
        public int Id;
        public string Tipo, Nome, Colore, Misura, Forma, Portata, Materiale, Img;
        public int Prezzo, LivWiki;
    }
    List<Galleggiante> galleggianti = new List<Galleggiante>();

    int livelloPescatore = 1;  // 1..100 come su Fishing Planet

    public Pesca()
    {
        Tick += OnTick;
        PuliziaPesciOrfani();
        LeggiLingua();
        CaricaNomiIt();
        CaricaEscheIt();
        CaricaForme();
        CaricaSuoni();
        CaricaSuonoLancio();
        ScriviSuoni();
        ScriviModelli();
        CaricaAree();
        CaricaLivelliAree();
        CaricaAccessi();
        CaricaPesciAree();
        CaricaPuntiCaldi();
        CaricaEsche();
        CaricaPesci();
        CaricaLenze();
        CaricaCanne();
        CaricaMulinelli();
        CaricaTerminali();
        CaricaEscheShop();
        CaricaCassette();
        CaricaPortacanne();
        CaricaNasse();
        CaricaGalleggianti();
        CaricaRobaccia();
        ScriviProvaGall();
        CaricaArtificiali();
        CaricaTornei();
        CaricaRecordTornei();
        OrdinaPerLivello();
        CaricaAcque();
        ScriviAcque();
        ScriviQuaderno();
        // il negozio NON si scrive qui: qui il livello e' ancora 1 perche'
        // lo stato non e' stato letto. Lo scrive ScaffaliDelNegozio() dopo
        // CaricaStato(), e poi ogni volta che sali di livello.
        // quando gli script si ricaricano la canna vecchia resta appesa
        // alla mano: si toglie all'uscita e si ripulisce all'avvio
        Aborted += OnAborted;
        PulisciCanneRimaste();
        // SI FA SENTIRE ALL'AVVIO.
        // Se la mod non compila non lo dice nessuno: resta solo il
        // trainer e ti chiedi perche' la pesca non risponde. Cosi'
        // invece all'avvio si presenta, e mentre gira scrive l'ora in
        // vivo.txt: quel file e' il battito, e il trainer lo guarda per
        // sapere se la mod c'e' davvero.
        Avviso("~g~Modulo pesca " + VERSIONE + "~s~  pronto.");
        Battito();

        CaricaStato();
        livelloPescatore = LivelloDa(xpTot);
        ViaSfocatura();
        // il campo torna dov'era, se la licenza e' ancora in corso
        if (inPesca && campoMesso) MettiCampo();
        ScaffaliDelNegozio();
        ScriviZone();
        ScriviTesta();
        MettiBlipPunti();
        // se ricarichi gli script con la licenza attiva, l'orologio era
        // rimasto in pausa: lo riprendiamo in mano da dove stava
        if (inPesca)
        {
            try
            {
                Function.Call(Hash.PAUSE_CLOCK, true);
                orologioPreso = true;
                prossimoMinuto = Game.GameTime + MS_PER_MINUTO;
            }
            catch { }
        }
        else
        {
            try { Function.Call(Hash.PAUSE_CLOCK, false); }
            catch { }
        }
        RiscriviTutto();
    }

    // la vetrina: tutte le categorie del negozio sotto una voce sola
    void CaricaEscheShop()
    {
        escheShop.Clear();
        string[] rows = LeggiRighe("esche_negozio.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 10) continue;
            EscaShop x = new EscaShop();
            int v;
            int.TryParse(c[0], out v); x.Id = v;
            x.Cat = c[1].Trim(); x.Nome = c[2].Trim(); x.Quantita = c[3].Trim();
            x.Peso = c[4].Trim();
            int.TryParse(c[5], out v); x.Prezzo = v;
            int.TryParse(c[6], out v); x.LivWiki = v;
            x.Amo = c[7].Trim();
            // sul wiki alcune esche non hanno l'amo consigliato e la cella
            // resta un trattino: non deve finire a schermo
            if (x.Amo == "-" || x.Amo == "/-" || x.Amo == "/") x.Amo = "";
            x.Pesci = c[8].Trim();
            x.Img = ImgOk("img\\esche\\" + c[9].Trim());
            escheShop.Add(x);
        }
        int j, k;
        for (j = 1; j < escheShop.Count; j++)
        {
            EscaShop t = escheShop[j];
            k = j - 1;
            while (k >= 0 && escheShop[k].LivWiki > t.LivWiki) { escheShop[k + 1] = escheShop[k]; k--; }
            escheShop[k + 1] = t;
        }
    }


    // TUTTO IL NEGOZIO IN ORDINE DI LIVELLO.
    // Le esche erano gia' ordinate cosi'; adesso lo sono anche le canne,
    // i mulinelli, le lenze, gli ami, i galleggianti, le nasse, le borse
    // e le artificiali. Cosi' scorrendo vedi la progressione.
    void OrdinaPerLivello()
    {
        int j, k;
        for (j = 1; j < canne.Count; j++)
        { Canna t = canne[j]; k = j - 1;
          while (k >= 0 && canne[k].LivWiki > t.LivWiki) { canne[k+1] = canne[k]; k--; }
          canne[k+1] = t; }
        for (j = 1; j < mulinelli.Count; j++)
        { Mulinello t = mulinelli[j]; k = j - 1;
          while (k >= 0 && mulinelli[k].LivWiki > t.LivWiki) { mulinelli[k+1] = mulinelli[k]; k--; }
          mulinelli[k+1] = t; }
        for (j = 1; j < lenze.Count; j++)
        { Lenza t = lenze[j]; k = j - 1;
          while (k >= 0 && lenze[k].LivWiki > t.LivWiki) { lenze[k+1] = lenze[k]; k--; }
          lenze[k+1] = t; }
        for (j = 1; j < terminali.Count; j++)
        { Terminale t = terminali[j]; k = j - 1;
          while (k >= 0 && terminali[k].LivWiki > t.LivWiki) { terminali[k+1] = terminali[k]; k--; }
          terminali[k+1] = t; }
        for (j = 1; j < galleggianti.Count; j++)
        { Galleggiante t = galleggianti[j]; k = j - 1;
          while (k >= 0 && galleggianti[k].LivWiki > t.LivWiki) { galleggianti[k+1] = galleggianti[k]; k--; }
          galleggianti[k+1] = t; }
        for (j = 1; j < nasse.Count; j++)
        { Nassa t = nasse[j]; k = j - 1;
          while (k >= 0 && nasse[k].LivWiki > t.LivWiki) { nasse[k+1] = nasse[k]; k--; }
          nasse[k+1] = t; }
        for (j = 1; j < cassette.Count; j++)
        { Cassetta t = cassette[j]; k = j - 1;
          while (k >= 0 && cassette[k].LivWiki > t.LivWiki) { cassette[k+1] = cassette[k]; k--; }
          cassette[k+1] = t; }
        for (j = 1; j < portacanne.Count; j++)
        { Portacanne t = portacanne[j]; k = j - 1;
          while (k >= 0 && portacanne[k].LivWiki > t.LivWiki) { portacanne[k+1] = portacanne[k]; k--; }
          portacanne[k+1] = t; }
        for (j = 1; j < artificiali.Count; j++)
        { Artificiale t = artificiali[j]; k = j - 1;
          while (k >= 0 && artificiali[k].LivWiki > t.LivWiki) { artificiali[k+1] = artificiali[k]; k--; }
          artificiali[k+1] = t; }
    }

    void ScriviNegozioEsche()
    {
        List<string> tipi = new List<string>();
        int t, i;
        for (t = 0; t < ESCA_COD.Length; t++)
        {
            List<string> voci = new List<string>();
            for (i = 0; i < escheShop.Count; i++)
            {
                EscaShop x = escheShop[i];
                if (x.Cat != ESCA_COD[t]) continue;
                string et = EscaIt(x.Nome);
                if (x.Quantita.Length > 0) et = Unisci(et, "x" + x.Quantita);
                if (x.Amo.Length > 0) et = Unisci(et, x.Amo);
                string ds = "Liv." + x.LivWiki + "   $" + Dollari(x.Prezzo);
                // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
                // Quello che non puoi ancora comprare resta scritto, spento
                // e non si preme: cosi' sai gia' dove stai andando.
                bool ok = (x.LivWiki <= livelloPescatore);
                if (voci.Count == 0)
                {
                    voci.Add("icone");
                    voci.Add("nota|Premi A per comprare");
                }
                voci.Add(et + "|compra_esca " + x.Id
                         + "|" + x.Img + "|" + ds + LivRosso(ok));
            }
            if (voci.Count == 0) continue;
            string file = "e_" + ESCA_COD[t] + ".txt";
            ScriviVoci(file, voci);
            tipi.Add("sottofile|" + ESCA_NOME[t] + " (" + (voci.Count - 2) + ")|" + file
                     + "||" + Banner() + "|" + ESCA_NOTA[t]);
        }
        ScriviVoci("e_tipi.txt", tipi);
    }

    void CaricaCassette()
    {
        cassette.Clear();
        string[] rows = LeggiRighe("cassette.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 9) continue;
            Cassetta x = new Cassetta();
            int v;
            int.TryParse(c[0], out v); x.Id = v;
            x.Nome = c[1].Trim(); x.Materiale = c[2].Trim();
            x.Attrezzi = c[3].Trim(); x.Lenze = c[4].Trim(); x.Mulinelli = c[5].Trim();
            int.TryParse(c[6], out v); x.Prezzo = v;
            int.TryParse(c[7], out v); x.LivWiki = v;
            x.Img = ImgOk("img\\cassette\\" + c[8].Trim());
            cassette.Add(x);
        }
    }

    void ScriviNegozioCassette()
    {
        List<string> v2 = new List<string>();
        int i;
        for (i = 0; i < cassette.Count; i++)
        {
            Cassetta x = cassette[i];
            string et = x.Nome + "   " + x.Attrezzi + " oggetti";
            if (x.Lenze.Length > 0) et += "   " + x.Lenze + " lenze";
            if (x.Mulinelli.Length > 0) et += "   " + x.Mulinelli + " mulinelli";
            string ds = "Liv." + x.LivWiki + "   $" + Dollari(x.Prezzo);
            // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
            // Quello che non puoi ancora comprare resta scritto, spento
            // e non si preme: cosi' sai gia' dove stai andando.
            bool ok = (x.LivWiki <= livelloPescatore);
            // anche le borse sono immagini larghe: banner sopra, riga pulita
            if (v2.Count == 0) v2.Add("nota|Premi A per comprare");
            v2.Add(et + "|compra_cassetta " + x.Id
                   + "|" + x.Img + "|" + ds + LivRosso(ok));
        }
        ScriviVoci("cassette_voci.txt", v2);
    }

    void CaricaPortacanne()
    {
        portacanne.Clear();
        string[] rows = LeggiRighe("rodcase.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 9) continue;
            Portacanne x = new Portacanne();
            int v;
            int.TryParse(c[0], out v); x.Id = v;
            x.Nome = c[1].Trim(); x.Materiale = c[2].Trim();
            int.TryParse(c[3], out v); x.Canne = v;
            int.TryParse(c[4], out v); x.Mulinelli = v;
            int.TryParse(c[5], out v); x.Lenze = v;
            int.TryParse(c[6], out v); x.Prezzo = v;
            int.TryParse(c[7], out v); x.LivWiki = v;
            x.Img = ImgOk("img\\portacanne\\" + c[8].Trim());
            portacanne.Add(x);
        }
    }

    void ScriviNegozioPortacanne()
    {
        List<string> v2 = new List<string>();
        int i;
        for (i = 0; i < portacanne.Count; i++)
        {
            Portacanne x = portacanne[i];
            string et = x.Nome + "   " + x.Canne + " canne   "
                      + x.Mulinelli + " mulinelli   " + x.Lenze + " lenze";
            string ds = "Liv." + x.LivWiki + "   $" + Dollari(x.Prezzo);
            // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
            // Quello che non puoi ancora comprare resta scritto, spento
            // e non si preme: cosi' sai gia' dove stai andando.
            bool ok = (x.LivWiki <= livelloPescatore);
            // le borse portacanne sono lunghe e basse come le canne:
            // in un'icona non si vedrebbero, quindi banner sopra
            if (v2.Count == 0) v2.Add("nota|Premi A per comprare");
            v2.Add(et + "|compra_portacanne " + x.Id
                   + "|" + x.Img + "|" + ds + LivRosso(ok));
        }
        ScriviVoci("portacanne_voci.txt", v2);
    }

    void CaricaNasse()
    {
        nasse.Clear();
        string[] rows = LeggiRighe("nasse.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 9) continue;
            Nassa x = new Nassa();
            int v; float g;
            int.TryParse(c[0], out v); x.Id = v;
            x.Tipo = c[1].Trim(); x.Nome = c[2].Trim(); x.Taglia = c[3].Trim();
            float.TryParse(c[4], NumberStyles.Float, CultureInfo.InvariantCulture, out g);
            x.KgPesce = g;
            float.TryParse(c[5], NumberStyles.Float, CultureInfo.InvariantCulture, out g);
            x.KgTotale = g;
            x.Materiale = c[6].Trim();
            int.TryParse(c[7], out v); x.Prezzo = v;
            int.TryParse(c[8], out v); x.LivWiki = v;
            // sul wiki c'e' una foto sola per famiglia, non per modello
            // le nasse cambiano faccia con la taglia: le immagini sono
            // divise per fasce di capienza, come le ha preparate lui
            string fimg = "filo.png";
            if (x.Tipo != "filo")
            {
                if (x.KgTotale >= 220f) fimg = "nassa_grande.png";
                else if (x.KgTotale >= 100f) fimg = "nassa_100_200.png";
                else if (x.KgTotale >= 30f) fimg = "nassa_30_90.png";
                else if (x.KgTotale >= 12f) fimg = "nassa_12_25.png";
                else fimg = "nassa_2_7.png";
            }
            x.Img = ImgOk("img\\nasse\\" + fimg);
            nasse.Add(x);
        }
    }

    void ScriviNegozioNasse()
    {
        List<string> tipi = new List<string>();
        string[] cod = new string[] { "nassa", "filo" };
        string[] nome = new string[] { "Nasse", "Fili portapesce" };
        string[] nota = new string[] {
            "Tengono il pesce vivo. Il primo numero e' il pesce piu' grosso che ci sta, il secondo quanti chili in tutto.",
            "Costano meno ma rovinano il pesce: se lo vuoi rilasciare, non va bene." };
        int t, i;
        for (t = 0; t < cod.Length; t++)
        {
            List<string> v2 = new List<string>();
            for (i = 0; i < nasse.Count; i++)
            {
                Nassa x = nasse[i];
                if (x.Tipo != cod[t]) continue;
                string et = x.Nome + " " + x.Taglia + "   pesce max "
                          + x.KgPesce.ToString("0.##", CultureInfo.InvariantCulture) + " kg   in tutto "
                          + x.KgTotale.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
                string ds = "Liv." + x.LivWiki + "   $" + Dollari(x.Prezzo);
                // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
                // Quello che non puoi ancora comprare resta scritto, spento
                // e non si preme: cosi' sai gia' dove stai andando.
                bool ok = (x.LivWiki <= livelloPescatore);
                if (v2.Count == 0) v2.Add("nota|Premi A per comprare");
                v2.Add(et + "|compra_nassa " + x.Id
                       + "|" + x.Img + "|" + ds + LivRosso(ok));
            }
            if (v2.Count == 0) continue;
            string file = "n_" + cod[t] + ".txt";
            ScriviVoci(file, v2);
            tipi.Add("sottofile|" + nome[t] + " (" + (v2.Count - 1) + ")|" + file
                     + "||" + Banner() + "|" + nota[t]);
        }
        ScriviVoci("n_tipi.txt", tipi);
    }

    void CaricaGalleggianti()
    {
        galleggianti.Clear();
        string[] rows = LeggiRighe("galleggianti.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 11) continue;
            Galleggiante x = new Galleggiante();
            int v;
            int.TryParse(c[0], out v); x.Id = v;
            x.Tipo = c[1].Trim(); x.Nome = c[2].Trim(); x.Colore = c[3].Trim();
            x.Misura = c[4].Trim(); x.Forma = c[5].Trim(); x.Portata = c[6].Trim();
            x.Materiale = c[7].Trim();
            int.TryParse(c[8], out v); x.Prezzo = v;
            int.TryParse(c[9], out v); x.LivWiki = v;
            x.Img = ImgOk("img\\galleggianti\\" + c[10].Trim());
            int k = galleggianti.Count;
            while (k > 0 && galleggianti[k - 1].LivWiki > x.LivWiki) k--;
            galleggianti.Insert(k, x);
        }
    }

    static string PortataIt(string p)
    {
        if (p == "Low") return "leggero";
        if (p == "Medium") return "medio";
        if (p == "High") return "pesante";
        return p.ToLower();
    }

    void ScriviNegozioGalleggianti()
    {
        List<string> v2 = new List<string>();
        int i;
        for (i = 0; i < galleggianti.Count; i++)
        {
            Galleggiante x = galleggianti[i];
            string et = Unisci(x.Nome, Corto(x.Misura));
            string ds = "Liv." + x.LivWiki + "   $" + Dollari(x.Prezzo)
                      + "   piombo " + PortataIt(x.Portata);
            // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
            // Quello che non puoi ancora comprare resta scritto, spento
            // e non si preme: cosi' sai gia' dove stai andando.
            bool ok = (x.LivWiki <= livelloPescatore);
            if (v2.Count == 0)
            {
                v2.Add("icone");
                v2.Add("nota|Premi A per comprare");
            }
            v2.Add(et + "|compra_galleggiante " + x.Id
                   + "|" + x.Img + "|" + ds + LivRosso(ok));
        }
        ScriviVoci("galleggianti_voci.txt", v2);
    }

    // quanti articoli ha in tutto una categoria.
    // Prima contava solo quello che potevi comprare adesso, e le voci
    // dicevano "Canne (1)" quando le canne sono quarantuno.
    // il "Liv.N" di una riga del negozio: giallo se ci arrivi, rosso se
    // e' ancora sopra il tuo livello. Solo la scritta: il fondo della riga
    // resta quello di tutte le altre.
    static string LivRosso(bool ok)
    {
        return ok ? "" : "|||235,90,80";
    }

    int Comprabili(string cat)
    {
        int n = 0, i;
        if (cat == "canna") { for (i = 0; i < canne.Count; i++) n++; }
        else if (cat == "mulinello") { for (i = 0; i < mulinelli.Count; i++) n++; }
        else if (cat == "lenza") { for (i = 0; i < lenze.Count; i++) n++; }
        else if (cat == "terminale") { for (i = 0; i < terminali.Count; i++) n++; }
        else if (cat == "galleggiante") { for (i = 0; i < galleggianti.Count; i++) n++; }
        else if (cat == "artificiale") { for (i = 0; i < artificiali.Count; i++) n++; }
        else if (cat == "esca") { for (i = 0; i < escheShop.Count; i++) n++; }
        else if (cat == "cassetta") { for (i = 0; i < cassette.Count; i++) n++; }
        else if (cat == "portacanne") { for (i = 0; i < portacanne.Count; i++) n++; }
        else if (cat == "nassa") { for (i = 0; i < nasse.Count; i++) n++; }
        return n;
    }

    void ScriviNegozio()
    {
        List<string> v = new List<string>();
        // IL NEGOZIO E' SEMPRE LO STESSO.
        // Prima sull'acqua diventava un baracchino con dentro quattro
        // cose buttate in fila, senza categorie e senza ordine: un
        // pasticcio. Adesso e' sempre il negozio grande, con le sue
        // categorie; cambia solo il nome fuori - "Negozio del golf" - e
        // il prezzo, che sul posto e' il triplo.
        // Quello che compri mentre peschi te lo metti addosso subito, e
        // finisce quando finisce il posto in cassetta.
        // IL NEGOZIO E' TUTTO APERTO.
        // Nessun numero fra parentesi e nessuna categoria nascosta: quello
        // che non puoi ancora comprare si vede lo stesso, e premendolo ti
        // dice che livello serve.
        v.Add("sottofile|Canne|c_tipi.txt||" + Banner()
              + "|Il primo pezzo: decide che pesca puoi fare.");
        v.Add("sottofile|Mulinelli|m_tipi.txt||" + Banner()
              + "|La frizione decide quanto pesce riesci a tenere.");
        v.Add("sottofile|Lenze|l_tipi.txt||" + Banner()
              + "|Monofilo, fluorocarbon, trecciato e da mare.");
        v.Add("sottofile|Ami e terminali|t_tipi.txt||" + Banner()
              + "|Ami, testine, rig, leader e piombi.");
        v.Add("sottofile|Esche|e_tipi.txt||" + Banner()
              + "|Comuni, vermi, fresche, boilies e da mare.");
        v.Add("sottofile|Esche artificiali|a_tipi.txt||" + Banner()
              + "|Cucchiaini, rotanti, minnow, siliconici.");
        v.Add("sottofile|Galleggianti|galleggianti_voci.txt||" + Banner()
              + "|Classici, waggler e slider.");
        v.Add("sottofile|Nasse e fili|n_tipi.txt||" + Banner()
              + "|Quanto pesce ti tieni prima di rientrare.");
        v.Add("sottofile|Portacanne|portacanne_voci.txt||" + Banner()
              + "|Per portarti dietro piu' di una canna.");
        v.Add("sottofile|Cassette e borse|cassette_voci.txt||" + Banner()
              + "|Per portarti dietro piu' roba minuta.");
        ScriviVoci("negozio.txt", v);
    }

    // il banner della Los Santos Fishermen's Association
    string Banner()
    {
        return ImgOk("img\\lsfa.png");
    }

    // il logo del marchio, se ce l'abbiamo
    string Logo(string marca)
    {
        string t = "";
        int i;
        for (i = 0; i < marca.Length; i++)
        {
            char c = marca[i];
            if (c == ' ') t += "_";
            else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')
                     || (c >= '0' && c <= '9') || c == '-' || c == '_') t += c;
        }
        return ImgOk("img\\marchi\\" + t + ".png");
    }

    static string Unisci(string a, string b)
    {
        if (a == null || a.Length == 0) return b;
        if (b == null || b.Length == 0) return a;
        return a + "   " + b;
    }

    string ImgOk(string rel)
    {
        try { if (File.Exists(Path.Combine(MY_DIR, rel))) return rel; }
        catch { }
        return "";
    }

    // "8.00 - 20.00" -> "8-20"
    static string Corto(string s)
    {
        string t = s.Replace(" ", "");
        // "8.00 - 20.50" -> "8 - 20.5"
        string[] p = t.Split('-');
        int q;
        for (q = 0; q < p.Length; q++)
        {
            string v = p[q];
            if (v.IndexOf('.') >= 0)
            {
                while (v.EndsWith("0")) v = v.Substring(0, v.Length - 1);
                if (v.EndsWith(".")) v = v.Substring(0, v.Length - 1);
            }
            p[q] = v;
        }
        return string.Join(" - ", p);
    }

    string[] LeggiRighe(string nome)
    {
        try
        {
            string f = Path.Combine(MY_DIR, nome);
            if (File.Exists(f)) return File.ReadAllLines(f);
        }
        catch { }
        return new string[0];
    }

    void CaricaCanne()
    {
        canne.Clear();
        string[] rows = LeggiRighe("canne.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 11) continue;
            Canna x = new Canna();
            int v;
            int.TryParse(c[0], out v); x.Id = v;
            x.Tipo = c[1].Trim(); x.Marca = c[2].Trim(); x.Modello = c[3].Trim();
            x.Lunghezza = c[4].Trim(); x.Esca = c[5].Trim(); x.LenzaKg = c[6].Trim();
            x.Potenza = c[7].Trim();
            int.TryParse(c[8], out v); x.LivWiki = v;
            int.TryParse(c[9], out v); x.Prezzo = v;
            x.Img = ImgOk("img\\attrezzi\\" + c[10].Trim());
            canne.Add(x);
        }
        int j, k;
        for (j = 1; j < canne.Count; j++)
        {
            Canna t = canne[j];
            k = j - 1;
            while (k >= 0 && canne[k].LivWiki > t.LivWiki) { canne[k + 1] = canne[k]; k--; }
            canne[k + 1] = t;
        }
    }

    void CaricaMulinelli()
    {
        mulinelli.Clear();
        string[] rows = LeggiRighe("mulinelli.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 12) continue;
            Mulinello x = new Mulinello();
            int v; float g;
            int.TryParse(c[0], out v); x.Id = v;
            x.Tipo = c[1].Trim(); x.Marca = c[2].Trim(); x.Serie = c[3].Trim();
            x.Misura = c[4].Trim(); x.Rapporto = c[5].Trim(); x.Recupero = c[6].Trim();
            x.Capacita = c[7].Trim();
            float.TryParse(c[8], NumberStyles.Float, CultureInfo.InvariantCulture, out g);
            x.Frizione = g;
            int.TryParse(c[9], out v); x.LivWiki = v;
            int.TryParse(c[10], out v); x.Prezzo = v;
            x.Img = ImgOk("img\\attrezzi\\" + c[11].Trim());
            mulinelli.Add(x);
        }
        int j, k;
        for (j = 1; j < mulinelli.Count; j++)
        {
            Mulinello t = mulinelli[j];
            k = j - 1;
            while (k >= 0 && mulinelli[k].LivWiki > t.LivWiki) { mulinelli[k + 1] = mulinelli[k]; k--; }
            mulinelli[k + 1] = t;
        }
    }

    void CaricaTerminali()
    {
        terminali.Clear();
        string[] rows = LeggiRighe("terminali.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 11) continue;
            Terminale x = new Terminale();
            int v;
            int.TryParse(c[0], out v); x.Id = v;
            x.Cat = c[1].Trim(); x.Marca = c[2].Trim(); x.Modello = c[3].Trim();
            x.Misura = c[4].Trim(); x.Mm = c[5].Trim(); x.Kg = c[6].Trim();
            x.Pezzi = c[7].Trim();
            int.TryParse(c[8], out v); x.Prezzo = v;
            int.TryParse(c[9], out v); x.LivWiki = v;
            x.Img = ImgOk("img\\terminali\\" + c[10].Trim());
            x.Grammi = (c.Length > 11) ? c[11].Trim() : "";
            // il disegno dell'amo va accanto alla scatolina: il trainer
            // legge due immagini separate dal piu'
            x.Forma = "";
            if (c.Length > 12 && c[12].Trim().Length > 0)
            {
                string ff = ImgOk("img\\terminali\\" + c[12].Trim());
                if (ff.Length > 0) x.Forma = ff;
            }
            terminali.Add(x);
        }
        int j, k;
        for (j = 1; j < terminali.Count; j++)
        {
            Terminale t = terminali[j];
            k = j - 1;
            while (k >= 0 && terminali[k].LivWiki > t.LivWiki) { terminali[k + 1] = terminali[k]; k--; }
            terminali[k + 1] = t;
        }
    }

    void ScriviNegozioTerminali()
    {
        List<string> tipi = new List<string>();
        int t, i;
        for (t = 0; t < TERM_COD.Length; t++)
        {
            List<string> modelli = new List<string>();
            for (i = 0; i < terminali.Count; i++)
                if (terminali[i].Cat == TERM_COD[t] && !modelli.Contains(terminali[i].Modello))
                    modelli.Add(terminali[i].Modello);
            if (modelli.Count == 0) continue;

            List<string> vociTipo = new List<string>();
            int m;
            for (m = 0; m < modelli.Count; m++)
            {
                List<string> vociMod = new List<string>();
                string img0 = "", marcaMod = "";
                for (i = 0; i < terminali.Count; i++)
                {
                    Terminale x = terminali[i];
                    if (x.Cat != TERM_COD[t] || x.Modello != modelli[m]) continue;
                    if (img0.Length == 0 && x.Img.Length > 0) img0 = x.Img;
                    if (marcaMod.Length == 0) marcaMod = x.Marca;
                    // l'etichetta si compone solo dei pezzi che esistono davvero:
                    // niente trattini segnaposto, che il trainer li scambia
                    // per intestazioni di sezione
                    string et = "";
                    if (x.Cat == "jig")
                    {
                        et = x.Misura;
                        // per le testine il campo kg contiene la misura dell'amo
                        if (x.Kg.Length > 0) et = Unisci(et, x.Kg);
                    }
                    else if (x.Cat == "leader")
                    {
                        if (x.Misura.Length > 0) et = x.Misura + " m";
                        if (x.Mm.Length > 0) et = Unisci(et, x.Mm + " mm");
                        if (x.Kg.Length > 0) et = Unisci(et, x.Kg + " kg");
                    }
                    else
                    {
                        et = x.Misura;
                        if (x.Mm.Length > 0) et = Unisci(et, x.Mm + " mm");
                    }
                    if (et.Length == 0) et = x.Modello;
                    if (x.Pezzi.Length > 0) et = Unisci(et, "x" + x.Pezzi);
                    string ds = "Liv." + x.LivWiki + "   $" + Dollari(x.Prezzo);
                    // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
                    // Quello che non puoi ancora comprare resta scritto, spento
                    // e non si preme: cosi' sai gia' dove stai andando.
                    bool ok = (x.LivWiki <= livelloPescatore);
                    if (vociMod.Count == 0)
                    {
                        vociMod.Add("icone");
                        vociMod.Add("nota|Premi A per comprare");
                    }
                    vociMod.Add(et + "|compra_terminale " + x.Id
                                + "|" + x.Img
                                + (x.Forma.Length > 0 ? "+" + x.Forma : "")
                                + "|" + ds + LivRosso(ok));
                }
                if (vociMod.Count == 0) continue;   // modello senza niente di comprabile
                string fileMod = "t_" + TERM_COD[t] + "_" + (m + 1) + ".txt";
                ScriviVoci(fileMod, vociMod);
                string logoT = Logo(marcaMod);
                if (logoT.Length == 0) logoT = img0;
                vociTipo.Add("sottofile|" + modelli[m] + "|" + fileMod + "||" + logoT
                             + "|" + (vociMod.Count - 2) + " misure.");
            }
            if (vociTipo.Count == 0) continue;
            string fileTipo = "t_" + TERM_COD[t] + ".txt";
            ScriviVoci(fileTipo, vociTipo);
            tipi.Add("sottofile|" + TERM_NOME[t] + " (" + vociTipo.Count + ")|" + fileTipo
                     + "||" + Banner() + "|" + TERM_NOTA[t]);
        }
        ScriviVoci("t_tipi.txt", tipi);
    }

    void ScriviNegozioCanne()
    {
        List<string> tipi = new List<string>();
        int t, i;
        for (t = 0; t < CANNA_COD.Length; t++)
        {
            List<string> modelli = new List<string>();
            for (i = 0; i < canne.Count; i++)
                if (canne[i].Tipo == CANNA_COD[t] && !modelli.Contains(canne[i].Modello))
                    modelli.Add(canne[i].Modello);
            if (modelli.Count == 0) continue;

            List<string> vociTipo = new List<string>();
            int m;
            for (m = 0; m < modelli.Count; m++)
            {
                List<string> vociMod = new List<string>();
                string img0 = "", lenza0 = "", pot0 = "", marcaMod = "";
                for (i = 0; i < canne.Count; i++)
                {
                    Canna x = canne[i];
                    if (x.Tipo != CANNA_COD[t] || x.Modello != modelli[m]) continue;
                    if (marcaMod.Length == 0) { img0 = x.Img; lenza0 = x.LenzaKg; pot0 = x.Potenza; marcaMod = x.Marca; }
                    string et = x.Lunghezza + " m   lenza " + Corto(x.LenzaKg) + " kg";
                    // il peso di lancio non ce l'hanno tutte: le canne da
                    // galleggiante e le telescopiche sul wiki non lo portano.
                    // Se manca non si scrive "esca g" e basta.
                    string pl = (x.Esca != null && x.Esca.Trim().Length > 0)
                                ? ("esca " + x.Esca + " g   ") : "";
                    string ds = "Liv." + x.LivWiki + "   " + pl
                              + x.Potenza + "   $" + Dollari(x.Prezzo);
                    // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
                    // Quello che non puoi ancora comprare resta scritto, spento
                    // e non si preme: cosi' sai gia' dove stai andando.
                    bool ok = (x.LivWiki <= livelloPescatore);
                    // le canne sono lunghe e basse: in un'icona non si vedrebbero,
                    // quindi qui resta il banner grande sopra la lista
                    if (vociMod.Count == 0) vociMod.Add("nota|Premi A per comprare");
                    vociMod.Add(et + "|compra_canna " + x.Id
                                + "|" + x.Img + "|" + ds + LivRosso(ok));
                }
                if (vociMod.Count == 0) continue;
                string fileMod = "c_" + CANNA_COD[t] + "_" + (m + 1) + ".txt";
                ScriviVoci(fileMod, vociMod);
                string logoC = Logo(marcaMod);
                if (logoC.Length == 0) logoC = img0;
                vociTipo.Add("sottofile|" + modelli[m] + "|" + fileMod + "||" + logoC
                             + "|" + pot0 + ".  Lenza " + Corto(lenza0) + " kg.");
            }
            if (vociTipo.Count == 0) continue;
            string fileTipo = "c_" + CANNA_COD[t] + ".txt";
            ScriviVoci(fileTipo, vociTipo);
            tipi.Add("sottofile|" + CANNA_NOME[t] + " (" + vociTipo.Count + ")|" + fileTipo
                     + "||" + Banner() + "|" + vociTipo.Count + " modelli.");
        }
        ScriviVoci("c_tipi.txt", tipi);
    }

    void ScriviNegozioMulinelli()
    {
        List<string> tipi = new List<string>();
        int t, i;
        for (t = 0; t < MUL_COD.Length; t++)
        {
            List<string> serie = new List<string>();
            for (i = 0; i < mulinelli.Count; i++)
                if (mulinelli[i].Tipo == MUL_COD[t] && !serie.Contains(mulinelli[i].Serie))
                    serie.Add(mulinelli[i].Serie);
            if (serie.Count == 0) continue;

            List<string> vociTipo = new List<string>();
            int m;
            for (m = 0; m < serie.Count; m++)
            {
                List<string> vociMod = new List<string>();
                string img0 = "", marcaMod2 = "";
                float fMin = 0f, fMax = 0f;
                bool primo = true;
                for (i = 0; i < mulinelli.Count; i++)
                {
                    Mulinello x = mulinelli[i];
                    if (x.Tipo != MUL_COD[t] || x.Serie != serie[m]) continue;
                    if (primo) { img0 = x.Img; fMin = x.Frizione; fMax = x.Frizione; marcaMod2 = x.Marca; primo = false; }
                    if (x.Frizione < fMin) fMin = x.Frizione;
                    if (x.Frizione > fMax) fMax = x.Frizione;
                    string et = "mis. " + x.Misura + " - frizione "
                              + x.Frizione.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
                    string ds = "Liv." + x.LivWiki + "   $" + Dollari(x.Prezzo);
                    // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
                    // Quello che non puoi ancora comprare resta scritto, spento
                    // e non si preme: cosi' sai gia' dove stai andando.
                    bool ok = (x.LivWiki <= livelloPescatore);
                    if (vociMod.Count == 0) { vociMod.Add("icone"); vociMod.Add("nota|Premi A per comprare"); }
                    vociMod.Add(et + "|compra_mulinello " + x.Id
                                + "|" + x.Img + "|" + ds + LivRosso(ok));
                }
                if (vociMod.Count == 0) continue;
                string fileMod = "m_" + MUL_COD[t] + "_" + (m + 1) + ".txt";
                ScriviVoci(fileMod, vociMod);
                string logoM = Logo(marcaMod2);
                if (logoM.Length == 0) logoM = img0;
                vociTipo.Add("sottofile|" + serie[m] + "|" + fileMod + "||" + logoM
                             + "|Frizione da " + fMin.ToString("0.0", CultureInfo.InvariantCulture)
                             + " a " + fMax.ToString("0.0", CultureInfo.InvariantCulture)
                             + " kg.  " + (vociMod.Count - 2) + " misure.");
            }
            if (vociTipo.Count == 0) continue;
            string fileTipo = "m_" + MUL_COD[t] + ".txt";
            ScriviVoci(fileTipo, vociTipo);
            tipi.Add("sottofile|" + MUL_NOME[t] + " (" + vociTipo.Count + ")|" + fileTipo
                     + "||" + Banner() + "|" + vociTipo.Count + " serie.");
        }
        ScriviVoci("m_tipi.txt", tipi);
    }

    void CaricaLenze()
    {
        lenze.Clear();
        try
        {
            string f = Path.Combine(MY_DIR, "lenze.txt");
            if (!File.Exists(f)) return;
            string[] rows = File.ReadAllLines(f);
            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string r = rows[i].Trim();
                if (r.Length == 0 || r[0] == '#') continue;
                string[] c = r.Split('|');
                if (c.Length < 10) continue;
                Lenza l = new Lenza();
                int v; float g;
                int.TryParse(c[0], out v); l.Id = v;
                l.Tipo = c[1].Trim();
                l.Marca = c[2].Trim();
                l.Prodotto = c[3].Trim();
                l.Mm = c[4].Trim();
                float.TryParse(c[5], NumberStyles.Float, CultureInfo.InvariantCulture, out g);
                l.Kg = g;
                int.TryParse(c[6], out v); l.Metri = v;
                int.TryParse(c[7], out v); l.Prezzo = v;
                int.TryParse(c[8], out v); l.LivWiki = v;
                l.Img = ImgOk("img\\lenze\\" + c[9].Trim());
                lenze.Add(l);
            }
            int j, k;
            for (j = 1; j < lenze.Count; j++)
            {
                Lenza t = lenze[j];
                k = j - 1;
                while (k >= 0 && lenze[k].LivWiki > t.LivWiki) { lenze[k + 1] = lenze[k]; k--; }
                lenze[k + 1] = t;
            }
        }
        catch { }
    }

    static string SoloNome(string prodotto)
    {
        string t = prodotto;
        t = t.Replace(" fishing line", "");
        t = t.Replace(" Serie", "");
        t = t.Replace(" mono", "");
        t = t.Replace(" braid", "");
        t = t.Replace(" fluorocarbon", "");
        return t.Trim();
    }

    void ScriviNegozioLenze()
    {
        List<string> tipi = new List<string>();
        int t, i;
        for (t = 0; t < TIPO_COD.Length; t++)
        {
            List<string> modelli = new List<string>();
            for (i = 0; i < lenze.Count; i++)
                if (lenze[i].Tipo == TIPO_COD[t] && !modelli.Contains(lenze[i].Prodotto))
                    modelli.Add(lenze[i].Prodotto);

            List<string> vociTipo = new List<string>();
            int m;
            for (m = 0; m < modelli.Count; m++)
            {
                List<string> vociMod = new List<string>();
                float kgMin = 0f, kgMax = 0f;
                bool primo = true;
                string img0 = "", marcaMod = "";
                for (i = 0; i < lenze.Count; i++)
                {
                    Lenza l = lenze[i];
                    if (l.Tipo != TIPO_COD[t] || l.Prodotto != modelli[m]) continue;
                    if (primo) { kgMin = l.Kg; kgMax = l.Kg; img0 = l.Img; marcaMod = l.Marca; primo = false; }
                    if (l.Kg < kgMin) kgMin = l.Kg;
                    if (l.Kg > kgMax) kgMax = l.Kg;
                    string et = l.Mm + " mm - " + l.Kg.ToString("0.0", CultureInfo.InvariantCulture)
                              + " kg - " + l.Metri + " m";
                    string ds = "Liv." + l.LivWiki + "   $" + Dollari(l.Prezzo);
                    // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
                    // Quello che non puoi ancora comprare resta scritto, spento
                    // e non si preme: cosi' sai gia' dove stai andando.
                    bool ok = (l.LivWiki <= livelloPescatore);
                    if (vociMod.Count == 0) { vociMod.Add("icone"); vociMod.Add("nota|Premi A per comprare"); }
                    vociMod.Add(et + "|compra_lenza " + l.Id
                                + "|" + l.Img + "|" + ds + LivRosso(ok));
                }
                string fileMod = "l_" + TIPO_COD[t] + "_" + (m + 1) + ".txt";
                if (vociMod.Count == 0) continue;
                ScriviVoci(fileMod, vociMod);
                string riass = "Carico da " + kgMin.ToString("0.0", CultureInfo.InvariantCulture)
                             + " a " + kgMax.ToString("0.0", CultureInfo.InvariantCulture)
                             + " kg.  " + (vociMod.Count - 2) + " misure.";
                string logo = Logo(marcaMod);
                if (logo.Length == 0) logo = img0;
                vociTipo.Add("sottofile|" + SoloNome(modelli[m]) + "|" + fileMod
                             + "||" + logo + "|" + riass);
            }
            if (vociTipo.Count == 0) continue;
            string fileTipo = "l_" + TIPO_COD[t] + ".txt";
            ScriviVoci(fileTipo, vociTipo);
            tipi.Add("sottofile|" + TIPO_NOME[t] + "|" + fileTipo + "||" + Banner()
                     + "|" + TIPO_NOTA[t]);
        }
        ScriviVoci("l_tipi.txt", tipi);
    }

    void CaricaPesci()
    {
        pesci.Clear();
        try
        {
            string f = Path.Combine(MY_DIR, "pesci.txt");
            if (!File.Exists(f)) return;
            string[] rows = File.ReadAllLines(f);
            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string r = rows[i].Trim();
                if (r.Length == 0 || r[0] == '#') continue;
                string[] c = r.Split('|');
                if (c.Length < 15) continue;
                Specie s = new Specie();
                s.Nome = c[0].Trim();
                s.Img = ImgOk("img\\pesci\\" + c[1].Trim());
                float g;
                float.TryParse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture, out g);
                s.KgC = g;
                float.TryParse(c[3], NumberStyles.Float, CultureInfo.InvariantCulture, out g);
                s.KgT = g;
                float.TryParse(c[4], NumberStyles.Float, CultureInfo.InvariantCulture, out g);
                s.KgU = g;
                int p;
                int.TryParse(c[5], out p); s.PrC = p;
                int.TryParse(c[6], out p); s.PrT = p;
                int.TryParse(c[7], out p); s.PrU = p;
                int.TryParse(c[8], out p); s.Livello = p;
                int.TryParse(c[9], out p); s.Denti = p;
                s.Amo = c[10].Trim();
                s.Famiglia = c[11].Trim();
                s.Esche = Numeri(c[12]);
                s.Art = Numeri(c[13]);
                string[] z = c[14].Split(',');
                int k;
                for (k = 0; k < z.Length; k++) z[k] = z[k].Trim().ToUpper();
                s.Zone = z;
                s.Quando = (c.Length > 15) ? c[15].Trim() : "sempre";
                if (s.Quando.Length == 0) s.Quando = "sempre";
                s.Rarita = 3;
                if (c.Length > 16) { int rr; if (int.TryParse(c[16].Trim(), out rr) && rr >= 1 && rr <= 5) s.Rarita = rr; }
                s.Pred = 0;
                if (c.Length > 17 && c[17].Trim() == "1") s.Pred = 1;
                s.TMin = -1f; s.TMax = -1f; s.TOtt = -1f;
                pesci.Add(s);
            }
        }
        catch { }
        CaricaTemperature();
    }

    // temperature_pesci.txt: nome|min|max|ottimo, gradi dell'acqua.
    // Numeri nostri, indicativi (vedi l'intestazione del file).
    void CaricaTemperature()
    {
        try
        {
            string f = Path.Combine(MY_DIR, "temperature_pesci.txt");
            if (!File.Exists(f)) return;
            string[] rows = File.ReadAllLines(f);
            int i, k;
            for (i = 0; i < rows.Length; i++)
            {
                string r = rows[i].Trim();
                if (r.Length == 0 || r[0] == '#') continue;
                string[] c = r.Split('|');
                if (c.Length < 4) continue;
                float a, b, o;
                if (!float.TryParse(c[1], NumberStyles.Float, CultureInfo.InvariantCulture, out a)) continue;
                if (!float.TryParse(c[2], NumberStyles.Float, CultureInfo.InvariantCulture, out b)) continue;
                if (!float.TryParse(c[3], NumberStyles.Float, CultureInfo.InvariantCulture, out o)) continue;
                string n = c[0].Trim();
                for (k = 0; k < pesci.Count; k++)
                    if (pesci[k].Nome == n) { pesci[k].TMin = a; pesci[k].TMax = b; pesci[k].TOtt = o; }
            }
        }
        catch { }
    }

    // QUANTO VALE LA TEMPERATURA PER QUESTO PESCE.
    // 1 alla sua temperatura ottima, scende fino a temp_bordo ai bordi del
    // suo intervallo, e fuori dall'intervallo cala ancora fino a zero dopo
    // temp_fuori gradi. Pesce senza dati: 1, come prima. temp_pesi=0 spegne.
    float QuantoValeTemperatura(Specie s)
    {
        return QuantoValeTemperaturaA(s, GradiAcqua());
    }

    float QuantoValeTemperaturaA(Specie s, float t)
    {
        if (s.TMin < 0f || LeggiF("temp_pesi", 1f) < 0.5f) return 1f;
        float bordo = LeggiF("temp_bordo", 0.4f);
        float fuori = LeggiF("temp_fuori", 4f);
        if (t >= s.TMin && t <= s.TMax)
        {
            float meta = (t < s.TOtt) ? (s.TOtt - s.TMin) : (s.TMax - s.TOtt);
            if (meta < 0.5f) meta = 0.5f;
            float d = Math.Abs(t - s.TOtt) / meta;
            if (d > 1f) d = 1f;
            return 1f - (1f - bordo) * d;
        }
        float oltre = (t < s.TMin) ? (s.TMin - t) : (t - s.TMax);
        if (oltre >= fuori) return 0f;
        return bordo * (1f - oltre / fuori);
    }

    void CaricaEsche()
    {
        esche.Clear(); escheTipo.Clear();
        try
        {
            string f = Path.Combine(MY_DIR, "esche.txt");
            if (!File.Exists(f)) return;
            string[] rows = File.ReadAllLines(f);
            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string r = rows[i].Trim();
                if (r.Length == 0 || r[0] == '#') continue;
                string[] c = r.Split('|');
                if (c.Length < 3) continue;
                esche.Add(c[1].Trim());
                escheTipo.Add(c[2].Trim());
            }
        }
        catch { }
    }

    static int[] Numeri(string csv)
    {
        string[] p = csv.Split(',');
        List<int> l = new List<int>();
        int q;
        for (q = 0; q < p.Length; q++)
        {
            int v;
            if (int.TryParse(p[q].Trim(), out v)) l.Add(v);
        }
        return l.ToArray();
    }

    // I NOMI DELLE ESCHE DI UN PESCE.
    // Gli id in pesci.txt sono quelli di esche_negozio.txt, e vanno cercati
    // per ID, non per posizione: prima si prendeva la riga numero N di
    // esche.txt, che e' un altro catalogo, e uscivano nomi a caso.
    string NomiEsche(int[] ids, int quante)
    {
        string t = "";
        int q, k, messe = 0;
        for (q = 0; q < ids.Length && messe < quante; q++)
        {
            string nome = "";
            for (k = 0; k < escheShop.Count; k++)
                if (escheShop[k].Id == ids[q]) { nome = EscaIt(escheShop[k].Nome); break; }
            if (nome.Length == 0) continue;
            if (t.Length > 0) t += ", ";
            t += nome;
            messe++;
        }
        return t;
    }

    // lo stesso per le artificiali, che stanno in artificiali.txt
    string NomiArtificiali(int[] ids, int quante)
    {
        string t = "";
        int q, k, messe = 0;
        for (q = 0; q < ids.Length && messe < quante; q++)
        {
            string nome = "";
            for (k = 0; k < artificiali.Count; k++)
                if (artificiali[k].Id == ids[q]) { nome = EscaIt(artificiali[k].Nome); break; }
            if (nome.Length == 0) continue;
            if (t.IndexOf(nome) >= 0) continue;
            if (t.Length > 0) t += ", ";
            t += nome;
            messe++;
        }
        return t;
    }


    string Scheda(Specie s)
    {
        string esca = NomiEsche(s.Esche, 6);
        if (esca.Length == 0) esca = NomiArtificiali(s.Art, 6);
        if (esca.Length == 0) esca = "-";
        // peso: se minimo e massimo coincidono non scrivo due volte lo stesso numero
        string kgMin = s.KgC.ToString("0.##", CultureInfo.InvariantCulture);
        string kgMax = s.KgU.ToString("0.##", CultureInfo.InvariantCulture);
        string peso = (s.KgU > s.KgC) ? (kgMin + "-" + kgMax) : kgMin;
        // prezzo: idem, e se quello dell'esemplare unico manca uso solo il comune
        int pMin = s.PrC, pMax = s.PrU;
        if (pMax <= 0) pMax = s.PrT;
        if (pMax <= 0) pMax = pMin;
        if (pMin <= 0) pMin = pMax;
        string prezzo = (pMax > pMin) ? ("$" + pMin + "-" + pMax) : ("$" + pMin);
        string t = peso + " kg   " + prezzo + "  Esche: " + esca + ".";
        if (s.Amo.Length > 0) t += "  Amo: " + s.Amo + ".";
        // la parola "leader" da sola: il trainer la mette in prima riga,
        // gialla, subito dopo la misura dell'amo
        if (s.Denti > 0) t += "  leader";
        return t;
    }

    // il gioco non sa disegnare i caratteri strani: fuori tutto
    // quello che non e' ASCII, se no vedi i quadratini
    static string SoloAscii(string t)
    {
        if (t == null) return "";
        char[] b = new char[t.Length];
        int n = 0, i;
        for (i = 0; i < t.Length; i++)
        {
            char c = t[i];
            if (c >= ' ' && c <= '~') { b[n] = c; n++; }
        }
        return new string(b, 0, n);
    }

    void ScriviVoci(string nome, List<string> righe)
    {
        try
        {
            // un menu vuoto non dice niente: spieghiamo perche' e' vuoto
            if (righe.Count == 0)
                righe.Add("Ancora niente|niente||");
            string[] a = new string[righe.Count];
            int i;
            for (i = 0; i < righe.Count; i++) a[i] = SoloAscii(righe[i]);
            File.WriteAllLines(Path.Combine(MY_DIR, nome), a);
        }
        catch { }
    }

    void ScriviQuaderno()
    {
        List<string> menu = new List<string>();
        int lu;
        for (lu = 0; lu < arNome.Count; lu++)
        {
            List<string> r = new List<string>();
            int i;
            for (i = 0; i < pesci.Count; i++)
            {
                Specie s = pesci[i];
                if (!PesceQui(s, lu)) continue;
                r.Add((r.Count + 1) + ". " + s.Nome + "|niente|" + s.Img + "|" + Scheda(s));
            }
            string conteggio = r.Count + " specie";
            // la prima riga e' quella del conteggio: senza immagine lasciava
            // il buco del banner, quindi ci mettiamo lo stemma
            // e il banner e' quello del lago, non lo stemma: lo stemma
            // resta solo per i posti che un banner non ce l'hanno
            string bnL = BannerArea(lu);
            if (bnL.Length == 0) bnL = Banner();
            r.Insert(0, "- " + r.Count + " specie -|niente|" + bnL + "|" + conteggio);
            ScriviVoci(FileLuogo(lu), r);
            menu.Add("sottofile|" + NomeLuogo(lu) + " (" + (r.Count - 1) + ")|" + FileLuogo(lu)
                     + "||" + bnL + "|" + conteggio);
        }
        ScriviVoci("studio_voci.txt", menu);
        ScriviDiario();
    }

    // IL DIARIO DI PESCA.
    // Stessa identica riga di "Studia i pesci", con gli stessi colori:
    // il trainer la spezza da solo in peso (bianco), amo (azzurro),
    // prezzo (verde) ed esche (arancio) - basta scriverla nella sua forma:
    //     <peso> kg  $<prezzo>  Esche: <...>.  Amo: <...>.
    // La differenza e' che qui NON sono i dati del catalogo: sono i tuoi.
    // Il peso e' il tuo record e si aggiorna quando lo batti, l'esca e
    // l'amo sono quelli con cui l'hai preso davvero, e in piu' ci sono
    // i punti guadagnati.
    void ScriviDiario()
    {
        List<string> r = new List<string>();
        int i;
        for (i = 0; i < pesci.Count; i++)
        {
            Specie s = pesci[i];
            if (Quanti(quaderno, s.Nome) <= 0) continue;

            float kg = record.ContainsKey(s.Nome) ? record[s.Nome] : 0f;
            // la classe del TUO record, non quella del catalogo
            string clas = ClasseDi(kg, s.KgT, s.KgU);

            string d = kg.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
            int vl = recVale.ContainsKey(s.Nome) ? recVale[s.Nome] : 0;
            int xp = recXp.ContainsKey(s.Nome) ? recXp[s.Nome] : 0;
            if (vl > 0) d += "   $" + vl;
            if (xp > 0) d += "   +" + xp + " XP";

            // le esche vanno a capo da sole, l'amo torna su in azzurro
            string es = recEsca.ContainsKey(s.Nome) ? recEsca[s.Nome] : "";
            if (es.Length == 0) es = "-"; else es = EscheIt(es);
            string sotto = "Esche: " + es + ".";
            string am = recAmo.ContainsKey(s.Nome) ? recAmo[s.Nome] : "";
            if (am.Length > 0) sotto += "  Amo: " + am + ".";

            // testo|comando|img|destra|colore|sotto|colDestra|colSotto
            r.Add((r.Count + 1) + ". " + s.Nome + "|niente|" + s.Img
                  + "|" + clas + "   " + d
                  + "||" + sotto
                  + "|" + ColoreClasse(clas));
        }
        ScriviVoci("diario_voci.txt", r);
    }

    // LA VERSIONE E IL BATTITO.
    // vivo.txt lo riscriviamo ogni due secondi con l'ora del gioco: se
    // quel file e' vecchio, la mod non sta girando.
    const string VERSIONE = "1.0";
    int prossimoBattito = 0;

    void Battito()
    {
        try
        {
            File.WriteAllText(Path.Combine(MY_DIR, "vivo.txt"),
                              VERSIONE + "|" + Game.GameTime);
        }
        catch { }
    }

    void OnTick(object sender, EventArgs e)
    {
        if (Game.GameTime >= prossimoBattito)
        {
            prossimoBattito = Game.GameTime + 2000;
            Battito();
        }
        // IL MENU NUOVO (RB + SINISTRA): con lui aperto il mondo e' fermo
        // e non gira altro
        if (MenuNuovo()) return;
        // la pescata gira a ogni frame: le barre devono essere fluide
        Pescata();
        // la robaccia appena tirata su penzola dalla canna un momento
        if (robaOra >= 0)
        {
            if (robaAppesaFino > 0 && Game.GameTime < robaAppesaFino)
                MuoviRoba(Game.GameTime, true);
            else if (robaAppesaFino > 0)
            {
                ViaRoba();
                robaOra = -1;
                robaAppesaFino = 0;
            }
        }
        DisegnaMessaggio();
        // I PESCI CHE PASSANO NON DIPENDONO DALLA CANNA: finche' hai la
        // licenza e stai in riva, passano. Con la lenza in acqua girano
        // attorno all'esca, se no in un punto d'acqua davanti a te.
        if (inPesca && inRivaOra) PesceDiPassaggio(Game.GameTime);
        else ViaPesceScena();
        // i consigli dei tasti in basso: finche' hai la licenza e sei in
        // riva, in ogni fase (anche senza canna in mano)
        if (inPesca && inRivaOra && !ruotaAperta
            && !Game.Player.Character.IsInVehicle()) Consigli();
        // il posto, l'esplorazione e la licenza
        if (!ruotaAperta) { DisegnaOrario(); DisegnaPosto(); }
        // LB: la ruota degli attrezzi al posto di quella delle armi
        Ruota();
        // mentre guardi l'inventario, l'HUD dell'attrezzatura: la stessa
        // roba che vedi quando peschi, cosi' si controlla com'e' armata
        if (fase == FASE_FERMO && Game.GameTime < hudCasaFino)
            DisegnaAttrezzatura();
        HudRegistrazione();
        MuoviOrologio();

        int ora = Game.GameTime;
        if (ora - ultimoGiro < 400) return;
        ultimoGiro = ora;
        // se cambi lingua nel trainer, i menu della pesca si riscrivono
        if (ora - ultimaLingua > 2000)
        {
            ultimaLingua = ora;
            int prima = lang;
            LeggiLingua();
            if (lang != prima) RiscriviTutto();
        }
        LeggiComandi();
        ControllaOrologio();
        ControllaTorneo();
        GiroRegistrazione();
        // se cambi acqua mentre giri, le licenze proposte cambiano da sole
        if (!inPesca)
        {
            int lu = LuogoQui();
            if (lu != luogoPrec)
            {
                luogoPrec = lu;
                ScriviPesca();
                // anche la voce del menu porta il nome del posto
                ScriviMenu();
            }
        }
    }

    int luogoPrec = -2;

    // ============================================================
    //  IL GIOCO VERO: soldi, roba comprata, licenza, giornata.
    //  Tutto quello che sta qui sotto e' roba NOSTRA, non del wiki.
    //  Lo stato sta in stato.txt, dentro questa cartella.
    // ============================================================

    // I SOLDI SONO QUELLI DI GTA: niente portafoglio separato.
    // Peschi, vendi il pesce e i dollari sono gli stessi con cui ti
    // compri la macchina.
    // Quale dei tre personaggi sei: Michael 0, Franklin 1, Trevor 2.
    // I soldi in storia stanno nello stat SP<n>_TOTAL_CASH.
    static string StatSoldi()
    {
        int n = 0;
        try
        {
            int mdl = Function.Call<int>(Hash.GET_ENTITY_MODEL, Game.Player.Character);
            if (mdl == Function.Call<int>(Hash.GET_HASH_KEY, "player_one")) n = 1;
            else if (mdl == Function.Call<int>(Hash.GET_HASH_KEY, "player_two")) n = 2;
        }
        catch { }
        return "SP" + n + "_TOTAL_CASH";
    }

    static int Soldi()
    {
        // prima il modo normale; se da' zero si legge lo stat, perche' su
        // GTA Enhanced Player.Money non sempre risponde
        try
        {
            int m = Game.Player.Money;
            if (m > 0) return m;
        }
        catch { }
        try
        {
            OutputArgument o = new OutputArgument();
            int h = Function.Call<int>(Hash.GET_HASH_KEY, StatSoldi());
            Function.Call<bool>(Hash.STAT_GET_INT, h, o, -1);
            return o.GetResult<int>();
        }
        catch { }
        return 0;
    }

    static void Paga(int quanto)
    {
        int prima = Soldi();
        int dopo = prima - quanto;
        if (dopo < 0) dopo = 0;
        try { Game.Player.Money = dopo; }
        catch { }
        // e per sicurezza anche sullo stat, che e' quello che il gioco legge
        try
        {
            int h = Function.Call<int>(Hash.GET_HASH_KEY, StatSoldi());
            Function.Call(Hash.STAT_SET_INT, h, dopo, true);
        }
        catch { }
        try
        {
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PURCHASE",
                          "HUD_LIQUOR_STORE_SOUNDSET", true);
        }
        catch { }
    }

    int xpTot = 0;
    string licZona = "";             // zona con la licenza attiva ("" = a casa)
    int licGiorni = 0;               // giornate ancora pagate
    bool inPesca = false;
    int oraPrec = -1;                // per accorgersi del cambio d'ora
    int oreFatte = 0;                // ore di gioco passate dall'alba
    // I MINUTI DI GIOCO FATTI DALL'ALBA.
    // Contare le ore in memoria non bastava: a ogni ricarica degli
    // script il contatore ripartiva da zero, il tempo che resta diceva
    // bugie e la giornata non finiva piu'. Questo si salva in stato.txt.
    int minutiFatti = 0;
    const int MINUTI_GIORNATA = 24 * 60;
    int ultimoGiro = 0;

    // quello che possiedi e quello che ti sei messo in borsa:
    // chiave "categoria:id", valore quante ne hai
    Dictionary<string, int> magazzino = new Dictionary<string, int>();
    Dictionary<string, int> borsa = new Dictionary<string, int>();

    static readonly string[] CAT_COD = new string[] {
        "canna", "mulinello", "lenza", "terminale", "galleggiante",
        "artificiale", "esca", "nassa", "cassetta", "portacanne" };
    static readonly string[] CAT_NOME = new string[] {
        "Canne", "Mulinelli", "Lenze", "Ami e terminali", "Galleggianti",
        "Esche artificiali", "Esche", "Nasse e fili", "Cassette e borse", "Portacanne" };
    static readonly string[] CAT_FILE = new string[] {
        "i_canna.txt", "i_mulinello.txt", "i_lenza.txt", "i_terminale.txt",
        "i_galleggiante.txt", "i_artificiale.txt", "i_esca.txt", "i_nassa.txt",
        "i_cassetta.txt", "i_portacanne.txt" };
    // L'ORDINE DEL NEGOZIO: le categorie di casa si vedono in quest'ordine,
    // che e' quello degli scaffali. Sono gli indici dentro CAT_COD.
    // la voce in fondo alle categorie, mentre peschi
    const string PESCATO = "Pescato del giorno";

    static readonly int[] CASA_ORD = new int[] { 0, 1, 2, 3, 6, 5, 4, 7, 9, 8 };

    // L'ARMATURA, NELLO STESSO ORDINE DELL'HUD.
    // Nell'HUD la colonna si costruisce dal basso in su - mulinello,
    // lenza, terminale, galleggiante - con la canna sopra a destra.
    // Letta dall'alto in basso e' questa. L'esca non c'e': quella si
    // monta sul posto.
    static readonly string[] CAT_FILE_B = new string[] {
        "b_canna.txt", "b_mulinello.txt", "b_lenza.txt", "b_terminale.txt",
        "b_galleggiante.txt", "b_artificiale.txt", "b_esca.txt", "b_nassa.txt",
        "b_cassetta.txt", "b_portacanne.txt" };

    static int Quanti(Dictionary<string, int> d, string k)
    {
        int v;
        if (d.TryGetValue(k, out v)) return v;
        return 0;
    }

    static void Aggiungi(Dictionary<string, int> d, string k, int n)
    {
        int v = Quanti(d, k) + n;
        if (v <= 0) d.Remove(k);
        else d[k] = v;
    }

    // i soldi dei tornei sono gia' in dollari nostri: qui si aggiunge
    // solo il punto delle migliaia, niente cambio dai crediti del wiki
    static string Soldo(int v)
    {
        return v.ToString("#,0", CultureInfo.InvariantCulture);
    }

    static string Kg(float v)
    {
        return v.ToString("0.##", CultureInfo.InvariantCulture);
    }

    static float Decimale(string s)
    {
        float v;
        if (s == null) return 0f;
        if (float.TryParse(s.Trim(), NumberStyles.Float,
                           CultureInfo.InvariantCulture, out v)) return v;
        return 0f;
    }

    static int Numero(string s)
    {
        int v;
        if (s == null) return 0;
        if (int.TryParse(s.Trim(), out v)) return v;
        return 0;
    }

    // ------------------------------------------------------------
    //  Un articolo qualunque del catalogo, cercato per categoria e id
    // ------------------------------------------------------------
    bool Articolo(string cat, int id, out string nome, out string img,
                  out int prezzo, out int liv)
    {
        nome = ""; img = ""; prezzo = 0; liv = 1;
        int i;
        if (cat == "canna")
        {
            for (i = 0; i < canne.Count; i++)
                if (canne[i].Id == id)
                {
                    nome = Unisci(canne[i].Marca + " " + canne[i].Modello, canne[i].Lunghezza);
                    img = canne[i].Img; prezzo = canne[i].Prezzo; liv = canne[i].LivWiki;
                    return true;
                }
        }
        else if (cat == "mulinello")
        {
            for (i = 0; i < mulinelli.Count; i++)
                if (mulinelli[i].Id == id)
                {
                    nome = Unisci(mulinelli[i].Marca + " " + mulinelli[i].Serie, mulinelli[i].Misura);
                    img = mulinelli[i].Img; prezzo = mulinelli[i].Prezzo; liv = mulinelli[i].LivWiki;
                    return true;
                }
        }
        else if (cat == "lenza")
        {
            for (i = 0; i < lenze.Count; i++)
                if (lenze[i].Id == id)
                {
                    nome = lenze[i].Marca + " " + SoloNome(lenze[i].Prodotto);
                    img = lenze[i].Img; prezzo = lenze[i].Prezzo; liv = lenze[i].LivWiki;
                    return true;
                }
        }
        else if (cat == "terminale")
        {
            for (i = 0; i < terminali.Count; i++)
                if (terminali[i].Id == id)
                {
                    nome = Unisci(terminali[i].Marca + " " + terminali[i].Modello, terminali[i].Misura);
                    img = terminali[i].Img; prezzo = terminali[i].Prezzo; liv = terminali[i].LivWiki;
                    return true;
                }
        }
        else if (cat == "galleggiante")
        {
            for (i = 0; i < galleggianti.Count; i++)
                if (galleggianti[i].Id == id)
                {
                    nome = Unisci(galleggianti[i].Nome, galleggianti[i].Colore);
                    img = galleggianti[i].Img; prezzo = galleggianti[i].Prezzo;
                    liv = galleggianti[i].LivWiki;
                    return true;
                }
        }
        else if (cat == "artificiale")
        {
            for (i = 0; i < artificiali.Count; i++)
                if (artificiali[i].Id == id)
                {
                    nome = Unisci(EscaIt(artificiali[i].Nome),
                                  ColoreIt(artificiali[i].Colore));
                    img = artificiali[i].Img; prezzo = artificiali[i].Prezzo;
                    liv = artificiali[i].LivWiki;
                    return true;
                }
        }
        else if (cat == "esca")
        {
            for (i = 0; i < escheShop.Count; i++)
                if (escheShop[i].Id == id)
                {
                    nome = EscaIt(escheShop[i].Nome); img = escheShop[i].Img;
                    prezzo = escheShop[i].Prezzo; liv = escheShop[i].LivWiki;
                    return true;
                }
        }
        else if (cat == "cassetta")
        {
            for (i = 0; i < cassette.Count; i++)
                if (cassette[i].Id == id)
                {
                    nome = cassette[i].Nome; img = cassette[i].Img;
                    prezzo = cassette[i].Prezzo; liv = cassette[i].LivWiki;
                    return true;
                }
        }
        else if (cat == "portacanne")
        {
            for (i = 0; i < portacanne.Count; i++)
                if (portacanne[i].Id == id)
                {
                    nome = portacanne[i].Nome; img = portacanne[i].Img;
                    prezzo = portacanne[i].Prezzo; liv = portacanne[i].LivWiki;
                    return true;
                }
        }
        else if (cat == "nassa")
        {
            for (i = 0; i < nasse.Count; i++)
                if (nasse[i].Id == id)
                {
                    nome = Unisci(nasse[i].Nome, nasse[i].Taglia); img = nasse[i].Img;
                    prezzo = nasse[i].Prezzo; liv = nasse[i].LivWiki;
                    return true;
                }
        }
        return false;
    }

    // ------------------------------------------------------------
    //  LO STATO SU FILE
    // ------------------------------------------------------------
    void CaricaStato()
    {
        magazzino.Clear();
        borsa.Clear();
        string[] rows = LeggiRighe("stato.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 2) continue;
            string k = c[0].Trim();
            if (k == "xp") xpTot = Numero(c[1]);
            else if (k == "licenza")
            {
                licZona = c[1].Trim();
                if (c.Length > 2) licGiorni = Numero(c[2]);
                // vecchi salvataggi: licenza = giornata in corso. Quelli
                // nuovi lo dicono con imp|in_pesca, letto dopo.
                inPesca = (licZona.Length > 0 && licGiorni > 0);
            }
            else if (k == "campo" && c.Length > 4)
            {
                campoX = LeggiNum(c[1]); campoY = LeggiNum(c[2]);
                campoZ = LeggiNum(c[3]); campoDir = LeggiNum(c[4]);
                campoMesso = true;
            }
            else if (k == "presosu" && c.Length > 1)
                presoSu[c[1].Trim()] = 1;
            else if (k == "bobina" && c.Length > 2)
                MettiBobina(Numero(c[1]), Numero(c[2]));
            else if (k == "bobina_casa" && c.Length > 2 && Numero(c[2]) > 0)
                bobineCasa.Add(Numero(c[1]) + "|" + Numero(c[2]));
            else if (k == "imbobinato" && c.Length > 1)
                metriInBobina = Numero(c[1]);
            else if (k == "casa" && c.Length > 2)
            {
                int v = Numero(c[2]);
                if (v > 0) magazzino[c[1].Trim()] = v;
            }
            else if (k == "preso" && c.Length > 2)
            {
                int v = Numero(c[2]);
                if (v > 0) quaderno[c[1].Trim()] = v;
            }
            else if (k == "presoqui" && c.Length > 3)
            {
                int v = Numero(c[3]);
                if (v > 0) presoQui[c[1].Trim() + "|" + c[2].Trim()] = v;
            }
            else if (k == "borsa" && c.Length > 2)
            {
                int v = Numero(c[2]);
                if (v > 0) borsa[c[1].Trim()] = v;
            }
            else if (k == "armato" && c.Length > 2)
            {
                armato[c[1].Trim()] = Numero(c[2]);
            }
            else if (k == "usato" && c.Length > 2)
            {
                int uv = Numero(c[2]);
                if (uv > 0) usati[c[1].Trim()] = uv;
            }
            else if ((k == "pausa_unico" || k == "pausa_trofeo") && c.Length > 2)
            {
                long tp;
                if (long.TryParse(c[2].Trim(), out tp))
                {
                    if (k == "pausa_unico") pausaUnico[c[1].Trim()] = tp;
                    else pausaTrofeo[c[1].Trim()] = tp;
                }
            }
            else if (k == "imp" && c.Length > 2)
            {
                if (c[1].Trim() == "avvisa_zona") avvisaZona = (Numero(c[2]) != 0);
                if (c[1].Trim() == "gall_zoom") gallZoom = Numero(c[2]);
                if (c[1].Trim() == "profondita_cm") profondita = Numero(c[2]) / 100f;
                if (c[1].Trim() == "minuti") minutiFatti = Numero(c[2]);
                if (c[1].Trim() == "soldi_nassa") soldiNassa = Numero(c[2]);
                if (c[1].Trim() == "in_pesca") inPesca = (Numero(c[2]) != 0) && licZona.Length > 0 && licGiorni > 0;
                if (c[1].Trim() == "frizione")
                {
                    frizione = Numero(c[2]);
                    if (frizione < 1) frizione = 1;
                }
            }
            else if (k == "record" && c.Length > 2)
            {
                float rk;
                if (float.TryParse(c[2].Trim(), NumberStyles.Float,
                                   CultureInfo.InvariantCulture, out rk))
                {
                    string nk = c[1].Trim();
                    record[nk] = rk;
                    if (c.Length > 3 && c[3].Trim().Length > 0) dovePreso[nk] = c[3].Trim();
                    if (c.Length > 4 && c[4].Trim().Length > 0) recEsca[nk] = c[4].Trim();
                    if (c.Length > 5 && c[5].Trim().Length > 0) recAmo[nk] = c[5].Trim();
                    if (c.Length > 6) recXp[nk] = Numero(c[6]);
                    if (c.Length > 7) recVale[nk] = Numero(c[7]);
                }
            }
        }
        livelloPescatore = LivelloDa(xpTot);

        // SALVATAGGI VECCHI: se un pezzo risulta montato ma non c'e' il
        // segno che e' stato tolto dalla cassetta, lo si toglie adesso.
        // Prima della regola un amo montato restava anche in cassetta:
        // dieci nella scatola piu' uno sulla canna.
        int qp;
        string[] daSistemare = new string[] { "terminale", "galleggiante" };
        for (qp = 0; qp < daSistemare.Length; qp++)
        {
            string ca = daSistemare[qp];
            if (!armato.ContainsKey(ca)) continue;
            int ia = armato[ca];
            if (ia < 0 || presoSu.ContainsKey(ca)) continue;
            if (QuantiPezzi(ca, ia) > 0) Consuma(ca, ia);
            presoSu[ca] = 1;
        }
    }

    void SalvaStato()
    {
        List<string> v = new List<string>();
        v.Add("# stato della pesca - lo scrive la mod, non toccarlo a mano");
        v.Add("xp|" + xpTot);
        v.Add("licenza|" + licZona + "|" + licGiorni);
        if (campoMesso)
            v.Add("campo|" + campoX.ToString("0.00", CultureInfo.InvariantCulture)
                  + "|" + campoY.ToString("0.00", CultureInfo.InvariantCulture)
                  + "|" + campoZ.ToString("0.00", CultureInfo.InvariantCulture)
                  + "|" + campoDir.ToString("0.0", CultureInfo.InvariantCulture));
        foreach (KeyValuePair<string, int> kv in magazzino)
            v.Add("casa|" + kv.Key + "|" + kv.Value);
        foreach (KeyValuePair<string, int> kv in borsa)
            v.Add("borsa|" + kv.Key + "|" + kv.Value);
        foreach (KeyValuePair<string, int> kv in quaderno)
            v.Add("preso|" + kv.Key + "|" + kv.Value);
        foreach (KeyValuePair<string, int> kv in presoQui)
            v.Add("presoqui|" + kv.Key + "|" + kv.Value);
        v.Add("imp|avvisa_zona|" + (avvisaZona ? "1" : "0"));
        v.Add("imp|gall_zoom|" + gallZoom);
        v.Add("imp|profondita_cm|" + (int)(profondita * 100f + 0.5f));
        v.Add("imp|frizione|" + frizione);
        v.Add("imp|soldi_nassa|" + soldiNassa);
        v.Add("imp|in_pesca|" + (inPesca ? "1" : "0"));
        v.Add("imp|minuti|" + minutiFatti);
        {
            long oraP = AdessoSec();
            foreach (KeyValuePair<string, long> kv in pausaUnico)
                if (kv.Value > oraP) v.Add("pausa_unico|" + kv.Key + "|" + kv.Value);
            foreach (KeyValuePair<string, long> kv in pausaTrofeo)
                if (kv.Value > oraP) v.Add("pausa_trofeo|" + kv.Key + "|" + kv.Value);
        }
        foreach (KeyValuePair<string, int> kv in usati)
            v.Add("usato|" + kv.Key + "|" + kv.Value);
        foreach (KeyValuePair<string, float> kv in record)
        {
            string dv = dovePreso.ContainsKey(kv.Key) ? dovePreso[kv.Key] : "";
            v.Add("record|" + kv.Key + "|"
                  + kv.Value.ToString("0.###", CultureInfo.InvariantCulture)
                  + "|" + dv
                  + "|" + (recEsca.ContainsKey(kv.Key) ? recEsca[kv.Key] : "")
                  + "|" + (recAmo.ContainsKey(kv.Key) ? recAmo[kv.Key] : "")
                  + "|" + (recXp.ContainsKey(kv.Key) ? recXp[kv.Key] : 0)
                  + "|" + (recVale.ContainsKey(kv.Key) ? recVale[kv.Key] : 0));
        }
        foreach (KeyValuePair<string, int> kv in armato)
            v.Add("armato|" + kv.Key + "|" + kv.Value);
        foreach (KeyValuePair<string, int> kv in presoSu)
            v.Add("presosu|" + kv.Key + "|1");
        int qbo;
        for (qbo = 0; qbo < bobine.Count; qbo++)
            v.Add("bobina|" + bobine[qbo]);
        for (qbo = 0; qbo < bobineCasa.Count; qbo++)
            v.Add("bobina_casa|" + bobineCasa[qbo]);
        v.Add("imbobinato|" + metriInBobina);
        try { File.WriteAllLines(Path.Combine(MY_DIR, "stato.txt"), v.ToArray()); }
        catch { }
    }

    // il livello che ti spetta con gli XP che hai
    int LivelloDa(int xp)
    {
        string[] rows = LeggiRighe("livelli.txt");
        int liv = 1, i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 2) continue;
            int l = Numero(c[0]);
            int soglia = Numero(c[1]);
            if (l > 0 && xp >= soglia && l > liv) liv = l;
        }
        return liv;
    }

    // ------------------------------------------------------------
    //  DOVE SEI: la zona di GTA tradotta in una delle nostre acque
    // ------------------------------------------------------------
    string zonaVista = "?";

    // ------------------------------------------------------------
    //  I PUNTI CHE HAI SEGNATO IN GIOCO.
    //  Sono loro a dire dove sei: il punto piu' vicino decide di che
    //  acqua si tratta, senza limiti di distanza. Il mare non si segna
    //  apposta, quello si riconosce dalla zona.
    // ------------------------------------------------------------
    class Punto
    {
        public string Tipo, Zona;
        public float X, Y, Z;
        public Blip B = null;
    }
    List<Punto> punti = new List<Punto>();
    bool puntiLetti = false;

    void CaricaPunti()
    {
        punti.Clear();
        puntiLetti = true;
        try
        {
            string f = Path.Combine(MY_DIR, "zone_marcate.txt");
            if (!File.Exists(f)) return;
            string[] r = File.ReadAllLines(f);
            int i;
            for (i = 0; i < r.Length; i++)
            {
                string s = r[i].Trim();
                if (s.Length == 0 || s[0] == '#') continue;
                string[] c = s.Split('|');
                if (c.Length < 5) continue;
                Punto q = new Punto();
                q.Tipo = c[0].Trim();
                q.Zona = c[1].Trim().ToUpper();
                if (!float.TryParse(c[2].Trim(), NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out q.X)) continue;
                if (!float.TryParse(c[3].Trim(), NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out q.Y)) continue;
                if (!float.TryParse(c[4].Trim(), NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out q.Z)) continue;
                punti.Add(q);
            }
        }
        catch { }
    }

    // a quale delle nostre acque appartiene un codice di zona
    int LuogoDaZona(string z)
    {
        int lu, q;
        for (lu = 0; lu < arZoneGta.Count; lu++)
            for (q = 0; q < arZoneGta[lu].Count; q++)
                if (arZoneGta[lu][q] == z) return lu;
        return -1;
    }

    // il punto segnato piu' vicino a dove sei

    // ------------------------------------------------------------
    //  I TORNEI
    //  Nome, immagine, pesce, tempo, livello, quota e premio vengono
    //  dalle pagine dei singoli tornei sul wiki. La zona e' nostra:
    //  i loro laghi non ci sono, quindi ogni torneo sta dove quel pesce
    //  vive da noi. Tutto in tornei.txt.
    // ------------------------------------------------------------
    class Torneo
    {
        public string Nome, Banner, Pesce, Zona, Punteggio, Attrezzi, Lago;
        public string PunteggioIt, AttrezziIt;
        public int Minuti, LivMin, Quota, Premio;
        // i traguardi: chili di quel pesce da mettere insieme, e i premi.
        // Sono NOSTRI, non del wiki: bilanciati sul peso medio del pesce,
        // sulla durata della gara e sul livello richiesto.
        public float KgBronzo, KgArgento, KgOro;
        public int PrBronzo, PrArgento, PrOro, ExTrofeo, ExUnico;
        // il record tuo: quanti chili hai fatto, che medaglia, e quanti
        // trofei ed esemplari unici hai preso in quella gara
        public float RecKg;
        public int RecMed, RecTrofei, RecUnici, RecFatte;
        // l'ora e il tempo che il torneo impone: sono scelti sull'orario
        // in cui quel pesce mangia davvero e sul carattere dell'acqua
        public int Ora;
        public string Meteo;
    }
    List<Torneo> tornei = new List<Torneo>();

    void CaricaTornei()
    {
        tornei.Clear();
        string[] rows = LeggiRighe("tornei.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 8) continue;
            Torneo t = new Torneo();
            t.Nome = c[0].Trim();
            t.Banner = c[1].Trim();
            t.Pesce = c[2].Trim();
            t.Zona = c[3].Trim();
            t.Minuti = Numero(c[4]);
            t.LivMin = Numero(c[5]);
            t.Quota = Numero(c[6]);
            t.Premio = Numero(c[7]);
            t.Punteggio = (c.Length > 8) ? c[8].Trim() : "";
            t.Attrezzi = (c.Length > 9) ? c[9].Trim() : "";
            t.Lago = (c.Length > 10) ? c[10].Trim() : "";
            t.PunteggioIt = (c.Length > 11) ? c[11].Trim() : "";
            t.AttrezziIt = (c.Length > 12) ? c[12].Trim() : "";
            t.KgBronzo = (c.Length > 13) ? Decimale(c[13]) : 0f;
            t.KgArgento = (c.Length > 14) ? Decimale(c[14]) : 0f;
            t.KgOro = (c.Length > 15) ? Decimale(c[15]) : 0f;
            t.PrBronzo = (c.Length > 16) ? Numero(c[16]) : 0;
            t.PrArgento = (c.Length > 17) ? Numero(c[17]) : 0;
            t.PrOro = (c.Length > 18) ? Numero(c[18]) : 0;
            t.ExTrofeo = (c.Length > 19) ? Numero(c[19]) : 0;
            t.ExUnico = (c.Length > 20) ? Numero(c[20]) : 0;
            t.Ora = (c.Length > 21) ? Numero(c[21]) : -1;
            t.Meteo = (c.Length > 22) ? c[22].Trim() : "";
            if (t.Minuti <= 0) t.Minuti = 45;
            tornei.Add(t);
        }

        // IN ORDINE DI LIVELLO.
        // Cosi' in cima trovi quelli che puoi gia' fare e scendendo
        // vedi dove stai andando, invece di cercare in mezzo a cinquanta.
        int a, b;
        for (a = 1; a < tornei.Count; a++)
        {
            Torneo tt = tornei[a];
            b = a - 1;
            while (b >= 0 && tornei[b].LivMin > tt.LivMin)
            {
                tornei[b + 1] = tornei[b];
                b--;
            }
            tornei[b + 1] = tt;
        }
    }

    // l'indice della nostra zona dal nome scritto in tornei.txt
    int LuogoDalNome(string nome)
    {
        int i;
        for (i = 0; i < arNome.Count; i++) if (arNome[i] == nome) return i;
        for (i = 0; i < arGruppo.Count; i++) if (arGruppo[i] == nome) return i;
        return -1;
    }

    // taglia una frase lunga in due pezzi da mettere su due righe
    static string Spezza(string t, int pezzo)
    {
        if (t == null) t = "";
        int max = 74;
        if (t.Length <= max) return (pezzo == 0) ? t : "";
        int taglio = t.LastIndexOf(' ', max);
        if (taglio < 20) taglio = max;
        if (pezzo == 0) return t.Substring(0, taglio).Trim();
        string resto = t.Substring(taglio).Trim();
        if (resto.Length > max) resto = resto.Substring(0, max - 1) + "...";
        return resto;
    }



    // ============================================================
    //  SVILUPPO - LE ACQUE
    //  Serve a segnare a mano dove si pesca davvero. Le zone di GTA sono
    //  grossolane e non c'entrano niente con l'acqua: "Vinewood Hills" e'
    //  la collina e il laghetto di Franklin insieme, e un fiume attraversa
    //  quattro zone diverse. Quindi l'acqua la definiscono i punti segnati
    //  qui, e il codice zona resta solo come promemoria.
    //  Si scrive in acque.txt:  nome|tipo|x|y|z|zona_gta
    // ============================================================
    class PuntoAcqua
    {
        public string Nome, Tipo, Zona;
        public float X, Y, Z;
    }
    List<PuntoAcqua> acque = new List<PuntoAcqua>();
    string regNome = "";          // registrazione in corso, "" = ferma
    string regTipo = "";
    const float REG_PASSO = 10f;  // un punto ogni dieci metri
    float ultTracX = 0f, ultTracY = 0f;
    bool ultTracValido = false;

    void CaricaAcque()
    {
        acque.Clear();
        string[] r = LeggiRighe("acque.txt");
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 5) continue;
            PuntoAcqua a = new PuntoAcqua();
            a.Nome = c[0].Trim();
            a.Tipo = c[1].Trim();
            a.X = Decimale(c[2]);
            a.Y = Decimale(c[3]);
            a.Z = Decimale(c[4]);
            a.Zona = (c.Length > 5) ? c[5].Trim() : "";
            acque.Add(a);
        }
    }

    void SalvaAcque()
    {
        List<string> v = new List<string>();
        v.Add("# LE ACQUE REGISTRATE ANDANDOCI SOPRA.");
        v.Add("# registrazione|tipo|x|y|z|zona_gta");
        v.Add("# Un punto ogni dieci metri. Il nome zona e' quello che il");
        v.Add("# gioco dava in QUEL punto: serve per dividere poi la");
        v.Add("# registrazione in piu' aree di pesca.");
        int i;
        for (i = 0; i < acque.Count; i++)
        {
            PuntoAcqua a = acque[i];
            v.Add(a.Nome + "|" + a.Tipo + "|"
                  + a.X.ToString("0.0", CultureInfo.InvariantCulture) + "|"
                  + a.Y.ToString("0.0", CultureInfo.InvariantCulture) + "|"
                  + a.Z.ToString("0.0", CultureInfo.InvariantCulture) + "|"
                  + a.Zona);
        }
        try { File.WriteAllLines(Path.Combine(MY_DIR, "acque.txt"), v.ToArray()); }
        catch { }
    }

    static string ZonaDiPunto(float x, float y, float z)
    {
        try { return Function.Call<string>(Hash.GET_NAME_OF_ZONE, x, y, z); }
        catch { return ""; }
    }

    List<string> NomiAcque()
    {
        List<string> n = new List<string>();
        int i;
        for (i = 0; i < acque.Count; i++)
            if (!n.Contains(acque[i].Nome)) n.Add(acque[i].Nome);
        return n;
    }

    int PuntiDi(string nome)
    {
        int i, q = 0;
        for (i = 0; i < acque.Count; i++) if (acque[i].Nome == nome) q++;
        return q;
    }

    // le zone di GTA che una registrazione ha attraversato, in ordine
    List<string> ZoneDi(string nome)
    {
        List<string> z = new List<string>();
        int i;
        for (i = 0; i < acque.Count; i++)
        {
            if (acque[i].Nome != nome) continue;
            string q = acque[i].Zona;
            if (q.Length == 0) q = "?";
            if (!z.Contains(q)) z.Add(q);
        }
        return z;
    }

    void AvviaRegistrazione(string tipo)
    {
        if (regNome.Length > 0) { FermaRegistrazione(); return; }
        // nome automatico: fiume 1, fiume 2, lago 1...
        int n = 1;
        while (PuntiDi(tipo + " " + n) > 0) n++;
        regNome = tipo + " " + n;
        regTipo = tipo;
        ultTracValido = false;
        Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
        Avviso("~g~Registro " + regNome + ".~s~  Percorrila tutta, dall'inizio alla fine.");
        ScriviAcque();
    }

    void FermaRegistrazione()
    {
        if (regNome.Length == 0) return;
        string f = regNome;
        List<string> z = ZoneDi(f);
        regNome = ""; regTipo = "";
        SalvaAcque();
        Avviso("~y~" + f + " chiusa.~s~  " + PuntiDi(f) + " punti, "
               + z.Count + (z.Count == 1 ? " zona." : " zone."));
        ScriviAcque();
    }

    // mentre ti muovi semina i punti da solo
    void GiroRegistrazione()
    {
        if (regNome.Length == 0) return;
        Vector3 p = Game.Player.Character.Position;
        if (ultTracValido)
        {
            float dx = p.X - ultTracX, dy = p.Y - ultTracY;
            if (dx * dx + dy * dy < REG_PASSO * REG_PASSO) return;
        }
        PuntoAcqua a = new PuntoAcqua();
        a.Nome = regNome; a.Tipo = regTipo;
        a.X = p.X; a.Y = p.Y; a.Z = p.Z;
        a.Zona = ZonaDiPunto(p.X, p.Y, p.Z);
        acque.Add(a);
        ultTracX = p.X; ultTracY = p.Y; ultTracValido = true;
        if ((acque.Count % 10) == 0) SalvaAcque();
    }

    // mentre registri, in alto: quanti punti e in che zona sei
    void HudRegistrazione()
    {
        if (regNome.Length == 0) return;
        Vector3 p = Game.Player.Character.Position;
        DisegnaRett(490f, 40f, 300f, 32f, 12, 26, 24, 225);
        DisegnaTesto(PuntiDi(regNome) + " punti     "
                     + ZonaDiPunto(p.X, p.Y, p.Z),
                     640f, 57f, 0.22f, 235, 245, 240);
    }

    // ============================================================
    //  PROVA I SUONI
    //  I nomi dei suoni di GTA non sono documentati: si trovano solo a
    //  tentativi. Qui si sentono uno per uno senza ricaricare, e quello
    //  che ti piace lo metti sul lancio. Resta scritto in
    //  suono_lancio.txt, cosi' non si perde.
    // ============================================================
    List<string> suoNome = new List<string>();
    List<string> suoSet = new List<string>();
    List<string> suoNota = new List<string>();
    string suoUltimoN = "";
    string suoUltimoS = "";

    void CaricaSuoni()
    {
        suoNome.Clear(); suoSet.Clear(); suoNota.Clear();
        string[] r = LeggiRighe("suoni_prova.txt");
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 2) continue;
            suoNome.Add(c[0].Trim());
            suoSet.Add(c[1].Trim());
            suoNota.Add(c.Length > 2 ? c[2].Trim() : "");
        }
    }

    // il suono del lancio scelto: nome e soundset
    string sulN = "";
    string sulS = "";

    void CaricaSuonoLancio()
    {
        sulN = LeggiS("suono_lancio", "");
        sulS = LeggiS("suono_lancio_set", "");
        string[] r = LeggiRighe("suono_lancio.txt");
        int i;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 2) continue;
            sulN = c[0].Trim(); sulS = c[1].Trim();
            break;
        }
    }

    void SalvaSuonoLancio()
    {
        try
        {
            List<string> v = new List<string>();
            v.Add("# il suono del lancio, scelto dal menu: nome|soundset");
            v.Add(sulN + "|" + sulS);
            ScriviVoci("suono_lancio.txt", v);
        }
        catch { }
    }

    void ScriviSuoni()
    {
        if (suoNome.Count == 0) CaricaSuoni();
        List<string> v = new List<string>();
        v.Add("titolo_finestra|PROVA I SUONI");
        v.Add("nota|" + (sulN.Length > 0
              ? ("Sul lancio adesso c'e': " + sulN)
              : "Sul lancio non c'e' ancora niente"));
        v.Add("Rileggi la lista dal file|suono_rileggi||"
              + "Puoi aggiungere righe tue in suoni_prova.txt: nome|soundset"
              + "|130,200,245");
        if (suoUltimoN.Length > 0)
            v.Add("Metti \"" + suoUltimoN + "\" sul lancio|suono_usa||"
                  + "L'ultimo che hai sentito diventa il fruscio del lancio."
                  + "|130,225,180");
        if (sulN.Length > 0)
            v.Add("Togli il suono dal lancio|suono_via||"
                  + "Si torna al lancio muto.|235,90,80");
        v.Add("- DA PROVARE -");
        int i;
        for (i = 0; i < suoNome.Count; i++)
            v.Add(suoNome[i] + "|prova_suono " + i + "||"
                  + suoSet[i] + (suoNota[i].Length > 0 ? "   -   " + suoNota[i] : ""));
        ScriviVoci("suoni_voci.txt", v);
    }

    // ============================================================
    //  LE VARIANTI DEL PESCE
    //  Il modello a_c_fish non e' uno solo: GTA gli da' una forma a caso
    //  fra quelle che ha dentro - piatte, affusolate, tozze. Qui si
    //  guardano una per una, si scrive cosa sono, e poi si lega ogni
    //  specie alla forma giusta.
    // ============================================================
    Ped pesceProva = null;
    int pesceProvaN = -1;
    int pesceQuante = -1;


    void ProvaPesce(int n)
    {
        try
        {
            if (pesceProva == null || !pesceProva.Exists())
            {
                Model m = new Model("a_c_fish");
                m.Request(800);
                if (!m.IsLoaded) { Avviso("~r~Modello non caricato."); return; }
                Ped p = Game.Player.Character;
                pesceProva = World.CreatePed(m, p.Position + p.ForwardVector * 2f);
                m.MarkAsNoLongerNeeded();
                if (pesceProva == null || !pesceProva.Exists())
                { pesceProva = null; return; }
                Function.Call(Hash.SET_ENTITY_INVINCIBLE, pesceProva, true);
                Function.Call(Hash.SET_ENTITY_COLLISION, pesceProva, false, false);
                Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, pesceProva, true);
                Function.Call(Hash.SET_PED_CAN_RAGDOLL, pesceProva, false);
                // TENERLO FERMO SI FA COSI'.
                // Togliere la gravita' e renderlo non dinamico non basta e
                // fa danni: il gioco lo lascia sprofondare sotto il mondo.
                // Il modo giusto e' congelarlo - resta dove lo metti - e
                // spostarlo noi.
                Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, pesceProva, false);
                Function.Call(Hash.FREEZE_ENTITY_POSITION, pesceProva, true);
                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, pesceProva, true, true);
            }
            if (pesceQuante < 0)
                pesceQuante = Function.Call<int>(
                    Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, pesceProva, 0);
            if (pesceQuante < 1) pesceQuante = 1;
            if (n < 0) n = pesceQuante - 1;
            if (n >= pesceQuante) n = 0;
            pesceProvaN = n;
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, pesceProva, 0, n, 0, 0);
            Avviso("~b~Modello " + n + "~s~ di " + pesceQuante);
            ScriviModelli();
        }
        catch { }
    }

    void ViaProvaPesce()
    {
        try
        {
            if (pesceProva != null && pesceProva.Exists())
            {
                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, pesceProva, true, true);
                pesceProva.Delete();
            }
        }
        catch { }
        pesceProva = null;
        pesceProvaN = -1;
    }

    // sta davanti a te, all'altezza degli occhi, e gira piano
    void TieniProvaPesce(int now)
    {
        if (pesceProva == null || !pesceProva.Exists()) return;
        try
        {
            Ped p = Game.Player.Character;
            if (p == null || !p.Exists()) return;
            // se per qualche motivo e' scappato via, si butta invece di
            // lasciarlo cadere per il mondo
            GTA.Math.Vector3 dd = pesceProva.Position - p.Position;
            if (dd.Length() > 25f) { ViaProvaPesce(); return; }
            GTA.Math.Vector3 q = p.Position + p.ForwardVector * 1.6f;
            float gr = (float)((now / 20) % 360);
            Function.Call(Hash.SET_ENTITY_ROTATION, pesceProva, 0f, 0f, gr, 2, true);
            Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, pesceProva,
                          q.X, q.Y, q.Z + 0.7f, false, false, false);
        }
        catch { }
    }

    // PROVA I GALLEGGIANTI.
    // Sono venticinque e quasi tutti chiedono un livello che non hai
    // ancora: per guardarli non ha senso comprarli. Qui si monta quello
    // che vuoi, gratis e senza controlli, giusto per vederlo in acqua.
    // Quello che monti da qui finisce nello zaino come gli altri: se non
    // lo vuoi tenere, premi di nuovo la sua riga e sparisce.
    void ScriviProvaGall()
    {
        List<string> v = new List<string>();
        v.Add("icone");
        v.Add("titolo_finestra|PROVA I GALLEGGIANTI");
        v.Add("nota|Premi uno e te lo monta, gratis. Premi di nuovo e lo toglie.");
        int id = InUso("galleggiante");
        int i;
        for (i = 0; i < galleggianti.Count; i++)
        {
            Galleggiante x = galleggianti[i];
            bool ora = (x.Id == id);
            v.Add(Unisci(x.Nome, x.Colore)
                  + "|prova_gall " + x.Id + "|" + x.Img + "|"
                  + Corto(x.Misura) + (ora ? "   montato" : ""));
        }
        ScriviVoci("provagall_voci.txt", v);
    }

    void ScriviModelli()
    {
        List<string> v = new List<string>();
        v.Add("titolo_finestra|MODELLI DEL PESCE");
        v.Add("nota|" + (pesceQuante > 0
              ? ("Il modello ha " + pesceQuante + " forme"
                 + (pesceProvaN >= 0 ? "   -   ora vedi la " + pesceProvaN : ""))
              : "Premi una forma per vederla"));
        v.Add("Vai avanti|mod_pesce_piu||La forma dopo.|130,225,180");
        v.Add("Vai indietro|mod_pesce_meno||La forma prima.|130,200,245");
        v.Add("Togli il pesce|mod_pesce_via||Lo fa sparire.|235,90,80");
        int q = (pesceQuante > 0) ? pesceQuante : 12;
        int i;
        v.Add("- LE FORME -");
        for (i = 0; i < q; i++)
            v.Add("Forma " + i + "|mod_pesce " + i + "||"
                  + (i == pesceProvaN ? "e' questa che vedi" : ""));
        ScriviVoci("modelli_voci.txt", v);
    }

    void ScriviAcque()
    {
        List<string> v = new List<string>();
        v.Add("titolo_finestra|REGISTRA LE ACQUE");
        v.Add("nota|" + (regNome.Length > 0
              ? ("Sto registrando " + regNome + " - " + PuntiDi(regNome) + " punti")
              : (acque.Count + " punti registrati")));

        if (regNome.Length > 0)
        {
            v.Add("Ferma la registrazione|reg_stop||"
                  + "Un punto ogni dieci metri mentre ti muovi.|235,90,80");
        }
        else
        {
            v.Add("Registra un fiume|reg_fiume||"
                  + "Parti dall'inizio e percorrilo tutto.|130,225,180");
            v.Add("Registra un lago|reg_lago||"
                  + "Girane tutta la riva.|130,225,180");
        }

        // DOVE SI ARRIVA. Il centro di un'area puo' cadere su uno scoglio
        // o in mezzo all'acqua: qui si segna il posto vero da cui si scende.
        int qui = LuogoQui();
        if (qui >= 0)
            v.Add("Segna il punto di partenza|acq_accesso||"
                  + arNome[qui] + " - "
                  + (arAcc[qui] ? "gia' segnato, premi per rifarlo qui dove sei."
                                : "il segnaposto e il blip verranno qui.")
                  + "|130,200,245");

        List<string> nn = NomiAcque();
        int i;
        if (nn.Count > 0)
        {
            v.Add("- REGISTRAZIONI -");
            for (i = 0; i < nn.Count; i++)
            {
                List<string> z = ZoneDi(nn[i]);
                string el = "";
                int q;
                for (q = 0; q < z.Count && q < 6; q++)
                    el = (el.Length > 0) ? (el + ", " + z[q]) : z[q];
                v.Add(nn[i] + "|reg_butta " + i + "||"
                      + PuntiDi(nn[i]) + " punti   "
                      + z.Count + (z.Count == 1 ? " zona: " : " zone: ") + el
                      + "   (premi per buttarla)");
            }
        }
        ScriviVoci("acque_voci.txt", v);
    }

    // ============================================================
    //  I TORNEI: iscrizione, cronometro, traguardi, premi, record
    // ============================================================
    int torneoOra = -1;        // quale torneo stai facendo, -1 nessuno
    int torneoFine = 0;        // Game.GameTime in cui scade
    float torneoKg = 0f;       // chili del pesce bersaglio messi insieme
    int torneoPezzi = 0, torneoTrofei = 0, torneoUnici = 0;

    // che medaglia valgono questi chili: 0 niente, 1 bronzo, 2 argento, 3 oro
    string CieloIt(string m)
    {
        if (m == null) m = "";
        m = m.ToUpper();
        if (m == "EXTRASUNNY") return L("bright sun", "sole pieno");
        if (m == "CLEAR") return L("clear", "sereno");
        if (m == "CLOUDS") return L("cloudy", "nuvoloso");
        if (m == "OVERCAST") return L("grey", "coperto");
        if (m == "RAIN") return L("rain", "pioggia");
        if (m == "FOGGY") return L("fog", "nebbia");
        return m.ToLower();
    }

    static int Medaglia(Torneo t, float kg)
    {
        if (t.KgOro > 0f && kg >= t.KgOro) return 3;
        if (t.KgArgento > 0f && kg >= t.KgArgento) return 2;
        if (t.KgBronzo > 0f && kg >= t.KgBronzo) return 1;
        return 0;
    }

    string NomeMedaglia(int m)
    {
        if (m == 3) return L("gold", "oro");
        if (m == 2) return L("silver", "argento");
        if (m == 1) return L("bronze", "bronzo");
        return L("nothing", "niente");
    }

    static int PremioMedaglia(Torneo t, int m)
    {
        if (m == 3) return t.PrOro;
        if (m == 2) return t.PrArgento;
        if (m == 1) return t.PrBronzo;
        return 0;
    }

    // i record stanno in un file loro, con il NOME del torneo come chiave:
    // cosi' se un giorno cambia l'ordine della lista non si perde niente
    void CaricaRecordTornei()
    {
        string[] r = LeggiRighe("tornei_record.txt");
        int i, k;
        for (i = 0; i < r.Length; i++)
        {
            string l = r[i].Trim();
            if (l.Length == 0 || l[0] == '#') continue;
            string[] c = l.Split('|');
            if (c.Length < 6) continue;
            for (k = 0; k < tornei.Count; k++)
            {
                if (tornei[k].Nome != c[0].Trim()) continue;
                tornei[k].RecKg = Decimale(c[1]);
                tornei[k].RecMed = Numero(c[2]);
                tornei[k].RecTrofei = Numero(c[3]);
                tornei[k].RecUnici = Numero(c[4]);
                tornei[k].RecFatte = Numero(c[5]);
                break;
            }
        }
    }

    void SalvaRecordTornei()
    {
        List<string> v = new List<string>();
        v.Add("# nome|kg|medaglia|trofei|unici|volte");
        int i;
        for (i = 0; i < tornei.Count; i++)
        {
            Torneo t = tornei[i];
            if (t.RecFatte <= 0) continue;
            v.Add(t.Nome + "|"
                  + t.RecKg.ToString("0.##", CultureInfo.InvariantCulture) + "|"
                  + t.RecMed + "|" + t.RecTrofei + "|" + t.RecUnici + "|" + t.RecFatte);
        }
        try { File.WriteAllLines(Path.Combine(MY_DIR, "tornei_record.txt"), v.ToArray()); }
        catch { }
    }

    // il pesce appena messo nella nassa conta per il torneo?
    // Conta solo il pesce bersaglio; se il torneo non ne ha uno, conta tutto.
    void PesceDelTorneo(string nome, float kg, string taglia)
    {
        if (torneoOra < 0 || torneoOra >= tornei.Count) return;
        Torneo t = tornei[torneoOra];
        string b = t.Pesce.Trim();
        if (b.Length > 0 && b[0] != '(' && b != nome) return;
        torneoKg += kg;
        torneoPezzi++;
        if (taglia == "ESEMPLARE UNICO") torneoUnici++;
        else if (taglia == "TROFEO") torneoTrofei++;
    }

    void ApriTorneo(int i)
    {
        if (i < 0 || i >= tornei.Count) return;
        Torneo t = tornei[i];
        if (torneoOra >= 0)
        { Avviso("~y~" + L("A competition is already running.",
                           "Hai gia' un torneo in corso.")); return; }
        if (livelloPescatore < t.LivMin)
        { Avviso("~r~" + L("Level " + t.LivMin + " needed.",
                           "Ci vuole il livello " + t.LivMin + ".")); return; }
        if (!inPesca)
        { Avviso("~y~" + L("Pay the day licence first.",
                           "Prima paga la giornata.")); return; }
        int luZ = LuogoDalNome(t.Zona);
        int luO = LuogoQui();
        if (luZ >= 0 && luO != luZ)
        { Avviso("~r~" + L("The competition is at " + t.Zona + ".",
                           "Il torneo e' a " + t.Zona + ".")); return; }
        if (Soldi() < t.Quota)
        { Avviso("~r~" + L("Entry is $" + Soldo(t.Quota) + ".",
                           "L'iscrizione costa $" + Soldo(t.Quota) + ".")); return; }

        Paga(t.Quota);

        // L'ORA E IL TEMPO DEL TORNEO.
        // Ogni gara ha la sua ora di partenza e il suo cielo, scelti
        // sull'orario in cui quel pesce mangia davvero: la gara notturna
        // ai pesci gatto comincia alle due, quella al luccio all'alba.
        if (t.Ora >= 0 && t.Ora <= 23)
        {
            try { Function.Call(Hash.SET_CLOCK_TIME, t.Ora, 0, 0); }
            catch { }
        }
        if (t.Meteo != null && t.Meteo.Length > 0)
        {
            try
            {
                Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, t.Meteo);
                Function.Call(Hash.SET_WEATHER_TYPE_PERSIST, t.Meteo);
            }
            catch { }
        }

        torneoOra = i;
        torneoFine = Game.GameTime + t.Minuti * 60000;
        torneoKg = 0f; torneoPezzi = 0; torneoTrofei = 0; torneoUnici = 0;
        Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
        Avviso("~g~" + t.Nome + "~s~  " + L("started", "al via") + ".  "
               + t.Minuti + " " + L("minutes", "minuti") + ".");
        Diario("torneo iniziato: " + t.Nome + " - quota " + t.Quota);
        RiscriviTutto();
    }

    // fine del torneo: ritirato = te ne sei andato tu, niente premi
    void ChiudiTorneo(bool ritirato)
    {
        if (torneoOra < 0 || torneoOra >= tornei.Count) { torneoOra = -1; return; }
        Torneo t = tornei[torneoOra];

        if (ritirato)
        {
            Avviso("~y~" + L("Withdrawn from ", "Ritirato da ") + t.Nome + ".");
            Diario("torneo abbandonato: " + t.Nome);
            torneoOra = -1;
            RiscriviTutto();
            return;
        }

        int med = Medaglia(t, torneoKg);
        int premio = PremioMedaglia(t, med);
        int extra = 0;
        if (torneoTrofei > 0) extra += t.ExTrofeo;
        if (torneoUnici > 0) extra += t.ExUnico;
        // gli extra si prendono solo se hai almeno il bronzo: se non hai
        // fatto il minimo non hai fatto la gara
        if (med == 0) extra = 0;
        int tot = premio + extra;
        if (tot > 0) Paga(-tot);

        // il record: vale il peso, e a parita' di peso la medaglia
        if (torneoKg > t.RecKg)
        {
            t.RecKg = torneoKg;
            t.RecMed = med;
            t.RecTrofei = torneoTrofei;
            t.RecUnici = torneoUnici;
        }
        t.RecFatte++;
        SalvaRecordTornei();

        string kgs = torneoKg.ToString("0.##", CultureInfo.InvariantCulture);
        if (med > 0)
            Avviso("~g~" + NomeMedaglia(med).ToUpper() + "~s~  " + kgs + " kg   $"
                   + Soldo(tot));
        else
            Avviso("~y~" + kgs + " kg.  " + L("Not even bronze.",
                                              "Nemmeno il bronzo."));
        Diario("torneo finito: " + t.Nome + " - " + kgs + " kg, medaglia "
               + med + ", premio " + tot);

        torneoOra = -1;
        RiscriviTutto();
    }

    // il cronometro, chiamato a ogni giro
    void ControllaTorneo()
    {
        if (torneoOra < 0) return;
        if (Game.GameTime >= torneoFine) ChiudiTorneo(false);
    }

    string TempoTorneo()
    {
        int ms = torneoFine - Game.GameTime;
        if (ms < 0) ms = 0;
        int sec = ms / 1000;
        int mi = sec / 60;
        int se = sec % 60;
        return mi + ":" + (se < 10 ? "0" : "") + se;
    }

    // LA SCHEDA DI UN TORNEO.
    // Insegna in cima, sotto un blocco di testo con tutto quello che c'e'
    // da sapere, e in fondo una sola cosa da fare: andarci.
    // IL FILE DELLA SCHEDA CAMBIA A SECONDA DA DOVE SI ENTRA.
    // Il trainer si ricorda da dove viene guardando il file: se la stessa
    // scheda la aprono due menu diversi, tornando indietro finisci
    // nell'altro. Quindi la pagina dei tornei usa t_gara_N.txt e quella
    // di "inizia a pescare" usa p_gara_N.txt.
    void ScriviUnTorneo(int i) { ScriviUnTorneo(i, "t_gara_"); }

    void ScriviUnTorneo(int i, string pre)
    {
        Torneo t = tornei[i];
        List<string> v = new List<string>();
        string ins = (t.Banner.Length > 0) ? ImgOk("img\\tornei\\" + t.Banner) : "";
        if (ins.Length > 0) v.Add("insegna|" + ins);

        string pesce = (t.Pesce.Length > 0) ? NomeIt(t.Pesce)
                       : L("everything that swims here", "tutto quello che c'e'");
        v.Add("testo|" + L("You fish for " + pesce + " at " + t.Zona + ", for "
                           + t.Minuti + " real minutes.",
                           "Si pesca " + pesce + " a " + t.Zona + ", per "
                           + t.Minuti + " minuti veri."));
        if (t.Ora >= 0)
            v.Add("testo|" + L("It starts at " + t.Ora + ":00, sky " + CieloIt(t.Meteo) + ".",
                               "Si parte alle " + t.Ora + ":00, cielo "
                               + CieloIt(t.Meteo) + "."));
        v.Add("testo|" + L("Entry is $" + Soldo(t.Quota)
                           + " and the winner takes $" + Soldo(t.Premio) + ".",
                           "Si entra con $" + Soldo(t.Quota)
                           + " e chi vince porta a casa $" + Soldo(t.Premio) + "."));

        string pun = L(t.Punteggio, (t.PunteggioIt.Length > 0) ? t.PunteggioIt : t.Punteggio);
        if (pun.Length > 0)
        {
            v.Add("testo|- " + L("How you win", "Come si vince"));
            v.Add("testo|" + pun.Replace("|", " "));
        }
        string att = L(t.Attrezzi, (t.AttrezziIt.Length > 0) ? t.AttrezziIt : t.Attrezzi);
        if (att.Length > 0)
        {
            v.Add("testo|- " + L("Required tackle", "Attrezzatura obbligatoria"));
            v.Add("testo|" + att.Replace("|", " "));
        }

        // I TRAGUARDI: chili di quel pesce da mettere insieme, e cosa pagano
        v.Add("testo|- " + L("Targets", "Traguardi"));
        v.Add("testo|" + L("Bronze", "Bronzo") + " " + Kg(t.KgBronzo)
              + " kg = $" + Soldo(t.PrBronzo) + ".    "
              + L("Silver", "Argento") + " " + Kg(t.KgArgento)
              + " kg = $" + Soldo(t.PrArgento) + ".    "
              + L("Gold", "Oro") + " " + Kg(t.KgOro)
              + " kg = $" + Soldo(t.PrOro) + ".");
        v.Add("testo|" + L("On top of the medal: one trophy fish pays $",
                           "Sopra la medaglia: un trofeo paga $")
              + Soldo(t.ExTrofeo) + ", "
              + L("one unique specimen pays $", "un esemplare unico paga $")
              + Soldo(t.ExUnico) + ".");

        // IL TUO RECORD
        v.Add("testo|- " + L("Your record", "Il tuo record"));
        if (t.RecFatte <= 0)
            v.Add("testo|" + L("Never fished.", "Mai fatto."));
        else
            v.Add("testo|" + Kg(t.RecKg) + " kg, "
                  + NomeMedaglia(t.RecMed) + ".    "
                  + t.RecTrofei + " " + L("trophies", "trofei") + ", "
                  + t.RecUnici + " " + L("uniques", "unici") + ".    "
                  + L("Fished ", "Fatto ") + t.RecFatte + " "
                  + L("times", "volte") + ".");

        // COSA PUOI FARE
        if (torneoOra == i)
        {
            v.Add(L("Withdraw", "Ritirati") + "|mollo_torneo|||235,90,80");
        }
        else if (torneoOra >= 0)
        {
            v.Add(L("Another competition is running", "Hai un altro torneo in corso")
                  + "|niente|||235,90,80");
        }
        else if (livelloPescatore < t.LivMin)
        {
            // il livello non ce l'hai, ma il posto lo puoi comunque
            // andare a vedere: e' meta' del gusto
            v.Add(L("Needs level " + t.LivMin + ": you are " + livelloPescatore,
                    "Ci vuole il livello " + t.LivMin + ": sei al " + livelloPescatore)
                  + "|niente|||235,90,80");
            v.Add(L("Go and see the spot anyway", "Vai comunque a vedere il posto")
                  + "|gps_torneo " + i + "|||130,225,180");
        }
        else
        {
            int luZ2 = LuogoDalNome(t.Zona);
            bool qui = (luZ2 < 0) || (LuogoQui() == luZ2);
            if (!qui)
                v.Add(L("Get to the spot", "Raggiungi il posto")
                      + "|gps_torneo " + i + "|||130,225,180");
            else if (inPesca)
                v.Add(L("Sign up", "Iscriviti") + " - $" + Soldo(t.Quota)
                      + "|iscr_torneo " + i + "|||130,225,180");
            else
                // la giornata non ce l'hai: si paga tutto in una volta
                v.Add(L("Pay and start", "Paga e comincia") + " - $" + Soldo(t.Quota)
                      + " + " + L("day licence", "la giornata")
                      + "|torneo_via " + i + "|||130,225,180");
        }

        ScriviVoci(pre + i + ".txt", v);
    }

    void ScriviTornei()
    {
        // Come "Studia i pesci": voci normali, e il trainer disegna da solo
        // in cima l'immagine della riga scelta con la sua scheda sotto.
        // Qui l'immagine e' il banner. Premendo si entra nella scheda.
        List<string> v = new List<string>();
        v.Add("nota|" + L("Press to open the competition", "Premi per aprire il torneo"));
        int i;
        for (i = 0; i < tornei.Count; i++)
        {
            Torneo t = tornei[i];
            string img = "niente";
            if (t.Banner.Length > 0)
            {
                string p = ImgOk("img\\tornei\\" + t.Banner);
                if (p.Length > 0) img = p;
            }
            if (img == "niente") img = Banner();
            // IN CIMA SOLO TRE COSE: quanto dura, quanto paga, che
            // livello vuole. Il resto sta dentro la scheda, se no la
            // fascia diventa una riga di roba scritta tutta uguale.
            string d = t.Minuti + " min   $" + Soldo(t.Premio)
                     + "   " + L("Lv.", "Liv.") + t.LivMin;

            // sottofile: etichetta | file | colore | immagine | descrizione
            v.Add("sottofile|" + t.Nome + "|t_gara_" + i + ".txt|"
                  + "|" + img + "|" + d);
            ScriviUnTorneo(i, "t_gara_");
            ScriviUnTorneo(i, "p_gara_");
        }
        ScriviVoci("tornei_voci.txt", v);
    }

    // ------------------------------------------------------------
    //  I PUNTI DI PESCA SULLA MAPPA
    //  Un blip col pesce su ognuno dei punti segnati a mano in gioco,
    //  col colore che dice che acqua e'.
    // ------------------------------------------------------------
    static int ColorePunto(string tipo)
    {
        if (tipo == "lago") return 3;        // azzurro
        if (tipo == "fiume") return 2;       // verde
        if (tipo == "torrente") return 24;   // verde chiaro
        if (tipo == "palude") return 52;     // verde scuro
        if (tipo == "canale") return 38;     // blu
        return 3;
    }

    string NomePunto(Punto q)
    {
        string t = q.Tipo;
        if (t != null && t.Length > 0) t = char.ToUpper(t[0]) + t.Substring(1);
        int lu, k;
        for (lu = 0; lu < arZoneGta.Count; lu++)
            for (k = 0; k < arZoneGta[lu].Count; k++)
                if (arZoneGta[lu][k] == q.Zona) return t + " - " + arNome[lu];
        return t + " - " + q.Zona;
    }

    void TogliBlipPunti()
    {
        int i;
        for (i = 0; i < blipAree.Count; i++)
        {
            try { if (blipAree[i] != null && blipAree[i].Exists()) blipAree[i].Delete(); }
            catch { }
        }
        blipAree.Clear();
    }

    // UN BLIP PER AREA, al centro dei suoi punti.
    // Se il centro cade fuori dall'acqua - succede con le anse e le rive
    // storte - si sposta sul punto registrato piu' vicino al centro, cosi'
    // il segno sta sempre sull'acqua.
    List<Blip> blipAree = new List<Blip>();

    void MettiBlipPunti()
    {
        TogliBlipPunti();
        int a;
        for (a = 0; a < arNome.Count; a++)
        {
            float cx = PuntoX(a), cy = PuntoY(a), cz = arCz[a];
            if (arAcc[a])
            {
                try
                {
                    Blip ba = World.CreateBlip(new GTA.Math.Vector3(cx, cy, cz));
                    if (ba != null && ba.Exists())
                    {
                        Function.Call(Hash.SET_BLIP_SPRITE, ba, 68);
                        Function.Call(Hash.SET_BLIP_COLOUR, ba, 0);
                        Function.Call(Hash.SET_BLIP_SCALE, ba, 0.85f);
                        Function.Call(Hash.SET_BLIP_AS_SHORT_RANGE, ba, false);
                        Function.Call(Hash.SET_BLIP_DISPLAY, ba, 3);
                        Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
                                      SoloAscii(arNome[a]));
                        Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, ba);
                        blipAree.Add(ba);
                    }
                }
                catch { }
                continue;
            }
            int best = -1; float bd = 0f;
            int i;
            for (i = 0; i < apX.Count; i++)
            {
                if (apA[i] != a) continue;
                float dx = apX[i] - cx, dy = apY[i] - cy;
                float d = dx * dx + dy * dy;
                if (best < 0 || d < bd) { bd = d; best = i; }
            }
            // oltre i cento metri dal centro vuol dire che il centro e'
            // finito sulla terra: si usa il punto vero
            if (best >= 0 && bd > 100f * 100f) { cx = apX[best]; cy = apY[best]; }
            try
            {
                Blip b = World.CreateBlip(new GTA.Math.Vector3(cx, cy, cz));
                if (b == null || !b.Exists()) continue;
                Function.Call(Hash.SET_BLIP_SPRITE, b, 68);          // il pesce
                // bianchi e basta, come i segni classici della mappa
                Function.Call(Hash.SET_BLIP_COLOUR, b, 0);
                Function.Call(Hash.SET_BLIP_SCALE, b, 0.85f);
                Function.Call(Hash.SET_BLIP_AS_SHORT_RANGE, b, false);
                // SOLO SULLA MAPPA GRANDE: sul radarino trentacinque pesci
                // in giro danno solo fastidio mentre guidi
                Function.Call(Hash.SET_BLIP_DISPLAY, b, 3);
                Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME,
                              SoloAscii(arNome[a]));
                Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, b);
                blipAree.Add(b);
            }
            catch { }
        }
    }

    // il punto segnato piu' vicino, ma non oltre "quanto" metri.
    // Con quanto = 0 non c'e' limite.
    int LuogoDaiPunti(GTA.Math.Vector3 pos, float quanto)
    {
        int best = -1;
        float bd = 0f;
        int i;
        for (i = 0; i < apX.Count; i++)
        {
            float dx = apX[i] - pos.X;
            float dy = apY[i] - pos.Y;
            float d = dx * dx + dy * dy;
            if (best < 0 || d < bd) { bd = d; best = i; }
        }
        if (best < 0) return -1;
        if (quanto > 0f && bd > quanto * quanto) return -1;
        return apA[best];
    }

    // l'area il cui punto registrato piu' vicino sta entro "raggio"
    // metri: -1 se sei piu' lontano di cosi'
    int LuogoVicino(float raggio)
    {
        try
        {
            Ped p = Game.Player.Character;
            if (p == null || !p.Exists()) return -1;
            GTA.Math.Vector3 pos = p.Position;
            int best = -1;
            float bd = 0f;
            int i;
            for (i = 0; i < apX.Count; i++)
            {
                float dx = apX[i] - pos.X;
                float dy = apY[i] - pos.Y;
                float d = dx * dx + dy * dy;
                if (best < 0 || d < bd) { bd = d; best = i; }
            }
            if (best >= 0 && bd <= raggio * raggio) return apA[best];
        }
        catch { }
        return -1;
    }

    int LuogoQui()
    {
        try
        {
            Ped p = Game.Player.Character;
            if (p == null || !p.Exists()) return -1;
            GTA.Math.Vector3 pos = p.Position;
            string z = Function.Call<string>(Hash.GET_NAME_OF_ZONE, pos.X, pos.Y, pos.Z);
            if (z != null) zonaVista = z.ToUpper().Trim();

            // COMANDA IL PUNTO REGISTRATO PIU' VICINO.
            // Le aree le abbiamo percorse a piedi e in barca, un punto ogni
            // dieci metri: quello e' il dato vero. I nomi zona di GTA non
            // c'entrano piu' niente - "Vinewood Hills" e' la collina e il
            // laghetto insieme, e un fiume attraversa quattro zone.
            int best = -1;
            float bd = 0f;
            int i;
            for (i = 0; i < apX.Count; i++)
            {
                float dx = apX[i] - pos.X;
                float dy = apY[i] - pos.Y;
                float d = dx * dx + dy * dy;
                if (best < 0 || d < bd) { bd = d; best = i; }
            }
            if (best >= 0 && bd <= RAGGIO_AREA * RAGGIO_AREA) return apA[best];
        }
        catch { }
        return -1;
    }

    // La zona da sola non basta: ALAMO e' grande e ci passa anche la
    // statale. Guardiamo se c'e' acqua intorno a noi.
    // ------------------------------------------------------------
    //  C'E' ACQUA QUI?
    //  In GTA i fiumi e i torrenti non sono acqua per GET_WATER_HEIGHT:
    //  quelle funzioni conoscono solo il mare e i grandi specchi. Per
    //  i corsi d'acqua l'unica che risponde e' la sonda verticale.
    //  Si prova in quest'ordine, dal caso piu' ovvio al piu' fino:
    //    1. hai i piedi in acqua                -> ovvio che si'
    //    2. sonda verticale intorno a te        -> vede fiumi e torrenti
    //    3. le due GET_WATER_HEIGHT             -> mare e laghi
    // ------------------------------------------------------------
    // L'ACQUA VERA E' PIU' ALTA DEL FONDO.
    // La sonda verticale del gioco, in collina, risponde sempre un metro
    // esatto sotto i piedi: non sta trovando acqua, sta restituendo il
    // TERRENO. Si smaschera cosi': l'acqua vera ha una profondita', quindi
    // il pelo dell'acqua sta almeno mezzo metro sopra il fondo. Se acqua e
    // suolo coincidono, quella non e' acqua, e' terra.
    float ultimoSuolo = 0f;

    // QUANTO LONTANA PUO' STARE L'ACQUA, in linea d'aria.
    // Non due limiti separati (tanti metri di lato, tanti di salto) ma uno
    // solo: la distanza vera fra te e il pelo dell'acqua. Cosi' dal ponte
    // di Zancudo, con la palude trenta metri piu' sotto, te lo dice lo
    // stesso. E' l'unico numero da girare se vuoi piu' o meno raggio.
    const float RAGGIO_ARIA = 50f;

    static bool InLineaDAria(float dx, float dy, float dz)
    {
        return (dx * dx + dy * dy + dz * dz) <= RAGGIO_ARIA * RAGGIO_ARIA;
    }

    float ultimaDistanza = 0f;

    bool AcquaRaggiungibile(float dx, float dy, float acquaZ, float mioZ)
    {
        float dz = acquaZ - mioZ;
        ultimaDistanza = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        return InLineaDAria(dx, dy, dz);
    }

    bool AcquaSopraIlFondo(float qx, float qy, float acquaZ)
    {
        ultimoSuolo = -9999f;
        try
        {
            OutputArgument g = new OutputArgument();
            if (!Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD,
                                     qx, qy, acquaZ + 50f, g, false))
                return true;                       // fondo sconosciuto: passa
            float suolo = g.GetResult<float>();
            ultimoSuolo = suolo;
            return (acquaZ > suolo + 0.5f);
        }
        catch { }
        return true;
    }

    bool VicinoAllAcqua()
    {
        try
        {
            Ped p = Game.Player.Character;
            if (p == null || !p.Exists()) return false;
            GTA.Math.Vector3 pos = p.Position;

            // 1. ci sei dentro
            if (Function.Call<bool>(Hash.IS_ENTITY_IN_WATER, p)) return true;
            if (Function.Call<float>(Hash.GET_ENTITY_SUBMERGED_LEVEL, p) > 0.01f) return true;

            int d;
            for (d = 0; d < RAGGIX.Length; d++)
            {
                float qx = pos.X + RAGGIX[d];
                float qy = pos.Y + RAGGIY[d];

                // 2. la sonda verticale: parte da sopra la testa e scende
                OutputArgument hv = new OutputArgument();
                bool cv = Function.Call<bool>(Hash.TEST_VERTICAL_PROBE_AGAINST_ALL_WATER,
                            qx, qy, pos.Z + 3f, 1, hv);
                if (cv && AcquaRaggiungibile(RAGGIX[d], RAGGIY[d], hv.GetResult<float>(), pos.Z))
                {
                    if (!AcquaSopraIlFondo(qx, qy, hv.GetResult<float>()))
                        continue;
                    return true;
                }

                // 3. mare e laghi
                OutputArgument h = new OutputArgument();
                if (Function.Call<bool>(Hash.GET_WATER_HEIGHT, qx, qy, pos.Z, h)
                    && AcquaRaggiungibile(RAGGIX[d], RAGGIY[d], h.GetResult<float>(), pos.Z))
                {
                    if (!AcquaSopraIlFondo(qx, qy, h.GetResult<float>()))
                        continue;
                    return true;
                }

                OutputArgument h2 = new OutputArgument();
                if (Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES, qx, qy, pos.Z, h2)
                    && AcquaRaggiungibile(RAGGIX[d], RAGGIY[d], h2.GetResult<float>(), pos.Z))
                {
                    if (!AcquaSopraIlFondo(qx, qy, h2.GetResult<float>()))
                        continue;
                    return true;
                }
            }
        }
        // Se qualcosa va storto la risposta e' NO. Prima qui c'era un
        // "return true" e bastava un errore qualunque perche' ti dicesse
        // che eri in riva dappertutto.
        catch { }
        return false;
    }

    // QUANTO VICINO ALL'ACQUA DEVI ESSERE.
    // Prima si arrivava a quaranta metri in orizzontale e dodici in
    // verticale: cosi' a casa di Franklin, col laghetto li' sopra, ti
    // diceva gia' "zona di pesca". Adesso devi essere sulla riva davvero,
    // otto metri al massimo.
    static readonly float[] RAGGIX = new float[] {
        0f,  2f, -2f,  0f,  0f,  2f, -2f,  2f, -2f,
        5f, -5f,  0f,  0f,  5f, -5f,  5f, -5f,
       10f,-10f,  0f,  0f, 10f,-10f, 10f,-10f,
       15f,-15f,  0f,  0f, 20f,-20f,  0f,  0f,
       14f,-14f, 14f,-14f,
       30f,-30f,  0f,  0f, 21f,-21f, 21f,-21f,
       40f,-40f,  0f,  0f, 28f,-28f, 28f,-28f,
       50f,-50f,  0f,  0f, 35f,-35f, 35f,-35f };
    static readonly float[] RAGGIY = new float[] {
        0f,  0f,  0f,  2f, -2f,  2f,  2f, -2f, -2f,
        0f,  0f,  5f, -5f,  5f, -5f, -5f,  5f,
        0f,  0f, 10f,-10f, 10f,-10f,-10f, 10f,
        0f,  0f, 15f,-15f,  0f,  0f, 20f,-20f,
       14f, 14f,-14f,-14f,
        0f,  0f, 30f,-30f, 21f, 21f,-21f,-21f,
        0f,  0f, 40f,-40f, 28f, 28f,-28f,-28f,
        0f,  0f, 50f,-50f, 35f, 35f,-35f,-35f };

    // ------------------------------------------------------------
    //  I COMANDI CHE ARRIVANO DAL TRAINER (comandi.txt)
    // ------------------------------------------------------------
    void LeggiComandi()
    {
        string f = Path.Combine(MY_DIR, "comandi.txt");
        string[] rows;
        try
        {
            if (!File.Exists(f)) return;
            rows = File.ReadAllLines(f);
            if (rows.Length == 0) return;
            File.WriteAllText(f, "");
        }
        catch { return; }

        int i;
        bool cambiato = false;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0) continue;
            Diario("arrivato: " + r);
            if (Esegui(r)) cambiato = true;
        }
        if (cambiato)
        {
            SalvaStato();
            RiscriviTutto();
        }
    }

    bool Esegui(string riga)
    {
        // il trainer attacca in fondo la posizione del giocatore:
        //    compra_canna 105 @-178.32|800.51|197.86|345.00
        // quella parte non ci serve, si taglia
        int chiocc = riga.IndexOf(" @");
        if (chiocc > 0) riga = riga.Substring(0, chiocc).Trim();

        string cmd = riga, arg = "";
        int sp = riga.IndexOf(' ');
        if (sp > 0) { cmd = riga.Substring(0, sp); arg = riga.Substring(sp + 1).Trim(); }

        // "compra_cibo" e' il bar, non il negozio: va tolto di mezzo prima,
        // se no lo mangia StartsWith("compra_") e il bar non funziona
        if (cmd == "compra_cibo") return Mangia(Numero(arg));
        if (cmd.StartsWith("compra_"))
        {
            string cat = cmd.Substring(7);
            return Compra(cat, Numero(arg));
        }
        // A su un'esca la sceglie e basta: niente equipaggiato/non
        // equipaggiato, le esche si consumano. Lo stesso che fa RB.
        // il trainer ci dice quale nostra pagina hai aperto: se vai a
        // toccare l'armatura la canna va riposta, non si cambia il
        // mulinello con la lenza in acqua
        // IL TRAINER DICE OGNI SECONDO QUALE PAGINA E' APERTA.
        // Serve per far vedere qualcosa solo mentre ci sei dentro: "apri"
        // arriva una volta sola e non dice mai quando chiudi.
        if (cmd == "vedi")
        {
            if (arg == "casa_voci.txt") hudCasaFino = Game.GameTime + 2500;
            // false apposta: non e' un cambiamento, e arriva ogni secondo.
            // Tornando true si risalvava e si riscriveva tutto di continuo.
            return false;
        }
        if (cmd == "apri")
        {
            // la canna resta in mano: rientra solo la lenza
            if (arg == "casa_voci.txt") RitiraLenza();
            return true;
        }
        // azzera il diario: due pressioni, che e' roba che non torna
        if (cmd == "imp_diario")
        {
            if (!diarioChiesto)
            {
                diarioChiesto = true;
                Avviso("~y~Premi ancora per azzerare il diario.");
                return true;
            }
            diarioChiesto = false;
            quaderno.Clear();
            presoQui.Clear();
            record.Clear();
            dovePreso.Clear();
            recEsca.Clear();
            recAmo.Clear();
            recXp.Clear();
            recVale.Clear();
            Avviso("~g~Diario azzerato.");
            return true;
        }
        if (cmd == "imp_reset")
        {
            // RICOMINCIA DA ZERO. Cancella tutto: diario, punti, livello,
            // e la roba comprata. I soldi restano i tuoi, quelli sono di GTA.
            if (!resetChiesto)
            {
                resetChiesto = true;
                Avviso("~r~Premi ancora: cancelli tutto e riparti da zero.");
                return true;
            }
            resetChiesto = false;
            FinePesca(false);
            fase = FASE_FERMO;
            quaderno.Clear();
            presoQui.Clear();
            record.Clear();
            dovePreso.Clear();
            recEsca.Clear();
            recAmo.Clear();
            recXp.Clear();
            recVale.Clear();
            magazzino.Clear();
            borsa.Clear();
            armato.Clear();
            usati.Clear();
            nassaOggi.Clear();
            xpTot = 0;
            livelloPescatore = 1;
            kgNassa = 0f;
            soldiNassa = 0;
            escaMontata = -1;
            frizione = 2;
            minutiFatti = 0;
            licZona = "";
            licGiorni = 0;
            SalvaStato();
            RiscriviTutto();
            Avviso("~g~Tutto azzerato. Sei di nuovo al livello 1.");
            return true;
        }
        if (cmd == "pesca_via")
        {
            if (!inPesca) { Avviso("~y~Prima paga la giornata."); return true; }
            int luV = LuogoQui();
            if (luV >= 0 && CodiceLuogo(luV) != licZona)
            {
                string bzV;
                Avviso("~r~La licenza e' per " + NomeChiosco(licZona, out bzV) + ".");
                return true;
            }
            if (!VicinoAllAcqua()) { Avviso("~y~Non sei in riva."); return true; }
            if (fase != FASE_FERMO) { Avviso("~y~Hai gia' la canna in mano."); return true; }
            int idv; string imgv, nomev;
            if (!Montato("canna", out idv, out imgv, out nomev))
            { Messaggio("Arma una canna dall'equipaggiamento."); return true; }
            if (!Montato("mulinello", out idv, out imgv, out nomev))
            { Messaggio("Arma il mulinello dall'equipaggiamento."); return true; }
            if (!Montato("lenza", out idv, out imgv, out nomev))
            { Messaggio("Imbobina una lenza sul mulinello."); return true; }
            if (!Montato("nassa", out idv, out imgv, out nomev))
            { Messaggio("Porta una nassa, o i pesci dove li metti?"); return true; }
            // e in punta ci vuole qualcosa che agganci
            string mancaQui = CosaMancaPerLanciare();
            if (mancaQui.Length > 0) { Messaggio(mancaQui); return true; }
            if (escaMontata < 0 && InUso("artificiale") < 0) CambiaEsca();
            fase = FASE_PRONTO;
            grillettoMollato = false;
            ScenaSu(Game.Player.Character);
            tastoDa = Game.GameTime + 500;
            return true;
        }
        if (cmd == "acq_accesso")
        {
            int la = LuogoQui();
            if (la < 0) { Avviso("~y~Non sei dentro nessuna area."); return true; }
            Vector3 pa = Game.Player.Character.Position;
            arAx[la] = pa.X; arAy[la] = pa.Y; arAcc[la] = true;
            SalvaAccessi();
            MettiBlipPunti();
            Avviso("~g~Punto di partenza segnato: " + arNome[la] + ".");
            ScriviAcque();
            return true;
        }
        if (cmd == "prova_gall")
        {
            int ig = Numero(arg);
            if (InUso("galleggiante") == ig)
            {
                armato["galleggiante"] = -1;
                Aggiungi(borsa, "galleggiante:" + ig, -1);
                Avviso("~y~Galleggiante tolto.");
            }
            else
            {
                Aggiungi(borsa, "galleggiante:" + ig, 1);
                armato["galleggiante"] = ig;
                string ng = "", ig2; int pg, lg;
                if (Articolo("galleggiante", ig, out ng, out ig2, out pg, out lg))
                    Avviso("~g~Montato: ~s~" + ng);
            }
            SalvaStato();
            ScriviProvaGall();
            RiscriviTutto();
            return true;
        }
        if (cmd == "mod_pesce") { ProvaPesce(Numero(arg)); return true; }
        if (cmd == "mod_pesce_piu") { ProvaPesce(pesceProvaN + 1); return true; }
        if (cmd == "mod_pesce_meno") { ProvaPesce(pesceProvaN - 1); return true; }
        if (cmd == "mod_pesce_via") { ViaProvaPesce(); ScriviModelli(); return true; }
        if (cmd == "prova_suono")
        {
            if (suoNome.Count == 0) CaricaSuoni();
            int ks = Numero(arg);
            if (ks >= 0 && ks < suoNome.Count)
            {
                suoUltimoN = suoNome[ks]; suoUltimoS = suoSet[ks];
                Suono(suoUltimoN, suoUltimoS);
                Avviso("~b~" + suoUltimoN + "~s~   (" + suoUltimoS + ")");
                ScriviSuoni();
            }
            return true;
        }
        if (cmd == "suono_rileggi")
        {
            CaricaSuoni();
            Avviso("~g~" + suoNome.Count + " suoni da provare.");
            ScriviSuoni();
            return true;
        }
        if (cmd == "suono_usa")
        {
            if (suoUltimoN.Length == 0) { Avviso("~y~Prima sentine uno."); return true; }
            sulN = suoUltimoN; sulS = suoUltimoS;
            SalvaSuonoLancio();
            Avviso("~g~Il lancio adesso fa: ~s~" + sulN);
            ScriviSuoni();
            return true;
        }
        if (cmd == "suono_via")
        {
            sulN = ""; sulS = "";
            SalvaSuonoLancio();
            Avviso("~y~Lancio muto.");
            ScriviSuoni();
            return true;
        }
        if (cmd == "reg_fiume") { AvviaRegistrazione("fiume"); return true; }
        if (cmd == "reg_lago") { AvviaRegistrazione("lago"); return true; }
        if (cmd == "reg_stop") { FermaRegistrazione(); return true; }
        if (cmd == "reg_butta")
        {
            int kb = Numero(arg);
            List<string> nb = NomiAcque();
            if (kb >= 0 && kb < nb.Count)
            {
                string via = nb[kb];
                int q7;
                for (q7 = acque.Count - 1; q7 >= 0; q7--)
                    if (acque[q7].Nome == via) acque.RemoveAt(q7);
                SalvaAcque();
                // il file e' cambiato: le aree vanno rilette, se no i blip
                // e LuogoQui riconoscono ancora una zona che non c'e' piu'
                CaricaAree();
                CaricaLivelliAree();
                CaricaAccessi();
                CaricaPesciAree();
                CaricaPuntiCaldi();
                MettiBlipPunti();
                Avviso("~y~Buttata: " + via);
            }
            ScriviAcque();
            return true;
        }
        if (cmd == "torneo_via")
        {
            int it2 = Numero(arg);
            if (it2 < 0 || it2 >= tornei.Count) return false;
            Torneo tv = tornei[it2];
            if (!inPesca)
            {
                int luV2 = LuogoQui();
                if (luV2 < 0) { Avviso("~y~Non sei in una zona di pesca."); return true; }
                if (!CompraLicenza(CodiceLuogo(luV2), 1)) return true;
            }
            ApriTorneo(it2);
            return true;
        }
        if (cmd == "iscr_torneo")
        {
            ApriTorneo(Numero(arg));
            return true;
        }
        if (cmd == "mollo_torneo")
        {
            ChiudiTorneo(true);
            return true;
        }
        if (cmd == "gps_zona")
        {
            int az = Numero(arg);
            if (az < 0 || az >= arNome.Count) return false;
            Function.Call(Hash.SET_NEW_WAYPOINT, PuntoX(az), PuntoY(az));
            Avviso("~g~Segnaposto su " + arNome[az] + ".");
            Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            return true;
        }
        if (cmd == "gps_torneo")
        {
            int it = Numero(arg);
            if (it < 0 || it >= tornei.Count) return false;
            Torneo tt = tornei[it];
            int lu = LuogoDalNome(tt.Zona);
            if (lu < 0) { Avviso("~y~Non so dove sia questa zona."); return true; }
            // il segnaposto va al centro dell'area del torneo
            Function.Call(Hash.SET_NEW_WAYPOINT, PuntoX(lu), PuntoY(lu));
            Avviso("~g~Segnaposto su " + tt.Zona + ".~s~  " + tt.Nome);
            Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            return true;
        }
        if (cmd == "imp_gall")
        {
            int gz = Numero(arg);
            if (gz < 0 || gz >= GALL_ZOOM.Length) gz = 0;
            gallZoom = gz;
            Avviso("~b~Galleggiante: " + GallZoomTxt());
            SalvaStato();
            ScriviImpostazioni();
            return true;
        }
        if (cmd == "imp_zone")
        {
            avvisaZona = !avvisaZona;
            Avviso(avvisaZona ? "~g~Zone di pesca: acceso"
                              : "~y~Zone di pesca: spento");
            return true;
        }
        if (cmd == "usa_esca")
        {
            int ide = Numero(arg);
            if (Quanti(borsa, "esca:" + ide) <= 0) return false;
            escaMontata = ide;
            string ne, ie; int pe, le;
            if (Articolo("esca", ide, out ne, out ie, out pe, out le))
                Avviso("~g~Esca: ~s~" + ne + "  x" + QuanteEsche(ide));
            Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            return true;
        }
        if (cmd == "arma")
        {
            string[] aa = arg.Split(' ');
            if (aa.Length < 2) return false;
            return Arma(aa[0], Numero(aa[1]));
        }
        if (cmd == "disarma_lenza") { DisarmaLenza(); return true; }
        if (cmd == "arma_bob") { return ArmaLenzaBobina(Numero(arg)); }
        if (cmd == "butta_bob") { return ButtaBobina(Numero(arg)); }
        if (cmd == "bob_casa") { return BobinaACasa(Numero(arg)); }
        if (cmd == "bob_borsa") { return BobinaInBorsa(Numero(arg)); }
        if (cmd == "butta_bobc") { return ButtaBobinaCasa(Numero(arg)); }
        if (cmd == "vendi_bobc") { return VendiBobinaCasa(Numero(arg)); }
        if (cmd == "butta")
        {
            string[] ab = arg.Split(' ');
            if (ab.Length < 3) return false;
            return Butta(ab[0], Numero(ab[1]), ab[2] == "casa");
        }
        if (cmd == "vendi")
        {
            string[] av = arg.Split(' ');
            if (av.Length < 2) return false;
            return Vendi(av[0], Numero(av[1]));
        }
        if (cmd == "equipaggia" || cmd == "lascia")
        {
            string[] a = arg.Split(' ');
            if (a.Length < 2) return false;
            return Sposta(a[0], Numero(a[1]), cmd == "equipaggia");
        }
        if (cmd == "licenza")
        {
            string[] a = arg.Split(' ');
            if (a.Length < 2) return false;
            return CompraLicenza(a[0], Numero(a[1]));
        }
        if (cmd == "licenza_tasca")
        {
            string[] a = arg.Split(' ');
            if (a.Length < 2) return false;
            return CompraLicenza(a[0], Numero(a[1]), false);
        }
        if (cmd == "inizia_pesca") return IniziaPesca();
        if (cmd == "marca") return Marca(arg);
        if (cmd == "smarca") return Smarca();
        if (cmd == "smetti") return FinePesca(true);
        if (cmd == "compra_cibo") return Mangia(Numero(arg));
        return false;
    }

    // il prezzo di oggi: al chiosco si paga il doppio
    int PrezzoOggi(int prezzoWiki)
    {
        int d = Dollari(prezzoWiki);
        // il chiosco sul posto costa il triplo: e' comodo, si paga
        if (inPesca) d = d * 3;
        if (d < 1) d = 1;
        return d;
    }

    // scrive in diario.txt cosa succede a ogni comando: serve solo per
    // capire i problemi, si toglie quando tutto va
    void Diario(string t)
    {
        try
        {
            File.AppendAllText(Path.Combine(MY_DIR, "diario.txt"),
                               DateTime.Now.ToString("HH:mm:ss") + "  " + t + "\r\n");
        }
        catch { }
    }

    bool Compra(string cat, int id)
    {
        string nome, img;
        int prezzo, liv;
        Diario("compra " + cat + " " + id + " - soldi letti: " + Soldi()
               + " - livello: " + livelloPescatore + " - inPesca: " + inPesca);
        if (!Articolo(cat, id, out nome, out img, out prezzo, out liv))
        {
            Diario("   RIFIUTATO: articolo non trovato");
            return false;
        }
        if (liv > livelloPescatore)
        {
            Diario("   RIFIUTATO: serve livello " + liv);
            Avviso("~r~" + nome + ": ci vuole il livello " + liv + ".");
            return false;
        }
        // SUL POSTO SI COMPRA TUTTO, MA CI DEVE STARE.
        // Prima il chiosco vendeva solo roba di consumo. Adesso vendono
        // tutto: il limite non e' piu' cosa tengono, e' quanto ti entra
        // in cassetta - quello che compri mentre peschi ce l'hai addosso
        // gia' pronto.
        if (inPesca && !CiSta(cat, id))
        {
            Diario("   RIFIUTATO: non ci sta piu' niente in " + cat);
            Avviso("~y~Non ci sta: " + Contatori());
            return false;
        }
        int costo = PrezzoOggi(prezzo);
        if (Soldi() < costo)
        {
            Diario("   RIFIUTATO: costa " + costo + " e ne hai " + Soldi());
            Avviso("~r~Ti servono $" + costo + ", ne hai " + Soldi() + ".");
            return false;
        }
        Paga(costo);
        // al chiosco quello che compri ce l'hai gia' addosso,
        // a casa finisce in magazzino
        Aggiungi(inPesca ? borsa : magazzino, cat + ":" + id, 1);
        // scritto per esteso cosi' si vede se i soldi si muovono davvero
        Diario("   COMPRATO " + nome + " per " + costo + ", restano " + Soldi());
        Avviso("~g~" + nome + "  ~r~-$" + costo + "  ~s~restano $" + Soldi());

        // Il consiglio arriva come AVVISO, non nel menu: il menu resta
        // pulito. Non blocca niente, la canna giusta magari la compri dopo.
        string cons = ConsiglioAcquisto(cat, id);
        if (cons.Length > 0)
        {
            if (cons.StartsWith("Non va")) Avviso("~y~" + cons);
            else Avviso("~b~" + cons);
        }
        return true;
    }

    // sul posto si vende solo roba di consumo
    static bool AlChiosco(string cat)
    {
        return (cat == "esca" || cat == "lenza" || cat == "terminale"
             || cat == "galleggiante" || cat == "artificiale");
    }

    // VENDERE LA ROBA DI CASA.
    // Si vende solo da casa, un pezzo per volta, e ci vogliono due colpi
    // di X: il primo dice quanto ti danno, il secondo lo vende. Se in
    // mezzo cambi riga la conferma decade, cosi' un tasto sbagliato non
    // ti svuota il magazzino.
    // Il prezzo di ritiro e' una percentuale di quello di listino:
    // "vendi_percento" in config.ini, di suo 50.
    int hudCasaFino = 0;
    string vendiChiesto = "";
    int vendiScade = 0;

    // BUTTARE VIA.
    // Una bobina con dieci metri avanzati non la vende nessuno: o te la
    // tieni o la butti. Due colpi di Y, come per vendere: il primo
    // chiede, il secondo butta. Non torna piu' indietro.
    string buttaChiesto = "";
    int buttaScade = 0;

    bool ChiediDueVolte(string chiave, string domanda)
    {
        int ora = Game.GameTime;
        if (buttaChiesto != chiave || ora > buttaScade)
        {
            buttaChiesto = chiave;
            buttaScade = ora + 5000;
            Messaggio(domanda);
            return false;
        }
        buttaChiesto = "";
        return true;
    }

    bool ButtaBobina(int i)
    {
        int id = BobinaId(i);
        int m = BobinaMetri(i);
        if (id < 0) return false;
        string nome, img;
        int prezzo, liv;
        if (!Articolo("lenza", id, out nome, out img, out prezzo, out liv)) return false;
        if (!ChiediDueVolte("bob" + i,
                "Premi ancora (Y) per gettare " + nome + " (" + m + " m)"))
            return true;
        if (i < 0 || i >= bobine.Count) return true;
        bobine.RemoveAt(i);
        Messaggio("Gettata: " + nome + "   " + m + " m");
        return true;
    }

    bool Butta(string cat, int id, bool daCasa)
    {
        Dictionary<string, int> d = daCasa ? magazzino : borsa;
        string k = cat + ":" + id;
        if (Quanti(d, k) <= 0) return false;
        string nome, img;
        int prezzo, liv;
        if (!Articolo(cat, id, out nome, out img, out prezzo, out liv)) return false;
        if (!ChiediDueVolte(k + (daCasa ? "c" : "b"),
                "Premi ancora (Y) per gettare " + nome))
            return true;
        if (!daCasa && Quanti(d, k) <= 1) SeArmatoSmonta(cat, id);
        Aggiungi(d, k, -1);
        Messaggio("Gettato: " + nome);
        return true;
    }

    bool Vendi(string cat, int id)
    {
        string k = cat + ":" + id;
        if (Quanti(magazzino, k) <= 0) { Messaggio("Non ce l'hai in casa."); return true; }
        if (inPesca) { Messaggio("Si vende da casa, non in riva."); return true; }

        string nome, img;
        int prezzo, liv;
        if (!Articolo(cat, id, out nome, out img, out prezzo, out liv)) return false;

        int perc = (int)LeggiF("vendi_percento", 50f);
        if (perc < 0) perc = 0;
        if (perc > 100) perc = 100;
        int reso = prezzo * perc / 100;

        int ora = Game.GameTime;
        if (vendiChiesto != k || ora > vendiScade)
        {
            vendiChiesto = k;
            vendiScade = ora + 5000;
            Messaggio("Premi ancora (X) per vendere " + nome + " a $" + Dollari(reso));
            return true;
        }

        vendiChiesto = "";
        magazzino[k] = Quanti(magazzino, k) - 1;
        if (magazzino[k] <= 0) magazzino.Remove(k);
        Paga(-reso);
        SalvaStato();
        RiscriviTutto();
        Messaggio("Venduto " + nome + "   +$" + Dollari(reso));
        return true;
    }

    bool Sposta(string cat, int id, bool versoBorsa)
    {
        string k = cat + ":" + id;
        Dictionary<string, int> da = versoBorsa ? magazzino : borsa;
        Dictionary<string, int> a = versoBorsa ? borsa : magazzino;
        if (Quanti(da, k) <= 0) return false;
        if (inPesca)
        {
            Avviso("~r~Sei fuori: la borsa e' quella che ti sei portato.");
            return false;
        }
        if (versoBorsa && !CiSta(cat, id))
        {
            Avviso("~r~Non ci sta piu': guarda cassetta e portacanne.");
            return false;
        }

        // l'equilibrio: qui si blocca, non quando peschi
        if (versoBorsa)
        {
            string perche;
            if (cat == "canna")
            {
                // monti la canna DOPO: allora sono lenza e mulinello gia'
                // in borsa a dover stare dentro il suo limite
                int q; string qi, qn;
                if (Montato("lenza", out q, out qi, out qn)
                    && !VaConLaCanna("lenza", q, id, out perche))
                {
                    Avviso("~r~Non e' equilibrata: ~s~" + perche);
                    return false;
                }
                if (Montato("mulinello", out q, out qi, out qn)
                    && !VaConLaCanna("mulinello", q, id, out perche))
                {
                    Avviso("~r~Non e' equilibrata: ~s~" + perche);
                    return false;
                }
            }
            else
            {
                int idc = CannaInBorsa();
                if (idc >= 0 && !VaConLaCanna(cat, id, idc, out perche))
                {
                    Avviso("~r~Non e' equilibrata: ~s~" + perche);
                    return false;
                }
            }
        }
        // verso casa: se era sulla canna, prima si smonta
        if (!versoBorsa && Quanti(borsa, k) <= 1) SeArmatoSmonta(cat, id);
        Aggiungi(da, k, -1);
        Aggiungi(a, k, 1);
        string nome, img;
        int prezzo, liv;
        if (!Articolo(cat, id, out nome, out img, out prezzo, out liv)) nome = cat;
        if (versoBorsa) Avviso("~g~Equipaggiato: ~s~" + nome);
        else Avviso("~y~Rimesso a casa: ~s~" + nome);
        return true;
    }

    // ------------------------------------------------------------
    //  L'EQUILIBRIO DELL'ATTREZZATURA (regola nostra, semplificata)
    //
    //  Nel wiki la portata della canna ("1.00 - 2.00") non e' la forza
    //  della canna: e' LA LENZA CHE QUELLA CANNA VUOLE. Da li' esce
    //  tutto il resto.
    //
    //  Si blocca solo il TROPPO FORTE, mai il troppo debole:
    //    - lenza oltre il massimo della canna  -> spacchi la canna
    //    - mulinello con frizione oltre quel massimo -> stessa cosa
    //  Montare piu' leggero invece si puo': non e' uno sbaglio, e' una
    //  scelta. Si spezza la lenza e perdi il pesce, e va bene cosi'.
    //
    //  Cosi' non serve simulare canne che si rompono: non ci arrivi.
    // ------------------------------------------------------------
    float MaxCanna(int idCanna)
    {
        int i;
        for (i = 0; i < canne.Count; i++)
            if (canne[i].Id == idCanna) return MaxKg(canne[i].LenzaKg);
        return 0f;
    }

    float FrizioneMul(int idMul)
    {
        int i;
        for (i = 0; i < mulinelli.Count; i++)
            if (mulinelli[i].Id == idMul) return mulinelli[i].Frizione;
        return 0f;
    }

    // un pezzo sta bene su quella canna?
    bool VaConLaCanna(string cat, int id, int idCanna, out string perche)
    {
        perche = "";
        float max = MaxCanna(idCanna);
        if (max <= 0f) return true;          // canna senza dato: non blocco

        if (cat == "lenza")
        {
            float kg = KgLenza(id);
            if (kg > max)
            {
                perche = "lenza da " + kg.ToString("0.##", CultureInfo.InvariantCulture)
                       + " kg su una canna da " + max.ToString("0.##", CultureInfo.InvariantCulture)
                       + " kg";
                return false;
            }
        }
        else if (cat == "mulinello")
        {
            float fr = FrizioneMul(id);
            if (fr > max)
            {
                perche = "frizione da " + fr.ToString("0.##", CultureInfo.InvariantCulture)
                       + " kg su una canna da " + max.ToString("0.##", CultureInfo.InvariantCulture)
                       + " kg";
                return false;
            }
        }
        return true;
    }

    // la canna che hai equipaggiato adesso (-1 se non ne hai)
    int CannaInBorsa()
    {
        int id; string img, nome;
        if (Montato("canna", out id, out img, out nome)) return id;
        return -1;
    }

    // tutte le canne che possiedi, a casa e in borsa
    List<int> LeMieCanne()
    {
        List<int> r = new List<int>();
        foreach (KeyValuePair<string, int> kv in magazzino)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length > 1 && c[0] == "canna" && kv.Value > 0) r.Add(Numero(c[1]));
        }
        foreach (KeyValuePair<string, int> kv in borsa)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length > 1 && c[0] == "canna" && kv.Value > 0 && !r.Contains(Numero(c[1])))
                r.Add(Numero(c[1]));
        }
        return r;
    }

    // Per il negozio: con quale delle tue canne va questo pezzo?
    // Non blocca niente, e' solo un consiglio: magari la canna giusta
    // la compri dopo.
    string ConsiglioAcquisto(string cat, int id)
    {
        if (cat != "lenza" && cat != "mulinello") return "";
        List<int> mie = LeMieCanne();
        if (mie.Count == 0) return "Non hai ancora canne";
        int i;
        for (i = 0; i < mie.Count; i++)
        {
            string perche;
            if (VaConLaCanna(cat, id, mie[i], out perche))
            {
                string nome, img; int prezzo, liv;
                if (Articolo("canna", mie[i], out nome, out img, out prezzo, out liv))
                    return "Va con la tua " + nome;
                return "Va con una canna che hai";
            }
        }
        return "Non va con nessuna canna che hai";
    }

    // ------------------------------------------------------------
    //  QUANTO CI STA IN BORSA
    // ------------------------------------------------------------
    void Capienza(out int maxCanne, out int maxMul, out int maxLenze, out int maxRoba)
    {
        // LO ZAINO: ce l'hai addosso da sempre, non si compra e non si perde.
        // Una canna in mano, il mulinello montato, due bobine e le tasche.
        // Il portacanne serve solo per portarne piu' di una; la cassetta per
        // portare piu' roba minuta. Sono miglioramenti, non il punto di
        // partenza: a livello 1 esci con lo zaino e basta.
        maxCanne = ZAINO_CANNE; maxMul = ZAINO_MUL;
        maxLenze = ZAINO_LENZE; maxRoba = ZAINO_ROBA;
        // portacanne e cassetta sono fissi: fanno posto appena li possiedi,
        // che stiano a casa o dietro
        List<KeyValuePair<string, int>> tutti = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, int> kv in borsa) tutti.Add(kv);
        foreach (KeyValuePair<string, int> kv in magazzino) tutti.Add(kv);
        foreach (KeyValuePair<string, int> kv in tutti)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length < 2 || kv.Value <= 0) continue;
            int id = Numero(c[1]);
            int i;
            if (c[0] == "portacanne")
            {
                for (i = 0; i < portacanne.Count; i++)
                    if (portacanne[i].Id == id)
                    {
                        maxCanne += portacanne[i].Canne * kv.Value;
                        maxMul += portacanne[i].Mulinelli * kv.Value;
                        maxLenze += portacanne[i].Lenze * kv.Value;
                    }
            }
            else if (c[0] == "cassetta")
            {
                for (i = 0; i < cassette.Count; i++)
                    if (cassette[i].Id == id)
                    {
                        maxRoba += Numero(cassette[i].Attrezzi) * kv.Value;
                        maxLenze += Numero(cassette[i].Lenze) * kv.Value;
                        maxMul += Numero(cassette[i].Mulinelli) * kv.Value;
                    }
            }
        }
    }

    // roba lunga (canne, portacanne, borse, nasse, mulinelli) va nel banner
    // sopra la lista: in un'icona a sinistra non si vedrebbe niente.
    // Roba piccola (lenze, ami, esche, galleggianti) sta bene a icone.
    static bool AIcone(string cat)
    {
        return (cat == "lenza" || cat == "terminale" || cat == "galleggiante"
             || cat == "artificiale" || cat == "esca");
    }

    const int ZAINO_CANNE = 1;
    const int ZAINO_MUL = 1;
    const int ZAINO_LENZE = 2;
    const int ZAINO_ROBA = 10;

    // quanti pezzi ci sono in una confezione (esche, ami, piombi...)
    int PerConfezione(string cat, int id)
    {
        int i;
        if (cat == "esca")
            for (i = 0; i < escheShop.Count; i++)
                if (escheShop[i].Id == id) return Numero(escheShop[i].Quantita);
        if (cat == "terminale")
            for (i = 0; i < terminali.Count; i++)
                if (terminali[i].Id == id) return Numero(terminali[i].Pezzi);
        return 0;
    }

    // l'etichetta di una riga: per la roba che si conta a pezzi scrivo il
    // TOTALE, non quante confezioni hai. I conti li fa la mod, non tu.
    string Etichetta(string cat, int id, string nome, int quante)
    {
        return nome;
    }

    // la quantita': quanti pezzi in tutto, non quante confezioni
    string Quantita(string cat, int id, int quante)
    {
        return Quantita(cat, id, quante, true);
    }

    // QUANTI PEZZI, E DI QUALE MUCCHIO.
    // Gli ami stanno in scatole da dieci: la riga deve dire i pezzi, non
    // le scatole. Ma "quelli usati" valgono solo per la roba che ti sei
    // portato: una scatola in casa e' intera per definizione. Contando
    // sempre la borsa, gli ami comprati e lasciati a casa risultavano
    // zero.
    string Quantita(string cat, int id, int quante, bool dallaBorsa)
    {
        int per = PerConfezione(cat, id);
        if (per > 0)
        {
            if (dallaBorsa) return "x" + QuantiPezzi(cat, id);
            return "x" + (per * quante);
        }
        return "x" + quante;
    }

    // i numeri che contano davvero di un pezzo equipaggiato
    // una bobina tagliata: stesso filo, ma i metri sono quelli che ha lei
    string DettaglioBobina(int id, int metri)
    {
        int i;
        for (i = 0; i < lenze.Count; i++)
            if (lenze[i].Id == id)
                return lenze[i].Mm + " mm   "
                     + lenze[i].Kg.ToString("0.##", CultureInfo.InvariantCulture) + " kg   "
                     + metri + " m";
        return metri + " m";
    }

    string Dettaglio(string cat, int id)
    {
        int i;
        if (cat == "canna")
            for (i = 0; i < canne.Count; i++)
                if (canne[i].Id == id)
                    return Corto(canne[i].LenzaKg) + " kg   " + canne[i].Lunghezza + " m";
        // il mulinello: la frizione, e i fili che ci stanno con i metri.
        // Il diametro conta: piu' sottile e' il filo, piu' ne entra.
        if (cat == "mulinello")
            for (i = 0; i < mulinelli.Count; i++)
                if (mulinelli[i].Id == id)
                {
                    string cp = mulinelli[i].Capacita;
                    if (cp == null) cp = "";
                    cp = cp.Replace(";", "  ").Replace("  ", " ").Trim();
                    return mulinelli[i].Frizione.ToString("0.##", CultureInfo.InvariantCulture)
                         + " kg   " + cp;
                }
        if (cat == "lenza")
            for (i = 0; i < lenze.Count; i++)
                if (lenze[i].Id == id)
                    return lenze[i].Mm + " mm   "
                         + lenze[i].Kg.ToString("0.##", CultureInfo.InvariantCulture) + " kg   "
                         + lenze[i].Metri + " m";
        if (cat == "nassa")
            for (i = 0; i < nasse.Count; i++)
                if (nasse[i].Id == id)
                    return "pesce " + nasse[i].KgPesce.ToString("0.##", CultureInfo.InvariantCulture)
                         + " kg   rete " + nasse[i].KgTotale.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
        // TUTTI I DATI CHE IL PEZZO HA, non solo uno: e' da questi che si
        // sceglie, non dalla marca.
        if (cat == "terminale")
            for (i = 0; i < terminali.Count; i++)
                if (terminali[i].Id == id)
                {
                    Terminale t = terminali[i];
                    string d = "";
                    if (t.Misura != null && t.Misura.Length > 0) d = Unisci(d, t.Misura);
                    if (t.Mm != null && t.Mm.Length > 0) d = Unisci(d, t.Mm + " mm");
                    if (t.Kg != null && t.Kg.Length > 0) d = Unisci(d, t.Kg + " kg");
                    if (t.Grammi != null && t.Grammi.Length > 0) d = Unisci(d, t.Grammi + " g");
                    return d;
                }
        if (cat == "esca")
            for (i = 0; i < escheShop.Count; i++)
                if (escheShop[i].Id == id)
                {
                    string d = "";
                    if (escheShop[i].Peso != null && escheShop[i].Peso.Length > 0)
                        d = Unisci(d, L("weight ", "peso ") + escheShop[i].Peso);
                    if (escheShop[i].Amo != null && escheShop[i].Amo.Length > 0)
                        d = Unisci(d, L("hook ", "amo ") + escheShop[i].Amo);
                    return d;
                }
        if (cat == "galleggiante")
            for (i = 0; i < galleggianti.Count; i++)
                if (galleggianti[i].Id == id)
                {
                    Galleggiante g = galleggianti[i];
                    string d = L("load ", "piombo ") + PortataIt(g.Portata);
                    if (g.Forma != null && g.Forma.Length > 0) d = Unisci(d, g.Forma);
                    if (g.Misura != null && g.Misura.Length > 0) d = Unisci(d, g.Misura);
                    return d;
                }
        if (cat == "artificiale")
            for (i = 0; i < artificiali.Count; i++)
                if (artificiali[i].Id == id)
                {
                    Artificiale ar = artificiali[i];
                    string d = ar.Tipo;
                    if (ar.Grammi != null && ar.Grammi.Length > 0) d = Unisci(d, ar.Grammi + " g");
                    if (ar.Cm != null && ar.Cm.Length > 0) d = Unisci(d, ar.Cm + " cm");
                    if (ar.Amo != null && ar.Amo.Length > 0) d = Unisci(d, L("hook ", "amo ") + ar.Amo);
                    return d;
                }
        if (cat == "portacanne")
            for (i = 0; i < portacanne.Count; i++)
                if (portacanne[i].Id == id)
                    return portacanne[i].Canne + " canne   " + portacanne[i].Mulinelli
                         + " mulinelli   " + portacanne[i].Lenze + " lenze";
        if (cat == "cassetta")
            for (i = 0; i < cassette.Count; i++)
                if (cassette[i].Id == id)
                    return cassette[i].Attrezzi + " oggetti   " + cassette[i].Lenze + " lenze";
        return "";
    }

    // il riquadro a destra: quello che hai equipaggiato, pezzo per pezzo,
    // con la sua immagine e i numeri che servono per decidere
    // QUELLO CHE STA IN CASA: il magazzino, riga per riga, nello stesso
    // formato del riquadro dell'equipaggiamento.
    //   nome|icona|dati|comando|quantita
    // Il comando e' "equipaggia": da casa la roba va in borsa.
    List<string> RigheCasa()
    {
        List<string> r = new List<string>();
        r.Add("IN CASA");
        r.Add("- spazio illimitato|||||190,195,205");
        int k;
        for (k = 0; k < CAT_COD.Length; k++)
        {
            foreach (KeyValuePair<string, int> kv in magazzino)
            {
                string[] c = kv.Key.Split(':');
                if (c.Length < 2 || c[0] != CAT_COD[k]) continue;
                int id = Numero(c[1]);
                string nome, img;
                int prezzo, liv;
                if (!Articolo(c[0], id, out nome, out img, out prezzo, out liv)) continue;
                r.Add(nome + "|" + img + "|" + Dettaglio(c[0], id)
                      + "|equipaggia " + c[0] + " " + c[1]
                      + "|" + Quantita(c[0], id, kv.Value));
            }
        }
        if (r.Count == 2) r.Add("Non hai niente in casa");
        return r;
    }

    List<string> RigheBorsa()
    {
        List<string> r = new List<string>();
        int mc, mm, ml, mr;
        Capienza(out mc, out mm, out ml, out mr);
        r.Add("EQUIPAGGIAMENTO");
        r.Add("- Canne " + InBorsa("canna") + "/" + mc
              + "   Mulinelli " + InBorsa("mulinello") + "/" + mm
              + "   Lenze " + InBorsa("lenza") + "/" + ml
              + "   Cassetta " + RobaMinuta() + "/" + mr
              + "   Nassa " + InBorsa("nassa") + "/1"
              + "|||||190,195,205");
        int k;
        for (k = 0; k < CAT_COD.Length; k++)
        {
            foreach (KeyValuePair<string, int> kv in borsa)
            {
                string[] c = kv.Key.Split(':');
                if (c.Length < 2 || c[0] != CAT_COD[k]) continue;
                int id = Numero(c[1]);
                string nome, img;
                int prezzo, liv;
                if (!Articolo(c[0], id, out nome, out img, out prezzo, out liv)) continue;
                // il quarto campo e' il comando: premendo A sulla riga a
                // destra il pezzo torna a casa
                r.Add(nome + "|" + img + "|" + Dettaglio(c[0], id)
                      + "|lascia " + c[0] + " " + c[1]
                      + "|" + Quantita(c[0], id, kv.Value));
            }
        }
        if (r.Count == 2) r.Add("Non hai equipaggiato niente");
        return r;
    }

    // ce l'hai, in cassetta o gia' montato sulla canna
    bool HoDavvero(string cat)
    {
        if (InBorsa(cat) > 0) return true;
        if (Armato(cat) >= 0) return true;
        return false;
    }

    // la lenza c'e' se ne hai una bobina in cassetta, una tagliata, o
    // del filo gia' sul mulinello
    bool HoLaLenza()
    {
        if (InBorsa("lenza") > 0) return true;
        if (bobine.Count > 0) return true;
        if (metriInBobina > 0) return true;
        return false;
    }

    int InBorsa(string cat)
    {
        int n = 0;
        foreach (KeyValuePair<string, int> kv in borsa)
            if (kv.Key.StartsWith(cat + ":")) n += kv.Value;
        return n;
    }

    // quanti TIPI diversi di quella roba hai, non quanti pezzi
    int TipiInBorsa(string cat)
    {
        int n = 0;
        foreach (KeyValuePair<string, int> kv in borsa)
            if (kv.Key.StartsWith(cat + ":") && kv.Value > 0) n++;
        return n;
    }

    // LA CASSETTA SI CONTA A TIPI, NON A PEZZI.
    // Un pacco di ami del #10 e' uno scomparto: che tu ne abbia un pacco
    // o tre, sempre quello scomparto e'. Due misure diverse invece sono
    // due scomparti. Contando i pezzi ci vorrebbe un camion per portarsi
    // cento vermi.
    // la nassa NON sta in cassetta: si porta a parte, come le canne
    int RobaMinuta()
    {
        return TipiInBorsa("terminale") + TipiInBorsa("galleggiante")
             + TipiInBorsa("artificiale") + TipiInBorsa("esca");
    }

    bool CiSta(string cat)
    {
        return CiSta(cat, -1);
    }

    // LA CASSETTA CONTA I TIPI, NON I PEZZI: un posto per ogni cosa
    // diversa, e di quella cosa ce ne stanno quante vuoi. Quindi un
    // secondo cucchiaino uguale a uno che hai gia' ci sta sempre, anche
    // a cassetta piena: non prende un posto nuovo.
    bool CiSta(string cat, int id)
    {
        // i contenitori si portano sempre: sono loro a fare il posto
        if (cat == "cassetta" || cat == "portacanne") return true;
        // LA NASSA VA A PARTE, non in cassetta: ma una sola alla volta
        if (cat == "nassa") return InBorsa("nassa") < 1;
        int mc, mm, ml, mr;
        Capienza(out mc, out mm, out ml, out mr);
        if (cat == "canna") return InBorsa("canna") < mc;
        if (cat == "mulinello") return InBorsa("mulinello") < mm;
        if (cat == "lenza") return InBorsa("lenza") < ml;
        if (id >= 0 && Quanti(borsa, cat + ":" + id) > 0) return true;
        return RobaMinuta() < mr;
    }

    // Cosa ti tiene la roba. Lo zaino ce l'hai da sempre; cassetta e
    // portacanne si aggiungono quando li compri e non si vedono: fanno
    // solo posto. Il portacanne da due ti fa portare una canna in piu',
    // quello da quattro tre in piu', e senza ne porti una sola.
    string NomeContenitore()
    {
        string s = "ZAINO";
        if (InBorsa("cassetta") > 0) s = s + " + CASSETTA";
        if (InBorsa("portacanne") > 0) s = s + " + PORTACANNE";
        return s;
    }

    string Contatori()
    {
        int mc, mm, ml, mr;
        Capienza(out mc, out mm, out ml, out mr);
        // la roba minuta sta nello zaino finche' non compri la cassetta:
        // scrivere sempre "Cassetta" era falso
        return "Canne " + InBorsa("canna") + "/" + mc
             + "  Mulinelli " + InBorsa("mulinello") + "/" + mm
             + "  Lenze " + InBorsa("lenza") + "/" + ml
             + "  Oggetti " + RobaMinuta() + "/" + mr
             + "  Nassa " + InBorsa("nassa") + "/1";
    }

    // ------------------------------------------------------------
    //  LA LICENZA E LA GIORNATA
    // ------------------------------------------------------------
    bool CompraLicenza(string zona, int giorni)
    {
        return CompraLicenza(zona, giorni, true);
    }

    // COMPRARE LA LICENZA E INIZIARE A PESCARE SONO DUE COSE. Dal menu
    // nuovo la compri dove vuoi (avvia=false): resta in tasca finche' non
    // sei sul posto e premi "Inizia a pescare" (IniziaPesca). Il trainer
    // vecchio fa le due cose insieme (avvia=true), com'era.
    bool CompraLicenza(string zona, int giorni, bool avvia)
    {
        if (inPesca) return false;
        int prezzo = PrezzoLicenza(zona, giorni);
        if (prezzo <= 0) return false;
        // IL LIVELLO DELL'ACQUA.
        // Come su Fishing Planet: in certi posti non ti fanno entrare
        // finche' non sei del livello giusto. Non e' che il pesce non c'e',
        // e' che quel lago non e' ancora aperto per te. Comprando da
        // lontano vale il tratto piu' basso di quell'acqua.
        int luLic2 = avvia ? LuogoQui() : IndiceLuogo(zona);
        int livMin = (luLic2 >= 0) ? LivelloArea(luLic2) : 0;
        if (!avvia)
        {
            int q;
            for (q = 0; q < arCodice.Count; q++)
                if (arCodice[q] == zona && LivelloArea(q) < livMin) livMin = LivelloArea(q);
        }
        if (luLic2 >= 0 && livelloPescatore < livMin)
        {
            Avviso("~r~" + arNome[luLic2] + ": ci vuole il livello " + livMin + ".");
            return false;
        }
        if (avvia && !AttrezzaturaMinima()) return false;
        if (Soldi() < prezzo)
        {
            Avviso("~r~La licenza costa $" + prezzo + ", hai " + Soldi() + ".");
            return false;
        }
        Paga(prezzo);
        licZona = zona;
        licGiorni = giorni;
        if (!avvia)
        {
            Avviso("~g~Licenza pagata: vai sul posto e inizia a pescare.");
            SalvaStato();
            return true;
        }
        return IniziaPesca();
    }

    // IL MINIMO PER PESCARE: canna, mulinello, lenza e la nassa.
    // Attenzione a dove si guarda: un pezzo ARMATO non sta piu' in
    // cassetta, sta sulla canna - e la lenza nemmeno li', sta sul
    // mulinello. Contando solo la borsa, chi si era gia' preparato
    // si sentiva dire di prepararsi.
    bool AttrezzaturaMinima()
    {
        if (!HoDavvero("canna") || !HoDavvero("mulinello")
         || !HoLaLenza() || !HoDavvero("nassa"))
        {
            Messaggio("Prima prepara l'attrezzatura: canna, mulinello, lenza e nassa.");
            return false;
        }
        return true;
    }

    // hai la licenza in tasca (comprata, non ancora usata) per quest'acqua?
    bool LicenzaInTasca(string zona)
    {
        return !inPesca && licZona == zona && licGiorni > 0;
    }

    // INIZIA A PESCARE: sul posto, con la licenza in tasca, parte la giornata
    bool IniziaPesca()
    {
        if (inPesca) return false;
        if (licZona.Length == 0 || licGiorni <= 0) { Avviso("~y~Prima compra la licenza."); return false; }
        int lu = LuogoQui();
        if (lu < 0 || CodiceLuogo(lu) != licZona)
        {
            Avviso("~y~Non sei sul posto della licenza: l'hai pagata per " + NomeGruppo(licZona) + ".");
            return false;
        }
        if (livelloPescatore < LivelloArea(lu))
        {
            Avviso("~r~" + arNome[lu] + ": ci vuole il livello " + LivelloArea(lu) + ".");
            return false;
        }
        if (!AttrezzaturaMinima()) return false;
        inPesca = true;
        Alba();
        SegnaCampo(Game.Player.Character);
        MettiCampo();
        Avviso("~g~Buona pesca.");
        SalvaStato();
        RiscriviTutto();
        return true;
    }


    // IL PREZZO DELLA LICENZA: paghi per quello che puoi pescare. Un'acqua
    // con piu' tratti ha una riga per ogni livello di tratto (sesta
    // colonna): vale la riga col livello piu' alto che non supera il tuo;
    // sotto il primo tratto vale la piu' bassa. La regola dei prezzi sta
    // in testa a licenze.txt.
    int PrezzoLicenza(string zona, int giorni)
    {
        string[] rows = LeggiRighe("licenze.txt");
        int i;
        int prezzo = 0, livTrov = -1, prezzoMin = 0, livMin = 9999;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 5) continue;
            if (c[0].Trim() != zona) continue;
            if (c[3].Trim() != (giorni + "g")) continue;
            int liv = (c.Length > 5) ? Numero(c[5]) : 0;
            int pr = Numero(c[4]);
            if (liv < livMin) { livMin = liv; prezzoMin = pr; }
            if (liv <= livelloPescatore && liv > livTrov) { livTrov = liv; prezzo = pr; }
        }
        return (livTrov >= 0) ? prezzo : prezzoMin;
    }

    // Porta l'orologio alle cinque e rallenta il tempo. Si usa lo stesso
    // sistema del trainer (voce "velocita' del tempo"): si ferma l'orologio
    // del gioco e poi gli si aggiunge un minuto ogni tot millisecondi.
    // La nativa che cambia direttamente la velocita' su Enhanced non c'e'.
    const int MS_PER_MINUTO = 5000;   // 5 secondi veri = un minuto di gioco
    bool orologioPreso = false;
    int prossimoMinuto = 0;

    void Alba()
    {
        try
        {
            Function.Call(Hash.SET_CLOCK_TIME, 5, 0, 0);
            Function.Call(Hash.PAUSE_CLOCK, true);
            orologioPreso = true;
            prossimoMinuto = Game.GameTime + MS_PER_MINUTO;
            oraPrec = 5;
            oreFatte = 0;
            minutiFatti = 0;
            // l'ora ce la teniamo NOI: vedi MuoviOrologio
            oraMia = 5;
            minutoMio = 0;
        }
        catch { }
    }

    void TempoNormale()
    {
        if (!orologioPreso) return;
        try { Function.Call(Hash.PAUSE_CLOCK, false); }
        catch { }
        orologioPreso = false;
    }

    // L'ORA LA TENIAMO NOI.
    // PAUSE_CLOCK da solo non basta: l'orologio del gioco ogni tanto
    // riparte - lo rimette in moto il trainer, o una missione, o il
    // gioco stesso - e allora i minuti che aggiungiamo noi si SOMMANO
    // ai suoi: la giornata che doveva durare due ore vola via in mezz'ora.
    // Percio' l'ora giusta la teniamo in due numeri nostri e la
    // riscriviamo a ogni giro con SET_CLOCK_TIME: qualunque cosa la
    // tocchi, un attimo dopo torna quella che diciamo noi.
    int oraMia = 5, minutoMio = 0;

    void MuoviOrologio()
    {
        if (!orologioPreso) return;
        if (!inPesca) { TempoNormale(); return; }
        // dopo una pausa lunga non si recupera il tempo perso
        if (Game.GameTime - prossimoMinuto > 10000)
        {
            prossimoMinuto = Game.GameTime + MS_PER_MINUTO;
            return;
        }
        while (Game.GameTime >= prossimoMinuto)
        {
            minutoMio++;
            if (minutoMio >= 60) { minutoMio = 0; oraMia++; }
            if (oraMia >= 24) oraMia = 0;
            prossimoMinuto += MS_PER_MINUTO;
            minutiFatti++;
        }
        try
        {
            Function.Call(Hash.SET_CLOCK_TIME, oraMia, minutoMio, 0);
            Function.Call(Hash.PAUSE_CLOCK, true);
        }
        catch { }
    }

    bool FinePesca(bool avvisa)
    {
        if (!inPesca) return false;
        // se avevi un torneo in corso finisce qui, e senza premio: sei
        // andato a casa prima del tempo
        if (torneoOra >= 0) ChiudiTorneo(true);
        VendiNassa();
        inPesca = false;
        licZona = "";
        licGiorni = 0;
        ViaCampo();
        campoMesso = false;
        // REGOLA: a fine giornata si smonta tutto, torna tutto in borsa
        DisarmaTutto();
        TempoNormale();
        if (avvisa) Avviso("~y~Giornata finita. Si torna a casa.");
        return true;
    }

    // la giornata di pesca finisce alle 21
    // ============================================================
    //  IL MENU NUOVO - la pausa nostra
    // ============================================================
    // RB + SINISTRA della croce apre (e chiude) il menu nuovo, che si
    // costruisce in parallelo al trainer. Aperto: il tempo del mondo va a
    // zero (Game.TimeScale, il trucco dei trainer: il gioco si congela ma
    // gli script disegnano), l'audio del mondo si abbassa, tutti i comandi
    // sono spenti tranne i nostri, e l'orologio della pesca si ferma.
    // B chiude. Per ora dentro c'e' solo il velo e il titolo.
    bool menuNuovoAperto = false;
    int menuNuovoPausaDa = 0;
    int menuNuovoTasto = 0;


    // COL TEMPO A ZERO IL TIMER DEL GIOCO STA FERMO: per i tasti del menu
    // si usa l'orologio del PC, se no l'antirimbalzo non scade mai.
    static int OraPc() { return Environment.TickCount; }

    bool MenuNuovo()
    {
        if (LeggiF("menu_nuovo", 1f) < 0.5f) return false;
        int now = OraPc();
        bool rb = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 44)
               || Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 0, 44);
        bool sx = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 174)
               || Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 174);
        bool combo = rb && sx && now > menuNuovoTasto;
        if (!menuNuovoAperto)
        {
            if (!combo) return false;
            menuNuovoAperto = true;
            menuNuovoTasto = now + 400;
            menuNuovoPausaDa = Game.GameTime;
            // GTA IN MUTO dal mixer di Windows (le scene audio del gioco non
            // spegnevano il mondo, e col tempo a zero il suono restava
            // gelato), il tempo a zero subito, e i suoni del menu sono
            // file nostri che passano dalla sessione di sistema di Windows.
            AbbassaAudio();
            try { Game.TimeScale = 0f; } catch { }
            SuonoMenu("menu_apri.wav");
            // LO SFONDO SFOCATO: col tempo a zero la transizione di GTA non
            // parte (e restava da sfocare all'uscita), quindi si usa il
            // timecycle della pausa, che e' immediato. menu_blur=0 lo toglie.
            if (LeggiF("menu_blur", 1f) > 0.5f)
            {
                try
                {
                    Function.Call(Hash.SET_TIMECYCLE_MODIFIER, LeggiS("menu_blur_tc", "hud_def_blur"));
                    Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, LeggiF("menu_blur_forza", 1f));
                }
                catch { }
            }
            return true;
        }
        // aperto: niente comandi al gioco
        Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);

        bool b = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 202);
        if (b && menuLato > 0 && now > menuNuovoTasto)
        {
            menuLato--; menuNuovoTasto = now + 150; TicMenu("BACK");
            b = false;
        }
        if (combo || b)
        {
            ChiudiMenuNuovo();
            return true;
        }
        DisegnaMenuNuovo();
        return true;
    }

    void ChiudiMenuNuovo()
    {
        if (!menuNuovoAperto) return;
        menuNuovoAperto = false;
        menuNuovoTasto = OraPc() + 400;
        SuonoMenu("menu_chiudi.wav");
        try { Game.TimeScale = 1f; } catch { }
        RialzaAudio();
        ViaSfocatura();
        // l'orologio della pesca non deve aver contato il tempo in pausa
        prossimoMinuto += Game.GameTime - menuNuovoPausaDa;
    }

    // L'AUDIO DEL GIOCO SI AMMUTOLISCE DAL MIXER DI WINDOWS: e' la sessione
    // audio del processo di GTA (quella che vedi nel mixer del volume),
    // messa in muto e rimessa com'era. Non tocca le opzioni del gioco.
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumeratorCom { }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        int Activate(ref Guid iid, int clsCtx, IntPtr activationParams,
                     [MarshalAs(UnmanagedType.IUnknown)] out object iface);
    }

    [Guid("BFA971F1-4D5E-40BB-935E-967039BFBEE4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionManager
    {
        int GetAudioSessionControl(IntPtr sessionGuid, int streamFlags, out IntPtr sessionControl);
        int GetSimpleAudioVolume(IntPtr sessionGuid, int streamFlags, out ISimpleAudioVolume volume);
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISimpleAudioVolume
    {
        int SetMasterVolume(float level, ref Guid eventContext);
        int GetMasterVolume(out float level);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid eventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }

    // I SUONI DEL MENU passano dalla sessione "suoni di sistema" di Windows
    // (PlaySound con SND_SYSTEM): cosi' si sentono anche con GTA in muto.
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    static extern bool PlaySound(string nome, IntPtr modulo, uint flag);
    const uint SND_ASYNC = 0x0001, SND_FILENAME = 0x00020000, SND_SYSTEM = 0x00200000, SND_NODEFAULT = 0x0002;

    void SuonoMenu(string file)
    {
        try
        {
            string f = Path.Combine(Path.Combine(MY_DIR, "suoni"), file);
            if (!File.Exists(f)) return;
            PlaySound(f, IntPtr.Zero, SND_ASYNC | SND_FILENAME | SND_SYSTEM | SND_NODEFAULT);
        }
        catch { }
    }

    ISimpleAudioVolume audioSessione = null;
    bool audioEraMuto = false;
    bool audioAbbassato = false;

    ISimpleAudioVolume SessioneAudio()
    {
        if (audioSessione != null) return audioSessione;
        try
        {
            IMMDeviceEnumerator en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
            IMMDevice dev;
            if (en.GetDefaultAudioEndpoint(0, 0, out dev) != 0 || dev == null) return null;
            Guid iid = new Guid("BFA971F1-4D5E-40BB-935E-967039BFBEE4");
            object o;
            if (dev.Activate(ref iid, 23, IntPtr.Zero, out o) != 0 || o == null) return null;
            IAudioSessionManager man = (IAudioSessionManager)o;
            ISimpleAudioVolume vol;
            if (man.GetSimpleAudioVolume(IntPtr.Zero, 0, out vol) != 0) return null;
            audioSessione = vol;
        }
        catch { audioSessione = null; }
        return audioSessione;
    }

    void AbbassaAudio()
    {
        if (audioAbbassato) return;
        try
        {
            ISimpleAudioVolume v = SessioneAudio();
            if (v == null) return;
            bool m;
            v.GetMute(out m);
            audioEraMuto = m;
            Guid g = Guid.Empty;
            v.SetMute(true, ref g);
            audioAbbassato = true;
        }
        catch { }
    }

    void RialzaAudio()
    {
        if (!audioAbbassato) return;
        try
        {
            ISimpleAudioVolume v = SessioneAudio();
            if (v != null) { Guid g = Guid.Empty; v.SetMute(audioEraMuto, ref g); }
        }
        catch { }
        audioAbbassato = false;
    }

    // un colore "r,g,b" dal config, con il suo valore di partenza
    int[] ColoreCfg(string chiave, int r, int g, int b)
    {
        int[] c = new int[] { r, g, b };
        string v = LeggiS(chiave, "");
        if (v.Length == 0) return c;
        string[] pz = v.Split(',');
        if (pz.Length < 3) return c;
        int x;
        if (int.TryParse(pz[0].Trim(), out x)) c[0] = x;
        if (int.TryParse(pz[1].Trim(), out x)) c[1] = x;
        if (int.TryParse(pz[2].Trim(), out x)) c[2] = x;
        return c;
    }

    // via ogni sfocatura, anche quella rimasta da una versione precedente
    void ViaSfocatura()
    {
        try { Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER); } catch { }
        try { Function.Call((Hash)0xEFACC8AEF94430D5, 0f); } catch { }   // TRANSITION_FROM_BLURRED, subito
    }

    // LE SCHEDE del menu nuovo, come quelle della pausa di GTA: LB e RB
    // le cambiano. Il contenuto arriva scheda per scheda.
    static readonly string[] SCHEDE_IT = { "ZONE", "EQUIPAGGIAMENTO", "NEGOZIO", "PESCI", "TORNEI", "IMPOSTAZIONI" };
    static readonly string[] SCHEDE_EN = { "SPOTS", "TACKLE", "SHOP", "FISH", "TOURNAMENTS", "SETTINGS" };
    int menuScheda = 0;

    // il tic del menu: un file nostro nella sessione di sistema di Windows
    void TicMenu(string nome)
    {
        SuonoMenu("menu_tic.wav");
    }

    // LA SIDEBAR: la lista a sinistra dentro il contenitore, righe scure e
    // quella scelta bianca col testo scuro, come nella pausa di GTA. Si
    // scorre con la croce (o la levetta). Per ora sta solo nella prima
    // scheda, con le zone di pesca.
    List<string> sbVoci = new List<string>();
    List<string> sbDestra = new List<string>();
    List<int> sbArea = new List<int>();
    int sbSel = 0, sbTop = 0;
    int sbScheda = -1;
    int menuLato = 0;         // 0 sulla lista a sinistra, 1 sul pannello a destra

    void RiempiSidebar()
    {
        sbVoci.Clear(); sbDestra.Clear(); sbArea.Clear();
        if (menuScheda == 0)
        {
            // le zone, nell'ordine della pagina del trainer (per livello)
            List<int> ord = new List<int>();
            int i, k;
            for (i = 0; i < arNome.Count; i++) ord.Add(i);
            for (i = 1; i < ord.Count; i++)
            {
                int t = ord[i]; int l1 = LivelloArea(t); k = i - 1;
                while (k >= 0 && LivelloArea(ord[k]) > l1) { ord[k + 1] = ord[k]; k--; }
                ord[k + 1] = t;
            }
            for (i = 0; i < ord.Count; i++)
            {
                sbVoci.Add(arNome[ord[i]]);
                sbDestra.Add(L("Lv.", "Liv.") + LivelloArea(ord[i]));
                sbArea.Add(ord[i]);
            }
        }
        if (menuScheda == 1)
        {
            // solo la roba che si sposta tra casa e cassetta; portacanne e
            // cassette sono fissi e stanno sotto, col loro banner
            int q;
            for (q = 0; q < EQ_ORD.Length; q++)
            {
                int ic = EQ_ORD[q];
                sbVoci.Add(CAT_NOME[ic]);
                sbDestra.Add("");
                sbArea.Add(ic);
            }
        }
        if (sbSel >= sbVoci.Count) sbSel = 0;
        sbTop = 0;
        sbScheda = menuScheda;
    }

    // L'EQUIPAGGIAMENTO: a destra della lista due colonne, quello che hai
    // a casa e quello che hai nella cassetta, per la categoria scelta.
    List<string> eqCasa = new List<string>();
    List<string> eqBorsa = new List<string>();
    int eqCat = -1, eqQuando = 0;
    int eqSelCasa = 0, eqTopCasa = 0, eqSelBorsa = 0, eqTopBorsa = 0;

    void RiempiEquip(int ic)
    {
        eqCasa.Clear(); eqBorsa.Clear();
        foreach (KeyValuePair<string, int> kv in magazzino)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length < 2 || c[0] != CAT_COD[ic] || kv.Value <= 0) continue;
            eqCasa.Add(kv.Key);
        }
        foreach (KeyValuePair<string, int> kv in borsa)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length < 2 || c[0] != CAT_COD[ic] || kv.Value <= 0) continue;
            eqBorsa.Add(kv.Key);
        }
        eqCasa.Sort(); eqBorsa.Sort();
        if (eqCat != ic) { eqSelCasa = 0; eqTopCasa = 0; eqSelBorsa = 0; eqTopBorsa = 0; }
        if (eqSelCasa >= eqCasa.Count) eqSelCasa = eqCasa.Count > 0 ? eqCasa.Count - 1 : 0;
        if (eqSelBorsa >= eqBorsa.Count) eqSelBorsa = eqBorsa.Count > 0 ? eqBorsa.Count - 1 : 0;
        eqCat = ic;
        eqQuando = OraPc();
    }

    // canne, mulinelli, lenze, ami, esche, artificiali, galleggianti, nasse
    static readonly int[] EQ_ORD = new int[] { 0, 1, 2, 3, 6, 5, 4, 7 };

    // il pezzo fisso che possiedi (casa o cassetta): chiave "cat:id" o ""
    string PossiedoFisso(string cat)
    {
        foreach (KeyValuePair<string, int> kv in borsa)
            if (kv.Value > 0 && kv.Key.StartsWith(cat + ":")) return kv.Key;
        foreach (KeyValuePair<string, int> kv in magazzino)
            if (kv.Value > 0 && kv.Key.StartsWith(cat + ":")) return kv.Key;
        return "";
    }

    // il banner di un pezzo fisso: l'immagine grande a sinistra, nome e
    // dati a destra. Se non ce l'hai, "nessuno".
    void BannerFisso(string cat, float x, float y, float w, float h)
    {
        DisegnaRett(x, y, w, h, 0, 0, 0, 120);
        // il riquadro dell'immagine ha sempre la stessa misura, cosi' il
        // testo parte sempre dallo stesso punto
        float ih = h - 8f, iw = ih * LeggiF("menu_eq_img_rapporto", 1.6f);
        float tx = x + 4f + iw + 12f;
        string chiave = PossiedoFisso(cat);
        if (chiave.Length == 0)
        {
            // senza cassetta c'e' lo zaino, che hai da sempre
            if (cat == "cassetta")
            {
                Sprite("img/cassette/Base.png", x + 4f, y + 4f, iw, ih);
                TestoMenu(L("Backpack", "Zaino"), tx, y + 6f, 0.26f, 0, 0, 245, 245, 250, 255);
                TestoMenu(ZAINO_ROBA + " oggetti   " + ZAINO_LENZE + " lenze", tx, y + 6f + 18f,
                          0.22f, 0, 0, 200, 202, 210, 255);
            }
            else
                TestoMenu(L("none", "nessuno"), x + 10f, y + h * 0.5f - 9f, 0.26f, 0, 0, 200, 202, 210, 255);
            return;
        }
        string[] c = chiave.Split(':');
        int id = Numero(c[1]);
        string nome, img; int prezzo, liv;
        if (!Articolo(cat, id, out nome, out img, out prezzo, out liv)) return;
        if (img.Length > 0) Sprite(img, x + 4f, y + 4f, iw, ih);
        TestoMenu(nome, tx, y + 6f, 0.26f, 0, 0, 245, 245, 250, 255);
        TestoMenu(Dettaglio(cat, id), tx, y + 6f + 18f, 0.22f, 0, 0, 200, 202, 210, 255);
    }

    void DisegnaSidebarEquip(float mx, float cy, float ch)
    {
        if (sbScheda != menuScheda) RiempiSidebar();
        float w = LeggiF("menu_sb_larga", 260f);
        float rh = LeggiF("menu_sb_riga", 26f);
        float pad = LeggiF("menu_sb_bordo", 8f);
        float bh = LeggiF("menu_eq_banner", 80f);
        float x = mx + pad;
        float y = cy + pad;
        y = SezioneColonna(L("ITEMS YOU CAN MOVE BETWEEN HOME AND TACKLE BOX",
                             "OGGETTI CHE PUOI SPOSTARE TRA CASA E CASSETTA"), x, y, w);
        int k;
        for (k = 0; k < sbVoci.Count; k++)
        {
            bool sel = (k == sbSel);
            if (sel) DisegnaRett(x, y, w, rh - 2f, 245, 245, 250, menuLato == 0 ? 255 : 170);
            else DisegnaRett(x, y, w, rh - 2f, 0, 0, 0, 120);
            int r = sel ? 20 : 245, g = sel ? 22 : 245, b = sel ? 28 : 250;
            TestoMenu(sbVoci[k], x + 10f, y + rh * 0.5f - 9f, 0.28f, 0, 0, r, g, b, 255);
            y += rh;
        }
        y += rh * 0.5f;
        y = SezioneColonna(L("FIXED ITEMS FOR EXPANSION", "OGGETTI FISSI PER L'ESPANSIONE"), x, y, w);
        DisegnaRett(x, y, w, rh - 2f, 0, 0, 0, 120);
        TestoMenu(CAT_NOME[9], x + 10f, y + rh * 0.5f - 9f, 0.28f, 0, 0, 245, 245, 250, 255);
        y += rh;
        BannerFisso("portacanne", x, y, w, bh);
        y += bh + 4f;
        DisegnaRett(x, y, w, rh - 2f, 0, 0, 0, 120);
        TestoMenu(CAT_NOME[8], x + 10f, y + rh * 0.5f - 9f, 0.28f, 0, 0, 245, 245, 250, 255);
        y += rh;
        BannerFisso("cassetta", x, y, w, bh);
    }

    void ColonnaEquip(List<string> lista, ref int sel, ref int top, bool attiva,
                      string titolo, string pie, float x, float y0, float w, float fondo)
    {
        float rh = LeggiF("menu_eq_riga", 26f);
        float y = SezioneColonna(titolo, x, y0, w);
        // in fondo la stessa riga del titolo, coi posti: la lista sta in mezzo
        float py = fondo - 20f;
        DisegnaRett(x, py, w, 20f, 255, 255, 255, 18);
        TestoMenu(pie, x + 8f, py + 3f, LeggiF("menu_eq_cont_testo", 0.22f), 0, 0, 200, 202, 210, 255);
        fondo = py - 4f;
        int righe = (int)((fondo - y) / rh);
        if (righe < 1) righe = 1;
        if (sel < top) top = sel;
        if (sel >= top + righe) top = sel - righe + 1;
        int i;
        for (i = 0; i < righe && top + i < lista.Count; i++)
        {
            string[] c = lista[top + i].Split(':');
            int id = Numero(c[1]);
            string nome, img; int prezzo, liv;
            if (!Articolo(c[0], id, out nome, out img, out prezzo, out liv)) { nome = lista[top + i]; img = ""; }
            int quante = 0;
            Dictionary<string, int> da = (lista == eqBorsa) ? borsa : magazzino;
            da.TryGetValue(lista[top + i], out quante);
            float ry = y + i * rh;
            bool s = attiva && top + i == sel;
            if (s) DisegnaRett(x, ry, w, rh - 2f, 245, 245, 250, 255);
            else DisegnaRett(x, ry, w, rh - 2f, 0, 0, 0, 120);
            int r = s ? 20 : 245, g = s ? 22 : 245, b = s ? 28 : 250;
            // l'immagine a sinistra, in un riquadro sempre uguale
            float ih = rh - 8f, iw = ih * LeggiF("menu_eq_img_rapporto", 1.6f);
            if (img != null && img.Length > 0) Sprite(img, x + 4f, ry + 3f, iw, ih);
            TestoMenu(nome, x + 4f + iw + 10f, ry + rh * 0.5f - 9f, 0.28f, 0, 0, r, g, b, 255);
            TestoMenu(Quantita(c[0], id, quante, lista == eqBorsa), x + w - 10f, ry + rh * 0.5f - 9f,
                      0.26f, 0, 2, s ? 60 : 245, s ? 62 : 205, s ? 70 : 80, 255);
        }
    }

    void DisegnaColonneEquip(float px, float py, float pw, float ph)
    {
        if (sbSel < 0 || sbSel >= sbArea.Count) return;
        int ic = sbArea[sbSel];
        if (eqCat != ic || OraPc() - eqQuando > 500) RiempiEquip(ic);
        float pad = LeggiF("menu_pn_bordo", 10f);
        float w = (pw - pad * 3f) * 0.5f;
        float fondo = py + ph - pad;
        ColonnaEquip(eqCasa, ref eqSelCasa, ref eqTopCasa, menuLato == 1,
                     L("AT HOME", "A CASA"), L("Unlimited space", "Spazio illimitato"),
                     px + pad, py + pad, w, fondo);
        // il titolo della colonna e' il nome di quello che porti: lo zaino
        // finche' non compri una cassetta, poi il nome della cassetta
        string tit = L("BACKPACK", "ZAINO");
        string kc = PossiedoFisso("cassetta");
        if (kc.Length > 0)
        {
            string nome, img; int prezzo, liv;
            if (Articolo("cassetta", Numero(kc.Split(':')[1]), out nome, out img, out prezzo, out liv))
                tit = nome.ToUpper();
        }
        ColonnaEquip(eqBorsa, ref eqSelBorsa, ref eqTopBorsa, menuLato == 2,
                     tit, Contatori(), px + pad * 2f + w, py + pad, w, fondo);
    }

    void DisegnaSidebar(float mx, float cy, float ch)
    {
        if (sbScheda != menuScheda) RiempiSidebar();
        float w = LeggiF("menu_sb_larga", 260f);
        float rh = LeggiF("menu_sb_riga", 26f);
        float pad = LeggiF("menu_sb_bordo", 8f);
        int righe = (int)((ch - pad * 2f) / rh);
        if (righe < 1) righe = 1;
        if (sbSel < sbTop) sbTop = sbSel;
        if (sbSel >= sbTop + righe) sbTop = sbSel - righe + 1;
        int i;
        for (i = 0; i < righe && sbTop + i < sbVoci.Count; i++)
        {
            int k = sbTop + i;
            float y = cy + pad + i * rh;
            bool sel = (k == sbSel);
            if (sel) DisegnaRett(mx + pad, y, w, rh - 2f, 245, 245, 250, menuLato == 0 ? 255 : 170);
            else DisegnaRett(mx + pad, y, w, rh - 2f, 0, 0, 0, 120);
            int r = sel ? 20 : 245, g = sel ? 22 : 245, b = sel ? 28 : 250;
            TestoMenu(sbVoci[k], mx + pad + 10f, y + rh * 0.5f - 9f, 0.28f, 0, 0, r, g, b, 255);
            if (sbDestra[k].Length > 0)
                TestoMenu(sbDestra[k], mx + pad + w - 10f, y + rh * 0.5f - 9f, 0.26f, 0, 2,
                          sel ? 60 : 245, sel ? 62 : 205, sel ? 70 : 80, 255);
        }
    }

    // IL PANNELLO DELLA ZONA, a destra della lista: il banner, due righe di
    // dati, i pesci che ci vivono con la loro foto, e in fondo il tasto
    // "Raggiungi il posto". Con DESTRA ci vai sopra, con SINISTRA torni.
    // il posto dove sei, per il menu: ricalcolato ogni mezzo secondo
    int luogoMenu = -1;
    int luogoMenuQuando = 0;
    int LuogoQuiMenu()
    {
        int now = OraPc();
        if (now - luogoMenuQuando > 500) { luogoMenuQuando = now; luogoMenu = LuogoQui(); }
        return luogoMenu;
    }

    int pnSel = 0;       // la riga scelta nella colonna (licenze, poi il tasto)
    int pnRighe = 0;     // quante righe di licenza ci sono adesso

    void RigaLicenza(float x, float cw2, float y, string testo, int prezzo, int k)
    {
        bool sel = (menuLato == 1 && pnSel == k);
        if (sel)
        {
            DisegnaRett(x + 6f, y - 3f, cw2 - 12f, 18f, 245, 245, 250, 255);
            TestoMenu(testo, x + 12f, y, 0.24f, 0, 0, 20, 22, 28, 255);
            TestoMenu("$" + prezzo, x + cw2 - 12f, y, 0.24f, 0, 2, 20, 22, 28, 255);
        }
        else
        {
            TestoMenu(testo, x + 12f, y, 0.24f, 0, 0, 200, 202, 210, 255);
            TestoMenu("$" + prezzo, x + cw2 - 12f, y, 0.24f, 0, 2, 130, 225, 180, 255);
        }
    }

    // una sezione della colonna: la riga scura col titolo in maiuscolo
    float SezioneColonna(string titolo, float x, float y, float w)
    {
        DisegnaRett(x, y, w, 20f, 255, 255, 255, 18);
        TestoMenu(titolo, x + 8f, y + 2f, 0.26f, 4, 0, 245, 245, 250, 255);
        return y + 24f;
    }

    void DisegnaPannelloZona(float px, float py, float pw, float ph)
    {
        if (sbSel < 0 || sbSel >= sbArea.Count) return;
        int a = sbArea[sbSel];
        float pad = LeggiF("menu_pn_bordo", 10f);
        // LA COLONNA DEL POSTO, sullo stile della scheda di Fishing Planet:
        // banner, ora e giornata, previsioni con la curva dell'attivita',
        // le licenze, e in fondo il tasto per raggiungere il posto.
        float cw2 = LeggiF("menu_pn2_larga", 200f);
        float x = px + pad, y = py + pad;
        float fondo = py + ph - pad;
        DisegnaRett(x, y, cw2, fondo - y, 0, 0, 0, 120);
        // il banner
        float bh = LeggiF("menu_pn2_banner_alto", 70f);
        string ban = BannerArea(a);
        if (ban.Length > 0)
        {
            float ih = cw2 * 191f / 630f;
            if (ih > bh) ih = bh;
            Sprite(ban, x, y, cw2, ih);
            y += ih;
        }
        int quanti = (a < arPesci.Count) ? arPesci[a].Count : 0;
        bool aperta = (livelloPescatore >= LivelloArea(a));
        float tx2 = x + 8f;
        // il nome e i dati
        y += 6f;
        TestoMenu(EntraMenu(arNome[a], 0.40f, 4, cw2 - 16f), tx2, y, 0.40f, 4, 0, 245, 245, 250, 255); y += 24f;
        TestoMenu(arTipo[a] + "   " + quanti + " " + L("species", "specie"), tx2, y, 0.24f, 0, 0, 200, 202, 210, 255); y += 16f;
        if (aperta) TestoMenu(L("level ", "livello ") + LivelloArea(a), tx2, y, 0.24f, 0, 0, 245, 205, 80, 255);
        else TestoMenu(L("level ", "livello ") + LivelloArea(a) + "   " + L("closed for you", "per te e' chiusa"), tx2, y, 0.24f, 0, 0, 235, 90, 80, 255);
        y += 22f;
        // l'ora e la giornata
        int hh = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        int mi = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
        TestoMenu(hh.ToString("00") + ":" + mi.ToString("00"), tx2, y, 0.34f, 4, 0, 245, 245, 250, 255);
        if (inPesca && licGiorni > 0 && licZona == CodiceLuogo(a))
            TestoMenu(L("License ", "Licenza ") + TempoCheResta(), x + cw2 - 8f, y + 4f, 0.24f, 0, 2, 200, 202, 210, 255);
        y += 26f;

        // LE PREVISIONI: meteo e gradi, e la curva dell'attivita' del posto
        y = SezioneColonna(L("FORECAST", "PREVISIONI"), x, y, cw2);
        float ic = 18f;
        Sprite("img\\hud\\meteo\\" + IconaMeteoHud() + ".png", tx2, y, ic, ic);
        TestoMenu(GradiAria().ToString("0", CultureInfo.InvariantCulture) + "\u00B0C", tx2 + ic + 6f, y - 2f, 0.30f, 0, 0, 245, 245, 250, 255);
        Sprite("img\\hud\\meteo\\acqua.png", tx2 + 90f, y, ic, ic);
        TestoMenu(GradiAcqua().ToString("0", CultureInfo.InvariantCulture) + "\u00B0C", tx2 + 90f + ic + 6f, y - 2f, 0.30f, 0, 0, 245, 245, 250, 255);
        y += 26f;
        float gw = cw2 - 16f, ga = LeggiF("menu_pn2_grafico_alto", 46f);
        string png = "img\\attivita\\" + a + "_" + ClasseMeteo() + ".png";
        if (File.Exists(Path.Combine(MY_DIR, png))) Sprite(png, tx2, y, gw, ga);
        float basso = y + ga;
        DisegnaRett(tx2, basso, gw, 1f, 255, 255, 255, 120);
        int k;
        for (k = 0; k <= 24; k += 6)
        {
            float xk = tx2 + gw * k / 24f;
            DisegnaRett(xk, basso, 1f, 3f, 255, 255, 255, 120);
            DisegnaTesto("" + ((5 + k) % 24), xk, basso + 3f, 0.17f, 200, 202, 210);
        }
        // la riga dell'ora di adesso
        float oraOra = hh + mi / 60f - 5f;
        while (oraOra < 0f) oraOra += 24f;
        DisegnaRett(tx2 + gw * oraOra / 24f, y, 1f, ga, 255, 255, 255, 200);
        y = basso + 22f;

        // LE LICENZE del posto: due righe che si comprano con A (quando
        // sei sulla colonna, la croce sceglie la riga)
        y = SezioneColonna(L("LICENSES", "LICENZE"), x, y, cw2);
        string cod = CodiceLuogo(a);
        int p1 = PrezzoLicenza(cod, 1), p3 = PrezzoLicenza(cod, 3);
        pnRighe = 0;
        if (inPesca && licZona == cod)
            TestoMenu(L("Active - ", "Attiva - ") + TempoCheResta(), tx2, y, 0.24f, 0, 0, 150, 235, 180, 255);
        else if (LicenzaInTasca(cod))
            TestoMenu(L("Bought: ", "Comprata: ") + licGiorni + (licGiorni == 1 ? L(" day", " giorno") : L(" days", " giorni")),
                      tx2, y, 0.24f, 0, 0, 150, 235, 180, 255);
        else if (p1 > 0)
        {
            RigaLicenza(x, cw2, y, L("Buy 1 day", "Acquista 1 giorno"), p1, 0);
            y += 20f;
            RigaLicenza(x, cw2, y, L("Buy 3 days", "Acquista 3 giorni"), p3, 1);
            pnRighe = 2;
        }
        else TestoMenu(L("No license needed", "Senza licenza"), tx2, y, 0.24f, 0, 0, 200, 202, 210, 255);

        // IL TASTO IN FONDO, a stati: lontano "Raggiungi il posto"; sul posto
        // "Ti trovi qui"; sul posto con la licenza in tasca "Inizia a
        // pescare"; con la giornata in corso "Stai pescando".
        float by = fondo - 34f;
        // dove sei adesso, non dove eri quando e' stata scritta la pagina
        bool qui = (LuogoQuiMenu() == a);
        // ogni stato ha il suo colore: verdolino per raggiungere, rosa
        // quando sei qui, arancione (quello di Fishing Planet) per iniziare
        string tb; bool attivo; int br, bg2, bb;
        if (!qui) { tb = L("Get to the spot", "Raggiungi il posto"); attivo = true; br = 130; bg2 = 225; bb = 180; }
        else if (inPesca && licZona == cod) { tb = L("Fishing", "Stai pescando"); attivo = false; br = 245; bg2 = 140; bb = 40; }
        else if (LicenzaInTasca(cod)) { tb = L("Start fishing", "Inizia a pescare"); attivo = true; br = 245; bg2 = 140; bb = 40; }
        else { tb = L("You are here", "Ti trovi qui"); attivo = false; br = 250; bg2 = 175; bb = 205; }
        bool selB = (menuLato == 1 && pnSel == pnRighe);
        if (attivo)
        {
            // pieno del suo colore, piu' acceso quando e' scelto
            DisegnaRett(x + 6f, by, cw2 - 12f, 26f, br, bg2, bb, selB ? 255 : 150);
            TestoMenu(tb, x + cw2 * 0.5f, by + 4f, 0.28f, 0, 1, 20, 22, 28, 255);
        }
        else
        {
            // spento: solo il bordo del colore e il testo colorato
            DisegnaRett(x + 6f, by, cw2 - 12f, 26f, br, bg2, bb, selB ? 60 : 35);
            TestoMenu(tb, x + cw2 * 0.5f, by + 4f, 0.28f, 0, 1, br, bg2, bb, 255);
        }

        // I PESCI: la colonna dei pesci (foto, nome, spunte comune/trofeo/
        // unico) e a destra la scheda del pesce scelto
        DisegnaColonnaPesci(a, x + cw2 + pad, py + pad, fondo, px + pw - pad);
    }

    // LA COLONNA DEI PESCI del posto, come in Fishing Planet: ogni riga ha
    // la foto, il nome e sotto le tre spunte (comune, trofeo, unico) che si
    // accendono quando l'hai preso. Si scorre con la croce; a destra la
    // scheda del pesce su cui stai.
    List<int> pcPesci = new List<int>();
    int pcArea = -1, pcSel = 0, pcTop = 0;

    void RiempiColonnaPesci(int a)
    {
        pcPesci.Clear();
        int i;
        for (i = 0; i < pesci.Count; i++)
            if (a < arPesci.Count && arPesci[a].Contains(pesci[i].Nome)) pcPesci.Add(i);
        pcArea = a; pcSel = 0; pcTop = 0;
    }

    // che taglia hai gia' preso di questa specie: 0 mai, 1 comune, 2 trofeo, 3 unico
    int TagliaPresa(Specie sp)
    {
        float kg;
        if (!record.TryGetValue(sp.Nome, out kg)) return 0;
        if (sp.KgU > 0f && kg >= sp.KgU) return 3;
        if (sp.KgT > 0f && kg >= sp.KgT) return 2;
        return 1;
    }

    void Spunta(float x, float y, bool si, string testo, int r, int g, int b)
    {
        // le tre taglie sono sempre accese: il quadratino e' vuoto, e
        // quando l'hai presa ci compare una X
        float q = 11f;
        DisegnaRett(x, y + 2f, q, q, r, g, b, 60);
        DisegnaRett(x, y + 2f, q, 1f, r, g, b, 255);
        DisegnaRett(x, y + 2f + q - 1f, q, 1f, r, g, b, 255);
        DisegnaRett(x, y + 2f, 1f, q, r, g, b, 255);
        DisegnaRett(x + q - 1f, y + 2f, 1f, q, r, g, b, 255);
        if (si) TestoMenu("X", x + q * 0.5f + LeggiF("menu_x_dx", 0f), y - 2f + LeggiF("menu_x_giu", 3f), 0.24f, 0, 1, r, g, b, 255);
        TestoMenu(testo, x + q + 4f, y + LeggiF("menu_spunta_giu", 2f), 0.19f, 0, 0, r, g, b, 255);
    }

    void DisegnaColonnaPesci(int a, float x, float y0, float fondo, float xFine)
    {
        if (pcArea != a) RiempiColonnaPesci(a);
        float cw = LeggiF("menu_pc_larga", 200f);
        float rh = LeggiF("menu_pc_riga", 92f);
        DisegnaRett(x, y0, cw, fondo - y0, 0, 0, 0, 120);
        int righe = (int)((fondo - y0) / rh);
        if (righe < 1) righe = 1;
        if (pcSel < pcTop) pcTop = pcSel;
        if (pcSel >= pcTop + righe) pcTop = pcSel - righe + 1;
        int i;
        for (i = 0; i < righe && pcTop + i < pcPesci.Count; i++)
        {
            Specie sp = pesci[pcPesci[pcTop + i]];
            float y = y0 + i * rh;
            bool sel = (menuLato == 2 && pcTop + i == pcSel);
            if (sel) DisegnaRett(x, y, cw, rh - 2f, 255, 255, 255, 40);
            // senza il nome: sta gia' nella scheda a destra
            float ih = rh - 30f, iw = ih * 1.6f;
            if (sp.Img.Length > 0) Sprite(sp.Img, x + (cw - iw) * 0.5f, y + 4f, iw, ih);
            int presa = TagliaPresa(sp);
            float sy2 = y + rh - 18f;
            Spunta(x + 8f, sy2, presa >= 1, L("Common", "Comune"), 200, 202, 210);
            Spunta(x + 72f, sy2, presa >= 2, L("Trophy", "Trofeo"), 130, 225, 180);
            Spunta(x + 134f, sy2, presa >= 3, L("Unique", "Unico"), 245, 205, 80);
            DisegnaRett(x, y + rh - 2f, cw, 1f, 255, 255, 255, 25);
        }
        // la scheda a destra
        if (pcSel >= 0 && pcSel < pcPesci.Count)
            DisegnaSchedaPesce(pesci[pcPesci[pcSel]], x + cw + 12f, y0, xFine, fondo);
    }

    string QuandoIt(string q)
    {
        if (q == "notte") return L("at night", "di notte");
        if (q == "alba_tramonto") return L("at dawn and dusk", "all'alba e al tramonto");
        if (q == "giorno") return L("by day", "di giorno");
        return L("all day", "tutto il giorno");
    }

    void DisegnaSchedaPesce(Specie sp, float x, float y, float xFine, float fondo)
    {
        float w = xFine - x;
        // il nome, e sotto in piccolo il nome inglese e la famiglia
        TestoMenu(NomeIt(sp.Nome).ToUpper(), x, y, 0.60f, 4, 0, 245, 245, 250, 255);
        y += 32f;
        string sotto = sp.Nome;
        if (sp.Famiglia.Length > 0) sotto += "   -   " + sp.Famiglia;
        TestoMenu(EntraMenu(sotto, 0.22f, 0, w), x, y, 0.22f, 0, 0, 150, 152, 160, 255);
        y += 26f;
        DisegnaRett(x, y, w, 1f, 255, 255, 255, 40);
        y += 12f;

        // LE TAGLIE: tre caselle coi colori delle spunte
        float cw3 = (w - 16f) / 3f;
        string[] et = { L("COMMON", "COMUNE"), L("TROPHY", "TROFEO"), L("UNIQUE", "UNICO") };
        float[] kg = { sp.KgC, sp.KgT, sp.KgU };
        int[] cr = { 200, 130, 245 }, cg = { 202, 225, 205 }, cb = { 210, 180, 80 };
        int presa = TagliaPresa(sp);
        int k;
        for (k = 0; k < 3; k++)
        {
            float cx = x + k * (cw3 + 8f);
            DisegnaRett(cx, y, cw3, 40f, cr[k], cg[k], cb[k], presa > k ? 60 : 20);
            TestoMenu(et[k], cx + 8f, y + 3f, 0.20f, 0, 0, cr[k], cg[k], cb[k], 255);
            TestoMenu(kg[k].ToString("0.##", CultureInfo.InvariantCulture) + " kg", cx + 8f, y + 17f, 0.30f, 4, 0, 245, 245, 250, 255);
            if (presa > k) TestoMenu(L("caught", "preso"), cx + cw3 - 8f, y + 20f, 0.19f, 0, 2, cr[k], cg[k], cb[k], 255);
        }
        y += 52f;

        // I DATI: etichetta grigia, valore bianco, su due colonne
        float c2 = x + w * 0.5f;
        // i colori di sempre: prezzo verde, amo azzurro, il resto bianco,
        // la temperatura gialla
        Dato(x, y, L("Price", "Prezzo"), "$" + sp.PrC + "/kg", 130, 225, 180);
        Dato(c2, y, L("Hook", "Amo"), sp.Amo, 130, 200, 245);
        y += 22f;
        Dato(x, y, L("Feeds", "Mangia"), QuandoIt(sp.Quando), 245, 245, 250);
        if (sp.TMin >= 0f)
            Dato(c2, y, L("Water", "Acqua"), sp.TMin.ToString("0") + "-" + sp.TMax.ToString("0") + "\u00B0C", 245, 205, 80);
        y += 22f;
        if (sp.Denti > 0)
        {
            TestoMenu(L("Teeth: leader needed", "Denti: serve il leader"), x, y, 0.24f, 0, 0, 235, 90, 80, 255);
            y += 22f;
        }
        y += 6f;
        DisegnaRett(x, y, w, 1f, 255, 255, 255, 40);
        y += 12f;

        // LE ESCHE PREFERITE, con la loro foto
        if (sp.Esche != null && sp.Esche.Length > 0)
        {
            TestoMenu(L("PREFERRED BAITS", "ESCHE PREFERITE"), x, y, 0.24f, 4, 0, 245, 205, 80, 255);
            y += 22f;
            float ie = LeggiF("menu_esca_img", 34f);
            int perRiga = (int)(w / (ie + 100f));
            if (perRiga < 1) perRiga = 1;
            float ew = w / perRiga;
            int i, n = 0;
            for (i = 0; i < sp.Esche.Length; i++)
            {
                for (k = 0; k < escheShop.Count; k++)
                    if (escheShop[k].Id == sp.Esche[i])
                    {
                        float ex = x + (n % perRiga) * ew;
                        float ey = y + (n / perRiga) * (ie + 6f);
                        if (ey + ie > fondo - 30f) break;
                        // Img e' gia' il percorso completo (img\esche\...)
                        if (escheShop[k].Img.Length > 0) Sprite(escheShop[k].Img, ex, ey, ie, ie);
                        // il nome non esce mai dalla sua casella: se e' lungo
                        // va a capo su due righe, non si accorcia
                        string r1, r2;
                        SpezzaMenu(EscaIt(escheShop[k].Nome), 0.23f, 0, ew - ie - 14f, out r1, out r2);
                        if (r2.Length == 0)
                            TestoMenu(r1, ex + ie + 6f, ey + ie * 0.5f - 8f, 0.23f, 0, 0, 200, 202, 210, 255);
                        else
                        {
                            TestoMenu(r1, ex + ie + 6f, ey + ie * 0.5f - 15f, 0.23f, 0, 0, 200, 202, 210, 255);
                            TestoMenu(r2, ex + ie + 6f, ey + ie * 0.5f - 1f, 0.23f, 0, 0, 200, 202, 210, 255);
                        }
                        n++;
                        break;
                    }
            }
            y += ((n + perRiga - 1) / perRiga) * (ie + 6f) + 10f;
        }
        // GLI ARTIFICIALI: solo i tipi
        if (sp.Art != null && sp.Art.Length > 0 && y < fondo - 30f)
        {
            TestoMenu(L("LURES", "ARTIFICIALI"), x, y, 0.24f, 4, 0, 130, 200, 245, 255);
            y += 22f;
            List<string> tipi = new List<string>();
            int i;
            for (i = 0; i < sp.Art.Length; i++)
                for (k = 0; k < artificiali.Count; k++)
                    if (artificiali[k].Id == sp.Art[i])
                    {
                        int t2 = Array.IndexOf(ART_COD, artificiali[k].Tipo);
                        string nt = (t2 >= 0) ? ART_NOME[t2] : artificiali[k].Tipo;
                        if (!tipi.Contains(nt)) tipi.Add(nt);
                        break;
                    }
            TestoMenu(string.Join(", ", tipi.ToArray()), x, y, 0.25f, 0, 0, 200, 202, 210, 255);
        }
    }

    // un testo su due righe: si spezza all'ultimo spazio che ci sta; se
    // nemmeno la seconda riga ci sta, quella si accorcia
    void SpezzaMenu(string t, float scala, int font, float maxW, out string r1, out string r2)
    {
        r1 = t; r2 = "";
        if (t == null) { r1 = ""; return; }
        if (LarghezzaTesto(t, scala, font) <= maxW) return;
        int taglio = -1;
        int i;
        for (i = 0; i < t.Length; i++)
            if (t[i] == ' ' && LarghezzaTesto(t.Substring(0, i), scala, font) <= maxW) taglio = i;
        if (taglio <= 0) { r1 = EntraMenu(t, scala, font, maxW); return; }
        r1 = t.Substring(0, taglio);
        r2 = EntraMenu(t.Substring(taglio + 1), scala, font, maxW);
    }

    // un testo che deve stare in tot pixel: se e' piu' largo si accorcia con un punto
    string EntraMenu(string t, float scala, int font, float maxW)
    {
        if (t == null) return "";
        if (LarghezzaTesto(t, scala, font) <= maxW) return t;
        while (t.Length > 2 && LarghezzaTesto(t + ".", scala, font) > maxW)
            t = t.Substring(0, t.Length - 1);
        return t.TrimEnd() + ".";
    }

    // etichetta grigia e valore bianco, uno sotto l'altro stretti
    void Dato(float x, float y, string etichetta, string valore, int r, int g, int b)
    {
        TestoMenu(etichetta.ToUpper(), x, y, 0.18f, 0, 0, 150, 152, 160, 255);
        TestoMenu(valore, x + 62f, y - 2f, 0.26f, 0, 0, r, g, b, 255);
    }

    void TastiEquip(int now)
    {
        if (sbVoci.Count == 0) return;
        bool su = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 27)
               || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 172);
        bool giu = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 19)
                || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 173);
        bool dx = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 175);
        bool sx = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 174);
        if (menuLato == 0 && (su || giu))
        {
            int n = sbVoci.Count;
            sbSel = (sbSel + (giu ? 1 : n - 1)) % n;
            menuNuovoTasto = now + 120; TicMenu("NAV_UP_DOWN");
        }
        // a destra si va solo dove c'e' qualcosa: una colonna vuota si salta
        else if (menuLato == 0 && dx && (eqCasa.Count > 0 || eqBorsa.Count > 0))
        {
            menuLato = eqCasa.Count > 0 ? 1 : 2;
            menuNuovoTasto = now + 150; TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 1 && sx) { menuLato = 0; menuNuovoTasto = now + 150; TicMenu("NAV_UP_DOWN"); }
        else if (menuLato == 1 && dx && eqBorsa.Count > 0) { menuLato = 2; menuNuovoTasto = now + 150; TicMenu("NAV_UP_DOWN"); }
        else if (menuLato == 2 && sx)
        {
            menuLato = eqCasa.Count > 0 ? 1 : 0;
            menuNuovoTasto = now + 150; TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 1 && (su || giu) && eqCasa.Count > 0)
        {
            int n = eqCasa.Count;
            eqSelCasa = (eqSelCasa + (giu ? 1 : n - 1)) % n;
            menuNuovoTasto = now + 120; TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 2 && (su || giu) && eqBorsa.Count > 0)
        {
            int n = eqBorsa.Count;
            eqSelBorsa = (eqSelBorsa + (giu ? 1 : n - 1)) % n;
            menuNuovoTasto = now + 120; TicMenu("NAV_UP_DOWN");
        }
    }

    void TastiSidebar(int now)
    {
        if (menuScheda == 1) { TastiEquip(now); return; }
        if (menuScheda != 0 || sbVoci.Count == 0) return;
        bool su = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 27)
               || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 172);
        bool giu = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 19)
                || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 173);
        bool dx = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 175);
        bool sx = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 174);
        bool ok = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 201);
        if (menuLato == 0 && (su || giu))
        {
            int n = sbVoci.Count;
            sbSel = (sbSel + (giu ? 1 : n - 1)) % n;
            menuNuovoTasto = now + 120;
            TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 0 && dx)
        {
            // si entra sulla prima licenza, non sul tasto in fondo: cosi'
            // si capisce che le licenze si comprano
            menuLato = 1; pnSel = 0; menuNuovoTasto = now + 150; TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 1 && sx)
        {
            menuLato = 0; menuNuovoTasto = now + 150; TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 1 && dx && pcPesci.Count > 0)
        {
            menuLato = 2; menuNuovoTasto = now + 150; TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 2 && sx)
        {
            menuLato = 1; menuNuovoTasto = now + 150; TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 2 && (su || giu))
        {
            int n = pcPesci.Count;
            if (n > 0) pcSel = (pcSel + (giu ? 1 : n - 1)) % n;
            menuNuovoTasto = now + 120;
            TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 1 && (su || giu))
        {
            int n = pnRighe + 1;
            pnSel = (pnSel + (giu ? 1 : n - 1)) % n;
            menuNuovoTasto = now + 120;
            TicMenu("NAV_UP_DOWN");
        }
        else if (menuLato == 1 && ok && sbSel < sbArea.Count)
        {
            int a = sbArea[sbSel];
            string cod = CodiceLuogo(a);
            menuNuovoTasto = now + 300;
            if (pnSel < pnRighe)
            {
                // compra la licenza: 1 o 3 giorni, resta in tasca
                if (CompraLicenza(cod, pnSel == 0 ? 1 : 3, false)) SuonoMenu("menu_apri.wav");
                pnSel = 0;
            }
            else if (LuogoQuiMenu() != a)
            {
                Esegui("gps_zona " + a);
                SuonoMenu("menu_apri.wav");
            }
            else if (LicenzaInTasca(cod))
            {
                if (IniziaPesca()) { SuonoMenu("menu_apri.wav"); ChiudiMenuNuovo(); }
            }
        }
    }

    void TastiMenuNuovo()
    {
        int now = OraPc();
        if (now < menuNuovoTasto) return;
        TastiSidebar(now);
        bool lb = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 37);
        bool rb = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 44);
        if (lb || rb)
        {
            int n = SCHEDE_IT.Length;
            menuScheda = (menuScheda + (rb ? 1 : n - 1)) % n;
            menuLato = 0;
            menuNuovoTasto = now + 150;
            TicMenu("NAV_UP_DOWN");
        }
    }

    void DisegnaMenuNuovo()
    {
        TastiMenuNuovo();
        // il pannello a tutto schermo: grigio scuro trasparente, lo stesso
        // grigio delle righe del trainer (menu_bg r,g,b e menu_velo alfa)
        int[] bg = ColoreCfg("menu_bg", 14, 22, 40);
        DisegnaRett(0f, 0f, 1280f, 720f, bg[0], bg[1], bg[2], (int)LeggiF("menu_velo", 190f));
        // LA CORNICE, come la pausa di GTA: titolo a sinistra, a destra
        // livello, ora e XP, le schede in fila, e sotto il contenitore.
        float mx = LeggiF("menu_x", 56f);
        float mw = LeggiF("menu_larga", 1168f);
        float ty = LeggiF("menu_titolo_y", 26f);
        TestoMenu(L("Fishing", "Pesca"), mx, ty, LeggiF("menu_titolo_testo", 1.1f), 4, 0, 245, 245, 250, 255);
        // a destra, sulla stessa riga del titolo: livello e XP
        float dx = mx + mw;
        float dy = ty + LeggiF("menu_liv_giu", 14f);
        // a destra di livello e XP, i soldi, in verde come dappertutto
        string soldiTxt = "$" + Soldi().ToString("N0", CultureInfo.InvariantCulture);
        float ts = LeggiF("menu_liv_testo", 0.55f);
        TestoMenu(soldiTxt, dx, dy, ts, 4, 2, 130, 225, 180, 255);
        float wSoldi = LarghezzaTesto(soldiTxt, ts, 4) + LeggiF("menu_soldi_gap", 18f);
        TestoMenu(L("LEVEL ", "LIV. ") + livelloPescatore + "   " + xpTot + " XP", dx - wSoldi, dy,
                  ts, 4, 2, 245, 245, 250, 255);

        // le schede
        float sy = LeggiF("menu_schede_y", 82f);
        float sh = LeggiF("menu_schede_alte", 40f);
        int n = SCHEDE_IT.Length;
        float gap = 3f;
        float sw = (mw - gap * (n - 1)) / n;
        int k;
        for (k = 0; k < n; k++)
        {
            float sx = mx + k * (sw + gap);
            bool sel = (k == menuScheda);
            if (sel)
            {
                DisegnaRett(sx, sy, sw, sh, 245, 245, 250, 255);
                DisegnaRett(sx, sy, sw, 4f, 150, 235, 180, 255);
                TestoMenu(L(SCHEDE_EN[k], SCHEDE_IT[k]), sx + sw * 0.5f, sy + sh * 0.5f - 9f, 0.30f, 0, 1, 20, 22, 28, 255);
            }
            else
            {
                DisegnaRett(sx, sy, sw, sh, 0, 0, 0, 150);
                TestoMenu(L(SCHEDE_EN[k], SCHEDE_IT[k]), sx + sw * 0.5f, sy + sh * 0.5f - 9f, 0.30f, 0, 1, 245, 245, 250, 255);
            }
        }
        // le frecce ai lati
        TestoMenu("<", mx - 24f, sy + sh * 0.5f - 12f, 0.42f, 0, 1, 245, 245, 250, 255);
        TestoMenu(">", mx + mw + 24f, sy + sh * 0.5f - 12f, 0.42f, 0, 1, 245, 245, 250, 255);

        // il contenitore
        float cy = sy + sh + LeggiF("menu_cont_giu", 12f);
        float ch = LeggiF("menu_cont_fondo", 630f) - cy;
        DisegnaRett(mx, cy, mw, ch, 0, 0, 0, (int)LeggiF("menu_cont_alfa", 150f));
        // la prima scheda ha la lista a sinistra, come le pagine di GTA,
        // e a destra il pannello della zona scelta
        if (menuScheda == 0)
        {
            DisegnaSidebar(mx, cy, ch);
            float sbw = LeggiF("menu_sb_larga", 260f) + LeggiF("menu_sb_bordo", 8f) * 2f;
            DisegnaPannelloZona(mx + sbw, cy, mw - sbw, ch);
        }
        if (menuScheda == 1)
        {
            DisegnaSidebarEquip(mx, cy, ch);
            float sbw = LeggiF("menu_sb_larga", 260f) + LeggiF("menu_sb_bordo", 8f) * 2f;
            DisegnaColonneEquip(mx + sbw, cy, mw - sbw, ch);
        }

        List<string> ic = new List<string>();
        List<string> tx = new List<string>();
        Voce(ic, tx, "lb", "TAB", L("Tab", "Scheda"));
        Voce(ic, tx, "rb", "Q", L("Tab", "Scheda"));
        if (menuScheda == 0 && menuLato == 0)
        {
            Voce(ic, tx, "croce_sugiu", "^ v", L("Choose", "Scegli"));
            Voce(ic, tx, "croce_dx", ">", L("Details", "Dettagli"));
        }
        if (menuScheda == 0 && menuLato == 1)
        {
            Voce(ic, tx, "croce_sugiu", "^ v", L("Choose", "Scegli"));
            Voce(ic, tx, "a", L("ENTER", "INVIO"), L("Confirm", "Conferma"));
            Voce(ic, tx, "croce_sxdx", "< >", L("List / Fish", "Lista / Pesci"));
        }
        if (menuScheda == 0 && menuLato == 2)
        {
            Voce(ic, tx, "croce_sugiu", "^ v", L("Fish", "Pesce"));
            Voce(ic, tx, "croce_sxdx", "<", L("Spot", "Posto"));
        }
        if (menuScheda == 1)
        {
            Voce(ic, tx, "croce_sugiu", "^ v", L("Choose", "Scegli"));
            Voce(ic, tx, "croce_sxdx", "< >", L("Column", "Colonna"));
        }
        Voce(ic, tx, "b", "ESC", L("Back", "Indietro"));
        DisegnaBarraTasti(ic, tx);
    }

    // L'ORA DEL GIOCO, in alto a sinistra (orario_x / orario_y)

    void DisegnaOrario()
    {
        if (!inPesca) return;
        int hh = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        int mi = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
        // solo l'ora: il giorno nel gioco non vuol dire niente
        float ox = LeggiF("orario_x", 24f);
        float oy = LeggiF("orario_y", 40f);
        string ora = hh.ToString("00") + ":" + mi.ToString("00");
        DisegnaTestoSinistra(ora, ox, oy, LeggiF("orario_testo", 0.42f), 245, 245, 250);
        // sotto, quanto manca alla licenza; piu' staccato, livello e XP
        if (licGiorni > 0)
            DisegnaTestoSinistra(L("License ", "Licenza ") + TempoCheResta(),
                                 ox, oy + LeggiF("orario_lic_giu", 24f), 0.22f, 200, 202, 210);
        // livello e XP sotto "Esplorazione", con lo stesso font
        DisegnaTestoSinistra(L("Level ", "Liv. ") + livelloPescatore + "   " + xpTot + " XP",
                             LeggiF("liv_x", 24f), LeggiF("liv_y", 75f), LeggiF("orario_liv_testo", 0.20f), 200, 202, 210);
    }

    // IL POSTO SULL'HUD: nome dell'acqua, sotto una riga bianca
    // trasparente che si riempie con le specie del posto gia' prese
    // (l'esplorazione, in percento), e quanto manca alla licenza.
    //   posto_x / posto_y   dove sta (in alto a sinistra del blocco)
    //   posto_barra         larghezza della riga
    void DisegnaPosto()
    {
        if (!inPesca) return;
        int a = LuogoQui();
        if (a < 0 || a >= arNome.Count) return;
        float px = LeggiF("posto_x", 24f);
        float py = LeggiF("posto_y", 40f);
        float bw = LeggiF("posto_barra", 160f);
        int qs = (a < arPesci.Count) ? arPesci[a].Count : 0;
        int scoperte = 0, q;
        for (q = 0; q < qs; q++)
            if (presoQui.ContainsKey(arNome[a] + "|" + arPesci[a][q])) scoperte++;
        int pct = (qs > 0) ? (int)(100f * scoperte / qs + 0.5f) : 0;
        DisegnaTestoSinistra(arNome[a], px, py, 0.30f, 245, 245, 250);
        // la riga dell'esplorazione ha la sua altezza (esplora_y): sta sotto
        // livello e XP, in alto a sinistra
        float by = LeggiF("esplora_y", 280f);
        DisegnaRett(px, by, bw, 2f, 255, 255, 255, 45);
        DisegnaRett(px, by, bw * pct / 100f, 2f, 255, 255, 255, 210);

        DisegnaTestoSinistra(L("Exploration ", "Esplorazione ") + pct + "%",
                             px, by + LeggiF("posto_lic_giu", 6f), 0.22f, 200, 202, 210);
        // sopra il grafico: l'icona del meteo con la temperatura dell'aria,
        // e l'icona dell'acqua con quella dell'acqua (le nostre)
        {
            float ty = LeggiF("temp_y", 368f);
            float ic = LeggiF("temp_icona", 14f);
            float sp = LeggiF("temp_spazio", 6f);
            float gap = LeggiF("temp_gap", 22f);
            string aria = GradiAria().ToString("0", CultureInfo.InvariantCulture) + "\u00B0C";
            string acqua = GradiAcqua().ToString("0", CultureInfo.InvariantCulture) + "\u00B0C";
            float tt = LeggiF("temp_testo", 0.24f);
            Sprite("img\\hud\\meteo\\" + IconaMeteoHud() + ".png", px, ty, ic, ic);
            float tg = LeggiF("temp_testo_giu", 3f);
            DisegnaTestoSinistra(aria, px + ic + sp, ty + ic * 0.5f - 10f + tg, tt, 245, 245, 250);
            float x2 = px + ic + sp + LeggiF("temp_larga", 34f) + gap;
            Sprite("img\\hud\\meteo\\acqua.png", x2, ty, ic, ic);
            DisegnaTestoSinistra(acqua, x2 + ic + sp, ty + ic * 0.5f - 10f + tg, tt, 245, 245, 250);
        }
        // il grafico resta dov'era: sotto il nome del posto (attivita_y)
        DisegnaAttivita(a, px, LeggiF("attivita_y", 390f), LeggiF("attivita_larga", bw));
    }

    // L'ATTIVITA' DELLA GIORNATA, stilizzata come nel wiki: una riga base
    // larga come quella dell'esplorazione, sotto le ore, sopra la curva
    // di quanto e' viva l'acqua ora per ora (dalle nostre regole: la
    // temperatura dell'acqua a quell'ora col meteo di adesso, e l'ora in
    // cui i pesci del posto mangiano). Un puntino segna l'ora di adesso.
    float[] attivCurva = new float[24];
    int attivCalcolata = 0;
    int attivArea = -1;

    void DisegnaAttivita(int a, float px, float py, float bw)
    {
        if (LeggiF("attivita", 1f) < 0.5f) return;
        int now = Game.GameTime;
        if (a != attivArea || now - attivCalcolata > 5000)
        {
            attivArea = a; attivCalcolata = now;
            int h, i;
            for (h = 0; h < 24; h++)
            {
                float aria = GradiAriaAlle(h + 0.5f);
                float acqua = 16f + (aria - 20f) * 0.45f;
                // LA MEDIA DEI PESCI DEL POSTO: quanto mangiano tutti
                // insieme a quell'ora. Il "meglio messo" da solo faceva una
                // riga piatta in cima: c'e' sempre qualcuno nella sua ora.
                float somma = 0f; int quanti = 0;
                for (i = 0; i < pesci.Count; i++)
                {
                    Specie sp = pesci[i];
                    if (sp.Zone != null && !PesceQui(sp, a)) continue;
                    somma += QuantoValeTemperaturaA(sp, acqua) * QuantoValeOraAlle(sp.Quando, h);
                    quanti++;
                }
                attivCurva[h] = (quanti > 0) ? somma / quanti : 0f;
            }
            // in scala: il momento migliore della giornata tocca il tetto
            float top = 0f;
            for (h = 0; h < 24; h++) if (attivCurva[h] > top) top = attivCurva[h];
            if (top > 0f) for (h = 0; h < 24; h++) attivCurva[h] = attivCurva[h] / top;
            // NON E' UNA TELEMETRIA: la curva si ammorbidisce, ogni ora
            // pesa con le due vicine, cosi' restano solo le onde grandi
            float[] lisc = new float[24];
            for (h = 0; h < 24; h++)
                lisc[h] = attivCurva[(h + 22) % 24] * 0.15f + attivCurva[(h + 23) % 24] * 0.2f
                        + attivCurva[h] * 0.3f
                        + attivCurva[(h + 1) % 24] * 0.2f + attivCurva[(h + 2) % 24] * 0.15f;
            for (h = 0; h < 24; h++) attivCurva[h] = lisc[h];
        }
        float alt = LeggiF("attivita_alt", 26f);
        float basso = py + alt;
        // LA COLLINETTA: il PNG fatto da gen_attivita.py per questo posto e
        // questo meteo (area sfumata dal blu al giallo con la linea sopra).
        // Se manca, la curva si disegna a pezzetti.
        string png = "img\\attivita\\" + a + "_" + ClasseMeteo() + ".png";
        bool conPng = File.Exists(Path.Combine(MY_DIR, png));
        if (conPng) Sprite(png, px, basso - alt, bw, alt);
        // la riga base e le ore
        DisegnaRett(px, basso, bw, 1f, 255, 255, 255, 120);
        // la giornata di pesca parte alle 5: la riga va dalle 5 alle 5
        float inizio = LeggiF("attivita_da", 5f);
        int k;
        for (k = 0; k <= 24; k += 6)
        {
            float xk = px + bw * k / 24f;
            DisegnaRett(xk, basso, 1f, 3f, 255, 255, 255, 120);
            DisegnaTesto("" + (((int)inizio + k) % 24), xk, basso + 3f, 0.17f, 200, 202, 210);
        }
        // la curva: un pezzetto ogni due pixel, con la fusione morbida
        // (coseno) fra un'ora e l'altra
        float passo = 2f;
        float x;
        if (!conPng)
            for (x = 0f; x < bw; x += passo)
            {
                float y = basso - ValoreCurva(inizio + x / bw * 24f) * alt;
                DisegnaRett(px + x, y - 1f, passo, 2f, 255, 255, 255, 210);
            }
        // l'ora di adesso
        int hh = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        int mi = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
        float oraOra = hh + mi / 60f - inizio;
        while (oraOra < 0f) oraOra += 24f;
        while (oraOra >= 24f) oraOra -= 24f;
        float xo = px + bw * oraOra / 24f;
        // una riga fine verticale, dalla base fino alla curva
        float yo = basso - ValoreCurva(hh + mi / 60f) * alt;
        DisegnaRett(xo, yo, 1f, basso - yo, 255, 255, 255, 230);
        // sotto le ore, in piccolo, cos'e'
        DisegnaTestoSinistra(L("Fish activity in this area", "Attivita' dei pesci in questa zona"), px,
                             basso + LeggiF("attivita_titolo_giu", 14f), 0.22f, 200, 202, 210);
    }

    // il valore della curva a un'ora con i decimali, fuso col coseno
    float ValoreCurva(float ora)
    {
        ora -= 0.5f;
        while (ora < 0f) ora += 24f;
        while (ora >= 24f) ora -= 24f;
        int h0 = (int)ora; float u = ora - h0;
        float w = (1f - (float)Math.Cos(u * 3.1415926f)) * 0.5f;
        return attivCurva[h0 % 24] * (1f - w) + attivCurva[(h0 + 1) % 24] * w;
    }

    // QUANTO MANCA ALLA FINE DELLA LICENZA, in tempo VERO.
    // Un minuto di gioco vale 5 secondi veri, una giornata sono 24 ore di
    // gioco: quindi due ore scarse di orologio da polso. Dire "resta un
    // giorno" non aiuta nessuno, sapere che mancano 47 minuti si'.
    string TempoCheResta()
    {
        long minutiGioco = (long)(MINUTI_GIORNATA - minutiFatti)
                         + (long)(licGiorni - 1) * MINUTI_GIORNATA;
        if (minutiGioco < 0L) minutiGioco = 0L;
        long secondiVeri = minutiGioco * MS_PER_MINUTO / 1000L;
        long h = secondiVeri / 3600L;
        long m = (secondiVeri % 3600L) / 60L;
        if (h > 0) return h + "h " + m + "m";
        return m + " min";
    }

    void ControllaOrologio()
    {
        if (!inPesca) return;
        int h;
        try { h = Function.Call<int>(Hash.GET_CLOCK_HOURS); }
        catch { return; }
        if (h == oraPrec) return;
        oraPrec = h;

        // la giornata dura 24 ore di gioco: dalle cinque del mattino alle
        // cinque del giorno dopo, notte compresa. Di notte si pesca.
        oreFatte++;
        if (minutiFatti < MINUTI_GIORNATA) return;
        oreFatte = 0;
        minutiFatti = 0;

        licGiorni--;
        if (licGiorni > 0)
        {
            VendiNassa();
            Alba();
            nassaOggi.Clear();
            kgNassa = 0f;
            Avviso("~y~Un'altra giornata: ne restano " + licGiorni + ".");
        }
        else FinePesca(true);
        SalvaStato();
        RiscriviTutto();
    }

    // il bar. Fame e sete le tiene il trainer, non noi: gli lasciamo
    // detto cosa hai preso in cibo.txt e ci pensa lui.
    static readonly string[] CIBO_NOME = new string[] {
        "", "Panino", "Bibita", "Birra", "Caffe'", "Pasto completo" };
    static readonly int[] CIBO_PREZZO = new int[] { 0, 12, 3, 5, 3, 15 };
    static readonly int[] CIBO_FAME = new int[] { 0, 60, 0, 10, 0, 70 };
    static readonly int[] CIBO_SETE = new int[] { 0, 20, 55, 45, 30, 60 };

    bool Mangia(int k)
    {
        if (k < 1 || k >= CIBO_NOME.Length) return false;
        int costo = CIBO_PREZZO[k];
        if (Soldi() < costo)
        {
            Avviso("~r~Ti servono $" + costo + ".");
            return false;
        }
        Paga(costo);
        try
        {
            File.AppendAllText(Path.Combine(MY_DIR, "cibo.txt"),
                CIBO_FAME[k] + "|" + CIBO_SETE[k] + "\r\n");
        }
        catch { }
        Avviso("~g~" + CIBO_NOME[k] + " ~s~- $" + costo);
        return true;
    }

    // ------------------------------------------------------------
    //  SEGNAPOSTI: giri la mappa, ti fermi su un'acqua e la marchi.
    //  Ogni riga finisce in zone_marcate.txt:  tipo|zonaGTA|x|y|z
    //  Da li' si ricava a quale delle nostre dieci acque va ogni zona.
    // ------------------------------------------------------------
    bool Marca(string tipo)
    {
        try
        {
            Ped p = Game.Player.Character;
            if (p == null || !p.Exists()) return false;
            GTA.Math.Vector3 pos = p.Position;
            string dove = "dove sei";

            // se hai messo il segnaposto sulla mappa grande, marchiamo quello:
            // cosi' si segnano i laghi senza andarci
            int blip = Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, 4);
            if (Function.Call<bool>(Hash.DOES_BLIP_EXIST, blip))
            {
                GTA.Math.Vector3 w = Function.Call<GTA.Math.Vector3>(Hash.GET_BLIP_INFO_ID_COORD, blip);
                if (w.X != 0f || w.Y != 0f)
                {
                    pos = w;
                    dove = "segnaposto";
                }
            }

            string z = Function.Call<string>(Hash.GET_NAME_OF_ZONE, pos.X, pos.Y, pos.Z);
            if (z == null) z = "?";
            z = z.ToUpper().Trim();
            string riga = tipo + "|" + z + "|"
                + pos.X.ToString("0.0", CultureInfo.InvariantCulture) + "|"
                + pos.Y.ToString("0.0", CultureInfo.InvariantCulture) + "|"
                + pos.Z.ToString("0.0", CultureInfo.InvariantCulture)
                + "|" + dove;
            File.AppendAllText(Path.Combine(MY_DIR, "zone_marcate.txt"), riga + "\r\n");
            CaricaPunti();
            Avviso("~g~Segnato ~s~" + z + " come " + tipo + " (" + dove + ")");
        }
        catch { return false; }
        return true;
    }

    // il mare e l'oceano non si marcano: la costa si capisce da sola
    // cancella l'ultima riga segnata, per quando si preme il tasto sbagliato
    bool Smarca()
    {
        try
        {
            string f = Path.Combine(MY_DIR, "zone_marcate.txt");
            if (!File.Exists(f)) return false;
            List<string> r = new List<string>();
            string[] tutte = File.ReadAllLines(f);
            int i;
            for (i = 0; i < tutte.Length; i++)
                if (tutte[i].Trim().Length > 0) r.Add(tutte[i]);
            if (r.Count == 0) return false;
            string ultima = r[r.Count - 1];
            r.RemoveAt(r.Count - 1);
            File.WriteAllLines(f, r.ToArray());
            CaricaPunti();
            Avviso("~y~Cancellato: ~s~" + ultima.Replace("|", "  "));
        }
        catch { return false; }
        return true;
    }

    static readonly string[] MARCA_COD = new string[] {
        "lago", "fiume", "torrente", "palude", "canale" };
    static readonly string[] MARCA_NOME = new string[] {
        "Lago", "Fiume", "Torrente", "Palude", "Canale di citta'" };

    void ScriviMarca()
    {
        List<string> v = new List<string>();
        v.Add("nota|Metti il segnaposto sulla mappa, poi premi A");
        int i;
        for (i = 0; i < MARCA_COD.Length; i++)
            v.Add("Segna qui: " + MARCA_NOME[i] + "|marca " + MARCA_COD[i] + "|"
                  + Banner() + "|Se c'e' il segnaposto marco quello, se no dove sei. Zona qui: " + zonaVista);
        v.Add("Cancella l'ultimo segnato|smarca|" + Banner()
              + "|Se hai premuto il tasto sbagliato.");
        ScriviVoci("marca_voci.txt", v);
    }

    void Avviso(string t)
    {
        try { Notification.PostTicker(t, false); }
        catch { }
    }

    // ------------------------------------------------------------
    //  LA FASCIA IN BASSO
    // ------------------------------------------------------------
    // I messaggi della pescata non stanno piu' nel ticker in alto a
    // sinistra: stanno in basso al centro, su una fascia scura, scritti
    // in bianco, come le scritte di servizio del gioco. Niente colori e
    // niente suoni: si legge e basta.
    // I colori del ticker (~y~ e compagnia) vengono tolti dal testo,
    // se no si leggono a lettere.
    string msgTxt = "";
    int msgFino = 0;

    void Messaggio(string t)
    {
        if (t == null) t = "";
        t = t.Replace("~y~", "").Replace("~r~", "").Replace("~g~", "")
             .Replace("~b~", "").Replace("~s~", "").Replace("~w~", "")
             .Replace("~p~", "").Replace("~o~", "");
        msgTxt = t.Trim();
        msgFino = Game.GameTime + (int)LeggiF("messaggio_ms", 3000f);
    }

    void DisegnaMessaggio()
    {
        if (msgTxt.Length == 0) return;
        if (Game.GameTime > msgFino) { msgTxt = ""; return; }
        float y = LeggiF("messaggio_y", 636f);
        float sc = LeggiF("messaggio_scala", 0.36f);
        float alt = LeggiF("messaggio_alt", 30f);
        // la fascia si allarga con la scritta, ma resta centrata
        float w = 28f + msgTxt.Length * sc * 19.5f;
        if (w < 200f) w = 200f;
        if (w > 940f) w = 940f;
        DisegnaRett(640f - w * 0.5f, y - 5f, w, alt,
                    (int)LeggiF("messaggio_r", 0f),
                    (int)LeggiF("messaggio_g", 0f),
                    (int)LeggiF("messaggio_b", 0f),
                    (int)LeggiF("messaggio_alfa", 150f));
        DisegnaTesto(msgTxt, 640f, y, sc, 255, 255, 255);
    }

    // ------------------------------------------------------------
    //  I MENU CHE CAMBIANO
    // ------------------------------------------------------------
    // IL POSTO SI CHIAMA COL SUO NOME.
    // "Golf" e' il gruppo della licenza, non il posto: il posto e'
    // "Laghetti del golf". Un gruppo puo' tenere undici tratti - Alamo
    // Sea li tiene - e dire "Alamo Sea" quando sei sulla Riva ovest non
    // dice dove sei. Qui si prende sempre il nome del tratto.
    int AreaOra()
    {
        int q = LuogoQui();
        if (inPesca)
        {
            if (q >= 0 && CodiceLuogo(q) == licZona) return q;
            return IndiceLuogo(licZona);
        }
        return q;
    }

    string NomeArea(int a)
    {
        if (a >= 0 && a < arNome.Count) return arNome[a];
        return "";
    }

    string NomeChiosco(string zona, out string bar)
    {
        bar = "Bar";
        string[] rows = LeggiRighe("negozi_zona.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 2) continue;
            if (c[0].Trim() != zona) continue;
            if (c.Length > 2 && c[2].Trim().Length > 0) bar = c[2].Trim();
            return c[1].Trim();
        }
        string g = NomeGruppo(zona);
        return (g.Length > 0) ? g : "Chiosco";
    }

    // quante voci diverse ci sono dentro (non la somma delle quantita')
    static int Quanti0(Dictionary<string, int> d)
    {
        int n = 0;
        foreach (KeyValuePair<string, int> kv in d)
            if (kv.Value > 0) n++;
        return n;
    }

    // la voce "Inizia a pescare": a casa le licenze, in pesca la giornata
    // il banner del posto dove sei: quello del lago, non il logo
    // dell'associazione. Se il posto non ce l'ha, torna vuoto.
    string BannerArea(int a)
    {
        if (a < 0 || a >= arNome.Count) return "";
        return ImgOk("img\\zone\\" + SlugArea(arNome[a]) + ".png");
    }

    void ScriviPesca()
    {
        List<string> v = new List<string>();
        // IN CIMA CI VA IL POSTO DOVE SEI.
        // Il logo dell'associazione va bene a casa; qui sei su un'acqua
        // precisa, e vedere quella e' meglio di vedere sempre lo stesso
        // rettangolo.
        int luB = inPesca ? IndiceLuogo(licZona) : LuogoQui();
        if (inPesca)
        {
            int lq = LuogoQui();
            if (lq >= 0 && CodiceLuogo(lq) == licZona) luB = lq;
        }
        string insP = BannerArea(luB);
        // MENTRE C'E' UN TORNEO comanda il torneo: e' quello che stai
        // facendo, il posto passa in secondo piano.
        if (torneoOra >= 0 && torneoOra < tornei.Count
            && tornei[torneoOra].Banner.Length > 0)
        {
            string insT = ImgOk("img\\tornei\\" + tornei[torneoOra].Banner);
            if (insT.Length > 0) insP = insT;
        }
        // NON un'insegna: il banner va messo come immagine delle righe.
        // Il riquadro grande in cima e' quello della riga scelta, e se
        // le righe si portano dietro il logo dell'associazione e' il
        // logo che ti ritrovi in cima, col banner del posto sotto - due
        // banner uno sull'altro. Cosi' invece in cima c'e' il posto.
        if (insP.Length == 0) insP = Banner();
        if (!inPesca)
        {
            int lu = LuogoQui();
            bool acqua = VicinoAllAcqua();
            if (lu < 0 || !acqua)
            {
                v.Add("nota|Zona " + zonaVista + "   acqua: " + (acqua ? "si" : "no"));
                v.Add("Qui non si pesca|niente|" + insP
                      + "|" + (lu < 0 ? ("La zona " + zonaVista + " non e' una delle nostre dieci acque.")
                                      : ("Sei nella zona giusta ma non in riva: avvicinati all'acqua.")));
            }
            else
            {
                string zona = CodiceLuogo(lu);
                v.Add("nota|" + NomeLuogo(lu));
                if (livelloPescatore < LivelloArea(lu))
                {
                    v.Add(L("Level " + LivelloArea(lu) + " needed here",
                            "Qui ci vuole il livello " + LivelloArea(lu))
                          + "|niente|" + insP + "|"
                          + L("You are level " + livelloPescatore
                              + ". Fish somewhere easier and come back.",
                              "Sei al livello " + livelloPescatore
                              + ". Fatti le ossa altrove e torna.")
                          + "|235,90,80");
                    VociTorneiDelPosto(v, lu);
                    ScriviVoci("pesca_voci.txt", v);
                    return;
                }
                int[] tagli = new int[] { 1, 3 };
                int t;
                for (t = 0; t < tagli.Length; t++)
                {
                    int pr = PrezzoLicenza(zona, tagli[t]);
                    if (pr <= 0) continue;
                    string et = L("Pay and fish free - ", "Paga e pesca libera - ")
                              + tagli[t]
                              + (lang == 1
                                 ? (tagli[t] == 1 ? " giorno" : " giorni")
                                 : (tagli[t] == 1 ? " day" : " days"));
                    // quanti soldi hai sta gia' scritto in cima al trainer
                    v.Add(et + "|licenza " + zona + " " + tagli[t] + "|" + insP
                          + "|$" + pr + "   "
                          + L("Starts at 5 and you fish 24 hours, night included.",
                              "Si parte alle 5 e si pesca 24 ore, notte compresa."));
                }
                // I TORNEI DI QUESTO POSTO.
                // Stessa pagina: paghi la giornata e la quota in un colpo
                // solo, e il banner del torneo sale in cima.
                VociTorneiDelPosto(v, lu);
            }
        }
        else
        {
            string nome = NomeArea(AreaOra());
            v.Add("nota|" + nome + " - restano " + TempoCheResta()
                  + "   -   Livello " + livelloPescatore + "   XP " + xpTot);
            // INIZIA A PESCARE STA QUI, SOPRA EQUIPAGGIAMENTO.
            // E' la prima cosa che vuoi quando hai gia' pagato la giornata:
            // non deve stare dentro un sottomenu.
            if (fase == FASE_FERMO)
            {
                int luP = LuogoQui();
                if (luP >= 0 && CodiceLuogo(luP) != licZona)
                {
                    string bzP;
                    v.Add(L("You are not where your licence is",
                            "Non sei nel posto della licenza")
                          + "|niente|" + insP + "|"
                          + L("You paid for ", "L'hai pagata per ")
                          + NomeChiosco(licZona, out bzP) + "|235,90,80");
                }
                else if (!VicinoAllAcqua())
                    v.Add(L("You are not on the bank", "Non sei in riva")
                          + "|niente|" + insP + "|"
                          + L("Get closer to the water.", "Avvicinati all'acqua.")
                          + "|235,90,80");
                else
                    v.Add(L("Start fishing", "Inizia a pescare")
                          + "|!pesca_via|" + insP + "|"
                          + L("take the rod in hand", "prendi la canna in mano")
                          + "|130,225,180");
            }
            // E' LA STESSA PAGINA DI CASA: cambia solo che la parte di
            // casa non si vede, perche' da qui non ci arrivi.
            v.Add("sottofile|" + L("Tackle", "Equipaggiamento")
                  + "|casa_voci.txt||" + insP
                  + "|" + Contatori());
            // I PESCI DI DOVE SEI e il diario: qui dentro, insieme
            // all'equipaggiamento. Mentre peschi sono le due cose che
            // guardi davvero.
            int luD = LuogoQui();
            if (luD < 0) luD = IndiceLuogo(licZona);
            if (luD >= 0)
            {
                v.Add("sottofile|" + L("Fish of ", "Studia i pesci di ")
                      + NomeArea(luD)
                      + "|" + FileLuogo(luD) + "||" + insP + "|"
                      + L("who lives here and what takes them",
                          "chi c'e' sotto e con cosa lo prendi"));
            }
            v.Add("sottofile|" + L("Fishing log", "Diario di pesca")
                  + "|diario_voci.txt||" + insP + "|"
                  + Quanti0(quaderno) + L(" species caught so far",
                                          " specie prese finora"));
            VociTorneiDelPosto(v, LuogoQui());
            // niente chiosco qui: il chiosco E' il negozio, e sta fuori
            // nel menu grande. Niente "Studia i pesci" doppio: e' la
            // stessa pagina che c'e' gia' fuori.
            // SMETTERE STA IN FONDO A QUESTA PAGINA.
            // Fuori, nel menu grande, restava scritto anche dopo che
            // avevi smesso. Qui dentro invece la giornata finisce e la
            // pagina non c'e' piu': rossa, e in fondo a tutto, cosi'
            // non la premi per sbaglio.
            v.Add(L("Stop fishing and go home", "Smetti di pescare e torna a casa")
                  + "|smetti|" + insP + "|"
                  + L("Ends the day: the licence you have left is lost.",
                      "Chiudi la giornata: la licenza che avanza la perdi.")
                  + "|235,90,80");
        }
        ScriviVoci("pesca_voci.txt", v);
    }

    // LE RIGHE DEI TORNEI DI QUESTO POSTO.
    // Non si paga da qui: ogni riga apre la scheda del torneo, la stessa
    // che c'e' nel menu dei tornei, con banner, regole, traguardi e record.
    // Si paga li' dentro, dopo aver letto cosa stai facendo.
    void VociTorneiDelPosto(List<string> v, int lu)
    {
        if (lu < 0) return;
        int i;
        for (i = 0; i < tornei.Count; i++)
        {
            Torneo t = tornei[i];
            if (LuogoDalNome(t.Zona) != lu) continue;

            string img = Banner();
            if (t.Banner.Length > 0)
            {
                string pb = ImgOk("img\\tornei\\" + t.Banner);
                if (pb.Length > 0) img = pb;
            }

            string d;
            if (torneoOra == i)
                d = L("Running", "In corso") + "   " + Kg(torneoKg) + " kg   "
                    + TempoTorneo();
            else
                d = t.Minuti + " min   $" + Soldo(t.PrOro)
                    + "   " + L("Lv.", "Liv.") + t.LivMin;

            // qui davanti al nome ci va "Torneo": in questa pagina ci sono
            // anche le righe della pesca libera, e se no non si capisce.
            // Nel menu dei tornei no: li' si sa gia' cosa sono.
            v.Add("sottofile|" + L("Competition ", "Torneo ") + t.Nome
                  + "|p_gara_" + i + ".txt||" + img + "|" + d);
            ScriviUnTorneo(i, "p_gara_");
        }
    }

    // ============================================================
    //  INVENTARIO DI CASA - si sta rifacendo da zero
    // ============================================================
    // Pagina nuova, vuota: si riempie un pezzo per volta.
    // Il vecchio ScriviInventario() sta ancora qui sotto, ma non lo
    // chiama piu' nessuno.
    // dove finisce davvero un'immagine dentro la sua casella
    string RettoSprite(string img, float x, float y, float w, float h)
    {
        float dw = w, dh = h;
        int iw, ih;
        if (MisuraPng(Path.Combine(MY_DIR, img), out iw, out ih)
            && iw > 0 && ih > 0)
        {
            float sx = w / (float)iw, sy = h / (float)ih;
            float sc = (sx < sy) ? sx : sy;
            dw = iw * sc; dh = ih * sc;
        }
        return (int)(x + (w - dw) * 0.5f) + "|" + (int)(y + (h - dh) * 0.5f)
             + "|" + (int)dw + "|" + (int)dh;
    }

    // le caselle dell'armatura sullo strumento, dall'alto in basso.
    //   rig|x|y|larghezza|altezza|comando
    void RigheArmatura(List<string> v)
    {
        int id; string img, nome;
        List<string> col = new List<string>();
        float my = 590f;
        int piano = 0;

        // LA CANNA: il rettangolo e' quello vero del disegno, non quello
        // della casella. L'immagine si stringe dentro 270x108 tenendo le
        // proporzioni, e girata di 90 gradi diventa alta quanto era larga.
        //   rig|x|y|larghezza|altezza|comando|margine
        if (Montato("canna", out id, out img, out nome))
        {
            float dw = 270f, dh = 108f;
            int iw, ih;
            if (MisuraPng(Path.Combine(MY_DIR, img), out iw, out ih)
                && iw > 0 && ih > 0)
            {
                float sx = 270f / (float)iw, sy = 108f / (float)ih;
                float sc = (sx < sy) ? sx : sy;
                dw = iw * sc; dh = ih * sc;
            }
            float cx = 1180f + 108f * 0.5f;
            float cy = 365f + 270f * 0.5f;
            // girata: quello che era largo diventa alto
            v.Add("rig|" + (int)(cx - dh * 0.5f) + "|" + (int)(cy - dw * 0.5f)
                  + "|" + (int)dh + "|" + (int)dw
                  + "|arma canna " + id
                  + "|" + (int)LeggiF("sel_canna", -9f)
                  + "|" + (int)LeggiF("sel_canna_alt", 0f));
        }

        // QUATTRO QUADRATI UGUALI.
        // Lato 50, quanto l'altezza della casella, centrati sulla
        // colonna: la colonna sta fra 1128 e 1256, quindi il centro e'
        // 1192. Il lato si cambia da config.ini con "sel_lato".
        //   rig|x|y|lato|lato|comando|margine|margine in alto
        float lato = LeggiF("sel_lato", 50f);
        float qx = 1192f - lato * 0.5f;
        if (Montato("mulinello", out id, out img, out nome))
        {
            col.Add("rig|" + (int)qx + "|" + (int)(my - piano * 54f - 3f)
                    + "|" + (int)lato + "|" + (int)lato
                    + "|arma mulinello " + id + "|0|0");
            piano++;
        }
        if (Montato("lenza", out id, out img, out nome))
        {
            col.Add("rig|" + (int)qx + "|" + (int)(my - piano * 54f - 3f)
                    + "|" + (int)lato + "|" + (int)lato
                    + "|disarma_lenza|0|0");
            piano++;
        }
        if (MontatoTerm("leader", out id, out img, out nome))
        {
            col.Add("rig|" + (int)qx + "|" + (int)(my - piano * 54f - 3f)
                    + "|" + (int)lato + "|" + (int)lato
                    + "|arma terminale " + id + "|0|0");
            piano++;
        }
        if (MontatoTerm("piombo", out id, out img, out nome))
        {
            col.Add("rig|" + (int)qx + "|" + (int)(my - piano * 54f - 3f)
                    + "|" + (int)lato + "|" + (int)lato
                    + "|arma terminale " + id + "|0|0");
            piano++;
        }
        if (Montato("galleggiante", out id, out img, out nome))
        {
            col.Add("rig|" + (int)qx + "|" + (int)(my - piano * 54f - 3f)
                    + "|" + (int)lato + "|" + (int)lato
                    + "|arma galleggiante " + id + "|0|0");
            piano++;
        }
        if (Montato("terminale", out id, out img, out nome))
        {
            col.Add("rig|" + (int)qx + "|" + (int)(my - piano * 54f - 3f)
                    + "|" + (int)lato + "|" + (int)lato
                    + "|arma terminale " + id + "|0|0");
            piano++;
        }

        // la colonna si costruisce dal basso in su: per il cursore serve
        // l'ordine di come si vede, dall'alto in basso
        int q;
        for (q = col.Count - 1; q >= 0; q--) v.Add(col[q]);
    }

    void ScriviCasa()
    {
        List<string> v = new List<string>();
        v.Add("titolo_finestra|CATEGORIE");
        v.Add("centra");
        // TRE FINESTRE.
        // In mezzo le categorie - le stesse del negozio, stesso ordine e
        // stessi nomi. A sinistra la roba di casa di QUELLA categoria: le
        // righe hanno la chiave del nome della categoria, e il trainer fa
        // vedere solo quelle della voce su cui sei. A destra quello che
        // ti porti dietro.
        // MENTRE SEI FUORI A PESCARE la parte di casa non c'e': la borsa
        // e' quella che ti sei portato, da casa non prendi piu' niente.
        // Il riquadro di sinistra sparisce e basta.
        if (inPesca) v.Add("nota|Sei fuori a pescare: da casa non prendi niente");
        else v.Add("pannello_sx|IN CASA");
        if (!inPesca)
        {
            // i colori sono quelli dei tasti del pad: A verde, X blu,
            // Y giallo. Il rosso resta a "disarma", che e' l'unico che toglie.
            v.Add("pannello_sx|- (A) equipaggia~   (X) vendi~   (Y) getta"
                  + "|||||130,225,180;110,175,255;245,205,80");
            v.Add("pannello_sx_pie|- spazio illimitato|||||190,195,205");
        }
        int qk;
        for (qk = 0; qk < CASA_ORD.Length && !inPesca; qk++)
        {
            int ic = CASA_ORD[qk];
            string ch = CAT_NOME[ic];
            int quanti = 0;
            foreach (KeyValuePair<string, int> kv in magazzino)
            {
                string[] cc = kv.Key.Split(':');
                if (cc.Length < 2 || cc[0] != CAT_COD[ic]) continue;
                int id = Numero(cc[1]);
                string nome, img;
                int prezzo, liv;
                if (!Articolo(cc[0], id, out nome, out img, out prezzo, out liv)) continue;
                // chiave|nome|icona|dati|comando|quantita
                // chiave|nome|icona|dati|comando A|quantita|colore|comando X
                // chiave|nome|icona|dati|comando A|quantita|colore|comando X
                //   |stato|colore stato|comando Y
                v.Add("pannello_sx_k|" + ch + "|" + nome + "|" + img
                      + "|" + Dettaglio(cc[0], id)
                      + "|equipaggia " + cc[0] + " " + cc[1]
                      + "|" + Quantita(cc[0], id, kv.Value, false)
                      + "||vendi " + cc[0] + " " + cc[1]
                      + "|||butta " + cc[0] + " " + cc[1] + " casa");
                quanti++;
            }
            // le bobine tagliate lasciate a casa, con i loro metri
            if (CAT_COD[ic] == "lenza")
            {
                int qbc;
                for (qbc = 0; qbc < bobineCasa.Count; qbc++)
                {
                    string[] cbc = bobineCasa[qbc].Split('|');
                    int idc2 = Numero(cbc[0]);
                    int mc3 = (cbc.Length > 1) ? Numero(cbc[1]) : 0;
                    string nc2, ic2; int pc2, lc2;
                    if (!Articolo("lenza", idc2, out nc2, out ic2, out pc2, out lc2)) continue;
                    v.Add("pannello_sx_k|" + ch + "|" + nc2 + "|" + ic2
                          + "|" + DettaglioBobina(idc2, mc3)
                          + "|bob_borsa " + qbc
                          + "|" + mc3 + " m"
                          + "||vendi_bobc " + qbc
                          + "|Tagliata|190,195,205"
                          + "|butta_bobc " + qbc);
                    quanti++;
                }
            }
            // il cassetto vuoto lo dice, ma non ci si va sopra
            if (quanti == 0)
                v.Add("pannello_sx_k|" + ch + "|Niente in casa");
        }

        // L'ARMATURA NON SI TOCCA PIU' DA QUI: si monta e si smonta con
        // la ruota (LB). L'HUD in basso a destra resta solo da vedere.

        // A DESTRA QUELLO CHE PORTI ADESSO, della stessa categoria: il
        // menu in mezzo comanda tutti e due i riquadri.
        int mc2, mm2, ml2, mr2;
        Capienza(out mc2, out mm2, out ml2, out mr2);
        v.Add("pannello|EQUIPAGGIAMENTO");
        // QUI NON SI MONTA: per quello c'e' la ruota. Resta getta, e lo
        // stato armato/disarmato in fondo alla riga.
        string capX = "(Y) getta";
        string colX = "245,205,80";
        v.Add("pannello|" + (inPesca
              ? ("- " + capX + "|||||" + colX)
              : ("- (A) sposta a casa~   " + capX
                 + "|||||130,225,180;" + colX)));

        v.Add("pannello_pie|- Canne " + InBorsa("canna") + "/" + mc2
              + "   Mulinelli " + InBorsa("mulinello") + "/" + mm2
              + "   Lenze " + InBorsa("lenza") + "/" + ml2
              + "   Cassetta " + RobaMinuta() + "/" + mr2
              + "   Nassa " + InBorsa("nassa") + "/1"
              + "|||||190,195,205");
        int qb;
        for (qb = 0; qb < CASA_ORD.Length; qb++)
        {
            int ib = CASA_ORD[qb];
            string chb = CAT_NOME[ib];
            int quantiB = 0;
            foreach (KeyValuePair<string, int> kv in borsa)
            {
                string[] cb = kv.Key.Split(':');
                if (cb.Length < 2 || cb[0] != CAT_COD[ib]) continue;
                int idb = Numero(cb[1]);
                string nb, ib2;
                int pb, lb;
                if (!Articolo(cb[0], idb, out nb, out ib2, out pb, out lb)) continue;
                // X non arma piu' niente: si arma dalla ruota
                bool siArma = SiArma(cb[0]);
                string cx = "";
                // e in fondo alla riga lo stato, come e' sempre stato:
                // verde se e' montato, grigio se sta in panchina
                bool suDiTe = siArma && EArmato(cb[0], idb);
                string stb = siArma ? (suDiTe ? "Armato" : "Disarmato") : "";
                // gli stessi colori della caption: blu "arma", rosso "disarma"
                string colb = suDiTe ? "110,175,255" : "235,90,80";
                v.Add("pannello_k|" + chb + "|" + nb + "|" + ib2
                      + "|" + Dettaglio(cb[0], idb)
                      + "|lascia " + cb[0] + " " + cb[1]
                      + "|" + Quantita(cb[0], idb, kv.Value)
                      + "||" + cx
                      + "|" + stb + "|" + colb
                      + "|butta " + cb[0] + " " + cb[1] + " borsa");
                quantiB++;
            }
            // LE BOBINE TAGLIATE: ognuna e' un pezzo a se', con i suoi
            // metri. Non sono confezioni nuove, quindi hanno la loro riga.
            if (CAT_COD[ib] == "lenza")
            {
                int qbo;
                for (qbo = 0; qbo < bobine.Count; qbo++)
                {
                    int idb2 = BobinaId(qbo);
                    string nb2, ib3;
                    int pb2, lb2;
                    if (!Articolo("lenza", idb2, out nb2, out ib3, out pb2, out lb2)) continue;
                    v.Add("pannello_k|" + chb + "|" + nb2 + "|" + ib3
                          + "|" + DettaglioBobina(idb2, BobinaMetri(qbo))
                          + "|" + (inPesca ? "niente" : ("bob_casa " + qbo)) + "|"
                          + "||"
                          + "|Tagliata|190,195,205"
                          + "|butta_bob " + qbo + "");
                    quantiB++;
                }
            }

            if (quantiB == 0)
                v.Add("pannello_k|" + chb + "|Niente in borsa");
        }

        // L'ARMATURA NON SI DISEGNA PIU' QUI SOTTO: mentre sei in questa
        // pagina si vede l'HUD vero, in basso a destra.

        // IL PESCATO DEL GIORNO.
        // Mentre sei fuori a pescare, in fondo alle categorie, c'e' la
        // nassa: scegliendola, a destra al posto dell'attrezzatura
        // compaiono i pesci che hai preso oggi, col peso, gli XP e
        // quanto valgono. A casa non serve: la nassa si svuota a fine
        // giornata.
        if (inPesca)
        {
            int qn;
            for (qn = nassaOggi.Count - 1; qn >= 0; qn--)
            {
                // la riga della nassa e' gia' fatta:
                //   nome|comando|img|destra|colore|sotto|colDestra|colSotto
                string[] rn = nassaOggi[qn].Split('|');
                if (rn.Length < 6) continue;
                v.Add("pannello_k|" + PESCATO + "|" + rn[0] + "|" + rn[2]
                      + "|" + rn[5]                    // peso, valore, XP
                      + "|niente|"                     // A non fa niente
                      + "||"                           // colore, comando X
                      + "|" + rn[3]                    // a destra che pesce e'
                      + "|" + (rn.Length > 6 ? rn[6] : "190,195,205"));
            }
            if (nassaOggi.Count == 0)
                v.Add("pannello_k|" + PESCATO + "|Nassa vuota");
        }

        // IN MEZZO LE CATEGORIE. Nessun numero sulle righe: pulite.
        int qc;
        for (qc = 0; qc < CASA_ORD.Length; qc++)
            v.Add(CAT_NOME[CASA_ORD[qc]] + "|niente||");
        // e in fondo, solo quando sei fuori, la cesta del pescato
        if (inPesca)
            v.Add(PESCATO + "|niente||"
                  + KgNassaDentro().ToString("0.0", CultureInfo.InvariantCulture)
                  + " kg");
        ScriviVoci("casa_voci.txt", v);
    }

    // l'inventario di casa: e' il negozio, ma con quello che possiedi
    void ScriviInventario()
    {
        List<string> menu = new List<string>();
        if (inPesca)
        {
            menu.Add("nota|Sei fuori a pescare");
            menu.Add("Sei fuori casa in questo momento|niente|" + Banner()
                     + "|Riapre stasera, quando finisce la giornata.");
            ScriviVoci("inventario_voci.txt", menu);
            return;
        }
        menu.Add("titolo_finestra|INVENTARIO DI CASA");
        // L'EQUIPAGGIAMENTO STA SOTTO, NELLA STESSA LISTA.
        // Prima le categorie di casa, poi una riga di sezione, poi quello
        // che ti porti. Si scorre e ci arrivi, senza saltare a destra in
        // un riquadro a parte. E scorre quanto serve: nell'equipaggiamento
        // di roba ce ne puo' finire tanta.
        menu.Add("icone");
        // l'intestazione resta un'intestazione, come e' sempre stata:
        // sotto ci va una riga sola con quello che c'e' da sapere
        menu.Add("- IN CASA -");
        // testo|comando|img|destra|colore riga|riga sotto|col.destra
        menu.Add("|niente||spazio illimitato|||190,195,205");

        int k;
        for (k = 0; k < CAT_COD.Length; k++)
        {
            List<string> v = new List<string>();
            int quante = 0;
            foreach (KeyValuePair<string, int> kv in magazzino)
            {
                string[] c = kv.Key.Split(':');
                if (c.Length < 2 || c[0] != CAT_COD[k]) continue;
                string nome, img;
                int prezzo, liv;
                if (!Articolo(c[0], Numero(c[1]), out nome, out img, out prezzo, out liv)) continue;
                if (v.Count == 0)
                {
                    if (AIcone(CAT_COD[k])) v.Add("icone");
                    v.Add("nota|Premi A per equipaggiare");
                    v.Add("titolo_finestra|" + CAT_NOME[k].ToUpper() + " IN CASA");
                    int qp;
                    List<string> pan = RigheBorsa();
                    for (qp = 0; qp < pan.Count; qp++) v.Add("pannello|" + pan[qp]);
                }
                string et = Unisci(nome, Quantita(c[0], Numero(c[1]), kv.Value));
                v.Add(et + "|equipaggia " + c[0] + " " + c[1] + "|" + img
                      + "|Liv." + liv + "   $" + Dollari(prezzo));
                quante += kv.Value;
            }
            if (v.Count == 0)
            {
                v.Add("Ancora niente|niente||");
                v.Add("titolo_finestra|" + CAT_NOME[k].ToUpper() + " IN CASA");
                int qp2;
                List<string> pan2 = RigheBorsa();
                for (qp2 = 0; qp2 < pan2.Count; qp2++) v.Add("pannello|" + pan2[qp2]);
            }
            ScriviVoci(CAT_FILE[k], v);
            // le voci ci sono sempre, come nel negozio: se dentro non c'e'
            // niente lo dice la voce stessa
            // niente banner qui: cosi' la lista e il riquadro della borsa
            // partono tutti e due dalla stessa altezza
            // niente logo qui: ripetuto dieci volte e' solo rumore
            menu.Add("sottofile|" + CAT_NOME[k] + " (" + quante + ")|" + CAT_FILE[k]
                     + "|||");
        }

        // EQUIPAGGIAMENTO, e sotto quanto ci sta ancora.
        // Prima l'intestazione cominciava con "ZAINO" e si portava
        // dietro tutti i numeri: sembrava che lo zaino fosse un pezzo
        // dell'equipaggiamento, e i conti non ci stavano. L'intestazione
        // resta com'era, i limiti vanno sulla riga sotto, in viola: un
        // colore che qui dentro non usa nessun altro.
        menu.Add("- EQUIPAGGIAMENTO -");
        menu.Add("|niente||" + Contatori() + "|||190,150,245");
        int kb;
        int quantiB = 0;
        for (kb = 0; kb < CAT_COD.Length; kb++)
        {
            foreach (KeyValuePair<string, int> kv in borsa)
            {
                string[] cb = kv.Key.Split(':');
                if (cb.Length < 2 || cb[0] != CAT_COD[kb]) continue;
                int idb = Numero(cb[1]);
                string nb, ib;
                int pb, lb;
                if (!Articolo(cb[0], idb, out nb, out ib, out pb, out lb)) continue;
                string etb = Unisci(nb, Quantita(cb[0], idb, kv.Value));
                string stb = SiArma(cb[0])
                           ? (EArmato(cb[0], idb) ? "Equipaggiato" : "Non equipaggiato")
                           : "";
                string cmb;
                if (SiArma(cb[0])) cmb = "arma " + cb[0] + " " + cb[1];
                else if (cb[0] == "esca") cmb = "usa_esca " + cb[1];
                else cmb = "lascia " + cb[0] + " " + cb[1];
                string colb = EArmato(cb[0], idb) ? "130,225,180" : "150,155,165";
                // testo|comando|icona|destra|colore riga|sotto|col.destra
                menu.Add(etb + "|" + cmb + "|" + ib + "|" + stb
                         + "||" + Dettaglio(cb[0], idb)
                         + "|" + (stb.Length > 0 ? colb : ""));
                quantiB++;
            }
        }
        if (quantiB == 0)
            menu.Add("Lo zaino e' vuoto|niente|" + Banner()
                     + "|Premi A sulla roba qui sopra per portartela.");

        ScriviVoci("inventario_voci.txt", menu);
    }

    // l'equipaggiamento: quello che ti porti dietro oggi
    // L'EQUIPAGGIAMENTO NON E' UN NEGOZIO.
    // E' l'armatura che hai addosso: una lista sola, tutti i pezzi in
    // fila con la loro icona, come il riquadro che si vede a destra
    // mentre sposti la roba da casa. Niente categorie da aprire.
    void ScriviEquip()
    {
        List<string> menu = new List<string>();
        menu.Add("icone");
        menu.Add("nota|" + Contatori());
        menu.Add("titolo_finestra|EQUIPAGGIAMENTO");

        // SI COMINCIA DA QUI, non piu' col tasto X: quello serve a saltare,
        // e saltellare mentre sei in riva non deve farti tirare fuori la canna.
        if (inPesca && fase == FASE_FERMO)
        {
            int luE = LuogoQui();
            if (luE >= 0 && CodiceLuogo(luE) != licZona)
            {
                string bzE;
                menu.Add("Non sei nel posto della licenza|niente||l'hai pagata per "
                         + NomeChiosco(licZona, out bzE) + "|235,90,80");
            }
            else if (!VicinoAllAcqua())
                menu.Add("Non sei in riva|niente||avvicinati all'acqua|235,90,80");
            else
                menu.Add("Inizia a pescare|!pesca_via||prendi la canna in mano|130,225,180");
        }

        int k;
        int quanti = 0;
        for (k = 0; k < CAT_COD.Length; k++)
        {
            foreach (KeyValuePair<string, int> kv in borsa)
            {
                string[] c = kv.Key.Split(':');
                if (c.Length < 2 || c[0] != CAT_COD[k]) continue;
                int id = Numero(c[1]);
                string nome, img;
                int prezzo, liv;
                if (!Articolo(c[0], id, out nome, out img, out prezzo, out liv)) continue;
                string et = Unisci(nome, Quantita(c[0], id, kv.Value));

                // LA NASSA E' UNA PORTA, non una riga qualsiasi: a destra
                // dice quanto pesce ci sta dentro adesso su quanto regge, e
                // la freccia apre l'elenco di quello che hai preso oggi.
                if (c[0] == "nassa")
                {
                    menu.Add("sottofile|" + et + "|nassa_voci.txt||" + img
                             + "|" + KgNassaDentro().ToString("0.0", CultureInfo.InvariantCulture)
                             + " / " + ((int)KgNassaMax()) + " kg");
                    quanti++;
                    continue;
                }

                // a destra lo STATO, sotto il nome i dati del pezzo
                string stato = SiArma(c[0])
                             ? (EArmato(c[0], id) ? "Equipaggiato" : "Non equipaggiato")
                             : "";
                string cmdRiga;
                if (SiArma(c[0])) cmdRiga = "arma " + c[0] + " " + c[1];
                else if (c[0] == "esca") cmdRiga = "usa_esca " + c[1];
                else cmdRiga = inPesca ? "niente" : ("lascia " + c[0] + " " + c[1]);
                // verde quando e' in pesca, grigio quando sta in panchina:
                // il giallo resta ai dati, che ora stanno sotto il nome
                string colStato = EArmato(c[0], id) ? "130,225,180" : "150,155,165";
                menu.Add(et + "|" + cmdRiga + "|" + img + "|" + stato
                         + "||" + Dettaglio(c[0], id)
                         + "|" + (stato.Length > 0 ? colStato : ""));
                quanti++;
            }
        }

        if (quanti == 0)
            menu.Add("Lo zaino e' vuoto|niente|" + Banner()
                     + "|Vai in Inventario di casa e premi A su quello che vuoi portarti.");

        ScriviVoci("equip_voci.txt", menu);
    }
    // il chiosco del posto: quello che serve in QUELL'acqua, al doppio.
    // Non lo decidiamo a mano: lo dicono i pesci che ci vivono.
    void ScriviChiosco()
    {
        List<string> v = new List<string>();
        if (!inPesca)
        {
            v.Add("nota|Il chiosco apre quando compri la licenza");
            ScriviVoci("chiosco_voci.txt", v);
            return;
        }
        // IL TRATTO DOVE SEI, non il primo del gruppo: con la licenza di
        // Alamo Sea sono undici tratti, e ognuno ha i suoi pesci
        int lu = LuogoQui(), i;
        if (lu < 0 || CodiceLuogo(lu) != licZona)
        {
            lu = -1;
            for (i = 0; i < arNome.Count; i++)
                if (CodiceLuogo(i) == licZona) { lu = i; break; }
        }
        if (lu < 0)
        {
            ScriviVoci("chiosco_voci.txt", v);
            return;
        }

        // 1. quante volte ogni esca compare fra i pesci di qui
        Dictionary<int, int> voti = new Dictionary<int, int>();
        float kgMax = 0f;
        for (i = 0; i < pesci.Count; i++)
        {
            Specie s = pesci[i];
            int z;
            if (!PesceQui(s, lu)) continue;
            if (s.KgU > kgMax) kgMax = s.KgU;
            if (s.Esche != null)
                for (z = 0; z < s.Esche.Length; z++)
                    Aggiungi2(voti, s.Esche[z]);
        }

        v.Add("icone");
        v.Add("nota|Prezzi del posto: il triplo");

        // 2. le dieci esche piu' richieste da questi pesci.
        // IL CHIOSCO TIENE TUTTO. Prima saltava quello che era di livello
        // troppo alto: ai laghetti del golf, a livello uno, non restava
        // una sola esca e si vedevano soltanto ami e galleggianti. Adesso
        // c'e' tutto, e quello che non puoi ancora comprare porta il suo
        // Liv. in rosso, come al negozio grande.
        int messe = 0;
        while (messe < 10)
        {
            int best = -1, bestN = 0;
            foreach (KeyValuePair<int, int> kv in voti)
                if (kv.Value > bestN) { bestN = kv.Value; best = kv.Key; }
            if (best < 0) break;
            voti.Remove(best);
            string nomeEsca = (best >= 0 && best < esche.Count) ? esche[best] : "";
            for (i = 0; i < escheShop.Count; i++)
            {
                if (escheShop[i].Nome != nomeEsca) continue;
                bool okE = (escheShop[i].LivWiki <= livelloPescatore);
                v.Add(EscaIt(escheShop[i].Nome) + "   x" + escheShop[i].Quantita
                      + "|compra_esca " + escheShop[i].Id + "|" + escheShop[i].Img
                      + "|Liv." + escheShop[i].LivWiki + "   $"
                      + PrezzoOggi(escheShop[i].Prezzo) + LivRosso(okE));
                messe++;
                break;
            }
        }

        // 3. tre lenze che reggano il pesce piu' grosso di qui
        int prese = 0;
        for (i = 0; i < lenze.Count && prese < 4; i++)
        {
            if (lenze[i].Kg < kgMax * 0.6f) continue;
            bool okL = (lenze[i].LivWiki <= livelloPescatore);
            v.Add(lenze[i].Marca + " " + lenze[i].Mm + " mm   " + lenze[i].Kg + " kg"
                  + "|compra_lenza " + lenze[i].Id + "|" + lenze[i].Img
                  + "|Liv." + lenze[i].LivWiki + "   $" + PrezzoOggi(lenze[i].Prezzo)
                  + LivRosso(okL));
            prese++;
        }

        // 4. quattro ami e un paio di galleggianti
        prese = 0;
        for (i = 0; i < terminali.Count && prese < 6; i++)
        {
            if (terminali[i].Cat != "amo") continue;
            bool okA = (terminali[i].LivWiki <= livelloPescatore);
            v.Add(Unisci(terminali[i].Marca + " " + terminali[i].Modello, terminali[i].Misura)
                  + "|compra_terminale " + terminali[i].Id + "|" + terminali[i].Img
                  + "|Liv." + terminali[i].LivWiki + "   $" + PrezzoOggi(terminali[i].Prezzo)
                  + LivRosso(okA));
            prese++;
        }
        prese = 0;
        for (i = 0; i < galleggianti.Count && prese < 4; i++)
        {
            bool okG = (galleggianti[i].LivWiki <= livelloPescatore);
            v.Add(Unisci(galleggianti[i].Nome, galleggianti[i].Colore)
                  + "|compra_galleggiante " + galleggianti[i].Id + "|" + galleggianti[i].Img
                  + "|Liv." + galleggianti[i].LivWiki + "   $"
                  + PrezzoOggi(galleggianti[i].Prezzo) + LivRosso(okG));
            prese++;
        }
        ScriviVoci("chiosco_voci.txt", v);
    }

    static void Aggiungi2(Dictionary<int, int> d, int k)
    {
        int v;
        if (d.TryGetValue(k, out v)) d[k] = v + 1;
        else d[k] = 1;
    }

    // il bar: gli stessi numeri di fame e sete del trainer
    void ScriviBar()
    {
        List<string> v = new List<string>();
        v.Add("nota|Da mangiare e da bere");
        v.Add("Panino|compra_cibo 1||$12   Toglie la fame.");
        v.Add("Bibita|compra_cibo 2||$3   Toglie la sete.");
        v.Add("Birra|compra_cibo 3||$5   Toglie la sete, ma la mira peggiora.");
        v.Add("Caffe'|compra_cibo 4||$3   Sveglia.");
        v.Add("Pasto completo|compra_cibo 5||$15   Fame e sete insieme.");
        ScriviVoci("bar_voci.txt", v);
    }

    // IL MENU DELLA MOD.
    // Lo riscriviamo noi perche' una voce deve cambiare nome: finche' sei
    // a casa e' "Inizia a pescare", con la licenza attiva diventa "Torna
    // a pescare", che e' quello che stai facendo davvero.
    // Il trainer si accorge che il file e' cambiato e aggiorna la scritta.
    void ScriviMenu()
    {
        List<string> v = new List<string>();
        v.Add("# Pannello di Pesca dentro il menu del trainer.");
        v.Add("# Lo riscrive la mod: non modificarlo a mano.");
        v.Add("sempre_attiva");
        // La prima voce dice DOVE sei: "Inizia a pescare a Tongva Valley".
        // Se non sei su un'acqua nostra resta secca, che e' gia' la
        // risposta: qui non si pesca.
        // "INIZIA A PESCARE" C'E' SOLO SE SEI SUL POSTO.
        // A Rockford Hills quella voce apriva una pagina che diceva "qui
        // non si pesca": una riga per dirti che non serve a niente. Se
        // non sei su un'acqua nostra non c'e' proprio, e la prima voce
        // diventa "Zone di pesca", che e' quello che ti serve davvero:
        // scegli dove andare, ci vai, e li' compare la licenza.
        // COME PRIMA: vale il raggio dell'area, quattrocento metri.
        // Provato a stringerlo e provato a chiedere di essere in riva:
        // tutte e due le volte la voce spariva dove serviva. Meglio
        // vederla un po' troppo presto che non vederla affatto.
        int luM = inPesca ? -1 : LuogoQui();
        if (inPesca || luM >= 0)
        {
            string vPesca, dPesca;
            if (inPesca)
            {
                vPesca = "Giornata di pesca a " + NomeArea(AreaOra());
                dPesca = "Restano " + TempoCheResta()
                       + "   Livello " + livelloPescatore + "   XP " + xpTot;
            }
            else
            {
                vPesca = "Inizia a pescare a " + NomeArea(luM);
                dPesca = "Compra la licenza e si parte.";
            }
            // anche qui il banner del posto: e' la riga che parla di
            // quell'acqua, non dell'associazione
            int luBM = inPesca ? IndiceLuogo(licZona) : luM;
            string imgM = BannerArea(luBM);
            if (imgM.Length == 0) imgM = "img\\lsfa.png";
            v.Add("sottofile|" + vPesca + "|pesca_voci.txt||" + imgM + "|" + dPesca);
        }
        // DOVE SI PESCA: tutte le aree in ordine di livello, col prezzo
        // della licenza e il segnaposto. Sta subito sotto "Inizia a
        // pescare" perche' e' la prima domanda di chi comincia.
        v.Add("sottofile|" + L("Fishing spots", "Zone di pesca")
              + "|zone_voci.txt||img\\lsfa.png|"
              + L(arNome.Count + " spots, from level 1 up.",
                  arNome.Count + " posti, dal livello 1 in su."));
        // I tornei: la lista con i banner, e premendo uno ti mette il
        // segnaposto sulla mappa dove si corre.
        v.Add("sottofile|" + L("Competitions", "Tornei e competizioni")
              + "|tornei_voci.txt||img\\lsfa.png|"
              + L(tornei.Count + " competitions with cash prizes.",
                    tornei.Count + " tornei con premi in denaro."));
        // Il negozio e' uno solo: a casa e' quello grande, sull'acqua
        // diventa il baracchino del posto, col suo nome.
        string nomeNeg = "Negozio di Los Santos Fisherman";
        string descNeg = "Canne, mulinelli, lenze, ami, esche e provviste.";
        if (inPesca)
        {
            nomeNeg = "Negozio di " + NomeArea(AreaOra());
            descNeg = "Tutto, al triplo del prezzo.";
        }
        v.Add("sottofile|" + nomeNeg + "|negozio.txt||img\\lsfa.png|" + descNeg);
        // INVENTARIO DI CASA: pagina nuova, si sta rifacendo da zero.
        v.Add("sottofile|Inventario di casa|casa_voci.txt||img\\lsfa.png|"
              + "Quello che possiedi, e cosa ti porti dietro.");
        // STUDIA I PESCI non sta piu' qui.
        // Non ha senso studiare i pesci di un posto dove non sei: adesso
        // la voce sta dentro la giornata di pesca, e mostra i pesci di
        // dove sei. Il diario invece resta qui: quello e' tuo, non del
        // posto.
        v.Add("sottofile|Diario di pesca|diario_voci.txt||img\\lsfa.png|"
              + Quanti0(quaderno) + " specie su " + pesci.Count + " prese finora.");
        // "Smetti" non sta piu' qui: sta in fondo alla giornata di pesca,
        // dentro. Qui fuori restava scritto anche dopo che avevi smesso.
        v.Add("sottofile|Impostazioni|impostazioni_voci.txt||img\\lsfa.png|"
              + "Come si comporta la mod.");
        // LE VOCI DI SVILUPPO NON CI SONO PIU'.
        // Registra le acque, prova i suoni, modelli del pesce, prova i
        // galleggianti: tutte tolte dal menu. Il codice e i comandi
        // (registra, suono_prova, mod_pesce, prova_gall) sono ancora
        // tutti li' sotto e funzionano: per rimettere una voce basta
        // riscrivere la sua riga qui.
        ScriviVoci("menu.txt", v);
    }

    // LA NASSA DEI PESCI: quello che hai preso OGGI, l'ultimo in cima, con
    // il peso, quanto vale e quanti punti ti ha dato. Si svuota a fine
    // giornata, come la nassa vera.
    List<string> nassaOggi = new List<string>();

    void ScriviNassa()
    {
        List<string> v = new List<string>();
        v.Add("icone");
        v.Add("nota|" + KgNassaDentro().ToString("0.0", CultureInfo.InvariantCulture)
              + " kg su " + ((int)KgNassaMax()) + "   -   " + nassaOggi.Count
              + (nassaOggi.Count == 1 ? " pesce" : " pesci"));
        int i;
        for (i = nassaOggi.Count - 1; i >= 0; i--) v.Add(nassaOggi[i]);
        // vuota vuol dire vuota: nessuna riga, nessuna immagine
        ScriviVoci("nassa_voci.txt", v);
    }

    void ScriviImpostazioni()
    {
        List<string> v = new List<string>();
        // niente "icone" e niente Banner: qui immagini non ce ne sono, e il
        // logo dell'associazione usato come tappabuchi diventava una
        // iconcina appiccicata a ogni riga
        v.Add("nota|Si salvano da sole");
        // QUANTO SI VEDE IL GALLEGGIANTE.
        // La misura vera e' quella del wiki, ma su uno schermo lontano un
        // galleggiante da due centimetri sparisce: chi vuole se lo
        // ingrossa, senza toccare i dati.
        // E' una LISTA vera - destra e sinistra per scegliere, A per
        // confermare - non un bottone che gira sempre nello stesso verso.
        //   lista|etichetta|comando|valori|scritte|scelto
        v.Add("lista|Grandezza del galleggiante|imp_gall|0;1;2;3;4"
              + "|Vera;x1.3;x1.6;x2.0;x2.6|" + gallZoom);
        v.Add("Consiglia zone di pesca|imp_zone||"
              + (avvisaZona ? "Acceso" : "Spento")
              + "||Ti avvisa quando passi su un'acqua dove si pesca."
              + "|" + (avvisaZona ? "130,225,180" : "150,155,165"));
        v.Add((diarioChiesto ? "Sicuro? Premi ancora" : "Azzera il diario")
              + "|imp_diario||" + Quanti0(quaderno) + " specie"
              + "||Cancella tutto quello che hai pescato finora."
              + "|150,155,165");
        v.Add((resetChiesto ? "Sicuro? Premi ancora" : "Ricomincia da zero")
              + "|imp_reset||Liv. " + livelloPescatore + " - " + XpCorto(xpTot) + " XP"
              + "||Azzera XP, livello, diario e tutta l'attrezzatura comprata."
              + "|" + (resetChiesto ? "235,90,80" : "150,155,165"));
        ScriviVoci("impostazioni_voci.txt", v);
    }

    // I PUNTI SCRITTI CORTI: 0, 10, 100, 900, poi 1.2K, 12.5K.
    static string XpCorto(int xp)
    {
        if (xp < 1000) return xp.ToString();
        int k = xp / 1000;
        int d = (xp % 1000) / 100;
        if (d > 0) return k.ToString() + "." + d.ToString() + "K";
        return k.ToString() + "K";
    }

    // LA RIGA IN CIMA. Il trainer la legge da header.txt e la mette
    // nell'header dopo la temperatura, cosi' livello e punti si vedono
    // anche quando non stai pescando.
    void ScriviTesta()
    {
        try
        {
            File.WriteAllText(Path.Combine(MY_DIR, "header.txt"),
                              "Liv. " + livelloPescatore
                              + "   " + xpTot + " XP");
        }
        catch { }
    }

    // GLI SCAFFALI DEL NEGOZIO.
    // Sono dieci file grossi (527 artificiali, 381 terminali...) e si
    // riscrivono solo quando serve: all'avvio, e quando sali di livello.
    // Prima venivano scritti una volta sola all'avvio, per giunta prima di
    // leggere il salvataggio: il negozio restava fermo al livello 1 e la
    // roba nuova non compariva mai.
    int livelloDegliScaffali = -1;

    // i prezzi degli scaffali cambiano anche quando esci a pescare:
    // sul posto costa il triplo, e le pagine vanno rifatte
    bool scaffaliInPesca = false;

    void ScaffaliDelNegozio()
    {
        livelloDegliScaffali = livelloPescatore;
        scaffaliInPesca = inPesca;
        ScriviNegozioLenze();
        ScriviNegozioCanne();
        ScriviNegozioMulinelli();
        ScriviNegozioTerminali();
        ScriviNegozioEsche();
        ScriviNegozioCassette();
        ScriviNegozioPortacanne();
        ScriviNegozioNasse();
        ScriviNegozioGalleggianti();
        ScriviNegozioArtificiali();
    }

    // LE PAGINE PESANTI SI RIFANNO SOLO QUANDO SERVE.
    // "Zone di pesca" sono 36 file, il quaderno 37, i tornei 103: rifarli
    // a ogni pesce preso sono quattrocento scritture su disco e si sente.
    // Cambiano solo col livello, col posto o con un torneo, quindi si
    // ricordano com'erano l'ultima volta.
    int zoneScritteLiv = -1, zoneScritteQui = -2;
    int torneiScrittiLiv = -1, torneiScrittiOra = -2;
    int quadernoScrittoLiv = -1;

    void RiscriviTutto()
    {
        int prima = livelloPescatore;
        livelloPescatore = LivelloDa(xpTot);
        ScriviTesta();
        if (livelloPescatore != livelloDegliScaffali || inPesca != scaffaliInPesca)
        {
            ScaffaliDelNegozio();
            if (livelloPescatore > prima)
                Avviso("~g~Livello " + livelloPescatore
                       + ".~s~  In negozio c'e' roba nuova.");
        }
        ScriviMenu();
        ScriviPesca();
        ScriviCasa();
        ScriviInventario();
        ScriviEquip();
        // il chiosco non c'e' piu': il negozio e' uno solo
        ScriviMarca();
        ScriviNegozio();
        ScriviImpostazioni();
        ScriviNassa();

        int quiOra = LuogoQui();
        if (livelloPescatore != zoneScritteLiv || quiOra != zoneScritteQui)
        {
            zoneScritteLiv = livelloPescatore;
            zoneScritteQui = quiOra;
            ScriviZone();
        }
        if (livelloPescatore != torneiScrittiLiv || torneoOra != torneiScrittiOra)
        {
            torneiScrittiLiv = livelloPescatore;
            torneiScrittiOra = torneoOra;
            ScriviTornei();
        }
        // il quaderno scrive anche il diario: quello va rifatto a ogni
        // pesce, le trentasette pagine dello studio no
        ScriviDiario();
        if (livelloPescatore != quadernoScrittoLiv)
        {
            quadernoScrittoLiv = livelloPescatore;
            ScriviQuaderno();
        }
    }

    // ---------- esche artificiali (dati veri dal wiki) ----------
    class Artificiale
    {
        public int Id;
        public string Tipo, Nome, Colore, Grammi, Cm, Amo, Img;
        public int Prezzo, LivWiki;
    }
    List<Artificiale> artificiali = new List<Artificiale>();

    static readonly string[] ART_COD = new string[] {
        "cucchiaino", "rotante", "minnow", "jig", "siliconico", "mare" };
    static readonly string[] ART_NOME = new string[] {
        "Cucchiaini", "Rotanti", "Minnow e popper", "Jig da bass",
        "Siliconici", "Da mare" };
    static readonly string[] ART_DESC = new string[] {
        "Lamine di metallo che girano e brillano: luccio, persico, trota.",
        "Palettina rotante: fa vibrare l'acqua, i predatori la sentono da lontano.",
        "Pesciolini finti: crankbait, jerkbait, popper, rane e topolini.",
        "Jig pesanti con la gonnella: il bass sotto le sponde.",
        "Vermi, larve e code di silicone: si montano sull'amo o sulla testina.",
        "Pilker, squid jig e octopus: roba pesante per il mare." };

    void CaricaArtificiali()
    {
        artificiali.Clear();
        string[] rows = LeggiRighe("artificiali.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 10) continue;
            Artificiale x = new Artificiale();
            int v;
            int.TryParse(c[0], out v); x.Id = v;
            x.Tipo = c[1].Trim(); x.Nome = c[2].Trim(); x.Colore = c[3].Trim();
            x.Grammi = c[4].Trim(); x.Cm = c[5].Trim(); x.Amo = c[6].Trim();
            int.TryParse(c[7], out v); x.Prezzo = v;
            int.TryParse(c[8], out v); x.LivWiki = v;
            x.Img = ImgOk("img\\artificiali\\" + c[9].Trim());
            int k = artificiali.Count;
            while (k > 0 && artificiali[k - 1].LivWiki > x.LivWiki) k--;
            artificiali.Insert(k, x);
        }
    }

    void ScriviNegozioArtificiali()
    {
        int t;
        List<string> tipi = new List<string>();
        for (t = 0; t < ART_COD.Length; t++)
        {
            List<string> v = new List<string>();
            int i, quanti = 0;
            for (i = 0; i < artificiali.Count; i++)
            {
                Artificiale x = artificiali[i];
                if (x.Tipo != ART_COD[t]) continue;
                string et = Unisci(EscaIt(x.Nome), ColoreIt(x.Colore));
                if (x.Grammi.Length > 0) et += "   " + x.Grammi + " g";
                string ds = "Liv." + x.LivWiki + "   $" + Dollari(x.Prezzo);
                if (x.Amo.Length > 0) ds += "   Amo: " + x.Amo;
                // SI VEDE TUTTO IL NEGOZIO, in ordine di livello.
                // Quello che non puoi ancora comprare resta scritto, spento
                // e non si preme: cosi' sai gia' dove stai andando.
                bool ok = (x.LivWiki <= livelloPescatore);
                if (v.Count == 0)
                {
                    v.Add("icone");
                    v.Add("nota|Premi A per comprare");
                }
                v.Add(et + "|compra_artificiale " + x.Id
                      + "|" + x.Img + "|" + ds + LivRosso(ok));
                quanti++;
            }
            if (quanti == 0) continue;
            ScriviVoci("a_" + ART_COD[t] + ".txt", v);
            tipi.Add("sottofile|" + ART_NOME[t] + " (" + quanti + ")|a_" + ART_COD[t]
                     + ".txt||" + Banner() + "|" + ART_DESC[t]);
        }
        ScriviVoci("a_tipi.txt", tipi);
    }


    // ============================================================
    //  LA PESCATA: lancio, attesa, abboccata, recupero.
    //  Versione essenziale per provare: niente animazioni, quelle
    //  vengono dopo. Tutto quello che sta qui e' roba nostra.
    //
    //  Tasti (pad e tastiera insieme):
    //    grilletto destro / clic sinistro  =  carica il lancio, ferra, recupera
    // ============================================================

    const int FASE_FERMO = 0;
    const int FASE_CARICA = 1;
    const int FASE_ACQUA = 2;
    const int FASE_ABBOCCA = 3;
    const int FASE_LOTTA = 4;
    const int FASE_PRONTO = 5;
    const int FASE_CARD = 6;    // il pesce in mano: tieni o ributti

    int fase = FASE_FERMO;
    // vero solo dopo che hai davvero mollato il grilletto: serve a non
    // far ripartire un lancio con il dito ancora premuto dal recupero
    bool grillettoMollato = false;
    float potenza = 0f;          // 0..100 mentre carichi
    bool potenzaSu = true;
    int quandoAbbocca = 0;    // GameTime in cui il pesce prende
    int giroMulinello = 0;    // il tic tic del mulinello mentre giri

    // LA PROFONDITA' DELL'ESCA sotto il galleggiante: quanto filo c'e'
    // fra galleggiante e amo. Come in Fishing Planet si regola a passi di
    // 5 pollici (12,7 cm), da 5 a 99 pollici (2,50 m), con SU e GIU'
    // della croce, senza la lenza in acqua (canna in mano o in riva).
    // Se e' piu' del fondo l'esca tocca terra e il galleggiante si sdraia.
    float profondita = 1.0f;
    const float PROF_PASSO = 0.127f;
    const float PROF_MIN = 0.127f;
    const float PROF_MAX = 2.515f;
    int tastoProf = 0;

    void RegolaProfondita(int now)
    {
        // solo con la canna in mano, che senza l'HUD non lo vedi
        if (!inPesca || !inRivaOra || ruotaAperta) return;
        if (fase != FASE_PRONTO) return;
        // su e giu' della croce sono nostri qui (27 e' il telefono, 19 la ruota dei personaggi)
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 27, true);
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 19, true);
        if (now < tastoProf) return;
        bool su = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 27);
        bool giu = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 19);
        if (!su && !giu) return;
        profondita += su ? PROF_PASSO : -PROF_PASSO;
        if (profondita < PROF_MIN) profondita = PROF_MIN;
        if (profondita > PROF_MAX) profondita = PROF_MAX;
        tastoProf = now + 160;
        Suono("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
        SalvaStato();
    }

    // LA FRIZIONE: le tacche del cerchio, si gira con destra e sinistra.
    // COME IN FISHING PLANET: piu' tacche accese = piu' frizione = piu'
    // freno sulla bobina.
    //   tutte accese   = frizione TIRATA: il mulinello non molla, guadagni
    //                    lenza in fretta ma la tensione sale subito.
    //   1 tacca accesa = frizione MORBIDA: la tensione sale piano, pero'
    //                    il pesce si riprende il filo e non finisci mai.
    // Sta a te trovare la via di mezzo col pesce che hai attaccato.
    // QUANTE POSIZIONI: "friz_posizioni" in config, 12 come le tacche.
    // (Sul wiki il numero c'e' solo per una ventina di mulinelli, quasi
    // tutti 12: si usa 12 per tutti.) Le quattro tabelle sotto sono i
    // punti di riferimento da tirata a morbida: fra un punto e l'altro
    // si interpola, cosi' le posizioni possono essere quante si vuole.
    int frizione = 2;                 // 1..PosFrizione()
    static readonly float[] FRIZ_REC = new float[] { 1.45f, 1.15f, 0.90f, 0.65f };
    static readonly float[] FRIZ_TEN = new float[] { 1.65f, 1.25f, 0.95f, 0.70f };
    // i metri che il pesce riesce a portarsi via quando parte:
    // frizione tirata il mulinello non molla, morbida il filo scorre
    static readonly float[] FRIZ_MET = new float[] { 0.50f, 0.80f, 1.15f, 1.60f };
    // e i metri che guadagni tu quando lo tiri: tirata rende un filo di piu'
    static readonly float[] FRIZ_GUA = new float[] { 1.20f, 1.05f, 0.92f, 0.80f };

    int PosFrizione()
    {
        int n = (int)LeggiF("friz_posizioni", 12f);
        return (n < 2) ? 2 : n;
    }

    // il valore di una tabella alla posizione di frizione attuale
    float Friz(float[] tab)
    {
        int n = PosFrizione();
        if (frizione < 1) frizione = 1;
        if (frizione > n) frizione = n;
        // tabelle: indice 0 = tirata, 3 = morbida. Tutte le tacche = tirata.
        float f = (float)(n - frizione) / (float)(n - 1) * 3f;   // 0..3
        int k = (int)f;
        if (k > 2) k = 2;
        float u = f - k;
        return tab[k] + (tab[k + 1] - tab[k]) * u;
    }
    int tastoFriz = 0;

    // LO STRAPPO DEL PESCE.
    // Un pesce non sta fermo mentre lo tiri: ogni tanto parte e si porta
    // via lenza. E' qui che la frizione conta davvero:
    //   tirata  -> il filo non da', la tensione schizza, ma di metri gliene
    //              lasci pochi;
    //   morbida -> il mulinello canta e lui se ne prende tanti, pero' la
    //              lenza non rischia di spezzarsi.
    int strappoFine = 0;      // fino a quando sta tirando adesso
    // LE CORSE: ogni tanto il pesce parte e si porta via lenza sul serio,
    // virando da un lato. Quanto e quanto spesso lo decidono il suo peso
    // contro la tua attrezzatura, la stanchezza e la frizione.
    int corsaFine = 0, corsaProssima = 0;
    float corsaMetriSec = 0f, corsaVerso = 1f;
    int strappoDa = 0;        // quando puo' ripartire
    float strappoForza = 0f;
    float stanchezza = 0f;    // 0..1: quanto si e' consumato il pesce
    int clickPesce = 0;

    // IL PESCE APPENA PRESO: resta li' finche' non decidi.
    // I punti li hai gia' fatti - quelli non si perdono mai. Qui scegli
    // solo se tenerlo o rimetterlo in acqua, e se non ci sta nella nassa
    // la scelta non c'e': si ributta e basta.
    int cardPesce = -1;
    float cardKg = 0f;
    int cardXp = 0, cardVale = 0;
    string cardTaglia = "";

    // CHE PESCE E': comune, trofeo o unico. Nome e colore stanno qui,
    // in un posto solo, cosi' la finestra della cattura, la nassa e il
    // diario dicono la stessa cosa nello stesso colore.
    //   comune  bianco     trofeo  verde     unico  oro
    static string ClasseDi(float kg, float kgT, float kgU)
    {
        if (kgU > 0f && kg >= kgU) return "UNICO";
        if (kgT > 0f && kg >= kgT) return "TROFEO";
        return "COMUNE";
    }

    static string ColoreClasse(string clas)
    {
        if (clas == "TROFEO") return "130,225,180";
        if (clas == "UNICO") return "245,205,80";
        return "245,245,250";
    }
    string cardPerche = "";
    bool cardPuoTenere = false;
    int prossimoTocco = 0;    // quando il pesce assaggia
    int toccoFine = 0;
    int calmaFino = 0;        // fino a qui non si sente niente
    int scadeFerrata = 0;
    float tensione = 0f;         // 0..100: a 100 la lenza si spezza
    float recuperato = 0f;       // 0..100: a 100 il pesce e' tuo
    int pesceQui = -1;           // indice in pesci
    int dentiDa = 0;             // da quando ha in bocca il filo nudo
    float pesceKg = 0f;
    int ultimoMsg = 0;
    float metriLenza = 0f;
    int tastoDa = 0;      // antirimbalzo: dopo un cambio di fase il tasto tace

    void DisegnaRett(float px, float py, float pw, float ph, int r, int g, int b, int a)
    {
        Function.Call(Hash.DRAW_RECT, (px + pw * 0.5f) / 1280f, (py + ph * 0.5f) / 720f,
                      pw / 1280f, ph / 720f, r, g, b, a);
    }

    void DisegnaTesto(string txt, float x, float y, float scala, int r, int g, int b)
    {
        try
        {
            TextElement el = new TextElement(txt, new PointF(x, y), scala);
            el.Color = Color.FromArgb(255, r, g, b);
            el.Font = GTA.UI.Font.ChaletLondon;
            el.Alignment = Alignment.Center;
            el.Outline = true;
            el.Draw();
        }
        catch { }
    }

    // I tasti sono quelli della mod vecchia:
    //   24  ATTACCO (RT sul pad, clic col mouse)  e  51  CONTESTO (E)
    //       si tiene premuto per caricare, si molla per lanciare
    //   203 X / FRONTEND_X      prende la canna in mano e la ripone.
    //       Lo stesso tasto per tutti e due apposta: B e' "indietro"
    //       nel menu, e usarlo qui faceva ritirare la canna mentre
    //       tornavi indietro di una pagina.
    //   44  RIPARO (Q / RB)     cambia esca
    // IL GRILLETTO E' ANALOGICO, e questo e' il guaio.
    // Con una soglia sola, un grilletto tenuto premuto che oscilla
    // intorno a quella soglia sembra mollato e ripremuto decine di volte:
    // e' per questo che appena lanciato ritirava la lenza da solo, col
    // suo bip. Quindi due soglie: si conta premuto sopra 0.25, mollato
    // solo quando scende sotto 0.10. In mezzo resta com'era.
    static float ValoreRT()
    {
        float v = Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 24);
        float w = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, 24);
        if (w > v) v = w;
        if (v <= 0f
            && (Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 0, 24)
             || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 24)))
            v = 1f;
        return v;
    }

    bool rtGiuPrima = false;
    bool rtFronte = false;

    // il fronte si calcola UNA volta per fotogramma, in cima al giro:
    // se lo calcolassimo dentro i rami, nei fotogrammi in cui quel ramo
    // non gira lo stato resterebbe indietro
    void LeggiTasto()
    {
        float v = ValoreRT();
        bool ora = rtGiuPrima ? (v > 0.10f) : (v > 0.25f);
        rtFronte = (ora && !rtGiuPrima);
        rtGiuPrima = ora;
    }

    // dentro la pescata si guarda SEMPRE questo, mai il native diretto,
    // se no si torna al tremolio di prima
    bool TastoGiu() { return rtGiuPrima; }

    bool TastoPremuto() { return rtFronte; }

    // A / INVIO: la ferrata quando il pesce morde
    static bool TastoFerra()
    {
        return Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 201)
            || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 201);
    }

    // X: prende la canna in mano. Niente B, che nel menu torna indietro.
    static bool TastoCanna()
    {
        return Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 203)
            || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 203);
    }


    static bool TastoEsca()
    {
        return Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 44)
            || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 44);
    }

    // I SUONI FATTI DA NOI.
    // I nomi dei suoni di GTA in tanti casi non si sentono: la banca
    // audio non e' caricata e non c'e' verso di saperlo da fuori. Allora
    // il fruscio del lancio ce lo mettiamo noi, con un file wav dentro
    // la cartella "suoni". E' fuori dal mixer del gioco - il volume e'
    // quello di Windows - ma si sente sempre e lo puoi cambiare tu:
    // basta metterci il tuo wav con lo stesso nome.
    static SoundPlayer lettore = null;

    static void SuonoFile(string file)
    {
        if (file == null || file.Length == 0) return;
        try
        {
            string f = Path.Combine(Path.Combine(MY_DIR, "suoni"), file);
            if (!File.Exists(f)) return;
            if (lettore == null) lettore = new SoundPlayer();
            lettore.SoundLocation = f;
            lettore.Play();
        }
        catch { }
    }

    // gli stessi suoni della mod vecchia
    static void Suono(string nome, string set)
    {
        try { Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, nome, set, true); }
        catch { }
    }

    static void Vibra(int ms, int forza)
    {
        try { Function.Call(Hash.SET_CONTROL_SHAKE, 0, ms, forza); }
        catch { }
    }

    // quanto tiene l'attrezzatura montata: comanda il pezzo piu' debole
    float TenutaBorsa()
    {
        float lenza = 0f, friz = 0f, canna = 0f;
        int i;
        foreach (KeyValuePair<string, int> kv in borsa)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length < 2) continue;
            int id = Numero(c[1]);
            if (c[0] == "lenza")
                for (i = 0; i < lenze.Count; i++)
                    if (lenze[i].Id == id && lenze[i].Kg > lenza) lenza = lenze[i].Kg;
            if (c[0] == "mulinello")
                for (i = 0; i < mulinelli.Count; i++)
                    if (mulinelli[i].Id == id && mulinelli[i].Frizione > friz) friz = mulinelli[i].Frizione;
            if (c[0] == "canna")
                for (i = 0; i < canne.Count; i++)
                    if (canne[i].Id == id)
                    {
                        float k = MaxKg(canne[i].LenzaKg);
                        if (k > canna) canna = k;
                    }
        }
        float t = lenza;
        if (friz > 0f && friz < t) t = friz;
        if (canna > 0f && canna < t) t = canna;
        if (t <= 0f) t = 0.9f;
        return t;
    }

    // "1.50 - 3.20" -> 3.20
    static float MaxKg(string s)
    {
        if (s == null) return 0f;
        string[] p = s.Split('-');
        string ultimo = p[p.Length - 1].Trim();
        float v;
        if (float.TryParse(ultimo, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
        return 0f;
    }


    // ==================================================================
    //  LE REGOLE VERE DELL'ABBOCCATA
    //  Sono sempre accese (il modo arcade "abbocca di tutto" non c'e' piu').
    //  Per il sorteggio del pesce contano:
    //    1. il pesce vive in questa zona                (dato vero, wiki)
    //    2. la misura dell'amo e' la sua, o vicina      (dato vero, wiki)
    //    3. la tua lenza lo regge
    //    4. e' l'ora in cui quel pesce mangia           (biologia reale)
    //    5. quanto e' raro                              (dato vero, wiki)
    //    6. la canna e l'amo giusti per la sua famiglia
    //    7. la temperatura dell'acqua per quel pesce    (numeri nostri, temperature_pesci.txt)
    //  L'esca non scarta: pesce estratto con esca non sua abbocca 1 su 3.
    //  L'amo decide anche la taglia (comune / trofeo / unico).
    // ==================================================================

    // la scala degli ami, dal piu' piccolo al piu' grosso.
    // Sono le 27 misure che il negozio vende davvero.
    static readonly string[] AMI_SCALA = new string[] {
        "#16", "#14", "#12", "#10", "#8", "#6", "#4", "#2", "#1",
        "#1/0", "#2/0", "#3/0", "#4/0", "#5/0", "#6/0", "#7/0", "#8/0", "#9/0",
        "#10/0", "#11/0", "#12/0", "#13/0", "#14/0", "#15/0", "#16/0",
        "#17/0", "#18/0" };

    static int PostoAmo(string m)
    {
        if (m == null) return -1;
        m = m.Trim();
        if (m.Length == 0) return -1;
        if (m[0] != '#') m = "#" + m;
        int i;
        for (i = 0; i < AMI_SCALA.Length; i++)
            if (AMI_SCALA[i] == m) return i;
        return -1;
    }

    // "#4 - #3/0" diventa da 6 a 11.  "#8" diventa da 4 a 4.
    static void RangeAmo(string s, out int da, out int a)
    {
        da = -1; a = -1;
        if (s == null) return;
        string[] pz = s.Split('-');
        da = PostoAmo(pz[0]);
        a = (pz.Length > 1) ? PostoAmo(pz[pz.Length - 1]) : da;
        if (da < 0) da = a;
        if (a < 0) a = da;
        if (a >= 0 && da >= 0 && a < da) { int t = da; da = a; a = t; }
    }

    // l'amo che hai in punta adesso: o l'amo, o quello della testina
    int PostoAmoMontato()
    {
        int id; string img, nome;
        if (!Montato("terminale", out id, out img, out nome))
        {
            // a spinning l'amo e' quello dell'artificiale (colonna "amo")
            int ida = InUso("artificiale");
            if (ida < 0) return -1;
            int j;
            for (j = 0; j < artificiali.Count; j++)
                if (artificiali[j].Id == ida) return PostoAmo(artificiali[j].Amo);
            return -1;
        }
        int i;
        for (i = 0; i < terminali.Count; i++)
            if (terminali[i].Id == id)
            {
                if (terminali[i].Cat == "amo") return PostoAmo(terminali[i].Misura);
                if (terminali[i].Cat == "jig") return PostoAmo(terminali[i].Kg);
                return -1;
            }
        return -1;
    }

    // Un pesce che prende il #10 col #8 o col #12 ci prova ancora, col
    // #1/0 non ci pensa nemmeno: sono cinque misure di distanza.
    static readonly float[] AMO_FUORI = new float[] { 1f, 0.55f, 0.25f, 0.08f };

    float QuantoValeAmo(Specie s)
    {
        int mio = PostoAmoMontato();
        if (mio < 0) return 1f;              // niente amo: non filtriamo
        int da, a;
        RangeAmo(s.Amo, out da, out a);
        if (da < 0 || a < 0) return 1f;      // pesce senza misura sul wiki
        int fuori = 0;
        if (mio < da) fuori = da - mio;
        else if (mio > a) fuori = mio - a;
        if (fuori >= AMO_FUORI.Length) return 0f;
        return AMO_FUORI[fuori];
    }

    // L'ESCA DEVE ESSERE UNA DELLE SUE.
    // Le liste vengono dalla pagina del pesce sul wiki: "Preferred baits"
    // e "Preferred lures". Il controllo e' secco tutte e due le volte:
    // o quell'esca e' fra le sue, o quel pesce non c'e'.
    bool EscaGiusta(Specie s)
    {
        int i;
        int id; string img, nome;
        if (Montato("artificiale", out id, out img, out nome))
        {
            if (s.Art == null || s.Art.Length == 0) return false;
            for (i = 0; i < s.Art.Length; i++)
                if (s.Art[i] == id) return true;
            return false;
        }
        if (escaMontata >= 0 && s.Esche != null)
            for (i = 0; i < s.Esche.Length; i++)
                if (s.Esche[i] == escaMontata) return true;
        if (s.Esche == null || s.Esche.Length == 0) return true;
        return false;
    }

    // l'ora del giorno secondo l'abitudine vera della specie
    float QuantoValeOra(string quando)
    {
        return QuantoValeOraAlle(quando, Function.Call<int>(Hash.GET_CLOCK_HOURS));
    }

    float QuantoValeOraAlle(string quando, int hh)
    {
        bool notte = (hh >= 21 || hh < 5);
        bool piena = (hh >= 8 && hh < 18);
        bool mezza = (!notte && !piena);          // 5-8 e 18-21
        if (quando == "notte") return notte ? 1f : (mezza ? 0.45f : 0.12f);
        if (quando == "alba_tramonto") return mezza ? 1f : (piena ? 0.35f : 0.30f);
        if (quando == "giorno") return piena ? 1f : (mezza ? 0.45f : 0.10f);
        return 0.75f;                            // sempre: nessun picco suo
    }

    // quanto pesa la rarita': 1 comunissimo, 5 rarissimo
    static readonly float[] PESO_RARITA = new float[] { 60f, 100f, 55f, 28f, 12f, 5f };

    // LA TEMPERATURA. Stessa formula del trainer, tenerle uguali.
    // GTA non ha una temperatura: questa ce la calcoliamo noi.
    float GradiAria()
    {
        int hh = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        int mi = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
        return GradiAriaAlle(hh + mi / 60f);
    }

    // la stessa cosa, a un'ora qualunque: serve al grafico della giornata
    float GradiAriaAlle(float ora)
    {
        float fase = (ora - 4f) / 24f * 6.2831853f;
        float t = 20f + 7f * (-(float)Math.Cos(fase));
        t += GradiDelMeteoPesca();
        try
        {
            float z = Game.Player.Character.Position.Z;
            if (z > 50f) t -= (z - 50f) * 0.006f;
        }
        catch { }
        return t;
    }

    string MeteoOra()
    {
        string m = "CLEAR";
        try
        {
            int h = Function.Call<int>(Hash.GET_PREV_WEATHER_TYPE_HASH_NAME);
            string[] w = new string[] {
                "EXTRASUNNY", "CLEAR", "CLOUDS", "SMOG", "FOGGY", "OVERCAST",
                "RAIN", "THUNDER", "CLEARING", "NEUTRAL", "SNOW", "BLIZZARD",
                "SNOWLIGHT", "XMAS", "HALLOWEEN" };
            int i;
            for (i = 0; i < w.Length; i++)
                if (Function.Call<int>(Hash.GET_HASH_KEY, w[i]) == h)
                { m = w[i]; break; }
        }
        catch { }
        return m;
    }

    // l'icona del meteo per l'HUD (img\hud\meteo): di notte col sereno la luna
    string IconaMeteoHud()
    {
        string m = MeteoOra();
        int hh = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        bool notte = (hh >= 21 || hh < 5);
        if (m == "EXTRASUNNY" || m == "CLEAR") return notte ? "luna" : "sole";
        if (m == "CLEARING" || m == "SMOG" || m == "NEUTRAL") return notte ? "luna" : "variabile";
        if (m == "RAIN") return "pioggia";
        if (m == "THUNDER") return "temporale";
        if (m == "SNOW" || m == "SNOWLIGHT" || m == "BLIZZARD" || m == "XMAS") return "tormenta";
        return "nuvole";
    }

    // i meteo di GTA raggruppati in cinque, per i grafici (gen_attivita.py)
    string ClasseMeteo()
    {
        string m = MeteoOra();
        if (m == "EXTRASUNNY") return "sole";
        if (m == "CLEAR" || m == "CLEARING" || m == "SMOG" || m == "NEUTRAL") return "sereno";
        if (m == "RAIN" || m == "THUNDER") return "pioggia";
        if (m == "SNOW" || m == "SNOWLIGHT" || m == "BLIZZARD" || m == "XMAS") return "neve";
        return "nuvole";
    }

    float GradiDelMeteoPesca()
    {
        string m = MeteoOra();
        if (m == "EXTRASUNNY") return 4f;
        if (m == "CLEAR") return 2f;
        if (m == "CLEARING" || m == "SMOG") return 1f;
        if (m == "OVERCAST") return -1f;
        if (m == "FOGGY") return -2f;
        if (m == "RAIN") return -4f;
        if (m == "THUNDER") return -5f;
        if (m == "SNOWLIGHT") return -10f;
        if (m == "SNOW" || m == "XMAS") return -12f;
        if (m == "BLIZZARD") return -15f;
        return 0f;
    }

    // l'acqua e' lenta: si muove la meta' dell'aria
    float GradiAcqua() { return 16f + (GradiAria() - 20f) * 0.45f; }

    // quanto e' viva l'acqua adesso. Numero nostro: il wiki dice solo che
    // col freddo si pesca meglio a mezzogiorno, non da' una formula.
    // Quanto e' viva l'acqua adesso = il pesce di questo posto che sta
    // meglio con la temperatura di adesso (temperature_pesci.txt). Se uno
    // e' alla sua ottima l'acqua e' viva (1); se tutti stanno ai bordi si
    // aspetta di piu'; se nessuno e' nel suo intervallo, quasi ferma.
    float AttivitaAcqua()
    {
        int lu = LuogoQui();
        float f = 0f;
        int i;
        for (i = 0; i < pesci.Count; i++)
        {
            Specie s = pesci[i];
            if (lu >= 0 && s.Zone != null && !PesceQui(s, lu)) continue;
            float v = QuantoValeTemperatura(s);
            if (v > f) f = v;
        }
        if (f < 0.15f) f = 0.15f;
        if (f > 1f) f = 1f;
        return f;
    }

    // quanto aspetti prima che qualcosa prenda
    // COSA MANCA PER POTER LANCIARE.
    // Serve del filo sul mulinello e, in punta, qualcosa che agganci:
    // un amo, una testina, un rig o un artificiale. Il galleggiante e il
    // piombo da soli non pescano niente.
    string CosaMancaPerLanciare()
    {
        int id; string img, nome;
        if (!Montato("lenza", out id, out img, out nome) || metriInBobina <= 0)
            return "Non hai lenza sul mulinello: rimonta una bobina.";
        bool aggancia = (Armato("terminale") >= 0) || (InUso("artificiale") >= 0);
        if (!aggancia)
            return "In punta non c'e' niente: monta un amo o un artificiale.";
        return "";
    }

    // L'ATTESA DOPO IL LANCIO. La distanza del lancio non c'entra piu':
    // base fissa (attesa_base, ms) divisa per quanto e' viva l'acqua, piu'
    // un pezzo a caso (attesa_caso). Numeri nostri, in config.
    int AttesaAbboccata(float potenza)
    {
        int attesa = (int)LeggiF("attesa_base", 60000f);
        float att = AttivitaAcqua();
        attesa = (int)(attesa / att);
        return attesa + caso.Next((int)LeggiF("attesa_caso", 6000f));
    }

    // ---------- LA TECNICA ----------
    // Il wiki dice QUALI PESCI PRENDONO L'ARTIFICIALE (sezione "Preferred
    // lures" della loro pagina): quelli sono i predatori. La regola che
    // ci mettiamo noi e' che il predatore a galleggiante con l'esca
    // naturale abbocca poco, e quando abbocca e' piccolo. Col cucchiaino
    // e la canna giusta abbocca spesso e si aprono i grossi. Non e' un
    // divieto, e' una percentuale.
    bool AllArtificiale()
    {
        int id; string img, nome;
        return Montato("artificiale", out id, out img, out nome);
    }

    string TipoCannaOra()
    {
        int id; string img, nome;
        if (!Montato("canna", out id, out img, out nome)) return "";
        int i;
        for (i = 0; i < canne.Count; i++)
            if (canne[i].Id == id) return canne[i].Tipo;
        return "";
    }

    static bool CannaDaLancio(string t)
    {
        return (t == "spinning" || t == "casting" || t == "mare");
    }

    // torna quanto pesa la tecnica, e quanto sono improbabili i grossi:
    // pendenza 1 = tutti i pesi uguali, piu' e' alta piu' escono piccoli
    float QuantoValeTecnica(Specie s, out float pendenza)
    {
        pendenza = 1.7f;                 // i grossi sono gia' piu' rari
        bool predatore = (s.Pred == 1);
        string tc = TipoCannaOra();
        if (AllArtificiale())
        {
            // il cucchiaino con una match o una telescopica non lo lanci
            if (!CannaDaLancio(tc)) return 0.15f;
            pendenza = 1.2f;
            return 1f;
        }
        if (predatore) { pendenza = 6f; return 0.25f; }
        return 1f;
    }

    // ==============================================================
    //  L'ATTREZZO GIUSTO PER QUEL PESCE
    //  Non e' il peso: quello lo filtra gia' la lenza. E' che gli
    //  attrezzi il wiki li chiama col nome del pesce a cui servono -
    //  "Carp rods", "Carp Hooks", "Catfish", "Feeder rods", "Saltwater
    //  rods" - e se esistono un motivo ce l'hanno. Con la canna e l'amo
    //  suoi la carpa abbocca come deve; con una canna qualunque abbocca
    //  lo stesso, ma parecchio meno.
    //  L'accoppiata famiglia-attrezzo la scriviamo noi, ma il nome da
    //  cui la ricaviamo e' del wiki.
    // ==============================================================
    static string[] CanneBuonePer(string famiglia)
    {
        if (famiglia == "Carp family")
            return new string[] { "carpa", "feeder", "spod", "fondo" };
        if (famiglia == "Bream and Roach family")
            return new string[] { "match", "feeder", "telescopica" };
        if (famiglia == "Panfish family" || famiglia == "Crappie family"
            || famiglia == "Shinners and Minnows family" || famiglia == "Goby family")
            return new string[] { "match", "telescopica", "feeder" };
        if (famiglia == "Catfish family")
            return new string[] { "fondo", "carpa", "mare" };
        if (famiglia == "Sturgeon family")
            return new string[] { "fondo", "carpa" };
        if (famiglia == "Pike family" || famiglia == "Bass family"
            || famiglia == "Perch family" || famiglia == "Gar family"
            || famiglia == "Piranhas family")
            return new string[] { "spinning", "casting" };
        if (famiglia == "Trout and Char family" || famiglia == "Salmon family")
            return new string[] { "spinning", "casting", "match" };
        if (famiglia == "Saltwater Fish" || famiglia == "Tuna family"
            || famiglia == "Marlin and Mackerel family" || famiglia == "Drum family")
            return new string[] { "mare", "casting" };
        return null;                       // famiglia non scritta: non giudichiamo
    }

    // l'amo specialista: il wiki li vende col nome del pesce
    static string FamigliaDellAmo(string modello)
    {
        if (modello == null) return "";
        if (modello.IndexOf("Carp") >= 0) return "Carp family";
        if (modello.IndexOf("Catfish") >= 0) return "Catfish family";
        if (modello.IndexOf("Offset") >= 0) return "Bass family";
        if (modello.IndexOf("Livebait") >= 0) return "Saltwater Fish";
        return "";                         // Kirby, Octopus e simili: per tutto
    }

    string ModelloAmoMontato()
    {
        int id; string img, nome;
        if (!Montato("terminale", out id, out img, out nome)) return "";
        int i;
        for (i = 0; i < terminali.Count; i++)
            if (terminali[i].Id == id)
                return (terminali[i].Cat == "amo") ? terminali[i].Modello : "";
        return "";
    }

    // canna giusta + amo giusto = percentuale piena. Canna qualunque =
    // poco piu' della meta'. Canna di un altro mestiere = un terzo.
    // Ma non e' mai zero.
    float QuantoValeAttrezzo(Specie s)
    {
        float v = 1f;
        string[] buone = CanneBuonePer(s.Famiglia);
        if (buone != null)
        {
            string tc = TipoCannaOra();
            bool giusta = false;
            int i;
            for (i = 0; i < buone.Length; i++)
                if (buone[i] == tc) { giusta = true; break; }
            if (!giusta)
            {
                if (tc == "telescopica" || tc == "fondo" || tc == "match") v *= 0.60f;
                else v *= 0.35f;
            }
        }
        string fa = FamigliaDellAmo(ModelloAmoMontato());
        if (fa.Length > 0)
        {
            if (fa != s.Famiglia) v *= 0.55f;   // amo specialista di un altro
        }
        else v *= 0.80f;                        // amo generico: va, non e' il suo
        return v;
    }

    // IL PESCE CHE PRENDE, con tutte le regole accese.
    // Torna -1 se con quello che hai montato, qui e a quest'ora, non
    // abbocca niente: e allora si continua ad aspettare.
    int PescaUnPesceVero(float tenuta, out float kg)
    {
        kg = 0f;
        int lu = LuogoQui();
        int cal = CaldoDellEsca();
        string spCal = (cal >= 0) ? pcSpecie[cal] : "";
        float bonusTaglia = (cal >= 0) ? pcBonus[cal] : 1f;
        List<int> buoni = new List<int>();
        List<float> pesi = new List<float>();
        float somma = 0f;
        int i;
        for (i = 0; i < pesci.Count; i++)
        {
            Specie s = pesci[i];
            if (s.KgC > tenuta) continue;
            if (lu >= 0 && s.Zone != null)
            {
                if (!PesceQui(s, lu)) continue;
            }
            // L'ESCA NON SCARTA PIU': il pesce entra lo stesso, e se
            // l'esca non e' la sua lo decide dopo (abbocca 1 su 3).
            float pa = QuantoValeAmo(s);
            if (pa <= 0f) continue;
            float po = QuantoValeOra(s.Quando);
            float pend;
            float pt = QuantoValeTecnica(s, out pend) * QuantoValeAttrezzo(s);
            if (pt <= 0f) continue;
            int r = s.Rarita;
            if (r < 1 || r > 5) r = 3;
            float ptemp = QuantoValeTemperatura(s);
            if (ptemp <= 0f) continue;
            float peso = PESO_RARITA[r] * pa * po * pt * ptemp;
            // il punto caldo vale anche con le regole vere: se l'esca sta
            // sopra il posto di quella specie, quel pesce pesa sei volte
            if (spCal.Length > 0 && s.Nome == spCal) peso = peso * 6f;
            if (peso <= 0.01f) continue;
            buoni.Add(i);
            pesi.Add(peso);
            somma += peso;
        }
        if (buoni.Count == 0 || somma <= 0f) return -1;

        float tiro = (float)caso.NextDouble() * somma;
        int scelto = buoni[buoni.Count - 1];
        for (i = 0; i < buoni.Count; i++)
        {
            tiro -= pesi[i];
            if (tiro <= 0f) { scelto = buoni[i]; break; }
        }

        Specie sp = pesci[scelto];
        float alto = TettoUnico(sp);
        float tettoAmo = TettoAmo(sp);
        if (alto > tettoAmo) alto = tettoAmo;
        alto = TettoPausa(sp, alto);
        // IL POSTO HA UNA TAGLIA MASSIMA (aree_livello.txt): nei laghetti
        // del primo livello escono solo i comuni, i trofei stanno piu' in la'
        int tmA = TagliaMaxArea(lu);
        if (tmA == 1 && alto > sp.KgC) alto = sp.KgC;
        if (tmA == 2)
        {
            float trofeo2 = (sp.KgT > sp.KgC) ? sp.KgT : sp.KgC;
            if (sp.KgU > sp.KgT && trofeo2 >= sp.KgU) trofeo2 = sp.KgU - 0.001f;
            if (alto > trofeo2) alto = trofeo2;
        }
        if (alto > tenuta) alto = tenuta;
        float basso = sp.KgC * 0.6f;
        if (basso < 0.05f) basso = 0.05f;
        if (alto < basso) alto = basso;
        // la taglia non e' a caso piatta: con la tecnica sbagliata escono
        // quasi solo i piccoli, con quella giusta si aprono i grossi
        float pend2;
        QuantoValeTecnica(sp, out pend2);
        if (pend2 < 1f) pend2 = 1f;
        float tt = (float)Math.Pow(caso.NextDouble(), pend2);
        // sul punto profondo il sorteggio del peso si sposta verso l'alto
        if (bonusTaglia > 1f) tt = (float)Math.Pow(tt, 1.0 / bonusTaglia);
        kg = basso + tt * (alto - basso);
        if (kg < 0.05f) kg = 0.05f;
        return scelto;
    }

    // L'AMO DECIDE FINO A CHE TAGLIA SI PESCA (regola di Fishing Planet).
    // Il pesce ha il suo range di ami sul wiki: con l'amo alla misura
    // piccola del range escono solo i comuni, dalla meta' in su anche i
    // trofei, con la misura grande anche gli unici. Fuori dal range si
    // conta la misura del range piu' vicina. amo_taglia=0 spegne.
    float TettoAmo(Specie sp)
    {
        float unico = TettoUnico(sp);
        if (LeggiF("amo_taglia", 1f) < 0.5f) return unico;
        int mio = PostoAmoMontato();
        if (mio < 0) return unico;
        int da, a;
        RangeAmo(sp.Amo, out da, out a);
        if (da < 0 || a < 0) return unico;
        if (mio < da) mio = da;
        if (mio > a) mio = a;
        float trofeo = (sp.KgT > sp.KgC) ? sp.KgT : sp.KgC;
        // anche il trofeo ha il suo margine sopra il peso del wiki
        // (trofeo_extra), ma senza arrivare al peso dell'unico: quello
        // e' dell'amo grande
        trofeo = trofeo * (1f + LeggiF("trofeo_extra", 10f) / 100f);
        if (sp.KgU > sp.KgT && trofeo >= sp.KgU) trofeo = sp.KgU - 0.001f;
        if (a <= da) return unico;
        float f = (float)(mio - da) / (float)(a - da);
        if (f >= 0.999f) return unico;
        if (f >= 0.5f) return trofeo;
        return sp.KgC;
    }

    // LA PAUSA DOPO IL COLPO GROSSO: preso un unico, per un po' quella
    // specie non ne da' altri (tetto al trofeo); preso un trofeo, tetto al
    // comune. Minuti veri (unico_pausa_min, trofeo_pausa_min), salvati.
    Dictionary<string, long> pausaUnico = new Dictionary<string, long>();
    Dictionary<string, long> pausaTrofeo = new Dictionary<string, long>();

    static long AdessoSec()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }

    float TettoPausa(Specie sp, float alto)
    {
        long ora = AdessoSec();
        long t;
        if (pausaUnico.TryGetValue(sp.Nome, out t) && t > ora)
        {
            float trofeo = (sp.KgT > sp.KgC) ? sp.KgT : sp.KgC;
            if (alto > trofeo) alto = trofeo;
        }
        if (pausaTrofeo.TryGetValue(sp.Nome, out t) && t > ora)
        {
            if (alto > sp.KgC) alto = sp.KgC;
        }
        return alto;
    }

    void SegnaColpoGrosso(Specie sp, float kg)
    {
        long ora = AdessoSec();
        if (sp.KgU > 0f && kg >= sp.KgU)
        {
            pausaUnico[sp.Nome] = ora + (long)(LeggiF("unico_pausa_min", 20f) * 60f);
            pausaTrofeo[sp.Nome] = ora + (long)(LeggiF("trofeo_pausa_min", 5f) * 60f);
        }
        else if (sp.KgT > 0f && kg >= sp.KgT)
            pausaTrofeo[sp.Nome] = ora + (long)(LeggiF("trofeo_pausa_min", 5f) * 60f);
    }

    // IL TETTO DEGLI UNICI.
    // La tabella del wiki dice il peso dell'unico, ma in gioco ogni tanto
    // esce qualcosa di piu' grosso ancora: Clear Muskie dati a 30 kg
    // pescati da 35-37. Il tetto vero e' quello del wiki piu' una
    // percentuale, "unico_extra" in config.ini (di suo +20%). E' raro:
    // per arrivarci il sorteggio deve gia' essere sul massimo.
    float TettoUnico(Specie sp)
    {
        float alto = sp.KgU > sp.KgC ? sp.KgU : sp.KgC;
        if (sp.KgU <= sp.KgC) return alto;
        float piu = LeggiF("unico_extra", 20f) / 100f;
        if (piu < 0f) piu = 0f;
        return alto * (1f + piu);
    }

    // i denti: senza leader il pesce che morde il filo te lo porta via.
    // Pronta ma non ancora agganciata alla lotta.
    bool ServeLeader(Specie s)
    {
        if (s == null || s.Denti == 0) return false;
        // il leader ha la sua casella: o c'e' o non c'e'
        return Armato("leader") < 0;
    }

    Random caso = new Random();

    void Pescata()
    {
        Ped p = Game.Player.Character;
        if (p == null || !p.Exists() || p.IsDead)
        {
            if (inScena) ScenaGiu(p);
            fase = FASE_FERMO;
            return;
        }

        int now = Game.GameTime;
        bool inRiva = VicinoAllAcqua();
        inRivaOra = inRiva;
        RegolaProfondita(now);
        LeggiTasto();
        TieniProvaPesce(now);

        // Mentre si pesca la levetta sinistra e' della pesca e non delle
        // gambe, e i grilletti sono della canna e non delle armi.
        // Sono gli stessi blocchi della mod vecchia, piu' la mischia.
        if (fase != FASE_FERMO)
        {
            // destra e sinistra girano la frizione del mulinello
            // (non con la ruota aperta: li' destra e sinistra sono suoi)
            if (now > tastoFriz && !ruotaAperta)
            {
                bool sx = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 174)
                       || Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 0, 174);
                bool dx = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 175)
                       || Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 0, 175);
                if (sx && frizione > 1)
                {
                    frizione--; tastoFriz = now + 160;
                    Suono("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                }
                else if (dx && frizione < PosFrizione())
                {
                    frizione++; tastoFriz = now + 160;
                    Suono("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                }
            }
            // CI SI GIRA COL DIREZIONALE, NON CON LA TELECAMERA.
            // Prima il pescatore si metteva dove guardava la telecamera:
            // bastava girargli intorno per vederlo in faccia e lui si
            // voltava insieme a te, finendo a pescare nell'erba dietro le
            // spalle. Adesso la telecamera e' libera - ci giri intorno
            // quanto vuoi - e la canna la sposti tu con la levetta
            // sinistra: la lenza in acqua spazza a destra e a sinistra e
            // si porta dietro il pesce.
            try
            {
                float ax = Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 30);
                float dtG = (float)(now - ultimoGiroCanna) / 1000f;
                if (dtG <= 0f || dtG > 0.2f) dtG = 0.03f;
                ultimoGiroCanna = now;
                if (ax > 0.15f || ax < -0.15f)
                {
                    float g = ax * LeggiF("canna_gira_vel", 55f) * dtG;
                    if (fase == FASE_PRONTO || fase == FASE_CARICA)
                    {
                        // canna fuori dall'acqua: miri dove ti pare
                        // (se cammini ti giri camminando, qui non si tocca)
                        if (!(Cammina() && fase == FASE_PRONTO))
                            Function.Call(Hash.SET_ENTITY_HEADING, p, p.Heading - g);
                    }
                    else
                    {
                        // lenza in acqua: la canna spazza, ma dentro un
                        // settore, se no si finisce a pescare all'indietro
                        // SI MUOVE LA CANNA, NON IL PESCATORE.
                        // Prima girava tutto il corpo: sembrava che si
                        // voltasse, non che accompagnasse la lenza. Ora
                        // la canna va a destra e a sinistra da sola, e il
                        // corpo la segue solo per la parte che gli dici
                        // in "canna_gira_corpo" (0 = sta fermo).
                        float max = LeggiF("canna_gira_max", 40f);
                        scartoCanna -= g;
                        if (scartoCanna > max) scartoCanna = max;
                        if (scartoCanna < -max) scartoCanna = -max;
                        RuotaCanna(p, 0f, scartoCanna);
                        float qc = LeggiF("canna_gira_corpo", 0f);
                        if (qc > 0f)
                            Function.Call(Hash.SET_ENTITY_HEADING, p,
                                          dirBase + scartoCanna * qc);
                        // L'ESCA SI SPOSTA DI CENTIMETRI, NON DI GRADI.
                        // Prima le passavo una frazione dell'angolo della
                        // canna: ma un grado, a venti metri, sono trentacinque
                        // centimetri d'arco - muovevi la canna di un palmo e
                        // il galleggiante attraversava il lago. Adesso lo
                        // spostamento e' una misura vera, e piu' lontano sei
                        // meno angolo serve per farla.
                        float perGrado = LeggiF("esca_trascina_cm", 0.15f) / 100f;
                        escaScarto -= g * perGrado;
                        float latMax = LeggiF("esca_trascina_max_cm", 12f) / 100f;
                        if (escaScarto > latMax) escaScarto = latMax;
                        if (escaScarto < -latMax) escaScarto = -latMax;
                        float dist = metriLenza;
                        if (dist < 1f) dist = 1f;
                        escaDir = escaBase + (float)(Math.Atan2(escaScarto, dist)
                                                     * 180.0 / Math.PI);
                        AggiornaEsca(p, metriLenza);
                    }
                }
                // levetta indietro: la strappata, tira la lenza verso di te
                float ay = Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 31);
                // "strappata=0" in config la spegne (prova: col cammino
                // la levetta indietro faceva un passo avanti)
                if (ay > 0.5f && now > miaStrappataDa
                    && LeggiF("strappata", 1f) > 0.5f
                    && (fase == FASE_ACQUA || fase == FASE_LOTTA))
                {
                    miaStrappataDa = now + (int)LeggiF("anim_strappo_ms", 420f) + 220;
                    AvviaStrappo(p);
                    // LA STRAPPATA NON RECUPERA.
                    // E' solo il movimento della canna: il mulinello lo
                    // giri con il grilletto, sempre quello, e basta.
                    // Se in "strappo_metri" ci metti un numero, quella
                    // strappata si porta a casa anche quei metri.
                    float sm = LeggiF("strappo_metri", 0f);
                    if (sm > 0f && fase == FASE_ACQUA)
                    {
                        metriLenza -= sm;
                        if (metriLenza < 0f) metriLenza = 0f;
                        AggiornaEsca(p, metriLenza);
                    }
                }
            }
            catch { }
            // NON si tocca il 44 (RB): il trainer si apre con RB + GIU',
            // e mentre peschi devi poter aprire inventario ed equipaggiamento.
            // Il 27 e' il telefono: SU tirava fuori il cellulare in mezzo
            // a una lotta. Fuori anche 172 e 176, che sono le sue frecce.
            int[] via = new int[] { 30, 31, 24, 25, 22, 23, 21, 257,
                                    140, 141, 142, 143, 263, 264, 45,
                                    27, 172, 176 };
            int qv;
            bool gambe = Cammina() && FaseDiCammino();
            for (qv = 0; qv < via.Length; qv++)
            {
                if (gambe && (via[qv] == 30 || via[qv] == 31)) continue;
                Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, via[qv], true);
            }
            if (gambe)
            {
                // al passo: la levetta a fondo non fa correre
                Function.Call(Hash.SET_PED_MAX_MOVE_BLEND_RATIO, p, 1.0f);
            }
        }

        // ---- fermo: si comincia ----
        if (fase == FASE_FERMO)
        {
            if (!inRiva) return;
            if (p.IsInVehicle() && !p.IsSittingInVehicle()) return;
            // SENZA LICENZA non si dice niente di attrezzatura: stai solo
            // passando di li'. Al massimo il nome del posto, se lo vuoi.
            // Cosa ti manca te lo dice quando hai pagato e vuoi pescare.
            if (!inPesca)
            {
                // NIENTE SCRITTA IN GIRO PER LA MAPPA.
                // Al bordo dei cinquanta metri si accendeva e si spegneva
                // di continuo, e ogni volta suonava la notifica. Dove sei
                // te lo dice il menu della pesca quando lo apri, che e'
                // il posto giusto per saperlo.
                return;
            }

            // IN RIVA NON SI SCRIVE NIENTE.
            // Quello che manca te lo dice il menu quando premi
            // "Inizia a pescare": stare in riva non e' un errore.
            // L'unica cosa che si dice e' la licenza pagata altrove.
            int luOra = LuogoQui();
            if (luOra >= 0 && CodiceLuogo(luOra) != licZona)
            {
                string bzL;
                Messaggio("~y~Non sei nel posto della licenza: l'hai pagata per "
                          + NomeChiosco(licZona, out bzL));
            }
            return;
        }

        // ---- canna in mano, pronta a lanciare ----
        if (fase == FASE_PRONTO)
        {
            // CON LA CANNA IN MANO L'HUD C'E' GIA' TUTTO: barra, frizione
            // col mulinello, armatura. Non si aspetta il lancio.
            HudPesca();
            BarraCanna(0f, 130, 225, 180);
            TacchePrizione();
            PosaFerma(p);
            // CON LA RUOTA APERTA NON SI FA ALTRO: niente lancio, niente
            // cambio esca, niente riporre. I tasti sono della ruota.
            if (ruotaAperta) return;
            if (now > tastoDa && TastoCanna())
            {
                // niente suono qui: sembrava fosse successo qualcosa di grave.
                // Il messaggio basta. I pesci che passano restano: non
                // dipendono dalla canna.
                ViaRoba();
                robaOra = -1;
                Messaggio("~y~Canna ritirata.");
                ScenaGiu(p);
                fase = FASE_FERMO;
                return;
            }
            if (now > tastoDa && TastoEsca()) { CambiaEsca(); tastoDa = now + 300; }
            // IL GRILLETTO VA MOLLATO PRIMA DI RILANCIARE.
            // Recuperavi tenendo premuto, la lenza rientrava e con lo
            // stesso dito ancora giu' ripartiva subito un lancio a caso.
            if (!TastoGiu()) grillettoMollato = true;
            if (now > tastoDa && grillettoMollato && TastoGiu())
            {
                // SENZA ARMATURA NON SI LANCIA.
                // Dopo una rottura restavi con la canna in mano e la
                // lenza nuda: rilanciavi e non poteva abboccare niente,
                // perche' in punta non c'era piu' niente. Adesso te lo
                // dice e non parte il lancio.
                string manca = CosaMancaPerLanciare();
                if (manca.Length > 0)
                {
                    Messaggio(manca);
                    tastoDa = now + 1200;
                    grillettoMollato = false;
                    return;
                }
                grillettoMollato = false;
                fase = FASE_CARICA;
                potenza = 0f;
                potenzaSu = true;
            }
            return;
        }

        // ---- carica il lancio ----
        if (fase == FASE_CARICA)
        {
            // LA CARICA COSTA FATICA: la barra sale piu' piano di prima,
            // cosi' il massimo lo prendi solo se lo tieni premuto davvero
            float vc = LeggiF("carica_vel", 1.25f);
            if (potenzaSu) potenza += vc; else potenza -= vc;
            if (potenza >= 100f) { potenza = 100f; potenzaSu = false; }
            if (potenza <= 0f) { potenza = 0f; potenzaSu = true; }

            HudPesca();
            BarraCanna(potenza / 100f, 165, 95, 235);   // viola: stai caricando
            TacchePrizione();
            Metri(MetriDelLancio(potenza));
            // il corpo si piega indietro e la canna va indietro con lui,
            // tutti e due insieme alla carica
            PosaCarica(p, potenza / 100f);

            if (!TastoGiu())
            {
                // L'ESCA NON SI CONSUMA QUI.
                // Prima ogni lancio si mangiava un boccone: ritiravi a
                // vuoto cento volte e ti restava un verme. Ma il pane
                // sull'amo, se non lo mangia nessuno, quando ritiri c'e'
                // ancora. L'esca si perde quando abbocca il pesce - poi
                // che lo prendi, che si slama o che ti spezza la lenza
                // e' un altro discorso.
                AvviaFrustata(p, potenza / 100f);
                if (sulN.Length > 0) Suono(sulN, sulS);
                SuonoFile(LeggiS("suono_lancio_file", "lancio.wav"));
                dirBase = p.Heading;
                scartoCanna = 0f;
                escaDir = p.Heading;
                escaBase = p.Heading;
                escaScarto = 0f;
                Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                fase = FASE_ACQUA;
                // appena tocca l'acqua il cucchiaino e' in superficie:
                // da li' comincia a scendere
                profEsca = 0f;
                // piu' forte lanci, prima abbocca (sei sul pesce vero).
                // Con le regole vere accese contano anche l'ora, il meteo
                // e la temperatura dell'acqua.
                quandoAbbocca = now + AttesaAbboccata(potenza);
                // se e' finita a terra lo si dice subito, non dopo
                // trenta secondi di attesa a vuoto
                metriLenza = MetriDelLancio(potenza);
                AggiornaEsca(p, metriLenza);
                // FUORI DALL'ACQUA NON SI PESCA.
                // Erba, rocce, asfalto: la lenza rientra da sola e torni
                // con la canna in mano, pronto a rilanciare. Il messaggio
                // c'e', il suono no: quello a ripetizione era un martello.
                if (!EscaSullAcqua())
                {
                    Messaggio("~y~Fuori dall'acqua: la lenza e' rientrata.");
                    metriLenza = 0f;
                    escaInAcqua = false;
                    escaATerra = false;
                    fase = FASE_PRONTO;
                    grillettoMollato = false;
                    tastoDa = now + 300;
                    return;
                }
                tastoDa = now + 900;
            }
            return;
        }

        // ---- lenza in acqua ----
        // Il recupero e' quello della mod vecchia, che funzionava:
        //  - e' ANALOGICO: quanto premi decide quanto ritiri
        //  - il click del mulinello va piu' fitto se giri forte
        //  - recuperare piano AVVICINA l'abboccata: l'esca si muove e il
        //    pesce si incuriosisce. Girare non e' una resa, e' un modo di
        //    pescare.
        //  - la lenza rientrata non e' un fallimento: nessun bip, solo
        //    una riga che te lo dice
        if (fase == FASE_ACQUA)
        {
            HudPesca();
            BarraCanna(0f, 130, 225, 180);
            TacchePrizione();
            Metri(metriLenza);
            DisegnaLenza(now, false);
            // L'ESCA NON SI CAMBIA CON LA LENZA IN ACQUA.
            // L'esca sta sull'amo, e l'amo sta in fondo al lago: per
            // cambiarla si recupera. Prima si poteva, e cambiava il pesce
            // che stava gia' abboccando.
            if (now > tastoDa && TastoEsca())
            {
                tastoDa = now + 600;
            }

            float dtA = Game.LastFrameTime;
            // IL MULINELLO LO GIRA SOLO IL GRILLETTO.
            // Prima recuperava anche la levetta tirata indietro - era
            // cosi' dalla mod vecchia - e cosi' ogni volta che muovevi la
            // canna ti ritirava la lenza. La levetta serve a muovere la
            // canna, il grilletto a recuperare, e non si mischiano.
            float ritiro = ValoreRT();
            if (ritiro < 0.1f) ritiro = 0f;
            if (ritiro > 1f) ritiro = 1f;

            // A SPINNING L'ESCA VIVE IN VERTICALE.
            // Ferma affonda, tirata sale: e' il tira-e-molla che la fa
            // sembrare un pesciolino. Quanto in fretta lo dicono
            // "spin_affonda" e "spin_sale" in config.ini.
            if (!QuadranteGall())
            {
                if (ritiro > 0f)
                    profEsca -= LeggiF("spin_sale", 0.55f) * ritiro * dtA;
                else
                    profEsca += LeggiF("spin_affonda", 0.30f) * dtA;
                if (profEsca < 0f) profEsca = 0f;
                if (profEsca > 1f) profEsca = 1f;
            }
            // il braccio gira il mulinello solo mentre lo giri tu, e va
            // veloce quanto lo giri
            if (!Frustata(p, now) && !Strappo(p, now))
            {
                if (ritiro > 0f) Posa(p, ClipMulinello(), 0.5f + ritiro * ritiro * 2.6f);
                else PosaFerma(p);
            }

            if (ritiro > 0f)
            {
                // IL RECUPERO NON E' PIATTO.
                // Premendo appena si accompagna l'esca, premendo a fondo si
                // recupera davvero in fretta: la velocita' sale col quadrato
                // di quanto premi, e la moltiplica il mulinello che hai.
                // pianissimo se sfiori la leva, velocissimo a fondo:
                // la curva e' ripida apposta, non lineare
                float velRec = (0.10f + 13.5f * (float)Math.Pow(ritiro, 2.6))
                             * FattoreRecupero();
                metriLenza -= velRec * dtA;
                AggiornaEsca(p, metriLenza);
                quandoAbbocca -= (int)(600f * ritiro * dtA);
                if (now > giroMulinello)
                {
                    // il click segue la velocita' vera, non quanto premi
                    int passo = (int)(1100f / (velRec + 1.5f));
                    if (passo < 14) passo = 14;
                    if (passo > 240) passo = 240;
                    giroMulinello = now + passo;
                    try { Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "HACKING_CLICK", 0, true); }
                    catch { }
                }
                if (metriLenza <= 0.2f)
                {
                    metriLenza = 0f;
                    fase = FASE_PRONTO;
                    grillettoMollato = false;
                    tastoDa = now + 400;
                    if (robaOra >= 0) RobacciaSu(now);
                    else Messaggio("~y~Lenza ritirata.");
                    return;
                }
            }

            // L'ESCA A TERRA NON PESCA.
            // Se il lancio e' finito sul prato, sull'asfalto o dentro una
            // siepe, non c'e' niente da aspettare: l'attesa si sposta in
            // avanti a ogni giro, quindi non abbocchera' mai, e te lo
            // dice a chiare lettere invece di lasciarti li' a credere di
            // stare pescando.
            if (!EscaSullAcqua())
            {
                Messaggio("~y~Fuori dall'acqua: la lenza e' rientrata.");
                metriLenza = 0f;
                escaInAcqua = false;
                fase = FASE_PRONTO;
                grillettoMollato = false;
                tastoDa = now + 300;
                return;
            }

            // GLI ASSAGGI SONO POCHI E ARRIVANO ALLA FINE.
            // Un pesce non mordicchia per minuti: o e' sotto e prende, o
            // non c'e' e l'acqua sta ferma. I tocchi si sentono solo negli
            // ultimi secondi prima dell'abboccata - quando il pesce e'
            // davvero li' - e sono radi. Dopo uno spavento c'e' calma
            // vera: finche' non passa, non tocca niente.
            bool assaggio = false;
            if (quandoAbbocca - now < (int)LeggiF("assaggio_finestra", 2600f)
                && now > calmaFino)
            {
                if (now > prossimoTocco)
                {
                    prossimoTocco = now + (int)LeggiF("assaggio_pausa", 1200f)
                                  + caso.Next((int)LeggiF("assaggio_pausa_piu", 1400f));
                    toccoFine = now + (int)LeggiF("assaggio_dura", 260f);
                    Vibra(90, 45);
                }
                if (now < toccoFine) assaggio = true;

                // SE STRAPPI MENTRE MORDICCHIA, LO PERDI - MA SOLO A
                // GALLEGGIANTE.
                // Con l'esca ferma sotto il galleggiante il pesce si
                // avvicina e assaggia: se gli strappi via il boccone si
                // stacca. A spinning e' il contrario - recuperare E' la
                // tecnica, l'artificiale deve muoversi per lavorare, e un
                // pesce che insegue non si spaventa perche' tiri. Quindi
                // questa regola vale solo se il galleggiante ce l'hai.
                if (robaOra < 0 && HoIlGalleggiante() && ritiro > LeggiF("strappa_via", 0.35f))
                {
                    // L'ATTESA NUOVA LA DECIDONO LE REGOLE, non un
                    // numero fisso. Il pesce spaventato rimette
                    // l'orologio a zero: quanto ci mette a tornare lo
                    // dice la stessa funzione dell'attesa normale, che
                    // guarda ora, meteo, esca e rarita'. Con le regole
                    // spente sono pochi secondi; con quelle accese puo'
                    // essere parecchio, ed e' giusto cosi'.
                    int ferma = AttesaAbboccata(50f);
                    int minimo = (int)LeggiF("dopo_spavento_ms", 6000f);
                    if (ferma < minimo) ferma = minimo;
                    quandoAbbocca = now + ferma;
                    // e adesso l'acqua sta ferma davvero
                    calmaFino = now + minimo;
                    prossimoTocco = now + minimo;
                    toccoFine = 0;
                    assaggio = false;
                    // niente suono: questa cosa capita spesso e quel
                    // "pam pam" a ripetizione diventa un martello. Il
                    // messaggio scritto basta.
                    Messaggio("~y~Hai tirato troppo presto: se n'e' andato.");
                }
            }
            if (QuadranteGall())
                DisegnaGalleggiante(now, assaggio ? 5f : 0f, assaggio ? 1f : 0f);
            else DisegnaSpinning(now, assaggio ? 1f : 0f);
            GalleggianteInAcqua(now, 0f, assaggio ? 1f : 0f, ritiro);
            if (robaOra >= 0) MuoviRoba(now, false);

            if (now >= quandoAbbocca)
            {
                float tenuta = TenutaBorsa();
                dentiDa = 0;
                // SENZA NIENTE ALL'AMO NON ABBOCCA NESSUNO: solo robaccia,
                // una volta su tre circa (robaccia_prob_senza_esca).
                bool nienteAllAmo = (escaMontata < 0) && (InUso("artificiale") < 0);
                if (nienteAllAmo)
                {
                    if (caso.Next(100) < (int)LeggiF("robaccia_prob_senza_esca", 35f))
                    { ArrivaRobaccia(now); return; }
                    quandoAbbocca = now + 6000 + caso.Next(8000);
                    return;
                }
                pesceQui = PescaUnPesceVero(tenuta, out pesceKg);
                // L'ESCA NON E' LA SUA: abbocca solo 1 su 3 (ha fame). Le
                // altre volte se ne va, e una parte di quelle viene su la
                // robaccia. Con l'esca giusta la robaccia non esce mai.
                if (pesceQui >= 0 && !EscaGiusta(pesci[pesceQui]))
                {
                    if (caso.Next(100) >= (int)LeggiF("esca_sbagliata_abbocca", 33f))
                    {
                        pesceQui = -1;
                        if (caso.Next(100) < (int)LeggiF("robaccia_prob_esca_sbagliata", 25f))
                        { ArrivaRobaccia(now); return; }
                        quandoAbbocca = now + 6000 + caso.Next(8000);
                        return;
                    }
                }
                if (pesceQui < 0)
                {
                    // Non e' un errore: con questo amo, a quest'ora e con
                    // quest'acqua qui non mangia niente. La lenza resta in
                    // acqua e si continua ad aspettare.
                    quandoAbbocca = now + 6000 + caso.Next(8000);
                    return;
                }
                // e qui si vede: il pesce spunta all'amo
                MettiPesce();
                // QUI SE LA MANGIA: e' adesso che il boccone se ne va.
                if (escaMontata >= 0)
                {
                    if (!Consuma("esca", escaMontata))
                    {
                        Messaggio("~y~Esca finita.");
                        escaMontata = -1;
                    }
                    else SalvaStato();
                }
                fase = FASE_ABBOCCA;
                scadeFerrata = now + 1500;
                Vibra(300, 160);
            }
            return;
        }

        // ---- abbocca: ferra! ----
        if (fase == FASE_ABBOCCA)
        {
            HudPesca();
            BarraCanna(0f, 250, 210, 90);
            TacchePrizione();
            Metri(metriLenza);
            DisegnaLenza(now, true);
            if (QuadranteGall()) DisegnaGalleggiante(now, 14f, 2.2f);
            else DisegnaSpinning(now, 2.2f);
            GalleggianteInAcqua(now, 1f, 1f, 1f);
            AggiornaPesce(p, now, true);
            // LA FERRATA E' A (INVIO da tastiera): quando morde si ferra
            // con un tasto suo, non col grilletto del lancio e del recupero
            if (TastoFerra())
            {
                fase = FASE_LOTTA;
                tensione = 30f;
                recuperato = 0f;
                stanchezza = 0f;
                strappoFine = 0;
                strappoDa = 0;
                corsaFine = 0;
                corsaProssima = now + 1500 + caso.Next(3000);
                tastoDa = now + 300;
                return;
            }
            if (now >= scadeFerrata)
            {
                Suono("LOSER", "HUD_AWARDS");
                Messaggio("~y~Se n'e' andato.");
                TogliPesce();
                fase = FASE_PRONTO;
                grillettoMollato = false;
                tastoDa = now + 500;
            }
            return;
        }

        // ---- il pesce in mano: METTILO NELLA RETE o RIBUTTALO IN ACQUA ----
        if (fase == FASE_CARD)
        {
            PosaFerma(p);
            HudPesca();
            BarraCanna(0f, 130, 225, 180);
            TacchePrizione();
            AggiornaPesce(p, now, false);
            DisegnaFiloAppeso();
            Specie sc = pesci[cardPesce];

            // LA FINESTRA DEL PESCE si sposta e si rimpicciolisce dal
            // config: in mezzo allo schermo copriva proprio il pesce
            // che ti penzola dalla canna. Tutto quello che c'e' dentro
            // segue la larghezza, quindi la scheda si scala intera.
            float w = LeggiF("card_larga", 300f);
            float k = w / 380f;
            // i testi hanno un loro moltiplicatore: la finestra si puo'
            // stringere senza che le scritte diventino illeggibili
            float kt = k * LeggiF("card_testi", 1f);
            // senza la riga dei tasti (quelli stanno nella barra in basso);
            // se non puoi tenerlo, una riga in piu' col perche'
            float h = (cardPuoTenere ? 178f : 200f) * k;
            float x = LeggiF("card_x", 1280f - 300f - 24f);
            float y = LeggiF("card_y", 60f);
            float cx = x + w * 0.5f;

            // Un po' di verde d'acqua invece del nero: e' una pescata,
            // non un rapporto di polizia.
            DisegnaRett(x - 2f, y - 2f, w + 4f, h + 4f, 12, 26, 24, 240);
            DisegnaRett(x, y, w, 20f * k, 26, 74, 62, 250);
            DisegnaTesto(sc.Nome, cx, y + 2f * k, 0.33f * kt, 235, 245, 240);

            // la foto del pesce
            DisegnaRett(x, y + 20f * k, w, 122f * k, 46, 48, 54, 235);
            Sprite(sc.Img, x + 7f * k, y + 25f * k, w - 14f * k, 112f * k);

            // comune, trofeo o esemplare unico
            int cr = 245, cg = 245, cb = 250;                        // comune
            if (cardTaglia == "TROFEO") { cr = 130; cg = 225; cb = 180; }
            else if (cardTaglia == "ESEMPLARE UNICO") { cr = 245; cg = 205; cb = 80; }
            DisegnaRett(x, y + 142f * k, w, 16f * k, 20, 46, 40, 245);
            DisegnaTesto(cardTaglia, cx, y + 143f * k, 0.26f * kt, cr, cg, cb);

            // peso, valore, punti
            DisegnaRett(x, y + 158f * k, w, 20f * k, 16, 34, 30, 245);
            DisegnaTesto(cardKg.ToString("0.##", CultureInfo.InvariantCulture) + " kg",
                         x + 72f * k, y + 160f * k, 0.29f * kt, 235, 245, 240);
            DisegnaTesto("$" + cardVale, cx, y + 160f * k, 0.29f * kt, 130, 225, 180);
            DisegnaTesto("+" + cardXp + " XP", x + w - 72f * k, y + 160f * k, 0.29f * kt, 130, 200, 245);

            // I TASTI NON STANNO PIU' QUI: li dice la barra in basso.
            // Se il pesce non si puo' tenere, una riga col perche'.
            if (!cardPuoTenere)
            {
                DisegnaRett(x, y + 178f * k, w, 22f * k, 10, 22, 20, 250);
                DisegnaTesto(cardPerche, cx, y + 181f * k, 0.23f * kt, 245, 205, 80);
            }
            bool tieni = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 201)
                      || Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 201);
            bool ributta = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 202)
                        || Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 202);

            if (now > tastoDa && (tieni || ributta))
            {
                bool inRete = (tieni && cardPuoTenere);
                if (inRete)
                {
                    kgNassa += cardKg;
                    // se c'e' un torneo in corso questo pesce fa punteggio.
                    // Nel diario e nella nassa ci finisce lo stesso, come
                    // sempre: il torneo non toglie niente alla pescata.
                    PesceDelTorneo(sc.Nome, cardKg, cardTaglia);
                    Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                }
                else Suono("BACK", "HUD_FRONTEND_DEFAULT_SOUNDSET");

                // NELLA NASSA CI VA SOLO QUELLO CHE E' NELLA NASSA.
                // Un pesce ributtato non sta nella rete, quindi non compare
                // e non vale un soldo: di quella pescata ti restano gli XP,
                // che quelli te li sei guadagnati lo stesso.
                if (inRete)
                {
                    // A DESTRA CHE PESCE E'.
                    // Comune, trofeo o unico: e' il dato che guardi per
                    // primo, e va nella colonna. Peso, valore e XP
                    // stanno insieme sulla riga sotto, in quest'ordine.
                    string clas = (cardTaglia == "ESEMPLARE UNICO") ? "UNICO" : cardTaglia;
                    string colClas = ColoreClasse(clas);
                    nassaOggi.Add(sc.Nome + "|niente|" + sc.Img
                                  + "|" + clas
                                  + "||" + cardKg.ToString("0.##", CultureInfo.InvariantCulture)
                                  + " kg   $" + cardVale + "   +" + cardXp + " XP"
                                  + "|" + colClas + "|245,205,80");
                    // e il suo valore va nel conto della nassa: si incassa
                    // a fine giornata, quando la nassa si svuota
                    soldiNassa += cardVale;
                }

                cardPesce = -1;
                // nella rete o rimesso in acqua, dalla lenza sparisce
                TogliPesce();
                fase = FASE_PRONTO;
                grillettoMollato = false;
                tastoDa = now + 400;
                SalvaStato();
                RiscriviTutto();
            }
            return;
        }

        // ---- tira e molla ----
        if (fase == FASE_LOTTA)
        {
            DisegnaLenza(now, true);
            AggiornaPesce(p, now, true);

            // I DENTI TAGLIANO IL FILO.
            // Un predatore coi denti, senza il cavetto davanti, la lenza
            // te la sega e basta: non e' questione di quanto tiri. Regge
            // qualche secondo, il tempo di illuderti, poi se ne va con
            // tutto quello che aveva in bocca. Per quelli si monta il
            // leader, e senza non li prendi.
            // I PICCOLI NO.
            // Il cucciolo di luccio da quattro etti la lenza non la sega:
            // e' proprio quello che peschi senza cavetto, prima di
            // passare a quelli da quattro-cinque chili. Il taglio scatta
            // dal peso in su ("denti_kg" in config.ini).
            if (pesceQui >= 0 && pesceQui < pesci.Count
                && pesceKg >= LeggiF("denti_kg", 1.2f)
                && ServeLeader(pesci[pesceQui]))
            {
                if (dentiDa == 0) dentiDa = now;
                int quanto = (int)(LeggiF("denti_secondi", 3.5f) * 1000f);
                if (now - dentiDa > quanto)
                {
                    dentiDa = 0;
                    TogliPesce();
                    PerdiArmatura();
                    Vibra(400, 250);
                    fase = FASE_PRONTO;
                    grillettoMollato = false;
                    tastoDa = now + 600;
                    return;
                }
            }
            float tenuta = TenutaBorsa();
            float forza = pesceKg / tenuta;          // 0..1: quanto tira
            if (forza > 1f) forza = 1f;

            // LA CORDA TIRATA DA DUE PARTI.
            // Se tiro io e tira lui, la lenza non fa metri: sta ferma e si
            // carica. I metri li fa la DIFFERENZA fra le due forze, la
            // tensione la loro SOMMA. Non ha senso guadagnare lenza mentre
            // lui sta scappando.
            float spinta = ValoreRT();
            if (spinta < 0.1f) spinta = 0f;
            if (spinta > 1f) spinta = 1f;

            float dtL = Game.LastFrameTime;

            // ---- ogni tanto il pesce parte ----
            // SI STANCA. Piu' lo contrasti piu' si consuma, e da stanco
            // tira meno forte, per meno tempo, con pause piu' lunghe.
            stanchezza += (0.05f + spinta * 0.09f) * dtL;
            if (stanchezza > 1f) stanchezza = 1f;

            if (now > strappoFine && now > strappoDa)
            {
                // niente ritmo fisso: durata, pausa e forza cambiano
                // ogni volta, se no dopo tre strappi sai gia' cosa fa
                float fresco = 1f - stanchezza * 0.65f;
                strappoFine = now + 400 + caso.Next(1600 + (int)(fresco * 600f));
                strappoDa = strappoFine + 500 + caso.Next(2000)
                          + (int)(stanchezza * 1800f);
                strappoForza = (0.35f + forza * 0.85f) * fresco
                             * (0.65f + (float)caso.NextDouble() * 0.7f);
                if (strappoForza < 0.1f) strappoForza = 0.1f;
                Vibra(150 + caso.Next(200), 90 + (int)(strappoForza * 90f));
            }
            bool tiraLui = (now < strappoFine);
            // e dentro lo strappo non tira piatto: ondeggia
            float onda = 0.75f + 0.25f * (float)Math.Sin(now * 0.011);
            float forzaPesce = tiraLui ? (strappoForza * onda) : 0f;

            // I METRI: la differenza. Positiva recuperi, negativa se ne va,
            // vicino allo zero la corda e' tesa e non si muove nessuno.
            // E ci passa in mezzo la FRIZIONE: tirata il mulinello non
            // molla, lui fatica a prendere lenza e tu ne guadagni un po'
            // di piu'; morbida gli lascia scorrere via i metri. E' questo
            // che pareggia il conto con la tensione, che invece sale.
            float netto = spinta - forzaPesce;
            // mentre guadagni metri giri il mulinello, mentre lui scappa
            // resti in tiro con la canna piegata
            if (!Strappo(p, now))
            {
                if (netto > 0.05f) Posa(p, ClipMulinello(), 0.6f + netto * 0.9f);
                else Posa(p, LeggiS("anim_tira", "idle_b"), 0.25f);
            }
            float mFriz = (netto < 0f) ? Friz(FRIZ_MET) : Friz(FRIZ_GUA);
            metriLenza -= 3.0f * netto * mFriz * dtL;
            // ANCHE IN LOTTA L'ESCA SI MUOVE.
            // Qui non si aggiornava: i metri scendevano ma il punto restava
            // piantato dove aveva abboccato, e la lenza sembrava bloccata.
            AggiornaEsca(p, metriLenza);
            recuperato += 16f * Friz(FRIZ_REC) * netto * dtL;

            // LA TENSIONE: la somma. Quanto tiro io, piu' quanto tira lui
            // filtrato dalla frizione - tirata trasmette tutto, morbida
            // lascia scorrere. E pesa di piu' se il pesce e' grosso per la
            // lenza che hai montato: e' li' che si spezza.
            float caricoLui = forzaPesce * Friz(FRIZ_TEN);
            float carico = spinta + caricoLui;
            tensione += (6f + forza * 30f) * carico * dtL;
            // e sempre un filo di sfogo, tanto piu' quanto meno tiri
            tensione -= 34f * (1f - spinta * 0.7f) * dtL;

            // LA CORSA. Un pesce fresco e pesante parte spesso e lontano,
            // uno stanco o piccolo quasi mai. Mentre corre si porta via
            // metri (di piu' con la frizione morbida), vira da un lato e
            // carica la lenza: con la frizione chiusa e' li' che si rompe.
            float fresco2 = 1f - stanchezza;
            if (now > corsaFine && now > corsaProssima)
            {
                int ogni = (int)(LeggiF("corsa_ogni", 9f) * 1000f);
                corsaProssima = now + ogni / 2 + caso.Next(ogni) + (int)(stanchezza * ogni * 2f);
                if (caso.NextDouble() < (0.25f + 0.75f * forza) * fresco2)
                {
                    int duraC = (int)((1000f + caso.Next(2000)) * (0.6f + 0.4f * fresco2));
                    corsaFine = now + duraC;
                    float metriC = LeggiF("corsa_metri", 12f) * (0.15f + 0.85f * forza)
                                 * (0.4f + 0.6f * fresco2) * Friz(FRIZ_MET);
                    corsaMetriSec = metriC / (duraC / 1000f);
                    corsaVerso = (caso.Next(2) == 0) ? 1f : -1f;
                    Vibra(duraC > 2000 ? 600 : 400, 160 + (int)(forza * 90f));
                }
            }
            if (now < corsaFine)
            {
                metriLenza += corsaMetriSec * dtL;
                float tetto = metriInBobina - 1f;
                if (tetto > 0f && metriLenza > tetto) metriLenza = tetto;
                pesceVerso = corsaVerso;
                escaDir += corsaVerso * LeggiF("corsa_angolo", 60f) * 0.5f * dtL;
                tensione += LeggiF("corsa_tensione", 18f) * (0.3f + 0.7f * forza) * Friz(FRIZ_TEN) * dtL;
                AggiornaEsca(p, metriLenza);
            }

            // il mulinello: canta quando cede lenza, ticchetta quando ne
            // guadagni, tace quando siete fermi a tirare tutti e due
            if (forzaPesce > 0f && netto < 0f)
            {
                if (now > clickPesce)
                {
                    clickPesce = now + 70 + (int)(70f * (1f - strappoForza));
                    try { Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "HACKING_CLICK", 0, true); }
                    catch { }
                    Vibra(70, 45);
                }
            }
            else if (netto > 0.05f && tensione < 85f && now > giroMulinello)
            {
                giroMulinello = now + 200 - (int)(netto * 130f);
                try { Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "HACKING_CLICK", 0, true); }
                catch { }
            }

            if (tensione < 0f) tensione = 0f;
            if (recuperato < 0f) recuperato = 0f;
            if (metriLenza < 0f) metriLenza = 0f;
            HudPesca();
            // -1: i colori li mettono le tacche, blu -> verde -> giallo -> rosso
            BarraCanna(tensione / 100f, -1, 0, 0);
            TacchePrizione();
            // UN TESTO SOLO, sempre uguale: cambiarlo fa suonare il bip
            // di GTA a ogni strappo. Che stia tirando lo vedi dalla barra,
            // lo senti dal mulinello e te lo dice il pad.
            Metri(metriLenza);

            if (tensione >= 100f)
            {
                Suono("LOSER", "HUD_AWARDS");
                PerdiArmatura();
                Vibra(400, 250);
                TogliPesce();
                fase = FASE_PRONTO;
                grillettoMollato = false;
                tastoDa = now + 600;
                return;
            }
            // IL PESCE E' TUO QUANDO E' A RIVA, cioe' quando i metri sono
            // finiti. Prima si vinceva anche col contatore del recupero,
            // che correva per conto suo: capitava di 'prendere' un pesce
            // che stava ancora a sessanta metri.
            // GLI ULTIMI METRI SONO I PIU' DURI.
            // Sotto i tre metri il pesce vede la riva e si impunta: i metri
            // vengono via a fatica. Serve anche a far capire che la presa
            // avviene a zero e non a mezza strada.
            if (metriLenza < 3f && netto > 0f)
                metriLenza += 3.0f * netto * mFriz * dtL * 0.55f;

            if (metriLenza <= 0.5f)
            {
                // la fase la decide Preso(): apre la finestra del pesce e
                // ci resta finche' non scegli. Rimetterla qui a "pronto"
                // chiudeva la finestra nello stesso fotogramma in cui si apriva.
                tastoDa = now + 600;
                Preso();
            }
            return;
        }
    }

    // ============================================================
    //  L'HUD DELLA PESCA - stessa disposizione della mod vecchia,
    //  con l'attrezzatura nuova:
    //    canna in piedi a destra          1174, 374   130x330
    //    colonna del montaggio            1136, 650   112x44, sale di 54
    //    esca in alto a destra            1172, 8     96x38 + quantita'
    //    nassa in basso a sinistra        158, 589    256x102 + chili
    //    barra verticale                  1012, 420   8x120
    //    metri sopra la barra, galleggiante in colonna
    // ============================================================
    // "barra_x": dove sta la barra; con lei si spostano frizione, mulinello
    // e quadrante. La colonna e la canna hanno le loro voci in config.
    float BAR_X { get { return LeggiF("barra_x", 1012f); } }
    const float BAR_W = 8f;
    const float BAR_H = 240f;   // il doppio di prima: era troppo corta

    // DOVE COMINCIA LA BARRA. Da qui scendono la barra, le tacche della
    // frizione e i metri: alzando "barra_y" si avvicina tutto all'acqua.
    // Il quadrante invece sta per conto suo, a "quadrante_y".
    float BarY() { return LeggiF("barra_y", 420f); }
    // il centro del quadrante (galleggiante/spinning) e della scritta del
    // fondo: "quadrante_x", se manca sta sopra la barra
    float QuadX() { return LeggiF("quadrante_x", BAR_X + BAR_W * 0.5f); }

    // LE QUATTRO TACCHE DELLA FRIZIONE, in colonna accanto alla barra.
    // Accese dal basso: una sola = tirata, tutte e quattro = morbida.
    // LA FRIZIONE E' UN CERCHIO A TACCHE, come in Fishing Planet: dodici
    // tacche attorno a un disco scuro, accese in senso orario dall'alto.
    // Ogni tacca e' una posizione di frizione. In mezzo il mulinello.
    //   friz_cx / friz_cy   centro
    //   friz_diam           diametro
    // I CONSIGLI DEI TASTI: una riga in basso al centro dello schermo, a
    // "consigli_dal_fondo" dal fondo, che cambia con quello che stai
    // facendo. Ogni consiglio e' icona + testo; la riga si centra da sola
    // (la larghezza del testo e' stimata: "consigli_car" pixel a lettera).
    void Consigli()
    {
        List<string> ic = new List<string>();
        List<string> tx = new List<string>();
        // ogni voce: icona del pad | tasto di tastiera | testo
        if (fase == FASE_FERMO)
        {
            Voce(ic, tx, "lb", "TAB", L("Manage tackle", "Gestisci l'armatura"));
            Voce(ic, tx, "rb+croce_dx", "F7", L("Fishing menu", "Menu della pesca"));
        }
        else if (fase == FASE_PRONTO)
        {
            // i tasti di sinistra a sinistra, quelli di destra a destra,
            // X e la croce in mezzo
            Voce(ic, tx, "lb", "TAB", L("Manage tackle", "Gestisci l'armatura"));
            Voce(ic, tx, "x", L("SPACE", "SPAZIO"), L("Put the rod away", "Riponi la canna"));
            Voce(ic, tx, "croce_sxdx", "< >", L("Drag", "Frizione"));
            Voce(ic, tx, "croce_sugiu", "^ v", L("Bait depth", "Profondita' dell'esca"));
            Voce(ic, tx, "rb", "Q", L("Change bait", "Cambia esca"));
            Voce(ic, tx, "rt", L("CLICK", "CLIC"), L("Cast", "Lancia"));
        }
        else if (fase == FASE_ACQUA || fase == FASE_ABBOCCA || fase == FASE_LOTTA)
        {
            // "aggancia" sta sempre li', anche prima dell'abboccata: cosi'
            // non si legge solo nell'attimo in cui il pesce tira
            Voce(ic, tx, "lb", "TAB", L("Manage tackle", "Gestisci l'armatura"));
            Voce(ic, tx, "croce_sxdx", "< >", L("Drag", "Frizione"));
            Voce(ic, tx, "a", L("ENTER", "INVIO"), L("Hook the fish", "Aggancia il pesce"));
            Voce(ic, tx, "rt", L("CLICK", "CLIC"), L("Reel in", "Recupera la lenza"));
        }
        else if (fase == FASE_CARD)
        {
            Voce(ic, tx, "a", L("ENTER", "INVIO"), L("Keep", "Tieni"));
            Voce(ic, tx, "b", "ESC", L("Release", "Ributta"));
        }
        DisegnaBarraTasti(ic, tx);
    }

    // LA BARRA DEI TASTI: la usa la pesca e la usa il menu nuovo, sempre
    // la stessa. Ogni voce: icona del pad | tasto di tastiera | testo.
    void DisegnaBarraTasti(List<string> ic, List<string> tx)
    {
        if (ic.Count == 0) return;
        // TASTIERA O PAD: come fa GTA, si guarda l'ultimo input usato.
        // Con la tastiera le icone diventano riquadri col tasto scritto.
        bool tastiera = false;
        try { tastiera = Function.Call<bool>((Hash)0xA571D46727E2B718, 0); }
        catch { }

        float lato = LeggiF("consigli_lato", 22f);
        float y = 720f - LeggiF("consigli_dal_fondo", 20f) - lato;
        float sc = LeggiF("consigli_testo", 0.24f);
        float gap = LeggiF("consigli_gap", 30f);
        float car = LeggiF("consigli_car", 6.2f);
        float ty = y + lato * 0.5f - 9f + LeggiF("consigli_testo_giu", 1f);
        // larghezza di ogni icona (o riquadro del tasto), e totale per centrare
        float[] wi = new float[ic.Count];
        float tot = 0f;
        int i;
        for (i = 0; i < ic.Count; i++)
        {
            string[] pz = ic[i].Split('|');
            if (tastiera) wi[i] = pz[1].Length * car + 10f;
            else wi[i] = pz[0].Contains("+") ? (lato * 2f + 14f) : lato;
            tot += wi[i] + 6f + tx[i].Length * car + (i < ic.Count - 1 ? gap : 0f);
        }
        float x = LeggiF("consigli_centro", 640f) - tot * 0.5f;
        // il rettangolo scuro dietro, largo quanto le voci piu' un
        // margine, come quello di Rockstar; consigli_sfondo e' l'alfa
        int sfA = (int)LeggiF("consigli_sfondo", 150f);
        if (sfA > 0)
        {
            float sfH = LeggiF("consigli_sfondo_alto", lato + 10f);
            float sfM = LeggiF("consigli_sfondo_margine", 14f);
            DisegnaRett(x - sfM, y + lato * 0.5f - sfH * 0.5f, tot + sfM * 2f, sfH, 0, 0, 0, sfA);
        }
        for (i = 0; i < ic.Count; i++)
        {
            string[] pz = ic[i].Split('|');
            if (tastiera)
            {
                // il tasto di tastiera: un riquadro chiaro col nome dentro
                DisegnaRett(x, y + 1f, wi[i], lato - 2f, 210, 215, 220, 70);
                DisegnaRett(x, y + 1f, wi[i], 1f, 235, 238, 242, 200);
                DisegnaTesto(pz[1], x + wi[i] * 0.5f, ty + 1f, sc * 0.85f, 245, 245, 250);
            }
            else
            {
                // "rb+croce_dx": due icone con un + in mezzo
                string[] due = pz[0].Split('+');
                float xi = x;
                int k;
                for (k = 0; k < due.Length; k++)
                {
                    if (k > 0) { DisegnaTesto("+", xi + 7f, ty, sc, 245, 245, 250); xi += 14f; }
                    Sprite("img\\hud\\tasti\\" + due[k] + ".png", xi, y, lato, lato);
                    xi += lato;
                }
            }
            x += wi[i] + 6f;
            DisegnaTestoSinistra(tx[i], x, ty, sc, 245, 245, 250);
            x += tx[i].Length * car + gap;
        }
    }

    static void Voce(List<string> ic, List<string> tx, string pad, string tasto, string testo)
    {
        ic.Add(pad + "|" + tasto);
        tx.Add(testo);
    }

    void DisegnaTestoSinistra(string txt, float x, float y, float scala, int r, int g, int b)
    {
        try
        {
            TextElement el = new TextElement(txt, new PointF(x, y), scala);
            el.Color = Color.FromArgb(255, r, g, b);
            el.Font = GTA.UI.Font.ChaletLondon;
            el.Alignment = Alignment.Left;
            el.Outline = true;
            el.Draw();
        }
        catch { }
    }

    void TacchePrizione()
    {
        float fcx = LeggiF("friz_cx", BAR_X - 142f);
        float fcy = LeggiF("friz_cy", BarY() + BAR_H - 30f);
        float fd = LeggiF("friz_diam", 60f);
        // nel PNG il raggio esterno e' 248 su una meta' lato di 256
        float S = fd * 256f / 248f;
        Sprite("img\\hud\\friz_disco.png", fcx - S * 0.5f, fcy - S * 0.5f, S, S);
        int n = PosFrizione();
        if (frizione > n) frizione = n;
        int i;
        for (i = 0; i < n; i++)
        {
            // il PNG della tacca e' fatto per 12: con altre posizioni si
            // usa friz_on_<n>.png / friz_off_<n>.png se ci sono
            string suf = (n == 12) ? "" : ("_" + n);
            string img = (i < frizione) ? ("img\\hud\\friz_on" + suf + ".png")
                                        : ("img\\hud\\friz_off" + suf + ".png");
            SpriteInclinata(img, fcx - S * 0.5f, fcy - S * 0.5f, S, S, i * (360f / n) * LeggiF("ruota_verso", 1f));
        }
        // RIQUADRO DI PROVA (sviluppo): la casella di un'icona della
        // colonna (112x44), 20 px a sinistra del cerchio della frizione.
        // "prova_riquadro=0" lo toglie.
        if (LeggiF("prova_riquadro", 0f) > 0.5f)
        {
            // QUATTRO QUADRATI IN COLONNA, alle stesse altezze della
            // colonna vera (passo 54), 30 px a sinistra della frizione.
            // Nei primi due la SIMULAZIONE di quello che manca quando
            // peschi a galleggiante: il leader e il piombo. Gli altri
            // due restano vuoti: con questa armatura non c'e' altro.
            float pw = LeggiF("prova_w", 44f), ph = LeggiF("prova_h", 44f);
            float px = LeggiF("prova_x", fcx - fd * 0.5f - 30f - pw);
            float py = LeggiF("prova_y", fcy - ph * 0.5f);
            int k;
            for (k = 0; k < 4; k++)
            {
                float qy = py - k * 54f;
                DisegnaRett(px, qy, pw, ph, 70, 75, 85, 160);
            }
            // leader (forma col cavo) e piombo, coi loro dati finti
            Sprite("img\\terminali\\6214.png", px + 2f, py + 2f, pw - 4f, ph - 4f);
            DisegnaTesto("7.7 kg", px - 30f, py + 15f, 0.19f, 245, 245, 250);
            Sprite("img\\terminali\\5112.png", px + 2f, py - 54f + 2f, pw - 4f, ph - 4f);
            DisegnaTesto("15 g", px - 30f, py - 54f + 15f, 0.19f, 245, 245, 250);
        }
        // IN MEZZO IL MULINELLO, e sotto i suoi dati: frizione e metri
        int idm; string imm, nmm;
        if (Montato("mulinello", out idm, out imm, out nmm))
        {
            float mw = LeggiF("friz_mul", 46f);
            Sprite(imm, fcx - mw * 0.5f, fcy - mw * 0.5f, mw, mw);
            float fr = FrizioneMul(idm);
            int metri = MetriSuQuestoMulinello(idm);
            // "1.25 kg  8.4 / 65 m": la frizione, i metri fuori e quelli
            // che ci sono sul mulinello
            string rm = "";
            if (fr > 0f) rm = fr.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
            if (metri > 0)
            {
                // mentre carichi il lancio sono i metri che farai, se no quelli fuori davvero
                float fuori = (fase == FASE_CARICA) ? metriFuoriHud : metriLenza;
                string mf = fuori.ToString("0.0", CultureInfo.InvariantCulture) + " / " + metri + " m";
                rm = (rm.Length > 0) ? (rm + "  " + mf) : mf;
            }
            float ty = fcy + fd * 0.5f + LeggiF("friz_testo_giu", 4f);
            if (rm.Length > 0)
                DisegnaTesto(rm, fcx, ty, 0.19f, 245, 245, 250);
            // la profondita' dell'esca, sotto il quadrante: si regola con
            // SU e GIU' della croce
            float tyE = BarY() - 50f + LeggiF("caldo_giu", 8f) + LeggiF("prof_testo_giu", 14f);
            DisegnaTesto(L("Bait ", "Esca ") + profondita.ToString("0.00", CultureInfo.InvariantCulture) + " m",
                         QuadX(), tyE, 0.22f, 245, 245, 250);
            // sotto: la frizione inserita, in percentuale della massima
            int pct = (int)(100f * frizione / PosFrizione() + 0.5f);
            DisegnaTesto(L("drag ", "frizione ") + pct + "%", fcx, ty + LeggiF("friz_riga2", 12f),
                         0.19f, 245, 245, 250);
        }
    }

    // LA BARRA A TACCHE.
    // Cornice chiara, tacche spente scure, accese dal basso. Con un
    // colore (cr >= 0) sono tutte di quel colore - la carica del lancio,
    // le attese - con cr = -1 e' la tensione e i colori salgono con la
    // barra: blu, poi verde, poi giallo e rosso sul finale critico.
    //   barra_tacche   quante tacche
    //   barra_larga    larghezza delle tacche
    //   barra_spazio   spazio fra una tacca e l'altra
    void BarraCanna(float fill01, int cr, int cg, int cb)
    {
        if (fill01 < 0f) fill01 = 0f;
        if (fill01 > 1f) fill01 = 1f;
        int n = (int)LeggiF("barra_tacche", 24f);
        if (n < 4) n = 4;
        float larga = LeggiF("barra_larga", 18f);
        float spazio = LeggiF("barra_spazio", 2f);
        float x = BAR_X + BAR_W * 0.5f - larga * 0.5f;
        // LA TACCA E' ALTA COME CON 24 TACCHE IN 240 PX: se ne metti di
        // piu', la barra cresce verso l'alto, il fondo resta dov'e'.
        float th = (BAR_H - spazio * 23f) / 24f;
        float altezza = n * th + (n - 1) * spazio;
        float y0 = BarY() + BAR_H - altezza;
        // NIENTE CORNICE, NIENTE FONDO: solo le tacche, trasparenti.
        int accese = (int)(fill01 * n + 0.5f);
        int i;
        for (i = 0; i < n; i++)
        {
            // i = 0 e' in basso
            float y = y0 + altezza - (i + 1) * th - i * spazio;
            if (i >= accese)
            {
                DisegnaRett(x, y, larga, th, 210, 215, 220, (int)LeggiF("barra_alfa_spenta", 55f));
                continue;
            }
            int r = cr, g = cg, b = cb;
            if (cr < 0)
            {
                // sfumato tacca per tacca, come nell'immagine: blu in
                // basso, verde a meta', giallo in alto, rosso sul finale
                float t = (i + 0.5f) / n;
                float[] tp = new float[] { 0f, 0.40f, 0.75f, 1f };
                int[] rp = new int[] { 60, 80, 245, 240 };
                int[] gp = new int[] { 130, 220, 220, 70 };
                int[] bp = new int[] { 240, 90, 60, 60 };
                int k = 0;
                while (k < 2 && t > tp[k + 1]) k++;
                float u = (t - tp[k]) / (tp[k + 1] - tp[k]);
                if (u < 0f) u = 0f;
                if (u > 1f) u = 1f;
                r = (int)(rp[k] + (rp[k + 1] - rp[k]) * u);
                g = (int)(gp[k] + (gp[k + 1] - gp[k]) * u);
                b = (int)(bp[k] + (bp[k + 1] - bp[k]) * u);
            }
            DisegnaRett(x, y, larga, th, r, g, b, (int)LeggiF("barra_alfa_accesa", 190f));
        }
    }

    // QUANT'E' FONDO DOVE STA L'ESCA: pelo dell'acqua meno fondale,
    // col suolo che il gioco da' anche sott'acqua. Dove la sonda non
    // risponde torna -1 e non si scrive niente. Si rilegge ogni 300 ms.
    float fondoEsca = -1f;
    int fondoEscaQuando = 0;

    float FondoDellEsca()
    {
        int ora = Game.GameTime;
        if (ora - fondoEscaQuando < 300) return fondoEsca;
        fondoEscaQuando = ora;
        fondoEsca = -1f;
        if (!escaInAcqua) return -1f;
        try
        {
            float acqua = AcquaSottoEsca();
            OutputArgument g = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD,
                                    escaX, escaY, acqua + 50f, g, false))
            {
                float suolo = g.GetResult<float>();
                if (acqua > suolo) fondoEsca = acqua - suolo;
            }
        }
        catch { }
        return fondoEsca;
    }

    float metriFuoriHud = 0f;   // i metri di lenza fuori, per il testo del mulinello

    void Metri(float m)
    {
        // NIENTE SEGNI SUL PUNTO CALDO: dove c'e' il pesce lo scopri tu.
        // Testi bianchi e basta.
        int cr = 245, cg = 245, cb = 250;
        // UN DECIMALE. Con i metri interi il numero saltava 19, 18, 17 e
        // sembrava che la lenza corresse a scatti: cosi' invece scorre.
        float mm = (m < 0f) ? 0f : m;
        // QUANTI METRI HAI FUORI SU QUANTI NE HAI IN TUTTO.
        // Da soli i metri non dicono niente: dodici sono pochi con
        // sessantacinque di filo in bobina e sono la fine del mondo se
        // ne restano quindici. Il secondo numero e' quello che c'e'
        // davvero sul mulinello adesso, non quello della confezione.
        // I METRI FUORI NON SI SCRIVONO PIU' QUI: stanno sotto il
        // mulinello, nel cerchio della frizione ("8.4 / 65 m").
        metriFuoriHud = mm;
        // quant'e' fondo dove sta l'esca: solo con la lenza in acqua in
        // attesa, non quando abbocca o durante la lotta
        float fondo = (fase == FASE_ACQUA) ? FondoDellEsca() : -1f;
        if (fondo >= 0f)
            DisegnaTesto(L("Depth ", "Fondo ") + fondo.ToString("0.0", CultureInfo.InvariantCulture) + " m",
                         QuadX(), BarY() - 50f + LeggiF("caldo_giu", 8f), 0.22f, cr, cg, cb);
    }

    // legge larghezza e altezza dall'IHDR del PNG, per non stirare niente
    static bool MisuraPng(string file, out int w, out int h)
    {
        w = 0; h = 0;
        try
        {
            byte[] b = new byte[26];
            FileStream fs = File.OpenRead(file);
            int letti = fs.Read(b, 0, 26);
            fs.Close();
            if (letti < 26) return false;
            if (b[0] != 0x89 || b[1] != 0x50) return false;
            w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
            h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
            return (w > 0 && h > 0);
        }
        catch { return false; }
    }

    // disegna dentro il riquadro w x h SENZA stirare: l'immagine ci sta
    // dentro con le sue proporzioni vere, centrata
    void Sprite(string rel, float x, float y, float w, float h)
    {
        if (rel == null || rel.Length == 0) return;
        try
        {
            string f = Path.Combine(MY_DIR, rel);
            if (!File.Exists(f)) return;
            float dw = w, dh = h, dx = x, dy = y;
            int iw, ih;
            if (MisuraPng(f, out iw, out ih))
            {
                float sx = w / (float)iw, sy = h / (float)ih;
                float sc = (sx < sy) ? sx : sy;
                dw = iw * sc; dh = ih * sc;
                dx = x + (w - dw) * 0.5f;
                dy = y + (h - dh) * 0.5f;
            }
            CustomSprite s = new CustomSprite(f, new SizeF(dw, dh), new PointF(dx, dy));
            s.Draw();
        }
        catch { }
    }

    // Come Sprite, ma girata. w x h e' il riquadro dell'immagine come sta
    // nel catalogo, cioe' sdraiata; girandola di 90 gradi occupa h x w, e
    // (x, y) e' l'angolo in alto a sinistra di quel riquadro girato.
    // come Sprite(), ma inclinata di qualche grado. Il centro resta
    // quello della casella: serve per il cucchiaino che punta in giu'.
    void SpriteInclinata(string rel, float x, float y, float w, float h, float gradi)
    {
        if (rel == null || rel.Length == 0) return;
        try
        {
            string f = Path.Combine(MY_DIR, rel);
            if (!File.Exists(f)) return;
            float dw = w, dh = h;
            int iw, ih;
            if (MisuraPng(f, out iw, out ih) && iw > 0 && ih > 0)
            {
                float sx = w / (float)iw, sy = h / (float)ih;
                float sc = (sx < sy) ? sx : sy;
                dw = iw * sc; dh = ih * sc;
            }
            float cx = x + w * 0.5f;
            float cy = y + h * 0.5f;
            CustomSprite sp = new CustomSprite(f, new SizeF(dw, dh),
                                               new PointF(cx - dw * 0.5f, cy - dh * 0.5f));
            sp.Rotation = gradi;
            sp.Draw();
        }
        catch { }
    }

    void SpriteGirata(string rel, float x, float y, float w, float h, float gradi)
    {
        if (rel == null || rel.Length == 0) return;
        try
        {
            string f = Path.Combine(MY_DIR, rel);
            if (!File.Exists(f)) return;
            float dw = w, dh = h;
            int iw, ih;
            if (MisuraPng(f, out iw, out ih))
            {
                float sx = w / (float)iw, sy = h / (float)ih;
                float sc = (sx < sy) ? sx : sy;
                dw = iw * sc; dh = ih * sc;
            }
            // ruotando resta fermo il centro: e' li' che va messa
            float cx = x + h * 0.5f;
            float cy = y + w * 0.5f;
            CustomSprite s = new CustomSprite(f, new SizeF(dw, dh),
                                              new PointF(cx - dw * 0.5f, cy - dh * 0.5f));
            s.Rotation = gradi;
            s.Draw();
        }
        catch { }
    }

    // un numero scritto in un campo di testo, virgola inglese
    static float NumeroFloat(string s)
    {
        if (s == null) return 0f;
        float v;
        if (float.TryParse(s.Trim(), NumberStyles.Float,
                           CultureInfo.InvariantCulture, out v)) return v;
        return 0f;
    }

    // il campo esca_g della canna: "0.5 - 7", ma anche "18  154" senza trattino.
    // Prende il primo numero come minimo e l'ultimo come massimo.
    static void RangeGrammi(string s, out float gmin, out float gmax)
    {
        gmin = 0f; gmax = 0f;
        if (s == null) return;
        List<float> nums = new List<float>();
        string cur = "";
        int i;
        for (i = 0; i <= s.Length; i++)
        {
            char ch = (i < s.Length) ? s[i] : ' ';
            if ((ch >= '0' && ch <= '9') || ch == '.') cur += ch;
            else
            {
                if (cur.Length > 0) nums.Add(NumeroFloat(cur));
                cur = "";
            }
        }
        if (nums.Count == 0) return;
        gmin = nums[0];
        gmax = nums[nums.Count - 1];
        if (gmax < gmin) { float t = gmin; gmin = gmax; gmax = t; }
    }

    // IL PESO CHE HAI IN PUNTA, in grammi.
    // Artificiale, testina piombata e piombo hanno un peso VERO preso dal
    // wiki. Amo, esca naturale e galleggiante sul wiki NON hanno un peso:
    // se non hai niente di pesante montato resta 1.5 g simbolici, e questo
    // numero e' una scelta nostra, non un dato di Fishing Planet.
    float GrammiInPunta()
    {
        float g = 0f;
        int id; string img, nome;
        int i;
        if (Montato("artificiale", out id, out img, out nome))
        {
            for (i = 0; i < artificiali.Count; i++)
                if (artificiali[i].Id == id)
                { g += NumeroFloat(artificiali[i].Grammi); break; }
        }
        if (Montato("terminale", out id, out img, out nome))
        {
            for (i = 0; i < terminali.Count; i++)
                if (terminali[i].Id == id)
                { g += NumeroFloat(terminali[i].Grammi); break; }
        }
        if (g <= 0f) g = 1.5f;
        return g;
    }

    // QUANTO LONTANO ARRIVI.
    // Il wiki lo spiega: a lanciare non e' la lunghezza, e' il PESO che la
    // canna e' tarata per tirare (il campo esca_g) contro il peso che hai
    // davvero attaccato. Se il carico sta dentro il range della canna il
    // lancio e' pieno; se e' troppo leggero la canna non si carica e il
    // lancio si accorcia di brutto; se e' troppo pesante la canna non lo
    // tira via. Poi contano la lunghezza e il diametro della lenza.
    // QUANTO VA LONTANO QUESTO LANCIO.
    // Prima erano "potenza per 0,9", poi tagliati dal massimo che la tua
    // attrezzatura regge. Con la telescopica del livello uno quel massimo
    // e' una dozzina di metri: bastava caricare al quindici per cento per
    // essere gia' oltre il taglio, e da li' in poi caricavi a vuoto -
    // tutti i lanci uguali. Adesso la barra dice una frazione del tuo
    // massimo, e non e' una frazione dritta: e' curva, quindi i primi
    // colpetti fanno pochi metri e gli ultimi valgono tanto. Caricare
    // deve costare.
    float MetriDelLancio(float pot)
    {
        float tetto = MetriMaxLancio();
        if (tetto <= 0f) tetto = 12f;
        float t = pot / 100f;
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        float cur = LeggiF("lancio_curva", 1.7f);
        if (cur < 0.2f) cur = 0.2f;
        float m = tetto * (float)Math.Pow(t, cur);
        float minimo = LeggiF("lancio_minimo", 1.5f);
        if (m < minimo) m = minimo;
        return m;
    }

    float MetriMaxLancio()
    {
        float lung = 0f, gmin = 0f, gmax = 0f;
        int idc; string ic, nc;
        if (Montato("canna", out idc, out ic, out nc))
        {
            int i;
            for (i = 0; i < canne.Count; i++)
                if (canne[i].Id == idc)
                {
                    lung = NumeroFloat(canne[i].Lunghezza);
                    RangeGrammi(canne[i].Esca, out gmin, out gmax);
                    break;
                }
        }
        if (lung <= 0f) lung = 2f;
        float g = GrammiInPunta();
        float d;

        if (gmax <= 0f)
        {
            // canna da galleggiante (match, telescopica): sul wiki non ha un
            // peso di lancio, la lenza la si accompagna. Conta la lunghezza,
            // e un filo di peso in punta aiuta ad allungare.
            d = 4f + 4f * lung;
            float fg = 0.8f + 0.2f * (float)Math.Sqrt(g / 10f);
            if (fg > 1.35f) fg = 1.35f;
            d = d * fg;
        }
        else
        {
            if (gmin <= 0f || gmin >= gmax) gmin = gmax * 0.3f;

            // quanto arriverebbe questa canna col carico giusto in punta
            d = 6f + 5f * (float)Math.Sqrt(gmax) + 3f * lung;

            // e quanto ci arriva col carico che hai adesso
            float f;
            if (g < gmin)
            {
                f = 0.75f * (float)Math.Pow(g / gmin, 0.7);
                if (f < 0.15f) f = 0.15f;
            }
            else if (g > gmax)
            {
                f = gmax / g;
                if (f < 0.20f) f = 0.20f;
            }
            else f = 0.75f + 0.25f * (g - gmin) / (gmax - gmin);
            d = d * f;
        }

        // la lenza montata: sottile scorre, grossa frena
        int idl; string il2, nl2;
        if (Montato("lenza", out idl, out il2, out nl2))
        {
            int i;
            for (i = 0; i < lenze.Count; i++)
                if (lenze[i].Id == idl)
                {
                    float mm;
                    if (float.TryParse(lenze[i].Mm, NumberStyles.Float,
                                       CultureInfo.InvariantCulture, out mm))
                    {
                        float ff = 1.05f - mm * 0.4f;
                        if (ff < 0.7f) ff = 0.7f;
                        if (ff > 1.05f) ff = 1.05f;
                        d = d * ff;
                    }
                    break;
                }
        }

        // e comunque mai piu' della lenza che hai in bobina: un po' di filo
        // sul mulinello resta sempre, quindi il 90 per cento
        int idm; string im, nm;
        if (Montato("mulinello", out idm, out im, out nm))
        {
            int m = MetriSuQuestoMulinello(idm);
            if (m > 0 && d > m * 0.9f) d = m * 0.9f;
        }
        if (d < 5f) d = 5f;
        return d;
    }
    // il primo pezzo equipaggiato di una categoria
    // ------------------------------------------------------------
    //  ARMATO E DISARMATO
    //  Nello zaino ci sta anche la roba di scorta. Quello che pesca
    //  davvero e' solo il pezzo ARMATO: una canna, un mulinello, una
    //  lenza, un amo, un galleggiante. Il resto sta li' e aspetta.
    //  Si arma e si disarma con A sulla riga dell'equipaggiamento.
    // ------------------------------------------------------------
    Dictionary<string, int> armato = new Dictionary<string, int>();

    // QUELLO CHE E' GIA' STATO TOLTO DALLA CASSETTA.
    // Un amo montato non sta piu' in cassetta, e il conto e' gia' stato
    // fatto quando l'hai armato. Senza questo segno un salvataggio
    // vecchio - montato prima che la regola esistesse - resterebbe con
    // dieci ami in cassetta e uno sulla canna: undici in tutto.
    Dictionary<string, int> presoSu = new Dictionary<string, int>();

    // ============================================================
    //  LA LENZA SI TAGLIA
    // ============================================================
    // Una bobina e' un PEZZO A SE', con i suoi metri. Quella nuova ha i
    // metri del catalogo; quando la armi tagli quanto ne tiene il
    // mulinello, e il resto resta in cassetta come bobina tagliata.
    // Un rotolo da 137 su cui ne tagli 65 lascia una bobina da 72.
    // Se poi smonti la lenza, quei 65 tornano in cassetta come una
    // bobina SEPARATA da 65 - non si riattaccano ai 72. E se nel
    // frattempo ne hai persi 7 strappando, torna una bobina da 58.
    //   bobine  -> "376|65", una riga per ogni bobina tagliata
    //   armato["lenza"] + metriInBobina -> quella che sta sul mulinello
    List<string> bobine = new List<string>();
    int metriInBobina = 0;

    // i metri che il mulinello montato tiene di questa lenza
    int MetriDaTagliare(int idLenza)
    {
        int idm; string im, nm;
        if (!Montato("mulinello", out idm, out im, out nm)) return 0;
        return MetriSulMulinello(idm, TipoLenza(idLenza));
    }

    int BobinaId(int i)
    {
        if (i < 0 || i >= bobine.Count) return -1;
        string[] c = bobine[i].Split('|');
        return (c.Length > 0) ? Numero(c[0]) : -1;
    }

    int BobinaMetri(int i)
    {
        if (i < 0 || i >= bobine.Count) return 0;
        string[] c = bobine[i].Split('|');
        return (c.Length > 1) ? Numero(c[1]) : 0;
    }

    void MettiBobina(int id, int metri)
    {
        if (id < 0 || metri <= 0) return;
        bobine.Add(id + "|" + metri);
    }

    // LE BOBINE TAGLIATE LASCIATE A CASA: stessa forma "id|metri".
    // Da casa alla borsa e ritorno con A, come il resto.
    List<string> bobineCasa = new List<string>();

    bool BobinaACasa(int i)
    {
        if (i < 0 || i >= bobine.Count) return false;
        if (inPesca)
        {
            Avviso("~r~Sei fuori: la borsa e' quella che ti sei portato.");
            return false;
        }
        string b = bobine[i];
        bobine.RemoveAt(i);
        bobineCasa.Add(b);
        string nome, img; int prezzo, liv;
        if (!Articolo("lenza", Numero(b.Split('|')[0]), out nome, out img, out prezzo, out liv)) nome = "bobina";
        Avviso("~y~Rimessa a casa: ~s~" + nome);
        return true;
    }

    bool BobinaInBorsa(int i)
    {
        if (i < 0 || i >= bobineCasa.Count) return false;
        if (inPesca)
        {
            Avviso("~r~Sei fuori: la borsa e' quella che ti sei portato.");
            return false;
        }
        if (!CiSta("lenza"))
        {
            Avviso("~r~Non ci sta piu': guarda cassetta e portacanne.");
            return false;
        }
        string b = bobineCasa[i];
        bobineCasa.RemoveAt(i);
        bobine.Add(b);
        string nome, img; int prezzo, liv;
        if (!Articolo("lenza", Numero(b.Split('|')[0]), out nome, out img, out prezzo, out liv)) nome = "bobina";
        Avviso("~g~Equipaggiata: ~s~" + nome);
        return true;
    }

    // LA BOBINA TAGLIATA SI VENDE A METRO: il prezzo della confezione
    // diviso i suoi metri, per i metri rimasti, alla percentuale di vendita.
    bool VendiBobinaCasa(int i)
    {
        if (i < 0 || i >= bobineCasa.Count) return false;
        if (inPesca) { Messaggio("Si vende da casa, non in riva."); return true; }
        string[] c = bobineCasa[i].Split('|');
        int id = Numero(c[0]);
        int m = (c.Length > 1) ? Numero(c[1]) : 0;
        string nome, img; int prezzo, liv;
        if (!Articolo("lenza", id, out nome, out img, out prezzo, out liv)) return false;
        int tot = MetriLenza(id);
        if (tot <= 0) tot = m;
        int perc = (int)LeggiF("vendi_percento", 50f);
        if (perc < 0) perc = 0;
        if (perc > 100) perc = 100;
        int reso = (int)((long)prezzo * m / tot * perc / 100);
        string k = "bobc:" + i;
        int ora = Game.GameTime;
        if (vendiChiesto != k || ora > vendiScade)
        {
            vendiChiesto = k;
            vendiScade = ora + 5000;
            Messaggio("Premi ancora (X) per vendere " + nome + " (" + m + " m) a $" + Dollari(reso));
            return true;
        }
        vendiChiesto = "";
        if (i >= bobineCasa.Count) return true;
        bobineCasa.RemoveAt(i);
        Paga(-reso);
        SalvaStato();
        RiscriviTutto();
        Messaggio("Venduta " + nome + " (" + m + " m)   +$" + Dollari(reso));
        return true;
    }

    bool ButtaBobinaCasa(int i)
    {
        if (i < 0 || i >= bobineCasa.Count) return false;
        string[] c = bobineCasa[i].Split('|');
        int id = Numero(c[0]);
        int m = (c.Length > 1) ? Numero(c[1]) : 0;
        string nome, img; int prezzo, liv;
        if (!Articolo("lenza", id, out nome, out img, out prezzo, out liv)) return false;
        if (!ChiediDueVolte("bobc" + i,
                "Premi ancora (Y) per gettare " + nome + " (" + m + " m)"))
            return true;
        if (i >= bobineCasa.Count) return true;
        bobineCasa.RemoveAt(i);
        Messaggio("Gettata: " + nome + "   " + m + " m");
        return true;
    }

    // TAGLIA: dal rotolo al mulinello. Torna quanto ha tagliato.
    int Taglia(int idLenza, int daQuanti)
    {
        int quanto = MetriDaTagliare(idLenza);
        if (quanto <= 0 || quanto > daQuanti) quanto = daQuanti;
        metriInBobina = quanto;
        return daQuanti - quanto;      // quello che avanza sul rotolo
    }

    // ARMA UNA LENZA NUOVA: si apre una confezione del catalogo.
    bool ArmaLenzaNuova(int id)
    {
        if (Quanti(borsa, "lenza:" + id) <= 0)
        { Messaggio("Non hai questa lenza in cassetta."); return false; }
        string perche;
        int idc = InUso("canna");
        if (idc >= 0 && !VaConLaCanna("lenza", id, idc, out perche))
        { Messaggio("Non e' equilibrata: " + perche); return false; }

        DisarmaLenza();
        Aggiungi(borsa, "lenza:" + id, -1);
        int avanza = Taglia(id, MetriLenza(id));
        MettiBobina(id, avanza);
        armato["lenza"] = id;
        Messaggio("Imbobinati " + metriInBobina + " m"
                  + (avanza > 0 ? ("   restano " + avanza + " m sul rotolo") : ""));
        return true;
    }

    // ARMA UNA BOBINA GIA' TAGLIATA.
    bool ArmaLenzaBobina(int i)
    {
        int id = BobinaId(i);
        int m = BobinaMetri(i);
        if (id < 0 || m <= 0) return false;
        string perche;
        int idc = InUso("canna");
        if (idc >= 0 && !VaConLaCanna("lenza", id, idc, out perche))
        { Messaggio("Non e' equilibrata: " + perche); return false; }

        bobine.RemoveAt(i);
        DisarmaLenza();
        int avanza = Taglia(id, m);
        MettiBobina(id, avanza);
        armato["lenza"] = id;
        Messaggio("Imbobinati " + metriInBobina + " m"
                  + (avanza > 0 ? ("   restano " + avanza + " m") : ""));
        return true;
    }

    // SMONTA: quello che sta sul mulinello torna in cassetta come bobina
    // sua, coi metri che gli sono rimasti. Separata da tutto il resto.
    void DisarmaLenza()
    {
        int vecchia = Armato("lenza");
        if (vecchia >= 0 && metriInBobina > 0)
        {
            MettiBobina(vecchia, metriInBobina);
            Messaggio("Tolta: " + metriInBobina + " m tornano in cassetta.");
        }
        metriInBobina = 0;
        if (armato.ContainsKey("lenza")) armato["lenza"] = -1;
    }

    // le categorie che si armano: il resto (esche, nasse, cassette)
    // non si monta, si consuma o si porta e basta
    static bool SiArma(string cat)
    {
        return cat == "canna" || cat == "mulinello" || cat == "lenza"
            || cat == "terminale" || cat == "galleggiante" || cat == "artificiale";
    }

    // ============================================================
    //  LE CASELLE DEL TERMINALE
    // ============================================================
    // "terminale" e' una categoria sola in negozio, ma sulla canna sono
    // pezzi diversi che stanno insieme: il leader si lega alla lenza, il
    // piombo sta sul filo, e in fondo c'e' quello che aggancia il pesce -
    // amo, testina o rig. Percio' ognuno ha la sua casella, e la colonna
    // dell'HUD cresce con quello che monti.
    //   armato["terminale"] = quello che aggancia (amo, jig, rig)
    //   armato["leader"]    = il leader
    //   armato["piombo"]    = il piombo
    static string CasellaTerm(string sotto)
    {
        if (sotto == "leader") return "leader";
        if (sotto == "piombo") return "piombo";
        return "terminale";
    }

    // di che tipo e' questo terminale
    string SottoTerm(int id)
    {
        int i;
        for (i = 0; i < terminali.Count; i++)
            if (terminali[i].Id == id) return terminali[i].Cat;
        return "amo";
    }

    // il pezzo montato in una casella del terminale
    bool MontatoTerm(string casella, out int id, out string img, out string nome)
    {
        id = -1; img = ""; nome = "";
        if (!armato.ContainsKey(casella)) return false;
        int q = armato[casella];
        if (q < 0) return false;
        int p2, l2;
        if (!Articolo("terminale", q, out nome, out img, out p2, out l2)) return false;
        id = q;
        return true;
    }

    int Armato(string cat)
    {
        if (armato.ContainsKey(cat)) return armato[cat];
        return -1;
    }

    // SMONTA TUTTO: la lenza torna bobina, ami e galleggianti tornano in
    // cassetta, il resto si stacca dalla canna. A fine giornata si
    // riparte da zero: cosi' sulla canna non resta roba che poi vendi.
    void DisarmaTutto()
    {
        DisarmaLenza();
        string[] cas = new string[] { "terminale", "leader", "piombo", "galleggiante" };
        int i;
        for (i = 0; i < cas.Length; i++)
        {
            int id = Armato(cas[i]);
            if (id >= 0 && presoSu.ContainsKey(cas[i]))
            {
                Rimetti(cas[i] == "galleggiante" ? "galleggiante" : "terminale", id);
                presoSu.Remove(cas[i]);
            }
        }
        List<string> chiavi = new List<string>(armato.Keys);
        for (i = 0; i < chiavi.Count; i++) armato[chiavi[i]] = -1;
        escaMontata = -1;
    }

    // Un pezzo che se ne va dalla borsa (a casa, venduto, gettato) non
    // puo' restare sulla canna: prima si smonta.
    void SeArmatoSmonta(string cat, int id)
    {
        if (cat == "terminale") cat = CasellaTerm(SottoTerm(id));
        if (!SiArma(cat) && cat != "leader" && cat != "piombo") return;
        if (Armato(cat) != id) return;
        if (cat == "mulinello" || cat == "lenza") DisarmaLenza();
        if (presoSu.ContainsKey(cat)) presoSu.Remove(cat);
        armato[cat] = -1;
    }

    // Quello che sta pescando DAVVERO, ripiego compreso: se non hai mai
    // armato niente sta pescando il primo pezzo che hai nello zaino, e
    // il menu deve dirlo, se no ti mostra disarmato roba che e' in acqua.
    int InUso(string cat)
    {
        int id; string img, nome;
        if (Montato(cat, out id, out img, out nome)) return id;
        return -1;
    }

    bool EArmato(string cat, int id)
    {
        // un terminale puo' stare in tre caselle: amo, leader o piombo
        if (cat == "terminale") return Armato(CasellaTerm(SottoTerm(id))) == id;
        return SiArma(cat) && InUso(cat) == id;
    }

    // A sulla riga: se e' armato lo disarma, se no lo arma al posto
    // di quello che c'era. L'equilibrio si controlla qui.
    bool Arma(string cat, int id)
    {
        if (!SiArma(cat)) return false;

        // UN TERMINALE VA NELLA CASELLA DEL SUO TIPO.
        // Leader, piombo e amo non si scacciano a vicenda: stanno sulla
        // stessa lenza, uno sopra l'altro. Percio' la casella la decide
        // il tipo del pezzo, non la categoria del negozio.
        if (cat == "terminale") cat = CasellaTerm(SottoTerm(id));

        if (QuantiPezzi("terminale", id) <= 0 && Armato(cat) != id
            && (cat == "leader" || cat == "piombo")) return false;
        if (cat != "leader" && cat != "piombo"
            && QuantiPezzi(cat, id) <= 0 && InUso(cat) != id) return false;

        if (Armato(cat) == id || InUso(cat) == id)
        {
            // SI SMONTA: quello che si era preso torna indietro.
            // La lenza si riavvolge sulla bobina, l'amo e il galleggiante
            // tornano nella cassetta. Non si butta niente smontando: si
            // butta quando si spezza.
            if (cat == "lenza") { DisarmaLenza(); return true; }
            if (cat == "terminale" || cat == "galleggiante"
                || cat == "leader" || cat == "piombo")
            {
                string cq = (cat == "galleggiante") ? "galleggiante" : "terminale";
                if (presoSu.ContainsKey(cat)) { Rimetti(cq, id); presoSu.Remove(cat); }
            }
            // -1 vuol dire "disarmato apposta": senza questo tornerebbe
            // buono il ripiego e il pezzo si rimonterebbe da solo
            armato[cat] = -1;
            Messaggio("Tolto dall'armatura");
            return true;
        }

        // IL CUCCHIAINO E IL GALLEGGIANTE NON STANNO INSIEME.
        // A spinning il cucchiaino E' l'amo: si lega direttamente alla
        // lenza, senza galleggiante e senza amo sotto. Sono due modi di
        // pescare diversi, e la canna non li tiene tutti e due. Percio'
        // qui non si smonta niente da soli: si dice cosa va tolto prima.
        // COSA ESCLUDE COSA.
        // Il cucchiaino E' l'amo: si lega in punta alla lenza, e sotto non
        // ci va ne' amo ne' galleggiante. Il leader invece ci sta - anzi,
        // col luccio il cavetto di titanio serve proprio - e il piombo
        // pure. Quindi il cucchiaino litiga solo con amo e galleggiante.
        if (cat == "artificiale")
        {
            if (InUso("galleggiante") >= 0)
            { Messaggio("Prima smonta il galleggiante."); return false; }
            if (Armato("terminale") >= 0)
            { Messaggio("Prima smonta " + NomeTerminale(Armato("terminale")) + "."); return false; }
        }
        if (cat == "galleggiante" || cat == "terminale")
        {
            if (InUso("artificiale") >= 0)
            { Messaggio("Prima smonta il cucchiaino."); return false; }
        }

        // non si arma roba sbilanciata: e' la stessa regola del montaggio
        string perche;
        if (cat == "canna")
        {
            int q = InUso("lenza");
            if (q >= 0 && !VaConLaCanna("lenza", q, id, out perche))
            { Avviso("~r~Non e' equilibrata: ~s~" + perche); return false; }
            q = InUso("mulinello");
            if (q >= 0 && !VaConLaCanna("mulinello", q, id, out perche))
            { Avviso("~r~Non e' equilibrata: ~s~" + perche); return false; }
        }
        else
        {
            int idc = InUso("canna");
            if (idc >= 0 && !VaConLaCanna(cat, id, idc, out perche))
            { Avviso("~r~Non e' equilibrata: ~s~" + perche); return false; }
        }

        // SI MONTA: e da qui in poi quel pezzo non sta piu' in cassetta,
        // sta sulla canna. La lenza si imbobina - i metri se ne vanno
        // dalla bobina - l'amo e il galleggiante si tolgono dalla borsa.
        // la lenza ha i suoi comandi: ogni bobina e' un pezzo a se'
        if (cat == "lenza") return ArmaLenzaNuova(id);
        if (cat == "terminale" || cat == "galleggiante"
            || cat == "leader" || cat == "piombo")
        {
            string cq = (cat == "galleggiante") ? "galleggiante" : "terminale";
            if (QuantiPezzi(cq, id) <= 0)
            { Messaggio("Non ne hai piu' in cassetta."); return false; }
            // quello che c'era prima nella stessa casella torna in cassetta
            int vecchio = Armato(cat);
            if (vecchio >= 0 && vecchio != id && presoSu.ContainsKey(cat))
                Rimetti(cq, vecchio);
            Consuma(cq, id);
            presoSu[cat] = 1;
        }

        armato[cat] = id;
        Messaggio("Armato");
        return true;
    }

    // QUANDO LA LENZA SI SPEZZA si perde tutto quello che stava sotto il
    // punto di rottura: i metri che erano fuori, l'amo, il galleggiante
    // e l'esca. L'armatura e' da rifare.
    void PerdiArmatura()
    {
        // DOVE SI SPEZZA CAMBIA TUTTO.
        // Se c'e' il leader, si spezza LUI: e' il pezzo debole messo li'
        // apposta. Perdi il leader e quello che gli stava attaccato -
        // amo o cucchiaino, e l'esca - ma la lenza madre resta intera,
        // e quello che stava piu' su - piombo, galleggiante - resta al
        // suo posto. Senza leader invece se ne va un pezzo di lenza.
        int ld = Armato("leader");
        bool colLeader = (ld >= 0);

        int persi = 0;
        if (!colLeader)
        {
            persi = (int)LeggiF("lenza_persa", 3f);
            if (persi < 1) persi = 1;
            metriInBobina -= persi;
            if (metriInBobina < 0) metriInBobina = 0;
            if (metriInBobina <= 0 && armato.ContainsKey("lenza")) armato["lenza"] = -1;
        }

        // quello che sta sotto la rottura se ne va, e non torna in cassetta
        int t = InUso("terminale");
        if (t >= 0) { armato["terminale"] = -1; presoSu.Remove("terminale"); }
        int a = InUso("artificiale");
        if (a >= 0) { armato["artificiale"] = -1; presoSu.Remove("artificiale"); }
        bool avevaEsca = (escaMontata >= 0);
        escaMontata = -1;
        if (colLeader) { armato["leader"] = -1; presoSu.Remove("leader"); }

        // piombo e galleggiante stanno sopra il leader: si perdono solo
        // se a spezzarsi e' stata la lenza
        int pb = -1, g = -1;
        if (!colLeader)
        {
            pb = Armato("piombo");
            if (pb >= 0) { armato["piombo"] = -1; presoSu.Remove("piombo"); }
            g = InUso("galleggiante");
            if (g >= 0) { armato["galleggiante"] = -1; presoSu.Remove("galleggiante"); }
        }

        string che = "";
        if (colLeader) che = "il leader";
        else che = persi + " m di lenza";
        if (t >= 0) che += ", " + NomeTerminale(t);
        if (pb >= 0) che += ", il piombo";
        if (g >= 0) che += ", il galleggiante";
        if (a >= 0) che += ", il cucchiaino";
        if (avevaEsca) che += ", l'esca";

        // se e' stato il pesce a segare il filo lo dice, che e' un'altra
        // cosa dal tirare troppo
        string testa = colLeader ? "Leader spezzato: persi "
                                 : (dentiDa > 0 ? "Ti ha tranciato il filo: persi "
                                                : "Lenza spezzata: persi ");
        Messaggio(testa + che + ".");
        SalvaStato();
        RiscriviTutto();
    }

    // Il pezzo che sta pescando: quello armato. Se non hai armato niente
    // vale il primo di quel tipo che trovi nello zaino, cosi' chi non ci
    // vuole pensare non deve fare niente.
    bool Montato(string cat, out int id, out string img, out string nome)
    {
        id = -1; img = ""; nome = "";
        int scelto = Armato(cat);
        // la lenza c'e' solo se sul mulinello ci sta ancora del filo
        if (cat == "lenza" && metriInBobina <= 0) return false;
        // ARMATO VUOL DIRE SULLA CANNA, non in borsa: l'amo e il
        // galleggiante montati sono stati tolti dalla cassetta apposta.
        if (scelto >= 0)
        {
            int p2, l2;
            if (Articolo(cat, scelto, out nome, out img, out p2, out l2))
            { id = scelto; return true; }
        }
        // NIENTE RIPIEGO SU QUELLO CHE HAI IN BORSA.
        // Prima, se non avevi armato niente, la mod montava da sola il
        // primo pezzo che trovava in cassetta: comodo finche' armare non
        // costava niente, sbagliato adesso che montare toglie il pezzo
        // dalla scatola e taglia i metri di lenza. Si ritrovava roba
        // "armata" che non era mai stata montata, e i conti non
        // tornavano. Adesso sulla canna c'e' solo quello che ci hai
        // messo tu. La nassa fa eccezione: non si arma, o ce l'hai o no.
        if (cat == "nassa")
        {
            foreach (KeyValuePair<string, int> kv in borsa)
            {
                string[] c = kv.Key.Split(':');
                if (c.Length < 2 || c[0] != cat) continue;
                int prezzo, liv;
                if (!Articolo(cat, Numero(c[1]), out nome, out img, out prezzo, out liv)) continue;
                id = Numero(c[1]);
                return true;
            }
        }
        return false;
    }

    // quanti pezzi ne restano: le confezioni che hai, meno quelli usati
    int QuantiPezzi(string cat, int id)
    {
        int per = PerConfezione(cat, id);
        int conf = Quanti(borsa, cat + ":" + id);
        int tot = (per > 0) ? per * conf : conf;
        int gia = Quanti(usati, cat + ":" + id);
        int resta = tot - gia;
        return (resta > 0) ? resta : 0;
    }

    int QuanteEsche(int id) { return QuantiPezzi("esca", id); }

    // ne consuma uno. Quando la confezione e' finita sparisce dallo zaino.
    // L'INVERSO DI CONSUMA: rimette UN pezzo, non una confezione.
    // Gli ami stanno in scatole da dieci: smontandone uno deve tornare
    // un amo, non dieci.
    void Rimetti(string cat, int id)
    {
        string k = cat + ":" + id;
        int per = PerConfezione(cat, id);
        if (per > 1)
        {
            if (Quanti(usati, k) > 0) Aggiungi(usati, k, -1);
            else { Aggiungi(borsa, k, 1); Aggiungi(usati, k, per - 1); }
        }
        else Aggiungi(borsa, k, 1);
    }

    bool Consuma(string cat, int id)
    {
        string k = cat + ":" + id;
        if (QuantiPezzi(cat, id) <= 0) return false;
        Aggiungi(usati, k, 1);
        int per = PerConfezione(cat, id);
        if (per > 1)
        {
            // finita una confezione intera: si toglie dallo zaino e il
            // conto dei pezzi usati riparte da capo
            while (Quanti(usati, k) >= per && Quanti(borsa, k) > 0)
            {
                Aggiungi(usati, k, -per);
                Aggiungi(borsa, k, -1);
            }
        }
        else
        {
            usati.Remove(k);
            Aggiungi(borsa, k, -1);
        }
        if (Quanti(borsa, k) <= 0) usati.Remove(k);
        return true;
    }

    // IL BLOCCO IN BASSO A DESTRA DELL'HUD: la canna e la colonna del
    // montaggio. Sta in una funzione sua perche' lo disegna anche la
    // pagina dell'inventario, per far vedere com'e' armata la canna.
    // IL QUADRATO DIETRO A OGNI PEZZO MONTATO della colonna: 44x44,
    // centrato sulla casella dell'icona, colore e trasparenza delle
    // tacche spente della barra. Solo dove c'e' un pezzo.
    void QuadratoColonna(float mx, float ry)
    {
        if (LeggiF("colonna_quadrati", 1f) < 0.5f) return;
        float ql = LeggiF("colonna_quadrato", 44f);
        DisegnaRett(mx + (112f - ql) * 0.5f, ry + (44f - ql) * 0.5f, ql, ql,
                    210, 215, 220, (int)LeggiF("barra_alfa_spenta", 55f));
    }

    void DisegnaAttrezzatura()
    {
        int id; string img, nome;
        // la canna che hai montato davvero, con la sua immagine del catalogo.
        // Le immagini delle canne sono lunghe e basse: le mettiamo a destra
        // nel riquadro largo, senza stirarle.
        // girata di 90 gradi in orario, poi giu' di 200 e a destra di 50
        if (Montato("canna", out id, out img, out nome))
        {
            // "canna_hud_x": il bordo sinistro della striscia girata. La
            // canna dentro la PNG e' una fascia di 13 px al centro della
            // striscia (107 px): a 986 sta a 4 px dalla barra a tacche.
            float chx = LeggiF("canna_hud_x", 986f);
            float chy = LeggiF("canna_hud_y", 365f);
            SpriteGirata(img, chx, chy, 270f, 108f, 90f);
            // i chili che regge, 8 pixel sotto la canna
            string kgc = PortataCanna(KgCanna(id));
            if (kgc.Length > 0)
                DisegnaTesto(kgc, chx + 54f + LeggiF("canna_kg_dx", -4f),
                             chy + 270f + 8f + LeggiF("canna_kg_dy", -5f),
                             0.19f, 245, 245, 250);
        }

        // la colonna del montaggio, dal basso in su:
        // mulinello -> bobina di lenza -> terminale -> galleggiante
        // "colonna_x": la colonna sta in linea sopra il cerchio della
        // frizione (icone larghe 112, centrate sul cerchio a 952)
        float mx = LeggiF("colonna_x", 896f), my = LeggiF("colonna_y", 590f);
        int piano = 0;
        // A ogni pezzo la SUA misura, quella che conta guardandolo:
        //   mulinello -> i metri di filo che ci stanno
        //   lenza     -> i chili che regge prima di spezzarsi
        //   amo       -> la misura
        // Le scritte stanno a sinistra dell'icona, tutte incolonnate.
        float tx = mx + 9f + LeggiF("colonna_testo_dx", 8f);


        // IL MULINELLO STA NEL CERCHIO DELLA FRIZIONE, a sinistra della
        // colonna: qui la colonna parte dalla lenza, in basso.
        if (Montato("lenza", out id, out img, out nome))
        {
            float ry = my - piano * 54f;
            // la bobina un filo piu' piccola degli altri ("colonna_lenza"),
            // centrata nella stessa casella
            QuadratoColonna(mx, ry);
            float ll = LeggiF("colonna_lenza", 38f);
            Sprite(img, mx + (112f - ll) * 0.5f, ry + (44f - ll) * 0.5f, ll, ll);
            float kg = KgLenza(id);
            if (kg > 0f)
                DisegnaTesto(kg.ToString("0.##", CultureInfo.InvariantCulture) + " kg",
                             tx, ry + 15f, 0.19f, 245, 245, 250);
            piano++;
        }

        // SOPRA LA LENZA CI VA QUELLO CHE MONTI, IN ORDINE: prima il
        // piombo, poi il leader, poi il galleggiante, in cima quello che
        // aggancia il pesce. Ognuno con la SUA immagine, non con la
        // scatola: il leader e' un rotolo di filo, si vede.
        if (MontatoTerm("piombo", out id, out img, out nome))
        {
            float ryP = my - piano * 54f;
            QuadratoColonna(mx, ryP);
            string fp = FormaTerminale(id);
            Sprite(fp.Length > 0 ? fp : img, mx, ryP, 112f, 44f);
            string mp = MisuraTerminale(id);
            if (mp.Length > 0) DisegnaTesto(mp, tx, ryP + 15f, 0.19f, 245, 245, 250);
            piano++;
        }
        if (MontatoTerm("leader", out id, out img, out nome))
        {
            float ryL = my - piano * 54f;
            QuadratoColonna(mx, ryL);
            string fl = FormaTerminale(id);
            Sprite(fl.Length > 0 ? fl : img, mx, ryL, 112f, 44f);
            string ml = MisuraTerminale(id);
            if (ml.Length > 0) DisegnaTesto(ml, tx, ryL + 15f, 0.19f, 245, 245, 250);
            piano++;
        }
        if (Montato("galleggiante", out id, out img, out nome))
        {
            float ryG = my - piano * 54f;
            QuadratoColonna(mx, ryG);
            Sprite(img, mx, ryG, 112f, 44f);
            // la portata: quanto piombo regge, che e' il dato dell'armatura
            int ig;
            for (ig = 0; ig < galleggianti.Count; ig++)
                if (galleggianti[ig].Id == id && galleggianti[ig].Portata.Length > 0)
                {
                    DisegnaTesto(PortataIt(galleggianti[ig].Portata),
                                 tx, ryG + 15f, 0.19f, 245, 245, 250);
                    break;
                }
            piano++;
        }

        if (Montato("terminale", out id, out img, out nome))
        {
            float ry = my - piano * 54f;
            QuadratoColonna(mx, ry);
            string fa = FormaTerminale(id);
            Sprite(fa.Length > 0 ? fa : img, mx, ry, 112f, 44f);
            string mis = MisuraTerminale(id);
            if (mis.Length > 0)
                DisegnaTesto(mis, tx, ry + 15f, 0.19f, 245, 245, 250);
            piano++;
        }

    }

    void HudPesca()
    {
        int id; string img, nome;
        DisegnaCanna();

        // LA FASCIA DEL TORNEO: sta in alto al centro e dice solo quello
        // che serve mentre peschi: quanto manca, quanti chili hai fatto e
        // a che medaglia sei.
        if (torneoOra >= 0 && torneoOra < tornei.Count)
        {
            Torneo tg = tornei[torneoOra];
            int med = Medaglia(tg, torneoKg);
            float pros = (med == 0) ? tg.KgBronzo
                       : (med == 1) ? tg.KgArgento
                       : (med == 2) ? tg.KgOro : 0f;
            DisegnaRett(440f, 40f, 400f, 34f, 12, 26, 24, 225);
            DisegnaTesto(tg.Nome, 640f, 42f, 0.24f, 235, 245, 240);
            DisegnaTesto(TempoTorneo(), 460f, 58f, 0.24f, 245, 205, 80);
            DisegnaTesto(Kg(torneoKg) + " kg", 640f, 58f, 0.24f, 235, 245, 240);
            string dx2 = (pros > 0f)
                ? (Kg(pros) + " kg -> " + NomeMedaglia(med + 1))
                : NomeMedaglia(3).ToUpper();
            DisegnaTesto(dx2, 820f, 58f, 0.22f, 130, 200, 245);
        }

        DisegnaAttrezzatura();
        // LA NASSA IN BASSO A SINISTRA.
        // Non la foto del prodotto: un disegno solo, sempre lo stesso,
        // come il galleggiante e il cucchiaino del quadrante. La foto
        // della nassa che hai comprato sta in cassetta, qui serve solo
        // sapere quanti chili ci stanno dentro.
        //   nassa_x/nassa_y  dove sta, nassa_lato quanto e' grande,
        //   nassa_testo_y    l'altezza della scritta.
        if (Montato("nassa", out id, out img, out nome))
        {
            // sta in alto a sinistra dell'esca: icona piccola e, di
            // fianco, i chili che porti su quanti ne reggi
            float nx = LeggiF("nassa_x", 1058f);
            float ny = LeggiF("nassa_y", 20f);
            float nl = LeggiF("nassa_lato", 25f);
            Sprite("img\\nasse\\nassa_base.png", nx, ny, nl, nl);
            string kgN = KgNassaDentro().ToString("0.0", CultureInfo.InvariantCulture)
                       + "/" + ((int)KgNassaMax()) + " kg";
            float scN = LeggiF("nassa_testo", 0.30f);
            float txN = nx + nl + LeggiF("nassa_testo_x", 34f);
            float tyN = ny + nl * 0.5f - LeggiF("nassa_testo_su", 11f);
            DisegnaTesto(kgN, txN, tyN, scN, 245, 245, 250);
            // sotto, piccolo: il pesce piu' grosso che ci sta dentro.
            // Oltre quello lo rilasci, per quanto sia grande la rete.
            float kgMaxP = KgPesceMax();
            if (kgMaxP > 0f)
                DisegnaTesto("max " + kgMaxP.ToString("0.##", CultureInfo.InvariantCulture)
                             + " kg", txN, tyN + LeggiF("nassa_riga2", 14f),
                             LeggiF("nassa_testo2", 0.22f), 245, 245, 250);
        }

        // L'ESCA, IN ALTO A DESTRA.
        // A spinning l'esca E' il cucchiaino: non c'e' il pane, e nella
        // casella dell'esca ci sta l'artificiale che hai montato, con
        // quanti ne hai. E' li' che si guarda cosa stai offrendo.
        // COME IN FISHING PLANET: un cerchio leggero attorno all'esca, un
        // cerchietto in alto a destra con quante ne restano, e a
        // sinistra il nome dell'esca e l'amo che sta pescando.
        //   esca_cx / esca_cy    centro del cerchio grande
        //   esca_cerchio         diametro del cerchio grande
        //   esca_img             larghezza dell'immagine dentro
        //   esca_num_cerchio     diametro del cerchietto del numero
        //   esca_num_dx / dy     dove sta il cerchietto rispetto al centro
        //   esca_testo_dx        quanto a sinistra del cerchio sta il testo
        int idEsca = escaMontata;
        string imgEsca = "", nomeEsca = "";
        int prezzoE, livE;
        int idArt; string imgArt, nomeArt;
        bool conArt = Montato("artificiale", out idArt, out imgArt, out nomeArt);
        string imgSlot = "", nomeSlot = "";
        int quanteSlot = 0;
        if (conArt)
        {
            imgSlot = imgArt; nomeSlot = nomeArt;
            quanteSlot = QuantiPezzi("artificiale", idArt);
        }
        else
        {
            if (idEsca < 0 || !Articolo("esca", idEsca, out nomeEsca, out imgEsca, out prezzoE, out livE))
            {
                if (Montato("esca", out id, out img, out nome))
                { idEsca = id; imgEsca = img; nomeEsca = nome; }
                else idEsca = -1;
            }
            if (idEsca >= 0)
            {
                imgSlot = imgEsca; nomeSlot = nomeEsca;
                quanteSlot = QuanteEsche(idEsca);
            }
        }
        if (imgSlot.Length > 0)
        {
            float ecx = LeggiF("esca_cx", 1215f);
            float ecy = LeggiF("esca_cy", 62f);
            float ed = LeggiF("esca_cerchio", 92f);
            float ew2 = LeggiF("esca_img", 64f);
            float nd = LeggiF("esca_num_cerchio", 38f);
            float ndx = LeggiF("esca_num_dx", 34f);
            float ndy = LeggiF("esca_num_dy", -30f);
            float tdx = LeggiF("esca_testo_dx", 10f);
            Sprite("img\\hud\\cerchio.png", ecx - ed * 0.5f, ecy - ed * 0.5f, ed, ed);
            // l'immagine dentro, con le sue proporzioni
            Sprite(imgSlot, ecx - ew2 * 0.5f, ecy - ew2 * 0.5f, ew2, ew2);
            // il cerchietto col numero
            float ncx = ecx + ndx, ncy = ecy + ndy;
            // il cerchietto attorno al numero solo se "esca_num_cerchio" > 0
            if (nd > 0f)
                Sprite("img\\hud\\cerchio_piccolo.png", ncx - nd * 0.5f, ncy - nd * 0.5f, nd, nd);
            DisegnaTesto("" + quanteSlot, ncx + LeggiF("esca_num_tx", 2f), ncy - 8f + LeggiF("esca_num_ty", -2f),
                         LeggiF("esca_num_testo", 0.32f), 245, 245, 250);
            // a sinistra: il nome, e sotto l'amo che pesca
            float tx = ecx - ed * 0.5f - tdx;
            DisegnaTestoDestra(nomeSlot, tx, ecy - 22f, LeggiF("esca_nome_testo", 0.22f), 245, 245, 250);
            if (!conArt)
            {
                int idT2; string imT2, nmT2;
                if (Montato("terminale", out idT2, out imT2, out nmT2))
                {
                    string mis = MisuraTerminale(idT2);
                    if (mis.Length > 0)
                        DisegnaTestoDestra(L("Hook ", "Amo ") + mis, tx, ecy - 4f,
                                           LeggiF("esca_amo_testo", 0.22f), 245, 245, 250);
                }
            }
            else
            {
                // l'artificiale: la sua misura (grammi, cm) e l'amo che porta
                int idA2 = InUso("artificiale");
                string misA = MisuraArtificiale(idA2);
                int ia2;
                for (ia2 = 0; ia2 < artificiali.Count; ia2++)
                    if (artificiali[ia2].Id == idA2 && artificiali[ia2].Amo.Length > 0)
                    { misA = Unisci(misA, L("hook ", "amo ") + artificiali[ia2].Amo); break; }
                if (misA.Length > 0)
                    DisegnaTestoDestra(misA, tx, ecy - 4f, LeggiF("esca_amo_testo", 0.22f), 245, 245, 250);
            }
        }
    }

    // quanto e' largo un testo, in pixel su 1280
    float LarghezzaTesto(string t, float scala, int font)
    {
        try
        {
            Function.Call(Hash.BEGIN_TEXT_COMMAND_GET_SCREEN_WIDTH_OF_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, t);
            Function.Call(Hash.SET_TEXT_FONT, font);
            Function.Call(Hash.SET_TEXT_SCALE, scala, scala);
            return Function.Call<float>(Hash.END_TEXT_COMMAND_GET_SCREEN_WIDTH_OF_DISPLAY_TEXT, true) * 1280f;
        }
        catch { return t.Length * scala * 20f; }
    }

    // testo del menu: font a scelta (0 ChaletLondon, 4 ChaletComprime, 7 Pricedown),
    // allineamento 0 sinistra 1 centro 2 destra, senza contorno
    void TestoMenu(string txt, float x, float y, float scala, int font, int all, int r, int g, int b, int a)
    {
        try
        {
            TextElement el = new TextElement(txt, new PointF(x, y), scala);
            el.Color = Color.FromArgb(a, r, g, b);
            el.Font = (GTA.UI.Font)font;
            el.Alignment = (all == 1) ? Alignment.Center : (all == 2 ? Alignment.Right : Alignment.Left);
            el.Outline = false;
            el.Draw();
        }
        catch { }
    }

    void DisegnaTestoDestra(string txt, float x, float y, float scala, int r, int g, int b)
    {
        try
        {
            TextElement el = new TextElement(txt, new PointF(x, y), scala);
            el.Color = Color.FromArgb(255, r, g, b);
            el.Font = GTA.UI.Font.ChaletLondon;
            el.Alignment = Alignment.Right;
            el.Outline = true;
            el.Draw();
        }
        catch { }
    }

    // la misura dell'amo (o del terminale) montato
    // NELL'HUD L'AMO E' L'AMO, non la scatola.
    // La scatolina va bene in negozio, in cassetta e a casa: li' stai
    // comprando o spostando una confezione. Sulla canna invece ci sta
    // l'amo montato, e il wiki il disegno ce l'ha: e' il campo "forma".
    string FormaTerminale(int id)
    {
        int i;
        for (i = 0; i < terminali.Count; i++)
            if (terminali[i].Id == id)
                return (terminali[i].Forma.Length > 0)
                       ? terminali[i].Forma : terminali[i].Img;
        return "";
    }

    // "amo" va bene per gli ami, ma nella stessa categoria ci stanno
    // anche leader, rig e piombi: il messaggio deve dire quello giusto
    string NomeTerminale(int id)
    {
        int i;
        for (i = 0; i < terminali.Count; i++)
            if (terminali[i].Id == id)
            {
                string c = terminali[i].Cat;
                if (c == "leader") return "il leader";
                if (c == "rig") return "il rig";
                if (c == "piombo") return "il piombo";
                if (c == "jig") return "la testina";
                return "l'amo";
            }
        return "l'amo";
    }

    string MisuraArtificiale(int id)
    {
        int i;
        for (i = 0; i < artificiali.Count; i++)
            if (artificiali[i].Id == id)
            {
                string r = "";
                if (artificiali[i].Grammi.Length > 0) r = artificiali[i].Grammi + " g";
                if (artificiali[i].Cm.Length > 0)
                    r = (r.Length > 0) ? (r + "  " + artificiali[i].Cm + " cm")
                                       : (artificiali[i].Cm + " cm");
                return r;
            }
        return "";
    }

    string MisuraTerminale(int id)
    {
        int i;
        for (i = 0; i < terminali.Count; i++)
            if (terminali[i].Id == id) return terminali[i].Misura;
        return "";
    }

    // i chili che regge una lenza prima di spezzarsi
    float KgLenza(int id)
    {
        int i;
        for (i = 0; i < lenze.Count; i++)
            if (lenze[i].Id == id) return lenze[i].Kg;
        return 0f;
    }

    // la portata della canna scritta corta: "1.50 - 4.00" -> "1.5/4 kg"
    static string PortataCanna(string s)
    {
        if (s == null || s.Length == 0) return "";
        string[] p = s.Split('-');
        float a, b;
        if (p.Length < 2)
        {
            if (!float.TryParse(p[0].Trim(), NumberStyles.Float,
                                CultureInfo.InvariantCulture, out a)) return "";
            return a.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
        }
        if (!float.TryParse(p[0].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out a)) return "";
        if (!float.TryParse(p[1].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out b)) return "";
        return a.ToString("0.##", CultureInfo.InvariantCulture) + "/"
             + b.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
    }

    // la portata di una canna, come sta scritta nel wiki: "1.50 - 4.00"
    string KgCanna(int id)
    {
        int i;
        for (i = 0; i < canne.Count; i++)
            if (canne[i].Id == id) return canne[i].LenzaKg;
        return "";
    }

    // il tipo di una lenza: mono, fluoro, braid, mare
    string TipoLenza(int id)
    {
        int i;
        for (i = 0; i < lenze.Count; i++)
            if (lenze[i].Id == id) return lenze[i].Tipo;
        return "";
    }

    // QUANTO FILO STA SU UN MULINELLO.
    // Il wiki da' per ogni mulinello la capacita' a un diametro di
    // riferimento, un valore per tipo di filo:
    //     "mono 0.25/100;  braid 0.20/125"
    // cioe' 100 metri di monofilo dello 0.25, oppure 125 metri di
    // trecciato dello 0.20. Sulla bobina del mulinello c'e' un volume
    // fisso: piu' il filo e' sottile, piu' ne sta.
    // Il fluorocarbon e le lenze da mare li trattiamo come il monofilo,
    // perche' il wiki per loro non da' una riga a parte.
    int MetriSulMulinello(int idMul, string tipoLenza)
    {
        string cap = "";
        int i;
        for (i = 0; i < mulinelli.Count; i++)
            if (mulinelli[i].Id == idMul) { cap = mulinelli[i].Capacita; break; }
        if (cap == null || cap.Length == 0) return 0;

        string cerco = (tipoLenza == "braid") ? "braid" : "mono";
        string[] pezzi = cap.Split(';');
        for (i = 0; i < pezzi.Length; i++)
        {
            string s = pezzi[i].Trim().ToLower();
            if (s.Length == 0) continue;
            if (!s.StartsWith(cerco)) continue;
            // l'ultima barra, non la prima: un mulinello nel wiki ha
            // "0.18//80" con due barre
            int barra = s.LastIndexOf('/');
            if (barra < 0) continue;
            return Numero(s.Substring(barra + 1).Trim());
        }
        return 0;
    }

    // I metri di filo che stanno sul mulinello montato, calcolati col
    // tipo di lenza che ci hai messo sopra.
    // QUANTO RECUPERA IL MULINELLO CHE HAI MONTATO.
    // Sul wiki ogni mulinello ha i centimetri di filo per giro di manovella
    // ("recupero_cm"). Ottanta e' la media: sopra recuperi piu' in fretta,
    // sotto piu' piano. Cosi' un mulinello grosso si sente davvero.
    float FattoreRecupero()
    {
        int id; string img, nome;
        if (!Montato("mulinello", out id, out img, out nome)) return 1f;
        int i;
        for (i = 0; i < mulinelli.Count; i++)
        {
            if (mulinelli[i].Id != id) continue;
            float cm = NumeroPiuAlto(mulinelli[i].Recupero);
            if (cm <= 0f) return 1f;
            float f = cm / 80f;
            if (f < 0.6f) f = 0.6f;
            if (f > 1.8f) f = 1.8f;
            return f;
        }
        return 1f;
    }

    int MetriSuQuestoMulinello(int idMul)
    {
        int idl; string il, nl;
        if (!Montato("lenza", out idl, out il, out nl)) return 0;
        // se la lenza e' imbobinata, i metri veri sono quelli che ci sono
        // rimasti sopra, non quelli scritti sulla confezione
        if (metriInBobina > 0) return metriInBobina;
        int m = MetriSulMulinello(idMul, TipoLenza(idl));
        if (m <= 0) return 0;
        int bobina = MetriLenza(idl);
        if (bobina > 0 && m > bobina) m = bobina;
        return m;
    }

    // I metri che hai davvero in acqua: li decide il mulinello montato.
    // Se la bobina che hai ne contiene meno, di piu' non ne puoi mettere.
    int MetriMontati(int idLenza)
    {
        int idm; string im, nm;
        if (!Montato("mulinello", out idm, out im, out nm)) return 0;
        int m = MetriSulMulinello(idm, TipoLenza(idLenza));
        if (m <= 0) return 0;
        int bobina = MetriLenza(idLenza);
        if (bobina > 0 && m > bobina) m = bobina;
        return m;
    }

    // i metri della bobina di lenza montata
    int MetriLenza(int id)
    {
        int i;
        for (i = 0; i < lenze.Count; i++)
            if (lenze[i].Id == id) return lenze[i].Metri;
        return 0;
    }

    // quanto pesa il pescato che hai nella nassa e quanto ce ne sta
    float KgNassaDentro() { return kgNassa; }
    float kgNassa = 0f;
    int soldiNassa = 0;      // quanto vale il pesce nella nassa, si incassa a fine giornata

    // LA NASSA SI VENDE: a fine giornata (e a ogni giornata della licenza)
    // il pesce tenuto si incassa al prezzo al chilo del wiki.
    void VendiNassa()
    {
        if (soldiNassa <= 0) { soldiNassa = 0; return; }
        Paga(-soldiNassa);
        Avviso("~g~Pesce venduto: +$" + soldiNassa);
        Diario("nassa venduta: $" + soldiNassa);
        soldiNassa = 0;
    }

    // il pesce piu' grosso che ci sta dentro: oltre questo lo rilasci,
    // per quanto sia grande la rete
    float KgPesceMax()
    {
        int id; string img, nome;
        if (!Montato("nassa", out id, out img, out nome)) return 0f;
        int i;
        for (i = 0; i < nasse.Count; i++)
            if (nasse[i].Id == id) return nasse[i].KgPesce;
        return 0f;
    }

    float KgNassaMax()
    {
        int id; string img, nome;
        if (!Montato("nassa", out id, out img, out nome)) return 0f;
        int i;
        for (i = 0; i < nasse.Count; i++)
            if (nasse[i].Id == id) return nasse[i].KgTotale;
        return 0f;
    }

    // Q / RB: gira fra le esche che hai in borsa
    int escaMontata = -1;

    void CambiaEsca()
    {
        // A SPINNING IL TASTO CAMBIA IL CUCCHIAINO.
        // Non c'e' esca da infilare: quello che offri e' l'artificiale,
        // e con lo stesso tasto giri fra quelli che ti sei portato.
        if (InUso("artificiale") >= 0) { CambiaArtificiale(); return; }

        List<int> ids = new List<int>();
        foreach (KeyValuePair<string, int> kv in borsa)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length < 2 || c[0] != "esca") continue;
            ids.Add(Numero(c[1]));
        }
        if (ids.Count == 0)
        {
            Avviso("~y~Non hai esche in borsa.");
            return;
        }
        int dove = ids.IndexOf(escaMontata);
        escaMontata = ids[(dove + 1) % ids.Count];
        string nome, img;
        int prezzo, liv;
        if (Articolo("esca", escaMontata, out nome, out img, out prezzo, out liv))
            Avviso("~g~Esca: ~s~" + nome + "  x" + QuanteEsche(escaMontata));
        Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
    }

    void CambiaArtificiale()
    {
        List<int> ids = new List<int>();
        foreach (KeyValuePair<string, int> kv in borsa)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length < 2 || c[0] != "artificiale") continue;
            ids.Add(Numero(c[1]));
        }
        int ora = InUso("artificiale");
        if (ora >= 0 && !ids.Contains(ora)) ids.Add(ora);
        if (ids.Count < 2) { Messaggio("Non hai altri artificiali in cassetta."); return; }
        ids.Sort();
        int dove = ids.IndexOf(ora);
        int nuovo = ids[(dove + 1) % ids.Count];
        if (nuovo == ora) return;
        if (Arma("artificiale", nuovo))
        {
            string nome, img;
            int prezzo, liv;
            if (Articolo("artificiale", nuovo, out nome, out img, out prezzo, out liv))
                Messaggio(nome + "   x" + QuantiPezzi("artificiale", nuovo));
            Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            SalvaStato();
            RiscriviTutto();
        }
    }

    void Preso()
    {
        // FUORI DALL'ACQUA, APPESO AL FILO.
        // Il pesce che stava lottando non sparisce: passa sotto la punta
        // della canna e ci resta finche' non decidi se tenerlo.
        pesceAppeso = true;
        Specie s = pesci[pesceQui];
        string kg = pesceKg.ToString("0.##", CultureInfo.InvariantCulture);

        // XP: la formula sta in livelli.txt
        int volte = Quanti(quaderno, s.Nome);
        float b = 20f + pesceKg * 15f;
        float r = (volte == 0) ? 8f : (volte < 5 ? 3f : (volte < 20 ? 1.5f : 1.3f));
        float t = 1f;
        if (pesceKg >= s.KgU && s.KgU > 0f) t = 3f;
        else if (pesceKg >= s.KgT && s.KgT > 0f) t = 2f;
        Suono("CHECKPOINT_PERFECT", "HUD_MINI_GAME_SOUNDSET");

        // LA NASSA HA DUE LIMITI, e sono due cose diverse: il pesce
        // singolo piu' grosso che ci entra, e quanti chili in tutto.
        // L'ULTIMO pesce puo' sforare: finche' nella rete c'e' ancora
        // spazio il pesce entra lo stesso, anche se col suo peso si va
        // oltre. Quindi il massimo vero che puoi arrivare a portare a
        // casa e' kg_totale + kg_pesce_max. Piena vuol dire che sei gia'
        // arrivato al limite, non che il prossimo pesce lo supererebbe.
        // Qui non si decide niente: si guarda solo se ci starebbe.
        float max = KgNassaMax();
        float maxPesce = KgPesceMax();
        cardPuoTenere = true;
        cardPerche = "";
        if (max <= 0f)
        {
            cardPuoTenere = false;
            cardPerche = "Non hai una nassa dove metterlo";
        }
        else if (maxPesce > 0f && pesceKg > maxPesce)
        {
            cardPuoTenere = false;
            cardPerche = "Troppo grosso per questa nassa (max "
                       + maxPesce.ToString("0.##", CultureInfo.InvariantCulture) + " kg)";
        }
        else if (kgNassa >= max)
        {
            cardPuoTenere = false;
            cardPerche = "La nassa e' piena";
        }
        int guadagno = (int)(b * r * t);
        xpTot += guadagno;

        // QUANTO VALE: E' UN PREZZO AL CHILO.
        // Sul wiki "price_common = 70" vuol dire settanta crediti AL
        // CHILO, non settanta a pesce: un bluegill da 180 grammi e uno
        // da 400 non possono valere uguale. La fascia - comune, trofeo,
        // unico - decide la tariffa, il peso decide il totale.
        int tariffa = s.PrC;
        if (s.KgU > 0f && pesceKg >= s.KgU) tariffa = s.PrU;
        else if (s.KgT > 0f && pesceKg >= s.KgT) tariffa = s.PrT;
        // IL PREZZO DEL PESCE E' QUELLO DEL WIKI, AL CHILO, PIENO: la
        // divisione per dieci (CAMBIO) e' solo dell'attrezzatura, che ha i
        // prezzi gonfiati. Il pesce no: un luccio vale 170 al chilo.
        int vale = (int)(tariffa * pesceKg + 0.5f);

        Aggiungi(quaderno, s.Nome, 1);
        {
            int aq = LuogoQui();
            if (aq >= 0 && aq < arNome.Count) Aggiungi(presoQui, arNome[aq] + "|" + s.Nome, 1);
        }
        // il diario tiene il piu' grosso, non l'ultimo
        float vecchio = record.ContainsKey(s.Nome) ? record[s.Nome] : 0f;
        if (pesceKg > vecchio)
        {
            record[s.Nome] = pesceKg;
            int luR = LuogoQui();
            string bzR;
            dovePreso[s.Nome] = (luR >= 0)
                              ? NomeChiosco(CodiceLuogo(luR), out bzR) : "";
            // con che esca e con che amo: e' questo che fa di un elenco
            // un diario. Senza, non sai come ripetere la pescata.
            string ne2 = "", ie2; int pe2, le2;
            if (escaMontata >= 0
                && Articolo("esca", escaMontata, out ne2, out ie2, out pe2, out le2))
                recEsca[s.Nome] = ne2;
            else recEsca.Remove(s.Nome);
            int idT; string imT, nmT;
            if (Montato("terminale", out idT, out imT, out nmT))
                recAmo[s.Nome] = MisuraTerminale(idT);
            else recAmo.Remove(s.Nome);
            recXp[s.Nome] = guadagno;
            recVale[s.Nome] = vale;
        }
        int livPrima = livelloPescatore;
        livelloPescatore = LivelloDa(xpTot);


        if (livelloPescatore > livPrima)
            Avviso("~y~LIVELLO " + livelloPescatore + "!");
        Vibra(300, 120);

        // la finestra della cattura: resta li' finche' non scegli
        cardPesce = pesceQui;
        cardKg = pesceKg;
        cardXp = guadagno;
        cardVale = vale;
        if (s.KgU > 0f && pesceKg >= s.KgU) cardTaglia = "ESEMPLARE UNICO";
        else if (s.KgT > 0f && pesceKg >= s.KgT) cardTaglia = "TROFEO";
        else cardTaglia = "COMUNE";
        SegnaColpoGrosso(s, pesceKg);
        fase = FASE_CARD;

        SalvaStato();
        RiscriviTutto();
    }

    // IL DIARIO DI PESCA.
    // quaderno: quante volte hai preso ogni specie.
    // record:   il piu' grosso di quella specie che hai tirato su.
    // dovePreso: in che acqua l'hai fatto quel record.
    // Insieme fanno il diario: non un elenco di catture, ma il meglio
    // che hai fatto con ognuna delle 239 specie.
    // IMPOSTAZIONI DELLA MOD.
    // Se la pesca la fai quando ne hai voglia, la scritta "Zona di
    // pesca" ogni volta che passi vicino a una riva rompe. Si spegne.
    bool avvisaZona = true;
    bool diarioChiesto = false;
    bool resetChiesto = false;   // conferma per azzerare il diario

    // QUELLO CHE HAI CONSUMATO.
    // Le esche si comprano a confezioni ma si usano a pezzi: cento
    // bocconi di pane sono una confezione sola, e ogni volta che ne
    // infili uno sull'amo ne resta uno di meno. Qui teniamo il conto
    // dei pezzi gia' usati di ogni articolo; quando finiscono si
    // consuma una confezione e si riparte.
    Dictionary<string, int> usati = new Dictionary<string, int>();

    Dictionary<string, int> quaderno = new Dictionary<string, int>();
    // E PER POSTO: "area|specie" -> quante volte l'hai presa PROPRIO LI'.
    // L'esplorazione di un posto conta solo queste, non il quaderno.
    Dictionary<string, int> presoQui = new Dictionary<string, int>();
    Dictionary<string, float> record = new Dictionary<string, float>();
    Dictionary<string, string> dovePreso = new Dictionary<string, string>();
    // e con cosa l'hai preso, quando hai fatto quel record
    Dictionary<string, string> recEsca = new Dictionary<string, string>();
    Dictionary<string, string> recAmo = new Dictionary<string, string>();
    Dictionary<string, int> recXp = new Dictionary<string, int>();
    Dictionary<string, int> recVale = new Dictionary<string, int>();


    // ============================================================
    //  LA SCENA: canna in mano, posa del pescatore, galleggiante.
    //  Ripreso dalla mod vecchia, che queste cose le faceva bene.
    // ============================================================

    // i valori regolati a mano stanno in config.ini, come nella mod vecchia:
    // cosi' si ritoccano a gioco acceso senza ricompilare
    float LeggiF(string chiave, float dif)
    {
        try
        {
            string f = Path.Combine(MY_DIR, "config.ini");
            if (!File.Exists(f)) return dif;
            string[] r = File.ReadAllLines(f);
            int i;
            for (i = 0; i < r.Length; i++)
            {
                string l = r[i].Trim();
                if (l.Length == 0 || l[0] == '#') continue;
                int eq = l.IndexOf('=');
                if (eq < 1) continue;
                if (l.Substring(0, eq).Trim() != chiave) continue;
                float v;
                if (float.TryParse(l.Substring(eq + 1).Trim(),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
            }
        }
        catch { }
        return dif;
    }

    string LeggiS(string chiave, string dif)
    {
        try
        {
            string f = Path.Combine(MY_DIR, "config.ini");
            if (!File.Exists(f)) return dif;
            string[] r = File.ReadAllLines(f);
            int i;
            for (i = 0; i < r.Length; i++)
            {
                string l = r[i].Trim();
                if (l.Length == 0 || l[0] == '#') continue;
                int eq = l.IndexOf('=');
                if (eq < 1) continue;
                if (l.Substring(0, eq).Trim() != chiave) continue;
                string v = l.Substring(eq + 1).Trim();
                if (v.Length > 0) return v;
            }
        }
        catch { }
        return dif;
    }

    Prop cannaProp = null;
    string clipInCorso = "";
    bool inScena = false;

    void OnAborted(object sender, EventArgs e)
    {
        try
        {
            TogliCanna();
            TogliBlipPunti();
            ViaCampo();
            ChiudiMenuNuovo();
            Ped p = Game.Player.Character;
            if (p != null && p.Exists()) p.Task.ClearAll();
            if (orologioPreso) Function.Call(Hash.PAUSE_CLOCK, false);
        }
        catch { }
    }

    // le canne rimaste appese da una ricarica precedente
    void PulisciCanneRimaste()
    {
        try
        {
            Ped p = Game.Player.Character;
            if (p == null || !p.Exists()) return;
            int mdl = Function.Call<int>(Hash.GET_HASH_KEY, "prop_fishing_rod_01");
            int q;
            for (q = 0; q < 6; q++)
            {
                int ent = Function.Call<int>(Hash.GET_CLOSEST_OBJECT_OF_TYPE,
                            p.Position.X, p.Position.Y, p.Position.Z, 3f, mdl, false, false, false);
                if (ent == 0) break;
                Prop vecchia = (Prop)Entity.FromHandle(ent);
                if (!vecchia.Exists()) break;
                vecchia.IsPersistent = true;
                vecchia.Delete();
            }
        }
        catch { }
    }

    void MettiCanna(Ped p)
    {
        TogliCanna();
        try
        {
            Model m = new Model("prop_fishing_rod_01");
            if (!m.IsValid || !m.IsInCdImage) return;
            m.Request();
            int w = 0;
            while (!m.IsLoaded && w < 1500) { Script.Wait(50); w += 50; }
            if (!m.IsLoaded) return;
            cannaProp = World.CreateProp(m, p.Position + new GTA.Math.Vector3(0f, 0f, 1f), false, false);
            m.MarkAsNoLongerNeeded();
            if (cannaProp == null || !cannaProp.Exists()) { cannaProp = null; return; }
            int osso = Function.Call<int>(Hash.GET_PED_BONE_INDEX, p, 18905);  // mano destra
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, cannaProp, p, osso,
                          LeggiF("canna_x", 0.13f), LeggiF("canna_y", 0.10f),
                          LeggiF("canna_z", 0.01f), LeggiF("canna_rx", 0f),
                          LeggiF("canna_ry", 90f), LeggiF("canna_rz", 70f),
                          false, false, false, false, 2, true);
        }
        catch { }
    }

    // ============================================================
    //  LA LENZA
    //  Un filo disegnato dalla punta della canna al punto dove sta l'esca.
    //  Non e' una corda fisica di GTA - quelle sono capricciose e cadono
    //  a terra - e' una riga tirata a ogni fotogramma, che segue la canna
    //  quando lui si muove e l'esca quando la recuperi.
    // ============================================================
    // i metri della canna montata, letti da canne.txt
    float LunghezzaCannaMontata()
    {
        int id; string img, nome;
        if (!Montato("canna", out id, out img, out nome)) return 0f;
        int i;
        for (i = 0; i < canne.Count; i++)
            if (canne[i].Id == id) return NumeroPiuAlto(canne[i].Lunghezza);
        return 0f;
    }

    // dove sta la punta nel modello: l'asse piu' lungo e quanto misura
    int puntaAsse = 2;
    float puntaVal = 0f;
    bool puntaFatta = false;

    void MisuraCanna()
    {
        if (puntaFatta) return;
        puntaFatta = true;
        try
        {
            OutputArgument omin = new OutputArgument();
            OutputArgument omax = new OutputArgument();
            int hash = Function.Call<int>(Hash.GET_HASH_KEY, "prop_fishing_rod_01");
            Function.Call(Hash.GET_MODEL_DIMENSIONS, hash, omin, omax);
            GTA.Math.Vector3 mn = omin.GetResult<GTA.Math.Vector3>();
            GTA.Math.Vector3 mx = omax.GetResult<GTA.Math.Vector3>();
            float dx = mx.X - mn.X, dy = mx.Y - mn.Y, dz = mx.Z - mn.Z;
            if (dx >= dy && dx >= dz) { puntaAsse = 0; puntaVal = mx.X; }
            else if (dy >= dx && dy >= dz) { puntaAsse = 1; puntaVal = mx.Y; }
            else { puntaAsse = 2; puntaVal = mx.Z; }
            Diario("punta canna: asse " + puntaAsse + " a "
                   + puntaVal.ToString("0.##", CultureInfo.InvariantCulture) + " m");
        }
        catch { puntaAsse = 2; puntaVal = 1.4f; }
    }

    // ============================================================
    //  LA CANNA CHE SI PIEGA - PROVA
    // ============================================================
    // Il modello della canna e' rigido: non ha ossa e non si deforma.
    // La lenza pero' la disegniamo noi, segmento per segmento, e lo
    // stesso si puo' fare con la canna. Con "canna_disegnata=1" il
    // modello si nasconde e la canna la disegniamo: dal calcio alla
    // punta, curvandola verso il pesce di quanto tira.
    //   canna_piega      quanto si piega alla tensione massima (in parte
    //                    della lunghezza della canna)
    //   canna_spessore   quante righe affiancate per fare lo spessore
    // Se non convince, si rimette "canna_disegnata=0" e torna com'era.
    bool CannaDisegnata() { return LeggiF("canna_disegnata", 0f) > 0.5f; }

    // il calcio della canna: l'origine del modello in mano
    GTA.Math.Vector3 CalcioCanna()
    {
        try
        {
            if (cannaProp != null && cannaProp.Exists())
                return Function.Call<GTA.Math.Vector3>(
                    Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS,
                    cannaProp, 0f, 0f, 0f);
        }
        catch { }
        return GTA.Math.Vector3.Zero;
    }

    // IL CALCIO ARRIVA IN MANO.
    // L'origine del modello non e' il fondo della canna: disegnandola da
    // li' il calcio restava fuori dalle dita. Si allunga all'indietro di
    // "canna_calcio" metri, lungo l'asse della canna.
    GTA.Math.Vector3 CalcioEsteso()
    {
        GTA.Math.Vector3 b = CalcioCanna();
        GTA.Math.Vector3 t = PuntaDritta();
        if (b == GTA.Math.Vector3.Zero || t == GTA.Math.Vector3.Zero) return b;
        GTA.Math.Vector3 d = t - b;
        float l = d.Length();
        if (l < 0.01f) return b;
        return b - (d / l) * LeggiF("canna_calcio", 0.12f);
    }

    // la punta VERA quando la canna e' piegata: e' li' che nasce la lenza
    GTA.Math.Vector3 PuntaPiegata()
    {
        GTA.Math.Vector3 b = CalcioEsteso();
        GTA.Math.Vector3 t = PuntaDritta();
        if (b == GTA.Math.Vector3.Zero || t == GTA.Math.Vector3.Zero) return t;
        return PuntoSullaCanna(b, t, 1f);
    }

    // un punto lungo la canna, da 0 (calcio) a 1 (punta). La curva e' una
    // Bezier: il calcio resta dritto, la punta va verso il pesce.
    GTA.Math.Vector3 PuntoSullaCanna(GTA.Math.Vector3 b, GTA.Math.Vector3 t, float u)
    {
        float ten = tensione / 100f;
        if (ten < 0f) ten = 0f;
        if (ten > 1f) ten = 1f;
        float piega = LeggiF("canna_piega", 0.28f) * ten;

        // verso dove tira: l'esca in acqua
        GTA.Math.Vector3 verso = new GTA.Math.Vector3(0f, 0f, -1f);
        if (escaInAcqua)
        {
            GTA.Math.Vector3 e = new GTA.Math.Vector3(escaX, escaY, AcquaSottoEsca());
            GTA.Math.Vector3 d = e - t;
            float l = d.Length();
            if (l > 0.01f) verso = d / l;
        }

        float lung = (t - b).Length();
        // il punto di controllo sta a due terzi, tirato verso il pesce
        // il verso della piega: se il modello e' orientato al contrario
        // la canna si inarca in su invece che verso il pesce. "canna_verso"
        // lo gira: 1 o -1.
        float vs = LeggiF("canna_verso", -1f);
        GTA.Math.Vector3 c = b + (t - b) * 0.66f + verso * (lung * piega * vs);
        float w = 1f - u;
        return b * (w * w) + c * (2f * w * u) + t * (u * u);
    }

    // la punta che usa tutto il resto della mod: se la canna la
    // disegniamo noi e' quella piegata, se no quella del modello
    GTA.Math.Vector3 PuntaCanna()
    {
        if (CannaDisegnata()) return PuntaPiegata();
        return PuntaDritta();
    }

    // LA PUNTA COM'E' DISEGNATA NEL MODELLO, senza piega.
    GTA.Math.Vector3 PuntaDritta()
    {
        try
        {
            if (cannaProp != null && cannaProp.Exists())
            {
                // LA PUNTA VERA DEL MODELLO.
                // Il modello della canna in mano e' sempre lo stesso, quindi
                // la punta sta dove sta: la si chiede al gioco con le misure
                // del modello, invece di indovinare un numero. La lunghezza
                // scritta in canne.txt qui non c'entra: usarla mandava la
                // lenza a mezz'aria, oltre la punta che si vede.
                MisuraCanna();
                float agg = LeggiF("canna_agg", 0f);
                float ox = 0f, oy = 0f, oz = 0f;
                if (puntaAsse == 0) ox = puntaVal + agg;
                else if (puntaAsse == 1) oy = puntaVal + agg;
                else oz = puntaVal + agg;
                return Function.Call<GTA.Math.Vector3>(
                    Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS,
                    cannaProp, ox, oy, oz);
            }
        }
        catch { }
        Ped p = Game.Player.Character;
        if (p != null && p.Exists()) return p.Position + new GTA.Math.Vector3(0f, 0f, 1.2f);
        return GTA.Math.Vector3.Zero;
    }

    // LA CANNA DISEGNATA: una spezzata dal calcio alla punta, con lo
    // spessore fatto di righe affiancate e il colore che si schiarisce
    // verso il cimino, come una canna vera.
    void DisegnaCanna()
    {
        if (!CannaDisegnata())
        {
            // spenta la prova, il modello torna a vedersi
            try
            {
                if (cannaProp != null && cannaProp.Exists())
                {
                    Function.Call(Hash.SET_ENTITY_VISIBLE, cannaProp, true, false);
                    Function.Call(Hash.RESET_ENTITY_ALPHA, cannaProp);
                }
            }
            catch { }
            return;
        }
        try
        {
            GTA.Math.Vector3 b = CalcioEsteso();
            GTA.Math.Vector3 t = PuntaDritta();
            if (b == GTA.Math.Vector3.Zero || t == GTA.Math.Vector3.Zero) return;

            // il modello sparisce: al suo posto disegniamo noi.
            // Due modi: invisibile e trasparente. Su Enhanced uno dei due
            // ogni tanto non basta da solo.
            if (cannaProp != null && cannaProp.Exists())
            {
                Function.Call(Hash.SET_ENTITY_VISIBLE, cannaProp, false, false);
                Function.Call(Hash.SET_ENTITY_ALPHA, cannaProp, 0, false);
            }

            int n = (int)LeggiF("canna_tratti", 14f);
            if (n < 4) n = 4;

            // A MARKER INVECE CHE A RIGHE (prova): un cilindro per tratto.
            if (LeggiF("canna_marker", 0f) > 0.5f)
            {
                DisegnaCannaMarker(b, t, n);
                return;
            }

            // LO SPESSORE E' FINTO.
            // DRAW_LINE fa sempre una riga da un pixel, non si puo'
            // ingrossare. L'unico modo e' affiancarne qualcuna a un
            // pelo di distanza: poche e vicinissime sembrano una riga
            // piu' grossa, tante e larghe sembrano un tubo - e il tubo
            // faceva perdere il senso della canna.
            //   canna_righe   quante righe affiancate (1 = una sola)
            //   canna_passo   quanto sono distanti in metri
            // PIENA, NON A RIGHE.
            // Affiancando le righe solo in orizzontale e in verticale
            // veniva fuori una croce vuota in mezzo, e si vedevano le
            // fessure. Adesso le righe riempiono un CERCHIO attorno
            // all'asse: per ogni pezzo di canna si prendono due
            // direzioni perpendicolari e ci si mette dentro una griglia
            // di righe, tenendo solo quelle che stanno nel cerchio.
            // Il passo lo detta la telecamera, cosi' restano attaccate
            // sia da vicino che da lontano.
            //   canna_pieno   raggio della griglia: 1 sottile, 3 grossa
            int pieno = (int)LeggiF("canna_pieno", 2f);
            if (pieno < 0) pieno = 0;
            if (pieno > 4) pieno = 4;
            bool prova = LeggiF("canna_prova_rossa", 0f) > 0.5f;

            float dist = 2f;
            try
            {
                GTA.Math.Vector3 cam = Function.Call<GTA.Math.Vector3>(
                    Hash.GET_GAMEPLAY_CAM_COORD);
                dist = (b - cam).Length();
                if (dist < 0.5f) dist = 0.5f;
                if (dist > 40f) dist = 40f;
            }
            catch { }
            float passo = dist * LeggiF("canna_passo_k", 0.0011f);

            GTA.Math.Vector3 prec = PuntoSullaCanna(b, t, 0f);
            int i, gi, gj;
            for (i = 1; i <= n; i++)
            {
                float u = (float)i / (float)n;
                GTA.Math.Vector3 q = PuntoSullaCanna(b, t, u);

                GTA.Math.Vector3 d = q - prec;
                float ld = d.Length();
                if (ld < 0.0001f) { prec = q; continue; }
                d = d / ld;
                GTA.Math.Vector3 su = new GTA.Math.Vector3(0f, 0f, 1f);
                if (Math.Abs(d.Z) > 0.9f) su = new GTA.Math.Vector3(1f, 0f, 0f);
                GTA.Math.Vector3 e1 = GTA.Math.Vector3.Cross(d, su);
                float l1 = e1.Length();
                if (l1 < 0.0001f) { prec = q; continue; }
                e1 = e1 / l1;
                GTA.Math.Vector3 e2 = GTA.Math.Vector3.Cross(d, e1);

                int col = (int)LeggiF("canna_col_giu", 22f)
                        + (int)(LeggiF("canna_col_su", 60f) * u);
                int rr = prova ? 250 : col;
                int gg = prova ? 40 : col;
                int bb = prova ? 40 : (col + 8);

                // in punta si assottiglia fino a una riga sola
                int rag = (int)(pieno * (1f - u * 0.8f));
                for (gi = -rag; gi <= rag; gi++)
                {
                    for (gj = -rag; gj <= rag; gj++)
                    {
                        if (gi * gi + gj * gj > rag * rag + 1) continue;
                        GTA.Math.Vector3 off = e1 * (gi * passo) + e2 * (gj * passo);
                        Function.Call(Hash.DRAW_LINE,
                                      prec.X + off.X, prec.Y + off.Y, prec.Z + off.Z,
                                      q.X + off.X, q.Y + off.Y, q.Z + off.Z,
                                      rr, gg, bb, 255);
                    }
                }
                prec = q;
            }
        }
        catch { }
    }

    // LA CANNA A CILINDRI.
    // DRAW_MARKER tipo 1 e' un cilindro verticale con la base nel punto
    // dato: lo si ruota perche' stia lungo il tratto di canna e lo si
    // allunga quanto il tratto. Il diametro si assottiglia verso la
    // punta. Le rotazioni seguono la convenzione delle entita' di GTA
    // (asse su = Z ruotato di rx attorno a X e di rz attorno a Z).
    //   canna_marker        1 accende i cilindri, 0 torna alle righe
    //   canna_marker_diam   diametro al calcio in metri
    //   canna_marker_punta  diametro in punta, in frazione del calcio
    //   canna_marker_dir    1 passa la direzione al marker invece
    //                       delle rotazioni (se le rotazioni sbagliano)
    void DisegnaCannaMarker(GTA.Math.Vector3 b, GTA.Math.Vector3 t, int n)
    {
        float diam = LeggiF("canna_marker_diam", 0.014f);
        float fPunta = LeggiF("canna_marker_punta", 0.25f);
        bool usaDir = LeggiF("canna_marker_dir", 0f) > 0.5f;
        bool prova = LeggiF("canna_prova_rossa", 0f) > 0.5f;
        GTA.Math.Vector3 prec = PuntoSullaCanna(b, t, 0f);
        int i;
        for (i = 1; i <= n; i++)
        {
            float u = (float)i / (float)n;
            GTA.Math.Vector3 q = PuntoSullaCanna(b, t, u);
            GTA.Math.Vector3 d = q - prec;
            float ld = d.Length();
            if (ld < 0.0001f) { prec = q; continue; }
            d = d / ld;

            int col = (int)LeggiF("canna_col_giu", 22f)
                    + (int)(LeggiF("canna_col_su", 60f) * u);
            int rr = prova ? 250 : col;
            int gg = prova ? 40 : col;
            int bb = prova ? 40 : (col + 8);

            float dm = diam * (1f - (1f - fPunta) * u);
            float rx = 0f, rz = 0f;
            if (!usaDir)
            {
                float dz = d.Z;
                if (dz > 1f) dz = 1f;
                if (dz < -1f) dz = -1f;
                rx = (float)(Math.Acos(dz) * 180.0 / Math.PI);
                rz = (float)(Math.Atan2(d.X, -d.Y) * 180.0 / Math.PI);
            }
            Function.Call(Hash.DRAW_MARKER, 1,
                          prec.X, prec.Y, prec.Z,
                          usaDir ? d.X : 0f, usaDir ? d.Y : 0f, usaDir ? d.Z : 0f,
                          rx, 0f, rz,
                          dm, dm, ld,
                          rr, gg, bb, 255,
                          false, false, 2, false, 0, 0, false);
            prec = q;
        }
    }

    // ---- LA RUOTA DEGLI ATTREZZI ----
    // Mentre peschi LB non apre piu' la ruota delle armi: apre questa.
    // Se la lenza e' in acqua, LB prima ritira la canna e poi apre.
    // Tieni premuto LB, con la levetta destra scegli lo spicchio, con
    // SINISTRA e DESTRA della croce giri fra i pezzi di quella categoria
    // che hai in borsa - come si cambia arma dentro uno spicchio - lasci
    // LB e il pezzo si monta. Dodici spicchi in senso orario dall'alto,
    // uno per ogni posto dell'armatura; in basso tre pagine del menu:
    // lo ZAINO apre l'equipaggiamento, la NASSA il pescato del giorno,
    // PESCI DEL LAGO i pesci del posto dove sei. Ci lasci LB e si apre. La canna resta in mano: LB con la lenza
    // in acqua la ritira e basta, a riporre la canna ci pensa X. Cucchiaino e galleggiante/
    // amo si scacciano da soli: montando uno si smonta l'altro.
    //   ruota_x / ruota_y   centro, in pixel su 1280x720
    //   ruota_raggio        raggio esterno
    //   ruota_icona         lato del riquadro delle icone
    //   ruota_centro        diametro del disco in mezzo
    //   ruota_verso         1 o -1 se gli spicchi girano al contrario
    //   ruota_soglia        quanto va spinta la levetta (0..1)
    bool ruotaAperta = false;
    int ruotaSpicchio = -1;
    const int RUOTA_N = 12;
    int[] ruotaPos = new int[RUOTA_N];
    // "terminale" qui e' la casella dell'amo; leader e piombo sono le
    // altre due caselle dei terminali, con la stessa categoria in borsa
    static readonly string[] RUOTA_CAT = new string[] {
        "canna", "mulinello", "lenza", "leader", "piombo", "pesci",
        "zaino", "nassa", "terminale", "galleggiante", "esca", "artificiale" };
    static readonly string[] RUOTA_NOME = new string[] {
        "Canna", "Mulinello", "Lenza", "Leader", "Piombo", "Pesci del lago",
        "Zaino", "Nassa", "Amo", "Galleggiante", "Esca", "Cucchiaino" };

    class VoceRuota
    {
        public string Cat; public int Id; public int Bob;
        public string Nome; public string Img; public string Dett;
        public bool Montata; public int Sp;
    }

    void AggVoce(List<VoceRuota> v, string cat, int id, int bob, bool montata)
    {
        if (id < 0) return;
        int i;
        for (i = 0; i < v.Count; i++)
            if (v[i].Cat == cat && v[i].Id == id && v[i].Bob == bob) return;
        string nome, img; int prezzo, liv;
        if (!Articolo(cat, id, out nome, out img, out prezzo, out liv)) return;
        VoceRuota r = new VoceRuota();
        r.Cat = cat; r.Id = id; r.Bob = bob; r.Nome = nome; r.Img = img;
        r.Montata = montata; r.Dett = "";
        // ami, leader e piombi: la forma, come nell'HUD, non la scatola
        if (cat == "terminale")
        {
            string forma = FormaTerminale(id);
            if (forma.Length > 0) r.Img = forma;
        }
        // I DATI, NON LA MARCA: nella ruota si sceglie per quello che il
        // pezzo fa. Lenza: mm, kg e metri (la bobina tagliata coi suoi);
        // canna, mulinello, nassa: i loro dati dell'inventario; ami,
        // galleggianti ed esche: i dati piu' quanti ne hai.
        if (bob >= 0) r.Dett = DettaglioBobina(id, BobinaMetri(bob));
        else
        {
            r.Dett = Dettaglio(cat, id);
            if (cat == "terminale" || cat == "galleggiante" || cat == "esca")
            {
                int q = QuantiPezzi(cat, id);
                if (q > 0) r.Dett = (r.Dett.Length > 0 ? r.Dett + "   " : "") + "x" + q;
            }
        }
        v.Add(r);
    }

    // le voci di uno spicchio: prima "Vuoto" - il posto senza niente,
    // ci lasci LB e quello che c'era torna in borsa - poi quello che c'e'
    // montato, poi la borsa
    List<VoceRuota> VociRuota(int sp)
    {
        List<VoceRuota> v = new List<VoceRuota>();
        if (sp < 0 || sp >= RUOTA_CAT.Length) return v;
        string cat = RUOTA_CAT[sp];
        if (cat.Length == 0 || cat == "zaino" || cat == "nassa" || cat == "pesci") return v;
        VoceRuota vuoto = new VoceRuota();
        vuoto.Cat = cat; vuoto.Id = -1; vuoto.Bob = -1;
        vuoto.Nome = "Vuoto"; vuoto.Dett = "";
        // l'ombra del posto, se c'e' il PNG (img/ruota/vuoto_<categoria>.png)
        vuoto.Img = "img/ruota/vuoto_" + cat + ".png";
        v.Add(vuoto);
        bool term = (cat == "terminale" || cat == "leader" || cat == "piombo");
        string casella = cat;
        if (term)
        {
            if (Armato(casella) >= 0) AggVoce(v, "terminale", Armato(casella), -1, true);
            cat = "terminale";
        }
        else if (cat == "esca")
        {
            if (escaMontata >= 0) AggVoce(v, "esca", escaMontata, -1, true);
        }
        else
        {
            int idm = InUso(cat);
            if (idm >= 0) AggVoce(v, cat, idm, -1, true);
        }
        List<int> ids = new List<int>();
        foreach (KeyValuePair<string, int> kv in borsa)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length < 2 || c[0] != cat || kv.Value <= 0) continue;
            int idb = Numero(c[1]);
            // nello spicchio dell'amo solo gli ami, in quello del leader solo i leader
            if (term && CasellaTerm(SottoTerm(idb)) != casella) continue;
            ids.Add(idb);
        }
        ids.Sort();
        int i;
        for (i = 0; i < ids.Count; i++) AggVoce(v, cat, ids[i], -1, false);
        if (cat == "lenza")
            for (i = 0; i < bobine.Count; i++) AggVoce(v, "lenza", BobinaId(i), i, false);
        // "Vuoto" e' quello montato se non c'e' montato niente
        bool qualcosa = false;
        for (i = 1; i < v.Count; i++) if (v[i].Montata) qualcosa = true;
        vuoto.Montata = !qualcosa;
        for (i = 0; i < v.Count; i++) v[i].Sp = sp;
        return v;
    }

    // dove sta il cursore quando apri: sul pezzo montato
    int PosMontata(int sp)
    {
        List<VoceRuota> v = VociRuota(sp);
        int i;
        for (i = 0; i < v.Count; i++) if (v[i].Montata) return i;
        return 0;
    }

    // la lenza rientra e la canna resta in mano
    void RitiraLenza()
    {
        if (fase == FASE_FERMO || fase == FASE_PRONTO || fase == FASE_CARD) return;
        TogliPesce();
        ViaRoba();
        robaOra = -1;
        metriLenza = 0f;
        escaInAcqua = false;
        fase = FASE_PRONTO;
        grillettoMollato = false;
        tastoDa = Game.GameTime + 400;
        Messaggio("Lenza ritirata");
    }

    void Ruota()
    {
        Ped p = Game.Player.Character;
        if (!inPesca || p.IsInVehicle()) { ruotaAperta = false; return; }
        // niente ruota delle armi mentre peschi
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 37, true);
        bool lb = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, 37);
        // CON LA LENZA IN ACQUA LB LA RITIRA, la canna resta in mano:
        // non si cambia il mulinello col filo fuori. Col pesce in mano
        // (la scheda) non si apre: prima decidi se tenerlo.
        if (fase == FASE_CARD) { ruotaAperta = false; return; }
        if (lb && fase != FASE_FERMO && fase != FASE_PRONTO) RitiraLenza();
        if (!lb)
        {
            if (ruotaAperta)
            {
                ruotaAperta = false;
                // se il trainer non l'ha raccolto, via il file
                try
                {
                    string fc = Path.Combine(MY_DIR, "chiudi.txt");
                    if (File.Exists(fc)) File.Delete(fc);
                }
                catch { }
                MontaDallaRuota();
                // CHIUSA LA RUOTA: con una canna armata la prendi in mano
                // (stessi controlli di "Inizia a pescare": se manca
                // qualcosa te lo dice), senza canna la riponi.
                int idc; string imgc, nomec;
                bool cannaSu = Montato("canna", out idc, out imgc, out nomec);
                if (cannaSu && fase == FASE_FERMO) Esegui("pesca_via");
                else if (!cannaSu && fase == FASE_PRONTO)
                {
                    ViaRoba();
                    robaOra = -1;
                    ScenaGiu(p);
                    fase = FASE_FERMO;
                }
            }
            return;
        }
        if (!ruotaAperta)
        {
            ruotaAperta = true;
            ruotaSpicchio = -1;
            // il menu del trainer, se e' aperto, si chiude: glielo si
            // dice con un file, che lui legge e cancella
            try { File.WriteAllText(Path.Combine(MY_DIR, "chiudi.txt"), "1"); }
            catch { }
            int k;
            for (k = 0; k < RUOTA_N; k++) ruotaPos[k] = PosMontata(k);
            Suono("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
        }
        // CON LA RUOTA APERTA IL GIOCO NON RICEVE NIENTE: tutti i
        // comandi spenti, e si riaccendono solo la levetta destra e la
        // croce, che sono della ruota. Niente radio, niente telefono,
        // niente cambio arma, niente telecamera.
        Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
        Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, 37, true);   // LB: la ruota stessa
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 1, true);
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 2, true);
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 174, true);
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 175, true);
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 37, true);
        float sx = Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 1);
        float sy = Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 2);
        if (Math.Sqrt(sx * sx + sy * sy) > LeggiF("ruota_soglia", 0.5f))
        {
            float ang = (float)(Math.Atan2(sx, -sy) * 180.0 / Math.PI);
            if (ang < 0f) ang += 360f;
            float passo = 360f / RUOTA_N;
            int sp = ((int)((ang + passo * 0.5f) / passo)) % RUOTA_N;
            if (sp != ruotaSpicchio)
            {
                ruotaSpicchio = sp;
                Suono("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            }
        }
        if (ruotaSpicchio >= 0)
        {
            bool croceSx = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 174);
            bool croceDx = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 175);
            if (croceSx || croceDx)
            {
                int n = VociRuota(ruotaSpicchio).Count;
                if (n > 1)
                {
                    ruotaPos[ruotaSpicchio] = (ruotaPos[ruotaSpicchio] + (croceDx ? 1 : n - 1)) % n;
                    Suono("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                }
            }
        }
        DisegnaRuota();
    }

    // LB LASCIATO: SI APPLICA TUTTO QUELLO CHE HAI SCELTO, in ogni
    // spicchio, non solo quello sotto il cursore. Prima si leggono tutte
    // le scelte e poi si applicano - applicandone una cambiano le liste
    // delle altre - e prima gli smonta, poi i monta.
    void MontaDallaRuota()
    {
        // ZAINO: si apre l'equipaggiamento nel menu. Glielo si dice al
        // trainer con un file, come per chiudere.
        // "file|voce": la pagina, e la voce su cui mettere il cursore
        string pagina = "";
        if (ruotaSpicchio >= 0)
        {
            string cs = RUOTA_CAT[ruotaSpicchio];
            if (cs == "zaino") pagina = "casa_voci.txt";
            else if (cs == "nassa") pagina = "casa_voci.txt|" + PESCATO;
            else if (cs == "pesci") pagina = FileLuogo(LuogoQui());
        }
        if (pagina.Length > 0)
        {
            try { File.WriteAllText(Path.Combine(MY_DIR, "apri.txt"), pagina); }
            catch { }
        }
        List<VoceRuota> scelte = new List<VoceRuota>();
        int k;
        for (k = 0; k < RUOTA_N; k++)
        {
            List<VoceRuota> v = VociRuota(k);
            int pos = ruotaPos[k];
            if (v.Count == 0 || pos >= v.Count) continue;
            if (v[pos].Montata) continue;
            scelte.Add(v[pos]);
        }
        int giro;
        for (giro = 0; giro < 2; giro++)
        {
            for (k = 0; k < scelte.Count; k++)
            {
                bool smonta = scelte[k].Id < 0;
                if (smonta == (giro == 0)) ApplicaVoce(scelte[k]);
            }
        }
    }

    void ApplicaVoce(VoceRuota r)
    {
        string nomeSp = RUOTA_NOME[r.Sp];
        if (r.Id < 0)
        {
            // VUOTO: quello che c'era torna in borsa
            bool via = false;
            if (r.Cat == "esca") { escaMontata = -1; via = true; }
            else if (r.Cat == "lenza") { DisarmaLenza(); via = true; }
            else if (r.Cat == "terminale" || r.Cat == "leader" || r.Cat == "piombo")
            { if (Armato(r.Cat) >= 0) via = Arma("terminale", Armato(r.Cat)); }
            else if (InUso(r.Cat) >= 0) via = Arma(r.Cat, InUso(r.Cat));
            if (via)
            {
                Messaggio(nomeSp + ": smontato");
                Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                SalvaStato();
            }
            return;
        }
        // SI SCACCIANO DA SOLI: il cucchiaino manda via galleggiante e
        // amo, e loro mandano via il cucchiaino. Arma() da solo si
        // rifiuterebbe e direbbe "prima smonta": qui lo smonta lei.
        if (r.Cat == "artificiale")
        {
            if (InUso("galleggiante") >= 0) Arma("galleggiante", InUso("galleggiante"));
            if (Armato("terminale") >= 0) Arma("terminale", Armato("terminale"));
        }
        if (r.Cat == "galleggiante" || (r.Cat == "terminale"
            && CasellaTerm(SottoTerm(r.Id)) == "terminale"))
        {
            if (InUso("artificiale") >= 0) Arma("artificiale", InUso("artificiale"));
        }
        bool ok;
        if (r.Cat == "esca") { escaMontata = r.Id; ok = true; }
        else if (r.Bob >= 0) ok = ArmaLenzaBobina(r.Bob);
        else ok = Arma(r.Cat, r.Id);
        if (ok)
        {
            Messaggio(nomeSp + ": " + r.Nome);
            Suono("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            SalvaStato();
        }
    }

    // L'ICONA DELLO ZAINO E' QUELLO CHE PORTI DAVVERO: lo zaino di
    // partenza finche' non compri niente, poi la cassetta o la borsa
    // piu' capiente che hai dietro.
    string ImgZaino()
    {
        string img = "img/cassette/Base.png";
        int meglio = -1;
        foreach (KeyValuePair<string, int> kv in borsa)
        {
            string[] c = kv.Key.Split(':');
            if (c.Length < 2 || c[0] != "cassetta" || kv.Value <= 0) continue;
            int id = Numero(c[1]);
            int i;
            for (i = 0; i < cassette.Count; i++)
            {
                if (cassette[i].Id != id) continue;
                int cap = Numero(cassette[i].Attrezzi);
                string nome, im; int pr, lv;
                if (cap > meglio && Articolo("cassetta", id, out nome, out im, out pr, out lv))
                {
                    meglio = cap;
                    img = im;
                }
            }
        }
        return img;
    }

    void DisegnaRuota()
    {
        float cx = LeggiF("ruota_x", 640f);
        float cy = LeggiF("ruota_y", 360f);
        float R = LeggiF("ruota_raggio", 170f);
        float verso = LeggiF("ruota_verso", 1f);
        float ic = LeggiF("ruota_icona", 64f);
        // nel PNG il raggio esterno e' 120 su una meta' lato di 128
        float S = R * 2f * (128f / 120f);
        float passo = 360f / RUOTA_N;
        int i;
        for (i = 0; i < RUOTA_N; i++)
        {
            string img = (i == ruotaSpicchio) ? "img/ruota/spicchio_sel.png"
                                              : "img/ruota/spicchio.png";
            SpriteInclinata(img, cx - S * 0.5f, cy - S * 0.5f, S, S, i * passo * verso);
        }
        for (i = 0; i < RUOTA_N; i++)
        {
            double a = i * passo * Math.PI / 180.0;
            float rr = R * 0.78f;
            float px = cx + rr * (float)Math.Sin(a);
            float py = cy - rr * (float)Math.Cos(a);
            if (RUOTA_CAT[i] == "zaino")
            {
                float icz = LeggiF("ruota_icona_zaino", ic);
                Sprite(ImgZaino(), px - icz * 0.5f, py - icz * 0.5f, icz, icz);
                continue;
            }
            if (RUOTA_CAT[i] == "nassa")
            {
                // la nassa che porti, con la sua immagine del negozio
                int idn; string imn, nmn;
                if (!Montato("nassa", out idn, out imn, out nmn)) imn = "";
                float icn = LeggiF("ruota_icona_nassa", ic);
                Sprite(imn, px - icn * 0.5f, py - icn * 0.5f, icn, icn);
                continue;
            }
            if (RUOTA_CAT[i] == "pesci")
            {
                float icp = LeggiF("ruota_icona_pesci", ic);
                Sprite("img/ruota/pesci.png", px - icp * 0.5f, py - icp * 0.5f, icp, icp);
                continue;
            }
            List<VoceRuota> v = VociRuota(i);
            if (v.Count == 0) continue;
            int pos = ruotaPos[i];
            if (pos >= v.Count) pos = 0;
            // ruota_icona_<categoria> in config, se c'e', vince sulla misura generale
            float ici = LeggiF("ruota_icona_" + RUOTA_CAT[i], ic);
            Sprite(v[pos].Img, px - ici * 0.5f, py - ici * 0.5f, ici, ici);
        }
        // in mezzo niente sfondo: solo le scritte
        if (ruotaSpicchio < 0 || RUOTA_CAT[ruotaSpicchio].Length == 0) return;
        DisegnaTesto(RUOTA_NOME[ruotaSpicchio].ToUpper(), cx, cy - 28f, 0.24f, 200, 200, 200);
        if (RUOTA_CAT[ruotaSpicchio] == "zaino")
        {
            DisegnaTesto("Apri l'equipaggiamento", cx, cy - 8f, 0.26f, 255, 255, 255);
            return;
        }
        if (RUOTA_CAT[ruotaSpicchio] == "nassa")
        {
            DisegnaTesto("Il pescato del giorno", cx, cy - 8f, 0.26f, 255, 255, 255);
            return;
        }
        if (RUOTA_CAT[ruotaSpicchio] == "pesci")
        {
            DisegnaTesto("I pesci di questo posto", cx, cy - 8f, 0.26f, 255, 255, 255);
            return;
        }
        List<VoceRuota> vs = VociRuota(ruotaSpicchio);
        if (vs.Count == 0)
        {
            DisegnaTesto("Niente in borsa", cx, cy - 8f, 0.26f, 255, 255, 255);
            return;
        }
        int ps = ruotaPos[ruotaSpicchio];
        if (ps >= vs.Count) ps = 0;
        VoceRuota r = vs[ps];
        string nome = r.Nome;
        if (vs.Count > 1) nome += "  " + (ps + 1) + "/" + vs.Count;
        DisegnaTesto(nome, cx, cy - 8f, 0.28f, 255, 255, 255);
        // sotto solo i dati: "montato" non serve, e' la ruota della montatura
        string sotto = r.Dett;
        if (sotto.Length > 0) DisegnaTesto(sotto, cx, cy + 12f, 0.22f, 200, 200, 200);
    }

    // IL PELO DELL'ACQUA DOVE STA L'ESCA.
    // GET_WATER_HEIGHT sui laghetti piccoli e sui fiumi spesso non risponde:
    // e' lo stesso buco che avevamo trovato cercando la riva. Allora si
    // prova in tre modi, dal piu' preciso al piu' rozzo, e solo se falliscono
    // tutti si ripiega sul terreno.
    float acquaZmem = 0f;
    bool acquaZval = false;

    // L'ESCA E' FINITA IN ACQUA O SUL PRATO?
    // AcquaSottoEsca() si ricorda l'ultima quota buona, perche' vicino a
    // riva la sonda ogni tanto non risponde e il filo spariva dentro la
    // sponda. Quella memoria pero' nasconde proprio il caso che qui ci
    // serve: il lancio finito sull'erba. Questa invece non ricorda
    // niente - chiede e basta - e controlla anche che l'acqua stia sopra
    // il terreno, se no una pozza sotto una collina varrebbe come lago.
    bool EscaSullAcqua()
    {
        try
        {
            float z = 0f;
            bool trovata = false;
            OutputArgument oz = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_WATER_HEIGHT, escaX, escaY, 400f, oz))
            { z = oz.GetResult<float>(); trovata = true; }
            else if (Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES,
                                         escaX, escaY, 400f, oz))
            { z = oz.GetResult<float>(); trovata = true; }
            else
            {
                OutputArgument oh = new OutputArgument();
                if (Function.Call<bool>(Hash.TEST_VERTICAL_PROBE_AGAINST_ALL_WATER,
                                        escaX, escaY, 400f, 0, oh))
                { z = oh.GetResult<float>(); trovata = true; }
            }
            if (!trovata) return false;
            return AcquaSopraIlFondo(escaX, escaY, z);
        }
        catch { }
        return false;
    }

    float AcquaSottoEsca()
    {
        try
        {
            OutputArgument oz = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_WATER_HEIGHT, escaX, escaY, 400f, oz))
            {
                float z = oz.GetResult<float>();
                acquaZmem = z; acquaZval = true;
                return z;
            }
            if (Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES, escaX, escaY, 400f, oz))
            {
                float z = oz.GetResult<float>();
                acquaZmem = z; acquaZval = true;
                return z;
            }
            // la sonda verticale: parte da cento metri sopra e scende
            OutputArgument oh = new OutputArgument();
            if (Function.Call<bool>(Hash.TEST_VERTICAL_PROBE_AGAINST_ALL_WATER,
                                    escaX, escaY, 400f, 0, oh))
            {
                float z = oh.GetResult<float>();
                // quella nativa quando non c'e' acqua restituisce il suolo:
                // si accetta solo se sta sotto i piedi di chi pesca
                Ped pp = Game.Player.Character;
                float mio = (pp != null && pp.Exists()) ? pp.Position.Z : z;
                if (z < mio + 0.5f) { acquaZmem = z; acquaZval = true; return z; }
            }
        }
        catch { }
        // l'ultima acqua trovata da queste parti: meglio di niente.
        // Vicino a riva la sonda smette di rispondere, e senza questa
        // memoria il filo spariva dentro la sponda a sette-otto metri.
        if (acquaZval) return acquaZmem;
        Ped p = Game.Player.Character;
        return (p != null && p.Exists()) ? p.Position.Z - 1f : 0f;
    }

    // ============================================================
    //  IL PESCE CHE ABBOCCA SI VEDE.
    //  Quando abbocca si tira su un pesce vero del gioco e lo si mette
    //  all'amo: nuota a destra e a sinistra tirandosi dietro la lenza,
    //  e quando lo porti a riva resta appeso al filo sotto la punta
    //  della canna. Non e' un'animazione: e' un'entita' che spostiamo
    //  noi a ogni fotogramma, quindi fa esattamente quello che fa la
    //  lenza.
    // ============================================================
    Ped pescePed = null;
    bool pesceAppeso = false;
    float pesceAppesoX = 0f, pesceAppesoY = 0f, pesceAppesoZ = 0f;
    float pesceBoccaX = 0f, pesceBoccaY = 0f, pesceBoccaZ = 0f;
    float pesceSbanda = 0f;      // di quanto sta sbandando adesso
    int pesceCambio = 0;         // quando decide di cambiare direzione
    float pesceVerso = 1f;

    // I PESCI RIMASTI IN GIRO.
    // Se lo script muore o lo ricarichi mentre un pesce e' fuori, quello
    // resta li' e nessuno se lo ricorda piu'. All'avvio si guardano i
    // ped qui intorno: quelli dei nostri modelli, fermi e senza gravita',
    // sono roba nostra rimasta orfana e si buttano. I pesci veri del
    // gioco nuotano e non sono congelati, quindi non li tocca.
    void PuliziaPesciOrfani()
    {
        try
        {
            Ped p = Game.Player.Character;
            if (p == null || !p.Exists()) return;
            Ped[] vicini = World.GetNearbyPeds(p, 120f);
            int i;
            for (i = 0; i < vicini.Length; i++)
            {
                Ped q = vicini[i];
                if (q == null || !q.Exists()) continue;
                int mh = q.Model.Hash;
                if (mh != Function.Call<int>(Hash.GET_HASH_KEY, "a_c_fish")
                    && mh != Function.Call<int>(Hash.GET_HASH_KEY, "a_c_sharktiger")
                    && mh != Function.Call<int>(Hash.GET_HASH_KEY, "a_c_sharkhammer")
                    && mh != Function.Call<int>(Hash.GET_HASH_KEY, "a_c_stingray"))
                    continue;
                // i nostri orfani stanno per aria o per terra: un pesce
                // vero del gioco e' sempre in acqua
                if (Function.Call<bool>(Hash.IS_ENTITY_IN_WATER, q)) continue;
                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, q, true, true);
                Function.Call(Hash.FREEZE_ENTITY_POSITION, q, false);
                q.Delete();
            }
        }
        catch { }
    }

    void MettiPesce()
    {
        TogliPesce();
        try
        {
            string nomeM = (pesceQui >= 0 && pesceQui < pesci.Count)
                           ? ModelloDi(pesci[pesceQui].Nome) : "a_c_fish";
            Model m = new Model(nomeM);
            m.Request(600);
            // se quel modello non c'e', si ripiega sul pesce normale
            if (!m.IsLoaded)
            {
                m = new Model("a_c_fish");
                m.Request(600);
                if (!m.IsLoaded) return;
            }
            float z = AcquaSottoEsca();
            pescePed = World.CreatePed(m, new GTA.Math.Vector3(escaX, escaY, z - 0.3f));
            m.MarkAsNoLongerNeeded();
            if (pescePed == null || !pescePed.Exists()) { pescePed = null; return; }
            Function.Call(Hash.SET_ENTITY_INVINCIBLE, pescePed, true);
            Function.Call(Hash.SET_ENTITY_COLLISION, pescePed, false, false);
            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, pescePed, true);
            Function.Call(Hash.SET_PED_CAN_RAGDOLL, pescePed, false);
            Function.Call(Hash.SET_ENTITY_CAN_BE_DAMAGED, pescePed, false);
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, pescePed, false);
            Function.Call(Hash.FREEZE_ENTITY_POSITION, pescePed, true);
            // SENZA QUESTO NON SI CANCELLA.
            // Un ped creato dallo script GTA lo tratta come roba
            // dell'ambiente: la cancellazione la ignora e il pesce ti
            // resta piantato per terra. Dichiarandolo entita' di
            // missione diventa nostro, e se ne va quando glielo diciamo.
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, pescePed, true, true);
            // LA SAGOMA GIUSTA: quella della specie che ha abboccato,
            // non quella che sceglie il gioco a caso
            if (pesceQui >= 0 && pesceQui < pesci.Count)
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, pescePed, 0,
                              FormaDi(pesci[pesceQui].Nome), 0, 0);
            pesceAppeso = false;
            pesceSbanda = 0f;
            pesceVerso = 1f;
            pesceCambio = 0;
        }
        catch { pescePed = null; }
    }

    // LA TAGLIA DEL PESCE.
    // GTA non sa ridimensionare un ped: la funzione non esiste (in RDR2
    // si', nella cinque no). L'unico modo e' riscrivere a mano la
    // MATRICE dell'entita' - le sue tre direzioni piu' la posizione - e
    // moltiplicare le direzioni per un fattore. Di solito e' una
    // porcheria, perche' fisica e animazioni te la resettano; qui no,
    // perche' il pesce lo pilotiamo noi a ogni fotogramma e la riscriviamo
    // subito dopo.
    // Il fattore viene dal peso vero: un persico da due etti resta
    // piccolo, un siluro da ventisei chili viene grosso. Si usa la
    // radice cubica perche' il peso va col volume, non con la lunghezza.
    // LA TAGLIA PER ORA NON SI PUO' FARE.
    // Ridimensionare un ped in GTA si potrebbe solo riscrivendo la sua
    // matrice, e questa versione di ScriptHookVDotNet quella funzione
    // non ce l'ha: chiamarla a mano col numero, senza esserne sicuri,
    // vuol dire rischiare di far crashare il gioco. Quindi il pesce
    // resta della sua misura, e la taglia si vede dal modello.
    // ============================================================
    //  IL PESCE DI PASSAGGIO
    // ============================================================
    // Ogni tanto, mentre aspetti, un pesce attraversa il campo vicino
    // all'esca e se ne va. Non abbocca e non c'entra niente con la
    // pescata: serve a far vedere che sotto c'e' vita. E' una specie di
    // quelle che vivono davvero in quell'acqua, presa da pesci_aree.
    //   pesci_scena       1 acceso, 0 spento
    //   pesci_scena_ogni  ogni quanti secondi ci prova
    //   pesci_scena_dura  quanti secondi ci mette ad attraversare
    //   pesci_scena_via   a quanti metri dall'esca passa
    // IL GRUPPETTO CHE PASSA: da 1 a pesci_scena_gruppo pesci insieme,
    // ognuno con la sua sagoma, un po' di lato e un po' in ritardo
    // sugli altri, cosi' non nuotano in fila.
    const int SCENA_MAX = 3;
    Ped[] pesceScena = new Ped[SCENA_MAX];
    float[] scenaLato = new float[SCENA_MAX];
    float[] scenaDirQ = new float[SCENA_MAX];   // ognuno la sua rotta
    int[] scenaRit = new int[SCENA_MAX];
    int scenaN = 0;
    int scenaProssimo = 0, scenaDa = 0;
    float scenaX, scenaY, scenaDir, scenaLung;

    void ViaPesceScena()
    {
        int i;
        for (i = 0; i < SCENA_MAX; i++)
        {
            try
            {
                if (pesceScena[i] != null && pesceScena[i].Exists())
                {
                    Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, pesceScena[i], true, true);
                    Function.Call(Hash.FREEZE_ENTITY_POSITION, pesceScena[i], false);
                    pesceScena[i].Delete();
                }
            }
            catch { }
            pesceScena[i] = null;
        }
        scenaN = 0;
        scenaDa = 0;
    }

    bool inRivaOra = false;
    float scenaAcquaZ = 0f;   // il pelo dell'acqua dove passa

    // l'acqua in un punto qualsiasi: il pelo, o -9999 se li' non ce n'e'
    float AcquaA(float x, float y, float zRif)
    {
        try
        {
            OutputArgument oz = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_WATER_HEIGHT, x, y, 400f, oz))
                return oz.GetResult<float>();
            if (Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES, x, y, 400f, oz))
                return oz.GetResult<float>();
            OutputArgument oh = new OutputArgument();
            if (Function.Call<bool>(Hash.TEST_VERTICAL_PROBE_AGAINST_ALL_WATER, x, y, 400f, 0, oh))
            {
                float z = oh.GetResult<float>();
                if (z < zRif + 0.5f) return z;
            }
        }
        catch { }
        return -9999f;
    }

    void PesceDiPassaggio(int now)
    {
        if (LeggiF("pesci_scena", 1f) < 0.5f) { ViaPesceScena(); return; }

        int dura = (int)(LeggiF("pesci_scena_dura", 7f) * 1000f);

        // quelli che stanno passando adesso: li si porta avanti
        if (scenaN > 0)
        {
            bool finiti = true;
            int q;
            for (q = 0; q < scenaN; q++)
            {
                if (pesceScena[q] == null || !pesceScena[q].Exists()) continue;
                float t = (float)(now - scenaDa - scenaRit[q]) / (float)dura;
                if (t < 1f) finiti = false;
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
                // ognuno sulla sua rotta: parte lontano da un lato, passa
                // vicino all'esca e se ne va lontano dall'altro
                double rad = scenaDirQ[q] * Math.PI / 180.0;
                float dx = -(float)Math.Sin(rad), dy = (float)Math.Cos(rad);
                float avanti = (t - 0.5f) * scenaLung;
                float px = scenaX + dx * avanti - dy * scenaLato[q];
                float py = scenaY + dy * avanti + dx * scenaLato[q];
                float pz = scenaAcquaZ - LeggiF("pesci_scena_giu", 0.45f)
                         + (float)Math.Sin(now * 0.003 + q) * 0.06f;
                float coda = (float)Math.Sin(now * 0.012 + q * 2) * 8f;
                try
                {
                    Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, pesceScena[q],
                                  px, py, pz, false, false, false);
                    Function.Call(Hash.SET_ENTITY_ROTATION, pesceScena[q],
                                  0f, 0f, scenaDirQ[q] + coda, 2, true);
                }
                catch { }
            }
            if (finiti) ViaPesceScena();
            return;
        }

        // se non ce n'e' uno, ogni tanto se ne manda un altro
        if (now < scenaProssimo) return;
        int ogni = (int)(LeggiF("pesci_scena_ogni", 22f) * 1000f);
        scenaProssimo = now + ogni + caso.Next(ogni);

        // una specie di questa acqua, se no niente
        int lu = LuogoQui();
        List<int> qui = new List<int>();
        int i;
        for (i = 0; i < pesci.Count; i++)
            if (lu < 0 || PesceQui(pesci[i], lu)) qui.Add(i);
        if (qui.Count == 0) return;
        // NON SEMPRE LA STESSA SAGOMA.
        // A pescare a caso fra le specie del posto usciva quasi sempre la
        // stessa forma, perche' in un laghetto i panfish sono la meta'.
        // Allora prima si tira a sorte la FORMA fra quelle che ci sono
        // qui, e poi una specie con quella forma: cosi' si alternano.
        List<int> forme = new List<int>();
        for (i = 0; i < qui.Count; i++)
        {
            int fq = FormaDi(pesci[qui[i]].Nome);
            if (!forme.Contains(fq)) forme.Add(fq);
        }
        // quanti passano stavolta, e per ognuno una forma tirata a sorte
        int quanti = 1 + caso.Next((int)LeggiF("pesci_scena_gruppo", 3f));
        if (quanti < 1) quanti = 1;
        if (quanti > SCENA_MAX) quanti = SCENA_MAX;
        int[] scelti = new int[SCENA_MAX];
        int q2;
        for (q2 = 0; q2 < quanti; q2++)
        {
            int formaScelta = forme[caso.Next(forme.Count)];
            List<int> conForma = new List<int>();
            for (i = 0; i < qui.Count; i++)
                if (FormaDi(pesci[qui[i]].Nome) == formaScelta) conForma.Add(qui[i]);
            scelti[q2] = conForma[caso.Next(conForma.Count)];
        }
        int sc = scelti[0];

        try
        {
            // DOVE PASSA: attorno all'esca se e' in acqua; se no in un
            // punto d'acqua davanti a te, cercato girando attorno
            float ax, ay, az, baseDir;
            Ped pp = Game.Player.Character;
            if (escaInAcqua)
            {
                ax = escaX; ay = escaY; az = AcquaSottoEsca(); baseDir = escaDir;
            }
            else
            {
                float dist = LeggiF("pesci_scena_dist", 8f);
                ax = 0f; ay = 0f; az = -9999f; baseDir = 0f;
                int k;
                for (k = 0; k < 8 && az < -9000f; k++)
                {
                    float ang = pp.Heading + k * 45f;
                    double ra = ang * Math.PI / 180.0;
                    float qx = pp.Position.X - (float)Math.Sin(ra) * dist;
                    float qy = pp.Position.Y + (float)Math.Cos(ra) * dist;
                    float qz = AcquaA(qx, qy, pp.Position.Z);
                    if (qz > -9000f) { ax = qx; ay = qy; az = qz; baseDir = ang; }
                }
                if (az < -9000f) return;
            }
            scenaAcquaZ = az;
            // passa di fianco, non addosso
            float via = LeggiF("pesci_scena_via", 2.2f);
            scenaLung = LeggiF("pesci_scena_lungo", 14f);
            scenaDir = baseDir + 90f + caso.Next(60) - 30f;
            if (caso.Next(2) == 0) scenaDir += 180f;
            double rl = (baseDir + (caso.Next(2) == 0 ? 90f : -90f)) * Math.PI / 180.0;
            scenaX = ax + -(float)Math.Sin(rl) * via;
            scenaY = ay + (float)Math.Cos(rl) * via;
            float z0 = az - LeggiF("pesci_scena_giu", 0.45f);
            scenaN = 0;
            for (q2 = 0; q2 < quanti; q2++)
            {
                sc = scelti[q2];
                Model m = new Model(ModelloDi(pesci[sc].Nome));
                m.Request(400);
                if (!m.IsLoaded)
                {
                    m = new Model("a_c_fish");
                    m.Request(400);
                    if (!m.IsLoaded) continue;
                }
                Ped pz2 = World.CreatePed(m, new GTA.Math.Vector3(scenaX, scenaY, z0));
                m.MarkAsNoLongerNeeded();
                if (pz2 == null || !pz2.Exists()) continue;
                Function.Call(Hash.SET_ENTITY_INVINCIBLE, pz2, true);
                Function.Call(Hash.SET_ENTITY_COLLISION, pz2, false, false);
                Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, pz2, true);
                Function.Call(Hash.SET_PED_CAN_RAGDOLL, pz2, false);
                Function.Call(Hash.SET_ENTITY_CAN_BE_DAMAGED, pz2, false);
                Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, pz2, false);
                Function.Call(Hash.FREEZE_ENTITY_POSITION, pz2, true);
                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, pz2, true, true);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, pz2, 0,
                              FormaDi(pesci[sc].Nome), 0, 0);
                pesceScena[scenaN] = pz2;
                // il primo in mezzo, gli altri di lato e un po' indietro
                scenaLato[scenaN] = (scenaN == 0) ? 0f
                                  : ((scenaN == 1) ? 1f : -1f) * LeggiF("pesci_scena_lato", 0.7f);
                scenaRit[scenaN] = (scenaN == 0) ? 0 : (int)(dura * (0.08f + 0.10f * scenaN));
                // il primo tiene la rotta del gruppo, gli altri sbandano
                // di qualche grado, ognuno per conto suo
                float sband = LeggiF("pesci_scena_sbanda", 25f);
                scenaDirQ[scenaN] = (scenaN == 0) ? scenaDir
                                  : scenaDir + (caso.Next(2) == 0 ? 1f : -1f) * (8f + caso.Next((int)sband));
                scenaN++;
            }
            if (scenaN == 0) return;
            scenaDa = now;
        }
        catch { ViaPesceScena(); }
    }

    // ============================================================
    //  IL CAMPO
    // ============================================================
    // Comprata la licenza, dietro a te compare il tuo posto: la cassetta
    // aperta, lo zaino, le canne di riserva piantate nel terreno, il
    // secchio, la sedia. Sono prop di GTA (campo.txt), messi a terra
    // rispetto a dove guardi in quel momento; restano li' finche' la
    // licenza dura, anche se ti allontani. campo=0 in config li toglie.
    class PezzoCampo
    {
        public string Nome, Modello;
        public float Dietro, Lato, Rz, Rx, Giu;
    }
    List<PezzoCampo> campoPezzi = new List<PezzoCampo>();
    List<Prop> campoProps = new List<Prop>();
    float campoX = 0f, campoY = 0f, campoZ = 0f, campoDir = 0f;
    bool campoMesso = false;

    void CaricaCampo()
    {
        campoPezzi.Clear();
        string[] rows = LeggiRighe("campo.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 4) continue;
            PezzoCampo x = new PezzoCampo();
            x.Nome = c[0].Trim();
            x.Modello = c[1].Trim();
            x.Dietro = LeggiNum(c[2]);
            x.Lato = LeggiNum(c[3]);
            x.Rz = (c.Length > 4) ? LeggiNum(c[4]) : 0f;
            x.Rx = (c.Length > 5) ? LeggiNum(c[5]) : 0f;
            x.Giu = (c.Length > 6) ? LeggiNum(c[6]) : 0f;
            campoPezzi.Add(x);
        }
    }

    static float LeggiNum(string t)
    {
        float v;
        if (float.TryParse(t.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
        return 0f;
    }

    // dove stai adesso diventa il posto del campo
    void SegnaCampo(Ped p)
    {
        campoX = p.Position.X; campoY = p.Position.Y; campoZ = p.Position.Z;
        campoDir = p.Heading;
        campoMesso = true;
    }

    void MettiCampo()
    {
        ViaCampo();
        if (!campoMesso || !inPesca) return;
        if (LeggiF("campo", 1f) < 0.5f) return;
        if (campoPezzi.Count == 0) CaricaCampo();
        double rad = campoDir * Math.PI / 180.0;
        // "avanti" e' dove guardavi: dietro e' il contrario, lato e' la destra
        float fx = -(float)Math.Sin(rad), fy = (float)Math.Cos(rad);
        float rx = fy, ry = -fx;
        int i;
        for (i = 0; i < campoPezzi.Count; i++)
        {
            PezzoCampo x = campoPezzi[i];
            try
            {
                Model m = new Model(x.Modello);
                if (!m.IsValid || !m.IsInCdImage) continue;
                m.Request(500);
                if (!m.IsLoaded) continue;
                float px = campoX - fx * x.Dietro + rx * x.Lato;
                float py = campoY - fy * x.Dietro + ry * x.Lato;
                float pz = campoZ;
                OutputArgument oz = new OutputArgument();
                if (Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, px, py, campoZ + 2f, oz, false, false))
                    pz = oz.GetResult<float>();
                Prop pr = World.CreateProp(m, new GTA.Math.Vector3(px, py, pz + x.Giu), false, false);
                m.MarkAsNoLongerNeeded();
                if (pr == null || !pr.Exists()) continue;
                Function.Call(Hash.SET_ENTITY_ROTATION, pr, x.Rx, 0f, campoDir + x.Rz, 2, true);
                Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, pr, false);
                Function.Call(Hash.FREEZE_ENTITY_POSITION, pr, true);
                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, pr, true, true);
                campoProps.Add(pr);
            }
            catch { }
        }
    }

    void ViaCampo()
    {
        int i;
        for (i = 0; i < campoProps.Count; i++)
        {
            try
            {
                if (campoProps[i] != null && campoProps[i].Exists())
                {
                    Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, campoProps[i], true, true);
                    Function.Call(Hash.FREEZE_ENTITY_POSITION, campoProps[i], false);
                    campoProps[i].Delete();
                }
            }
            catch { }
        }
        campoProps.Clear();
    }

    // ============================================================
    //  LA ROBACCIA
    // ============================================================
    // Siamo pur sempre a Los Santos: ogni tanto invece del pesce viene
    // su una scarpa, un cono, un sacchetto. Vale due lire, non da' punti
    // e nella nassa non ci va. Le probabilita' salgono se peschi senza
    // esca. La tabella e' in robaccia.txt.
    class Roba
    {
        public string Nome, Modello;
        public int Dollari;
        public float Kg;
    }
    List<Roba> robaccia = new List<Roba>();
    int robaOra = -1;
    int robaAppesaFino = 0;
    Prop robaProp = null;

    void CaricaRobaccia()
    {
        robaccia.Clear();
        string[] rows = LeggiRighe("robaccia.txt");
        int i;
        for (i = 0; i < rows.Length; i++)
        {
            string r = rows[i].Trim();
            if (r.Length == 0 || r[0] == '#') continue;
            string[] c = r.Split('|');
            if (c.Length < 4) continue;
            Roba x = new Roba();
            x.Nome = c[0].Trim();
            x.Modello = c[1].Trim();
            x.Dollari = Numero(c[2]);
            float k;
            float.TryParse(c[3].Trim(), NumberStyles.Float,
                           CultureInfo.InvariantCulture, out k);
            x.Kg = k;
            robaccia.Add(x);
        }
    }

    // QUALCOSA HA PRESO ALL'AMO, MA NON E' UN PESCE. La roba si aggancia
    // sott'acqua dove sta l'esca e segue la lenza mentre recuperi; niente
    // abboccate finche' non l'hai tirata su.
    void ArrivaRobaccia(int now)
    {
        if (robaccia.Count == 0)
        {
            quandoAbbocca = now + 6000 + caso.Next(8000);
            return;
        }
        robaOra = caso.Next(robaccia.Count);
        MettiRoba();
        quandoAbbocca = now + 3600000;
        Vibra(200, 120);
        Messaggio("~y~Qualcosa ha preso: recupera.");
    }

    // lenza ritirata con la roba attaccata: penzola dalla canna un momento
    void RobacciaSu(int now)
    {
        if (robaOra < 0 || robaOra >= robaccia.Count) return;
        Roba x = robaccia[robaOra];
        robaAppesaFino = now + (int)LeggiF("roba_appesa_ms", 2500f);
        string t = "~y~Hai tirato su: " + x.Nome;
        if (x.Dollari > 0)
        {
            t += "  ~g~$" + x.Dollari;
            Paga(-x.Dollari);
        }
        Messaggio(t);
    }

    void MettiRoba()
    {
        ViaRoba();
        if (robaOra < 0 || robaOra >= robaccia.Count) return;
        try
        {
            Model m = new Model(robaccia[robaOra].Modello);
            m.Request(500);
            if (!m.IsLoaded) return;          // modello che non c'e': niente da vedere
            float z = AcquaSottoEsca();
            robaProp = World.CreateProp(m, new GTA.Math.Vector3(escaX, escaY, z - 0.2f),
                                        false, false);
            m.MarkAsNoLongerNeeded();
            if (robaProp == null || !robaProp.Exists()) { robaProp = null; return; }
            Function.Call(Hash.SET_ENTITY_COLLISION, robaProp, false, false);
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, robaProp, false);
            Function.Call(Hash.FREEZE_ENTITY_POSITION, robaProp, true);
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, robaProp, true, true);
        }
        catch { ViaRoba(); }
    }

    void ViaRoba()
    {
        try
        {
            if (robaProp != null && robaProp.Exists())
            {
                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, robaProp, true, true);
                Function.Call(Hash.FREEZE_ENTITY_POSITION, robaProp, false);
                robaProp.Delete();
            }
        }
        catch { }
        robaProp = null;
    }

    // mentre la tiri, la roba segue l'esca; alla fine penzola dalla canna
    void MuoviRoba(int now, bool appesa)
    {
        if (robaProp == null || !robaProp.Exists()) return;
        try
        {
            float gradi = (float)(now * 0.06) % 360f;
            if (appesa)
            {
                GTA.Math.Vector3 pt = PuntaCanna();
                if (pt == GTA.Math.Vector3.Zero) return;
                float dond = (float)Math.Sin(now * 0.0021) * 0.09f;
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, robaProp,
                              pt.X + dond, pt.Y + dond, pt.Z - LeggiF("roba_giu", 0.5f),
                              false, false, false);
            }
            else
            {
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, robaProp,
                              escaX, escaY, AcquaSottoEsca() - 0.2f, false, false, false);
            }
            Function.Call(Hash.SET_ENTITY_ROTATION, robaProp, 0f, 0f, gradi, 2, true);
        }
        catch { }
    }

    void PesceInPosa(float px, float py, float pz, float gradi, float rollio)
    {
        if (pescePed == null || !pescePed.Exists()) return;
        try
        {
            Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, pescePed,
                          px, py, pz, false, false, false);
            Function.Call(Hash.SET_ENTITY_ROTATION, pescePed,
                          rollio, 0f, gradi, 2, true);
        }
        catch { }
    }

    void TogliPesce()
    {
        try
        {
            if (pescePed != null && pescePed.Exists())
            {
                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, pescePed, true, true);
                Function.Call(Hash.FREEZE_ENTITY_POSITION, pescePed, false);
                pescePed.Delete();
            }
        }
        catch { }
        pescePed = null;
        pesceAppeso = false;
    }

    // il pesce sta all'amo: lo si mette li' a ogni giro
    // "tira" = sta lottando, quindi sbanda e si porta dietro la lenza
    void AggiornaPesce(Ped p, int now, bool tira)
    {
        if (pescePed == null || !pescePed.Exists()) return;
        try
        {
            if (pesceAppeso)
            {
                // FUORI DALL'ACQUA, A CIONDOLONI.
                // Sotto la punta della canna, a testa in giu', e si
                // dimena: un pendolo lento piu' uno scossone corto, come
                // un pesce appena tirato su.
                GTA.Math.Vector3 pt = PuntaCanna();
                if (pt == GTA.Math.Vector3.Zero) return;
                float giu = LeggiF("pesce_appeso_giu", 0.55f);
                float t = now / 1000f;
                float amp = LeggiF("pesce_dondola", 0.10f);
                float dx = (float)Math.Sin(t * 2.1) * amp;
                float dy = (float)Math.Cos(t * 1.7) * amp;
                pesceAppesoX = pt.X + dx;
                pesceAppesoY = pt.Y + dy;
                pesceAppesoZ = pt.Z - giu;
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, pescePed,
                              pesceAppesoX, pesceAppesoY, pesceAppesoZ,
                              false, false, false);
                // L'AMO STA IN BOCCA, non su una branchia.
                // Il gioco mette il pesce col suo centro sul punto che
                // gli diamo, e il centro sta a meta' corpo: il filo
                // finiva sul dorso. Qui si chiede al gioco dov'e' finita
                // la punta del muso e si sposta il pesce di quel tanto,
                // cosi' e' la bocca a stare appesa.
                float av = LeggiF("pesce_bocca_avanti", 0.22f);
                float sp = LeggiF("pesce_bocca_lato", 0f);
                // PRIMA SI CHIEDE AL GIOCO DOV'E' LA TESTA.
                // Il pesce e' inarcato dall'animazione e i modelli non
                // sono tutti della stessa lunghezza: uno spostamento
                // fisso dal centro non puo' andare bene per tutti. Se il
                // pesce ha l'osso della testa - e ce l'ha - quello e' il
                // punto vero, curvatura compresa. I numeri del config
                // restano come ritocco, e come ripiego se l'osso non
                // c'e'.
                GTA.Math.Vector3 bocca = GTA.Math.Vector3.Zero;
                bool ossoOk = false;
                try
                {
                    int osso = Function.Call<int>(Hash.GET_PED_BONE_INDEX,
                                                  pescePed, 31086);   // SKEL_Head
                    if (osso > 0)
                    {
                        GTA.Math.Vector3 ct = Function.Call<GTA.Math.Vector3>(
                            Hash.GET_WORLD_POSITION_OF_ENTITY_BONE, pescePed, osso);
                        GTA.Math.Vector3 c0 = pescePed.Position;
                        float dx2 = ct.X - c0.X, dy2 = ct.Y - c0.Y, dz2 = ct.Z - c0.Z;
                        float d2 = dx2 * dx2 + dy2 * dy2 + dz2 * dz2;
                        // se l'osso e' li' vicino e' quello buono
                        if (d2 > 0.0001f && d2 < 4f) { bocca = ct; ossoOk = true; }
                    }
                }
                catch { }
                if (!ossoOk)
                    bocca = Function.Call<GTA.Math.Vector3>(
                        Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS,
                        pescePed, sp, av, LeggiF("pesce_bocca_su", 0f));
                else
                {
                    // dall'osso della testa alla punta del muso, in avanti
                    GTA.Math.Vector3 dif = Function.Call<GTA.Math.Vector3>(
                        Hash.GET_OFFSET_FROM_ENTITY_IN_WORLD_COORDS,
                        pescePed, sp, LeggiF("pesce_muso", 0.08f),
                        LeggiF("pesce_bocca_su", 0f));
                    GTA.Math.Vector3 c1 = pescePed.Position;
                    bocca = new GTA.Math.Vector3(bocca.X + (dif.X - c1.X),
                                                 bocca.Y + (dif.Y - c1.Y),
                                                 bocca.Z + (dif.Z - c1.Z));
                }
                GTA.Math.Vector3 orig = pescePed.Position;
                pesceBoccaX = bocca.X; pesceBoccaY = bocca.Y; pesceBoccaZ = bocca.Z;
                PesceInPosa(pesceAppesoX - (bocca.X - orig.X),
                            pesceAppesoY - (bocca.Y - orig.Y),
                            pesceAppesoZ - (bocca.Z - orig.Z),
                            (float)((t * 26.0) % 360.0),
                            LeggiF("pesce_appeso_becca", 78f));
                return;
            }

            // IN ACQUA: sbanda a destra e a sinistra, e la lenza lo segue
            if (tira)
            {
                if (now > pesceCambio)
                {
                    pesceVerso = -pesceVerso;
                    pesceCambio = now + 600 + caso.Next(1400);
                }
                float dt = Game.LastFrameTime;
                // QUANTO SI SPOSTA LO DECIDE IL PESCE, NON IL CONFIG.
                // Un pesce da mezzo chilo si dimena sul posto; uno che
                // sta al limite di quello che regge la tua attrezzatura
                // ti porta la lenza da una parte all'altra. E piu' si
                // stanca, meno raggio fa: alla fine viene su dritto.
                // I numeri del config sono quelli del pesce AL LIMITE.
                float tenutaP = TenutaBorsa();
                float quanto = (tenutaP > 0f) ? (pesceKg / tenutaP) : 0.5f;
                if (quanto > 1f) quanto = 1f;
                if (quanto < 0f) quanto = 0f;
                float minP = LeggiF("pesce_sbanda_min", 0.35f);
                float scala = minP + (1f - minP) * quanto;
                scala *= 1f - stanchezza * LeggiF("pesce_sbanda_stanco", 0.45f);
                if (scala < 0.05f) scala = 0.05f;

                float forza = LeggiF("pesce_sbanda", 14f) * scala;
                pesceSbanda += pesceVerso * forza * dt;
                float max = LeggiF("pesce_sbanda_max", 22f) * scala;
                if (pesceSbanda > max) pesceSbanda = max;
                if (pesceSbanda < -max) pesceSbanda = -max;
                // e' il pesce che si porta dietro l'esca, non la canna
                escaDir += pesceVerso * forza * dt * LeggiF("pesce_tira_lenza", 0.35f);
                AggiornaEsca(p, metriLenza);
            }
            else pesceSbanda *= 0.96f;

            float za = AcquaSottoEsca();
            float giu2 = LeggiF("pesce_sotto", 0.25f);
            // guarda verso il pescatore, storto di quanto sta sbandando
            GTA.Math.Vector3 o = p.Position;
            double ang = Math.Atan2(o.X - escaX, o.Y - escaY) * 180.0 / Math.PI;
            PesceInPosa(escaX, escaY, za - giu2,
                        (float)(-ang) + pesceSbanda, 0f);
        }
        catch { }
    }

    // IL COLORE DELLA LENZA.
    // Non e' inventato e non e' scritto da nessuna parte: si prende
    // dalla foto della bobina che hai montato. Si guarda un pixel ogni
    // otto, si buttano via il bianco dello sfondo e il quasi-nero, e
    // quello che resta e' il colore vero di quel filo - il verdolino
    // dei monofili, il grigio dei fluorocarbon, l'oliva dei trecciati.
    // Si calcola una volta sola per bobina.
    string lenzaImgVista = "";
    int lenzaR = 235, lenzaG = 240, lenzaB = 245;

    void ColoreDellaLenza()
    {
        string img = "", nome = "";
        int id;
        if (!Montato("lenza", out id, out img, out nome)) return;
        if (img == null || img.Length == 0) return;
        if (img == lenzaImgVista) return;
        lenzaImgVista = img;
        lenzaR = 235; lenzaG = 240; lenzaB = 245;
        try
        {
            string f = Path.Combine(MY_DIR, img);
            if (!File.Exists(f)) return;
            using (Bitmap bm = new Bitmap(f))
            {
                long sr = 0, sg = 0, sb = 0, q = 0;
                int x, y;
                for (y = 0; y < bm.Height; y += 8)
                    for (x = 0; x < bm.Width; x += 8)
                    {
                        Color c = bm.GetPixel(x, y);
                        if (c.A < 200) continue;
                        int mx = c.R; if (c.G > mx) mx = c.G; if (c.B > mx) mx = c.B;
                        int mn = c.R; if (c.G < mn) mn = c.G; if (c.B < mn) mn = c.B;
                        if (mx > 240 && mn > 230) continue;   // sfondo bianco
                        if (mx < 30) continue;                // ombre
                        sr += c.R; sg += c.G; sb += c.B; q++;
                    }
                if (q < 10) return;
                int r = (int)(sr / q), g = (int)(sg / q), b = (int)(sb / q);
                // un filo si vede solo se e' piu' chiaro dell'acqua:
                // si tiene la tinta e si alza la luce
                int mx2 = r; if (g > mx2) mx2 = g; if (b > mx2) mx2 = b;
                if (mx2 < 1) mx2 = 1;
                float k = 210f / (float)mx2;
                if (k > 1f)
                {
                    r = (int)(r * k); g = (int)(g * k); b = (int)(b * k);
                    if (r > 255) r = 255;
                    if (g > 255) g = 255;
                    if (b > 255) b = 255;
                }
                lenzaR = r; lenzaG = g; lenzaB = b;
            }
        }
        catch { }
    }

    // IL GALLEGGIANTE IN ACQUA.
    // Quello che c'era e' un disegno sul bordo dello schermo: dice cosa
    // sta succedendo ma non sta in acqua. Questo invece sta li' dove il
    // filo entra, ondeggia con l'onda, balla quando il pesce assaggia e
    // sparisce sotto quando abbocca.
    // Si vede solo se un galleggiante ce l'hai montato: a spinning non
    // c'e', e infatti non deve esserci.
    // Il colore lo prende dalla foto del galleggiante che hai comprato,
    // con lo stesso sistema della lenza.
    // OGNI GALLEGGIANTE E' IL SUO.
    // Non un pallino uguale per tutti: forma, misure e colori vengono
    // dalla riga di galleggianti.txt di quello che hai montato, che sono
    // dati veri del wiki. "5 1/2" x 4/5"(14 x 2 cm) Oval" vuol dire
    // quattordici centimetri di lunghezza, due di spessore, forma ovale;
    // "Natural / Red / Green" sono le fasce, dal basso verso la punta.
    int gallVisto = -1;
    float gallLung = 0.14f;      // metri
    float gallSpess = 0.02f;
    bool gallPalla = false;
    bool gallLuce = false;       // i "Glow": fosforescenti
    List<int> gallCol = new List<int>();   // r,g,b, r,g,b, ...

    static void ColoreNome(string n, out int r, out int g, out int b)
    {
        string q = n.Trim().ToLower();
        r = 235; g = 235; b = 240;
        if (q.Length == 0) return;
        if (q.IndexOf("light green") >= 0) { r = 150; g = 225; b = 130; return; }
        if (q.IndexOf("natural") >= 0)     { r = 215; g = 185; b = 135; return; }
        if (q.IndexOf("orange") >= 0)      { r = 245; g = 140; b = 40;  return; }
        if (q.IndexOf("yellow") >= 0)      { r = 250; g = 215; b = 60;  return; }
        if (q.IndexOf("green") >= 0)       { r = 70;  g = 175; b = 80;  return; }
        if (q.IndexOf("blue") >= 0)        { r = 60;  g = 120; b = 225; return; }
        if (q.IndexOf("black") >= 0)       { r = 35;  g = 35;  b = 40;  return; }
        if (q.IndexOf("white") >= 0)       { r = 245; g = 245; b = 248; return; }
        if (q.IndexOf("red") >= 0)         { r = 225; g = 55;  b = 45;  return; }
    }

    // "(14 x 2 cm)" -> lunghezza 14, spessore 2. "(10 cm)" -> palla da 10.
    void MisureGalleggiante(string mis)
    {
        gallLung = 0.14f; gallSpess = 0.02f; gallPalla = false;
        if (mis == null) return;
        int a = mis.LastIndexOf('(');
        int b = mis.IndexOf(')', a + 1);
        if (a < 0 || b < 0) return;
        string dentro = mis.Substring(a + 1, b - a - 1).Replace("cm", " ").Trim();
        string[] pz = dentro.Split('x');
        float l = NumeroFloat(pz[0].Trim());
        if (l > 0f) gallLung = l / 100f;
        if (pz.Length > 1)
        {
            float d = NumeroFloat(pz[1].Trim());
            if (d > 0f) gallSpess = d / 100f;
        }
        else
        {
            // una misura sola: e' una palla, e quella misura e' il diametro
            gallPalla = true;
            gallSpess = gallLung;
        }
    }

    void DatiGalleggiante()
    {
        string img = "", nome = "";
        int id;
        if (!Montato("galleggiante", out id, out img, out nome)) { gallVisto = -1; return; }
        if (id == gallVisto) return;
        gallVisto = id;
        gallCol.Clear();
        int i;
        for (i = 0; i < galleggianti.Count; i++)
        {
            if (galleggianti[i].Id != id) continue;
            MisureGalleggiante(galleggianti[i].Misura);
            // I FOSFORESCENTI.
            // "Glow Bobber" e "Glowing Slim Float" sono quelli da notte:
            // il colore va acceso al massimo tenendo la tinta, se no di
            // notte un giallo mezzo spento non lo vedi.
            gallLuce = (galleggianti[i].Nome.ToLower().IndexOf("glow") >= 0);
            // i colori: separati da "/" o da "-n-", dal basso alla punta
            string c = galleggianti[i].Colore.Replace("-n-", "/");
            string[] pz = c.Split('/');
            int q;
            for (q = 0; q < pz.Length; q++)
            {
                int r2, g2, b2;
                ColoreNome(pz[q], out r2, out g2, out b2);
                if (gallLuce)
                {
                    // I FOSFORESCENTI HANNO TINTE LORO.
                    // Il verde di un Glow Bobber non e' il verde di un
                    // prato: e' quel giallo-verde acido che si vede al
                    // buio. Alzare la luce del verde normale dava un
                    // verde pieno, non quello. Qui le tinte fluo sono
                    // scritte a mano, una per colore.
                    string qq = pz[q].Trim().ToLower();
                    if (qq.IndexOf("green") >= 0)
                    { r2 = 200; g2 = 255; b2 = 40; }
                    else if (qq.IndexOf("yellow") >= 0)
                    { r2 = 250; g2 = 255; b2 = 80; }
                    else if (qq.IndexOf("orange") >= 0)
                    { r2 = 255; g2 = 165; b2 = 30; }
                    else if (qq.IndexOf("red") >= 0)
                    { r2 = 255; g2 = 80; b2 = 70; }
                    else if (qq.IndexOf("blue") >= 0)
                    { r2 = 90; g2 = 200; b2 = 255; }
                    else
                    {
                        int mx = r2; if (g2 > mx) mx = g2; if (b2 > mx) mx = b2;
                        if (mx > 40)
                        {
                            float kk = 255f / (float)mx;
                            r2 = (int)(r2 * kk); g2 = (int)(g2 * kk); b2 = (int)(b2 * kk);
                            if (r2 > 255) r2 = 255;
                            if (g2 > 255) g2 = 255;
                            if (b2 > 255) b2 = 255;
                        }
                    }
                }
                gallCol.Add(r2); gallCol.Add(g2); gallCol.Add(b2);
            }
            break;
        }
        if (gallCol.Count == 0)
        { gallCol.Add(225); gallCol.Add(55); gallCol.Add(45); }
    }

    // 0 = com'e' davvero, poi via via piu' grosso
    static readonly float[] GALL_ZOOM = new float[] { 1f, 1.3f, 1.6f, 2f, 2.6f };
    int gallZoom = 0;

    string GallZoomTxt()
    {
        if (gallZoom <= 0) return "Vera";
        return "x" + GALL_ZOOM[gallZoom].ToString("0.0",
               CultureInfo.InvariantCulture);
    }

    float GallZoom()
    {
        if (gallZoom < 0 || gallZoom >= GALL_ZOOM.Length) return 1f;
        return GALL_ZOOM[gallZoom];
    }

    // un pezzo di antennina, da un punto all'altro: tre righe appaiate,
    // se no da lontano un filo solo sparisce
    void RigaGall(GTA.Math.Vector3 a, GTA.Math.Vector3 b2, int r, int g, int b)
    {
        Function.Call(Hash.DRAW_LINE, a.X, a.Y, a.Z, b2.X, b2.Y, b2.Z,
                      r, g, b, 245);
        Function.Call(Hash.DRAW_LINE, a.X + 0.008f, a.Y, a.Z,
                      b2.X + 0.008f, b2.Y, b2.Z, r, g, b, 245);
        Function.Call(Hash.DRAW_LINE, a.X, a.Y + 0.008f, a.Z,
                      b2.X, b2.Y + 0.008f, b2.Z, r, g, b, 245);
    }

    bool HoIlGalleggiante()
    {
        string img = "", nome = "";
        int id;
        return Montato("galleggiante", out id, out img, out nome);
    }

    // affonda: 0 sta a galla, 1 e' sparito sotto
    // scossa: quanto balla adesso
    void GalleggianteInAcqua(int now, float affonda, float scossa, float tira)
    {
        if (!escaInAcqua) return;
        if (!HoIlGalleggiante()) return;
        try
        {
            DatiGalleggiante();
            float z = AcquaSottoEsca();
            float onda = (float)Math.Sin(now * 0.003) * 0.02f;
            float ballo = (float)Math.Sin(now * 0.045) * 0.05f * scossa;
            float giu = affonda * LeggiF("gall_affonda", 0.34f);
            float zc = z + onda + ballo - giu;
            int nc = gallCol.Count / 3;
            if (nc < 1) return;
            float zoom = GallZoom();

            if (gallPalla)
            {
                // le sferiche: mezza di un colore e mezza dell'altro.
                // La sfera del gioco vuole il RAGGIO, non il diametro:
                // passandogli i dieci centimetri della scheda veniva
                // larga venti, ed e' per questo che sembrava una boa.
                float d = gallSpess * zoom * 0.5f;
                // QUALE MEZZA STA SOPRA.
                // Nelle foto l'ordine dei due colori non e' coerente: il
                // "White-n-Red" ha il bianco sopra, il "Yellow-n-Green"
                // ha il verde - cioe' il secondo. Quindi non si indovina:
                // "gall_palla_gira" a 1 scambia le due mezze, ed e' un
                // numero che si cambia a gioco acceso.
                int sopra = 0, sotto = (nc > 1) ? 3 : 0;
                if (LeggiF("gall_palla_gira", 1f) > 0.5f && nc > 1)
                { sopra = 3; sotto = 0; }
                Function.Call(Hash.DRAW_MARKER, 28, escaX, escaY, zc + d * 0.18f,
                              0f, 0f, 0f, 0f, 0f, 0f, d, d, d,
                              gallCol[sopra], gallCol[sopra + 1], gallCol[sopra + 2],
                              240, false, false, 2, false, 0, 0, false);
                Function.Call(Hash.DRAW_MARKER, 28, escaX, escaY, zc - d * 0.18f,
                              0f, 0f, 0f, 0f, 0f, 0f, d * 0.98f, d * 0.98f, d * 0.98f,
                              gallCol[sotto], gallCol[sotto + 1], gallCol[sotto + 2],
                              240, false, false, 2, false, 0, 0, false);
                return;
            }

            // COM'E' FATTO DAVVERO UN GALLEGGIANTE.
            // Guardando le foto: il PRIMO colore e' il corpo, il bulbo
            // che sta a pelo d'acqua; l'ULTIMO e' la punta, un pezzetto
            // corto in cima; quelli in mezzo sono lo stelo, che e' la
            // parte lunga. Il Waggler Heavy e' bulbo rosso, stelo nero,
            // punta gialla - e prima io facevo tre fasce uguali, quindi
            // la punta non si vedeva.
            float sp = gallSpess * zoom * 0.5f;
            if (sp < 0.008f) sp = 0.008f;
            Function.Call(Hash.DRAW_MARKER, 28, escaX, escaY, zc,
                          0f, 0f, 0f, 0f, 0f, 0f, sp, sp, sp,
                          gallCol[0], gallCol[1], gallCol[2], 240,
                          false, false, 2, false, 0, 0, false);
            float ant = gallLung * LeggiF("gall_fuori", 0.55f) * zoom;
            float punta = LeggiF("gall_punta", 0.22f);      // quanto e' corta la cima

            // SI INCLINA VERSO CHI TIRA.
            // Il filo parte dal fondo del galleggiante e va verso la
            // canna: se recuperi, quel filo tira in avanti e il
            // galleggiante si corica verso di te. Piu' recuperi, piu' si
            // corica - e quando molli si rialza da solo.
            GTA.Math.Vector3 verso = GTA.Math.Vector3.Zero;
            try
            {
                Ped pg = Game.Player.Character;
                if (pg != null && pg.Exists())
                {
                    float dx = pg.Position.X - escaX, dy = pg.Position.Y - escaY;
                    float dl = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dl > 0.01f)
                    {
                        float inc = LeggiF("gall_inclina", 0.75f) * tira;
                        verso = new GTA.Math.Vector3(dx / dl * ant * inc,
                                                     dy / dl * ant * inc, 0f);
                    }
                }
            }
            catch { }

            // E SI CORICA SE IL TERMINALE E' PIU' LUNGO DEL FONDO, come nel
            // quadrante: 45 gradi quando l'esca tocca quasi, sdraiato
            // (gall_sdraiato) quando la profondita' impostata supera il fondo.
            float gradiAcqua = 0f;
            float fondoG = FondoDellEsca();
            if (fondoG > 0.05f)
            {
                if (profondita > fondoG * 1.05f) gradiAcqua = LeggiF("gall_sdraiato", 80f);
                else if (profondita > fondoG * 0.9f) gradiAcqua = 45f;
            }
            if (gradiAcqua > 0f)
            {
                // si corica di lato rispetto a chi guarda: verso il pescatore,
                // come quando tira, cosi' la direzione e' una sola
                float rad = gradiAcqua * 3.1415926f / 180f;
                float dl2 = 0f, dx2 = 0f, dy2 = 0f;
                try
                {
                    Ped pg2 = Game.Player.Character;
                    dx2 = pg2.Position.X - escaX; dy2 = pg2.Position.Y - escaY;
                    dl2 = (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                }
                catch { }
                if (dl2 > 0.01f)
                {
                    float lato = ant * (float)Math.Sin(rad);
                    verso = new GTA.Math.Vector3(dx2 / dl2 * lato, dy2 / dl2 * lato, 0f);
                    ant = ant * (float)Math.Cos(rad);
                }
            }
            GTA.Math.Vector3 basso = new GTA.Math.Vector3(escaX, escaY, zc);
            GTA.Math.Vector3 cima = new GTA.Math.Vector3(escaX + verso.X,
                                                         escaY + verso.Y, zc + ant);
            int steli = nc - 2;                             // i colori dello stelo
            if (steli < 1) steli = 1;
            int primoStelo = (nc > 2) ? 1 : 0;              // se ce n'e' uno solo, quello
            float fStelo = 1f - punta;
            int i2;
            for (i2 = 0; i2 < steli; i2++)
            {
                float t1 = fStelo * i2 / steli;
                float t2 = fStelo * (i2 + 1) / steli;
                int k2 = (primoStelo + i2) * 3;
                if (k2 + 2 >= gallCol.Count) k2 = 0;
                RigaGall(basso + (cima - basso) * t1, basso + (cima - basso) * t2,
                         gallCol[k2], gallCol[k2 + 1], gallCol[k2 + 2]);
            }
            // la punta, sempre l'ultimo colore
            int kp = (nc - 1) * 3;
            RigaGall(basso + (cima - basso) * fStelo, cima,
                     gallCol[kp], gallCol[kp + 1], gallCol[kp + 2]);
        }
        catch { }
    }

    // il filo corto che regge il pesce appeso alla canna
    void DisegnaFiloAppeso()
    {
        if (pescePed == null || !pescePed.Exists() || !pesceAppeso) return;
        try
        {
            GTA.Math.Vector3 a = PuntaCanna();
            if (a == GTA.Math.Vector3.Zero) return;
            ColoreDellaLenza();
            int alfa2 = (int)LeggiF("lenza_alfa", 130f) + 40;
            if (alfa2 > 255) alfa2 = 255;
            // il filo arriva UN PELO PIU' GIU' del punto calcolato: fra
            // quando spostiamo il pesce e quando il gioco ci dice dov'e'
            // finito l'osso passa un fotogramma, e resta un dito di
            // stacco. "lenza_giu_extra" lo chiude.
            float giuE = LeggiF("lenza_giu_extra", 0.12f);
            Function.Call(Hash.DRAW_LINE, a.X, a.Y, a.Z,
                          pesceAppesoX, pesceAppesoY, pesceAppesoZ - giuE,
                          lenzaR, lenzaG, lenzaB, alfa2);
        }
        catch { }
    }

    // il filo, e il puntino dell'esca sul pelo dell'acqua
    void DisegnaLenza(int now, bool tesa)
    {
        if (!escaInAcqua) return;
        try
        {
            GTA.Math.Vector3 a = PuntaCanna();
            if (a == GTA.Math.Vector3.Zero) return;
            float zAcqua = AcquaSottoEsca();
            GTA.Math.Vector3 b = new GTA.Math.Vector3(escaX, escaY, zAcqua);

            // L'ESCA RESTA IN ACQUA FINO ALLA FINE.
            // Zero metri vuol dire l'esca davanti ai piedi di chi pesca,
            // sul pelo dell'acqua: non in cima alla canna. Prima negli
            // ultimi metri la tiravo su verso la punta e volava per aria.

            // con la lenza molle il filo scende a pancia; tesa e' dritto
            ColoreDellaLenza();
            int alfa = (int)LeggiF("lenza_alfa", 130f);
            if (tesa) alfa += 30;
            if (alfa < 20) alfa = 20;
            if (alfa > 255) alfa = 255;
            // LA PANCIA NON E' SIMMETRICA.
            // Un filo teso fra due punti farebbe una curva uguale da tutte
            // e due le parti. Qui pero' da un capo c'e' la canna che tira
            // in alto e dall'altro l'acqua: verso l'acqua il filo si
            // appoggia, arriva quasi disteso invece che a picco. Quindi la
            // pancia si sposta verso il lato dell'esca.
            int n = 10;
            float pancia = tesa ? LeggiF("lenza_pancia_tesa", 0.06f)
                                : LeggiF("lenza_pancia_molle", 0.24f);
            // e ondeggia appena, che un filo fermo sembra un bastone
            pancia += 0.02f * (float)Math.Sin(now * 0.004);
            float vRiva = LeggiF("lenza_appoggio", 0.9f);
            GTA.Math.Vector3 prec = a;
            int i;
            for (i = 1; i <= n; i++)
            {
                float t = (float)i / n;
                GTA.Math.Vector3 q = a + (b - a) * t;
                q.Z -= pancia * (float)Math.Sin(t * Math.PI) * (1f - vRiva + vRiva * 2f * t);
                Function.Call(Hash.DRAW_LINE, prec.X, prec.Y, prec.Z,
                              q.X, q.Y, q.Z, lenzaR, lenzaG, lenzaB, alfa);
                prec = q;
            }

            // L'ONDA DOVE IL FILO TOCCA L'ACQUA.
            // Un cerchietto piatto che respira: si vede dove sta l'esca
            // anche da lontano, e da' l'idea che qualcosa galleggi.
            float r = 0.16f + 0.05f * (float)Math.Sin(now * 0.005);
            Function.Call(Hash.DRAW_MARKER, 25, b.X, b.Y, b.Z + 0.03f,
                          0f, 0f, 0f, 0f, 0f, 0f, r, r, r,
                          235, 240, 245, 90, false, false, 2,
                          false, 0, 0, false);
        }
        catch { }
    }

    void TogliCanna()
    {
        try
        {
            if (cannaProp != null && cannaProp.Exists()) cannaProp.Delete();
        }
        catch { }
        cannaProp = null;
    }

    // la posa del pescatore, suonata come animazione e non come scenario:
    // cosi' si puo' cambiare clip quando si tira
    const string DIZ_PESCA = "amb@world_human_stand_fishing@idle_a";

    // SI CAMMINA CON LA CANNA IN MANO, NON CON LA LENZA IN ACQUA.
    // Con "pesca_cammina", finche' non hai lanciato la posa della canna
    // prende solo il busto (flag 49) e le gambe restano al gioco: giri
    // al lago con la canna in mano, al passo. Appena lanci si sta fermi
    // come prima, posa a corpo intero: provato a camminare con la lenza
    // in acqua, il busto secondario mandava in confusione le gambe (si
    // girava e proseguiva da solo). Con 0 fermo sempre.
    bool Cammina()
    {
        return LeggiF("pesca_cammina", 1f) > 0.5f;
    }

    int FlagPosa()
    {
        return (Cammina() && fase == FASE_PRONTO) ? 49 : 1;
    }

    bool FaseDiCammino()
    {
        return fase == FASE_PRONTO;
    }

    void Posa(Ped p, string clip)
    {
        Posa(p, clip, 0.10f);
    }

    // LA CANNA SI TIENE FERMA.
    // La clip "idle_c" e' quella in cui gira il mulinello: usata sempre,
    // faceva trin-trin dall'inizio alla fine. Adesso da fermo si tiene
    // "idle_a" quasi congelata - respira e basta - e il mulinello lo si
    // gira solo quando lo giri davvero.
    int flagInCorso = -1;   // con che flag e' partita la clip in corso

    void Posa(Ped p, string clip, float velocita)
    {
        // STESSA CLIP MA FLAG DIVERSO = SI RIFA'. Dopo una rottura o un
        // pesce perso si torna con la canna in mano: la posa era partita
        // a corpo intero (lotta) e va rifatta a solo busto, se no le
        // gambe restano inchiodate e non si cammina piu'.
        if (clipInCorso == clip && flagInCorso == FlagPosa()) return;
        faseInCorso = -1f;
        try
        {
            Function.Call(Hash.REQUEST_ANIM_DICT, DIZ_PESCA);
            int w = 0;
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, DIZ_PESCA) && w < 1000)
            { Script.Wait(50); w += 50; }
            // la posa "solo busto" del cammino (secondaria) resterebbe
            // sopra le braccia anche dopo: via, prima di una posa intera
            if (FlagPosa() == 1) Function.Call(Hash.CLEAR_PED_SECONDARY_TASK, p);
            // E IL CONTRARIO: la posa a corpo intero (scheda, lotta) e' un
            // task che non finisce mai; se le si mette sopra la posa solo
            // busto, le gambe restano inchiodate. Prima si toglie.
            else if (flagInCorso == 1) Function.Call(Hash.CLEAR_PED_TASKS, p);
            Function.Call(Hash.TASK_PLAY_ANIM, p, DIZ_PESCA, clip,
                          8.0f, -8.0f, -1, FlagPosa(), 0.0f, false, false, false);
            try { Function.Call(Hash.SET_ENTITY_ANIM_SPEED, p, DIZ_PESCA, clip, velocita); }
            catch { }
            clipInCorso = clip;
            flagInCorso = FlagPosa();
        }
        catch { }
    }

    // quello che si vede quando stai fermo con la canna in mano
    string ClipFerma()
    {
        return LeggiS("anim_calma", "idle_a");
    }

    // LA POSA CONGELATA.
    // Rallentare non basta: tutte le clip della pesca a un certo punto
    // girano il mulinello. Qui l'animazione si inchioda su un fotogramma
    // preciso e ci resta, quindi la canna sta ferma davvero.
    // "anim_fermo_fase" nel config sceglie quale fotogramma: 0 = inizio,
    // 1 = fine. Se ne cambi il valore cambi la posa senza ricompilare.
    void Congela(Ped p, string clip)
    {
        FermaSu(p, clip, LeggiF("anim_fermo_fase", 0.22f));
    }

    // LA POSA SU UN FOTOGRAMMA, MESSA COME SI DEVE.
    // Prima si faceva partire la clip e subito le si metteva velocita'
    // zero: cosi' pero' si fermava anche la fusione con la posa di
    // prima, e il corpo restava com'era - si muoveva solo la canna.
    // Qui il fotogramma si passa alla TASK_PLAY_ANIM come punto di
    // partenza, quindi il pescatore ci va davvero, e ci va in un
    // decimo di secondo: e' quello lo scatto.
    float faseInCorso = -1f;

    void PosaSuFase(Ped p, string clip, float f, float fusione)
    {
        if (f < 0f) f = 0f;
        if (f > 0.99f) f = 0.99f;
        if (clipInCorso == clip && faseInCorso >= 0f && flagInCorso == FlagPosa()
            && f - faseInCorso < 0.002f && faseInCorso - f < 0.002f) return;
        try
        {
            Function.Call(Hash.REQUEST_ANIM_DICT, DIZ_PESCA);
            int w = 0;
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, DIZ_PESCA) && w < 1000)
            { Script.Wait(50); w += 50; }
            if (FlagPosa() == 1) Function.Call(Hash.CLEAR_PED_SECONDARY_TASK, p);
            else if (flagInCorso == 1) Function.Call(Hash.CLEAR_PED_TASKS, p);
            Function.Call(Hash.TASK_PLAY_ANIM, p, DIZ_PESCA, clip,
                          fusione, -8.0f, -1, FlagPosa(), f, false, false, false);
            Function.Call(Hash.SET_ENTITY_ANIM_SPEED, p, DIZ_PESCA, clip, 0.0f);
            Function.Call(Hash.SET_ENTITY_ANIM_CURRENT_TIME, p, DIZ_PESCA, clip, f);
            clipInCorso = clip;
            flagInCorso = FlagPosa();
            faseInCorso = f;
        }
        catch { }
    }

    // inchioda la clip su un fotogramma preciso (0 = inizio, 1 = fine)
    void FermaSu(Ped p, string clip, float f)
    {
        try
        {
            if (f < 0f) f = 0f;
            if (f > 0.99f) f = 0.99f;
            Function.Call(Hash.SET_ENTITY_ANIM_SPEED, p, DIZ_PESCA, clip, 0.0f);
            Function.Call(Hash.SET_ENTITY_ANIM_CURRENT_TIME, p, DIZ_PESCA, clip, f);
        }
        catch { }
    }

    // ferma: mette la posa e la inchioda
    void PosaFerma(Ped p)
    {
        // se una frustata era rimasta a meta' (il pesce abbocca subito),
        // la canna torna dritta prima di rimettersi in posa
        if (frustaFino != 0) { frustaFino = 0; RuotaCanna(p, 0f); }
        if (miaStrappataFino != 0) { miaStrappataFino = 0; RuotaCanna(p, 0f); }
        string c = ClipFerma();
        Posa(p, c);
        Congela(p, c);
    }

    // quello che si vede quando giri davvero il mulinello
    string ClipMulinello()
    {
        return LeggiS("anim_recupero", "idle_c");
    }

    // IL LANCIO.
    // Il dritto del tennis muoveva il braccio ma non era un lancio.
    // La clip del mulinello, invece, dentro ce l'ha gia': il busto che
    // va indietro e poi torna avanti. Non la si fa suonare, se ne
    // guidano i fotogrammi a mano - indietro mentre carichi, avanti di
    // colpo quando molli - e la canna gira insieme al corpo.
    // I tre fotogrammi e la durata stanno nel config: si cambiano senza
    // ricompilare.
    int frustaDa = 0;
    int frustaFino = 0;
    float frustaGiro = 0f;

    // la canna ruotata di "giro" gradi (avanti e indietro) e di
    // "lato" gradi (destra e sinistra) rispetto a come la tiene di solito
    void RuotaCanna(Ped p, float giro)
    {
        RuotaCanna(p, giro, scartoCanna);
    }

    void RuotaCanna(Ped p, float giro, float lato)
    {
        try
        {
            if (cannaProp == null || !cannaProp.Exists()) return;
            int oc = Function.Call<int>(Hash.GET_PED_BONE_INDEX, p, 18905);
            float rx = LeggiF("canna_rx", 0f);
            float ry = LeggiF("canna_ry", 90f);
            // A GALLEGGIANTE LA CANNA SI TIENE PIU' BASSA: con la lenza in
            // acqua e il galleggiante montato la punta scende di
            // "canna_gall_giu" gradi (a spinning resta com'e').
            float giuGall = 0f;
            if ((fase == FASE_ACQUA || fase == FASE_ABBOCCA || fase == FASE_LOTTA)
                && InUso("galleggiante") >= 0)
                giuGall = LeggiF("canna_gall_giu", -30f);
            float rz = LeggiF("canna_rz", 70f) + giro + giuGall;
            // SU QUALE ASSE LA CANNA VA DI LATO.
            // Il modello e' gia' girato di novanta gradi in mano, quindi
            // quale dei tre assi porti la punta a destra e a sinistra non
            // e' scontato. Sta nel config: se muovendola va storta invece
            // che di lato, si cambia la lettera in "canna_lato_asse".
            // "no" = la canna non si storce: a portarla di lato ci
            // pensa il corpo, che le gira insieme.
            string asse = LeggiS("canna_lato_asse", "no");
            if (asse == "x") rx += lato;
            else if (asse == "y") ry += lato;
            else if (asse == "z") rz += lato;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, cannaProp, p, oc,
                          LeggiF("canna_x", 0.13f), LeggiF("canna_y", 0.10f),
                          LeggiF("canna_z", 0.01f), rx, ry, rz,
                          false, false, false, false, 2, true);
        }
        catch { }
    }

    // mentre carichi: piu' carichi, piu' va indietro
    void PosaCarica(Ped p, float carica)
    {
        if (carica < 0f) carica = 0f;
        if (carica > 1f) carica = 1f;
        // IL MULINELLO NON GIRA NEMMENO MENTRE CARICHI.
        // Prima il fotogramma seguiva la barra della carica, che sale e
        // scende in continuazione: e la manovella girava con lei. Adesso
        // il corpo si piega indietro una volta sola e resta li'; a dire
        // quanto hai caricato ci pensa la canna, che va indietro da sola.
        PosaSuFase(p, ClipMulinello(), LeggiF("anim_lancio_a", 0.62f), 8.0f);
        RuotaCanna(p, LeggiF("canna_indietro", 35f) * carica);
    }

    // parte la frustata
    void AvviaFrustata(Ped p, float carica)
    {
        string c = ClipMulinello();
        Posa(p, c);
        frustaDa = Game.GameTime;
        int ms = (int)LeggiF("anim_lancio_ms", 420f);
        if (ms < 120) ms = 120;
        frustaFino = frustaDa + ms;
        frustaGiro = LeggiF("canna_indietro", 35f) * carica;
    }

    // LA STRAPPATA.
    // Levetta indietro: il pescatore tira verso di se', come quando
    // lancia ma al contrario, e si porta a casa qualche decimo di
    // lenza. Stesso trucco della frustata: due pose, e il movimento lo
    // fa la fusione tra le due. Niente manovella.
    int miaStrappataFino = 0;
    int miaStrappataDa = 0;

    void AvviaStrappo(Ped p)
    {
        miaStrappataFino = Game.GameTime + (int)LeggiF("anim_strappo_ms", 420f);
    }

    bool Strappo(Ped p, int now)
    {
        if (miaStrappataFino == 0) return false;
        if (now >= miaStrappataFino)
        {
            miaStrappataFino = 0;
            RuotaCanna(p, 0f);
            clipInCorso = "";
            return false;
        }
        PosaSuFase(p, LeggiS("anim_tira", "idle_b"),
                   LeggiF("anim_strappo_fase", 0.45f), 12.0f);
        RuotaCanna(p, LeggiF("canna_strappo", 22f));
        return true;
    }

    // true finche' la frustata e' in corso: chi la chiama, in quel
    // mentre, non tocca ne' la posa ne' la canna
    bool Frustata(Ped p, int now)
    {
        if (frustaFino == 0) return false;
        if (now >= frustaFino)
        {
            // finita la frustata l'esca ha toccato l'acqua
            SuonoFile(LeggiS("suono_tonfo_file", "tonfo.wav"));
            frustaFino = 0;
            RuotaCanna(p, 0f);
            clipInCorso = "";
            return false;
        }
        float t = (float)(now - frustaDa) / (float)(frustaFino - frustaDa);
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        // IL MULINELLO NON GIRA MENTRE LANCI.
        // Scorrere i fotogrammi della clip del mulinello, avanti o
        // indietro che sia, la manovella la fa girare lo stesso. Quindi
        // non si scorre niente: si resta inchiodati sul fotogramma
        // piegato indietro e poi, di colpo, si passa alla posa dritta.
        // E' la fusione tra le due pose - un ottavo di secondo - a fare
        // il movimento in avanti. Le mani non toccano la manovella.
        if (t < LeggiF("anim_lancio_scatto", 0.25f))
        {
            // ancora piegato indietro
            PosaSuFase(p, ClipMulinello(), LeggiF("anim_lancio_a", 0.62f), 8.0f);
        }
        else
        {
            // e via: la fusione veloce fra le due pose e' la frustata
            PosaSuFase(p, ClipFerma(), LeggiF("anim_fermo_fase", 0.22f), 24.0f);
        }
        // la canna: da dietro passa avanti oltre il normale, poi rientra
        float av = LeggiF("canna_avanti", 28f);
        float g;
        if (t < 0.5f) g = frustaGiro + (-av - frustaGiro) * (t / 0.5f);
        else g = -av * (1f - (t - 0.5f) / 0.5f);
        RuotaCanna(p, g);
        return true;
    }

    void ScenaSu(Ped p)
    {
        if (inScena && cannaProp != null && cannaProp.Exists()) return;
        inScena = true;
        TogliCanna();
        MettiCanna(p);
        PosaFerma(p);
    }

    void ScenaGiu(Ped p)
    {
        if (!inScena) return;
        inScena = false;
        TogliPesce();
        TogliCanna();
        clipInCorso = "";
        try { if (p != null && p.Exists()) p.Task.ClearAll(); }
        catch { }
    }

    // il galleggiante: la PNG vera che ondeggia sul pelo dell'acqua.
    // "giu" lo spinge sotto: 5 quando il pesce assaggia, 14 quando abbocca.
    // ============================================================
    //  IL QUADRANTE DELLO SPINNING
    // ============================================================
    // A spinning il galleggiante non c'e': l'esca e' un cucchiaino che
    // affonda da solo e si alza mentre recuperi. Quello che serve vedere
    // e' a che altezza sta fra il pelo dell'acqua e il fondo, perche' e'
    // li' che si decide se il predatore la vede. Tirando e mollando -
    // stop and go, jerking - il cucchiaino sale e riscende: e' cosi' che
    // si imita un pesciolino spaventato.
    //   profEsca: 0 = a pelo d'acqua, 1 = sul fondo
    float profEsca = 1f;
    float profPrec = 1f;      // dov'era il giro prima: da qui il verso
    float inclEsca = 0f;      // di quanto e' inclinata adesso

    // QUALE QUADRANTE SI VEDE.
    // Di suo quello del galleggiante se il galleggiante c'e'. Mettendo
    // "spin_prova 1" in config.ini si vede sempre quello dello spinning,
    // anche con la canna telescopica: serve per provarlo senza dover
    // smontare l'armatura. In acqua non cambia niente.
    bool QuadranteGall()
    {
        if (LeggiF("spin_prova", 0f) > 0.5f) return false;
        return HoIlGalleggiante();
    }

    void DisegnaSpinning(int now, float scossa)
    {
        float cx = QuadX();
        float alt = LeggiF("spin_alt", 62f);
        // IL QUADRANTE STA SOPRA I METRI.
        // Quello del galleggiante e' basso e ci sta; questo e' il doppio
        // e finiva sopra al numero dei metri. Si misura dal basso: il
        // fondo del quadrante resta "spin_su" pixel sopra la barra.
        float top = LeggiF("quadrante_y", 296f);
        float largo = 48f;

        // l'acqua fra il pelo e il fondo
        DisegnaAcqua(QualeAcqua(), cx, largo, top, alt);
        // il fondo
        DisegnaFondale(cx, largo, top, alt);

        // il cucchiaino, di lato, dove sta adesso
        float eh = LeggiF("spin_esca_h", 16f);
        float ew = eh * 3f;
        float p = profEsca;
        if (p < 0f) p = 0f;
        if (p > 1f) p = 1f;
        float ey = top + 3f + p * (alt - 6f - eh);
        // un filo di ondeggio, che sta ferma non e' credibile
        ey += (float)Math.Sin(now * 0.006) * 1.2f;

        // IL COLPO QUANDO MORDE.
        // Il galleggiante affonda e balla, e si vede. Qui non c'era
        // niente: sentivi il pad vibrare e sullo schermo non succedeva
        // nulla. Adesso l'esca sussulta - trema e viene tirata in giu' -
        // di quanto e' forte il tocco.
        float exx = 0f;
        if (scossa > 0f)
        {
            float amp = LeggiF("spin_scossa", 3.4f) * scossa;
            exx = (float)Math.Sin(now * 0.055) * amp;
            ey += (float)Math.Sin(now * 0.041) * amp * 0.8f
                + LeggiF("spin_scossa_giu", 2.2f) * scossa;
        }

        // IL MUSO SEGUE IL MOVIMENTO.
        // Il cucchiaino e' legato per la testa: quando affonda scende di
        // testa in giu', quando lo tiri sale di testa per prima e il
        // corpo viene dietro. Percio' si inclina, e l'inclinazione la
        // decide se sta scendendo o salendo.
        float verso = profEsca - profPrec;
        profPrec = profEsca;
        float mira = 0f;
        float gr = LeggiF("spin_incl", 32f);
        if (verso > 0.0004f) mira = gr;          // sta scendendo
        else if (verso < -0.0004f) mira = -gr;   // sta salendo
        // ci arriva piano, se no scatta
        float vel = LeggiF("spin_incl_vel", 4f) * Game.LastFrameTime;
        if (vel > 1f) vel = 1f;
        inclEsca += (mira - inclEsca) * vel;

        // QUI CI VA IL DISEGNO, NON LA FOTO.
        // Il quadrante dice a che altezza sta l'esca, non quale sia:
        // quale sia si legge nell'armatura, dove c'e' la foto vera.
        // COL CUCCHIAINO si vede il cucchiaino, e si inclina col
        // movimento. CON L'AMO E L'ESCA FRESCA si vede l'amo col verme,
        // che scende dritto e non fa il pesciolino: sono due modi di
        // pescare diversi e si devono distinguere a colpo d'occhio.
        if (InUso("artificiale") >= 0)
            SpriteInclinata("img\\artificiali\\cucchiaino_base.png",
                            cx - ew * 0.5f + exx, ey, ew, eh,
                            inclEsca + exx * 1.8f);
        else
        {
            float aw = eh * 1.15f;
            SpriteInclinata("img\\terminali\\amo_base.png",
                            cx - aw * 0.5f + exx, ey - eh * 0.15f,
                            aw, eh * 1.3f, exx * 2.2f);
        }
    }

    // l'immagine dell'artificiale montato
    string ImgArtificiale()
    {
        int id; string img, nome;
        if (Montato("artificiale", out id, out img, out nome)) return img;
        return "";
    }

    // IL FONDALE DEL QUADRANTE: sabbia in mare, fango e alghe nel lago e
    // in palude, sassi nel fiume e nel torrente. Prende spazio verso
    // l'alto, il bordo sotto resta dov'era. "fondale_alt" e' l'altezza.
    // che acqua e': 0 mare, 1 lago (e palude), 2 fiume (e torrente)
    int QualeAcqua()
    {
        string tipo = "";
        int lu = LuogoQui();
        if (lu >= 0 && lu < arTipo.Count) tipo = arTipo[lu];
        if (tipo == "lago" || tipo == "palude") return 1;
        if (tipo == "fiume" || tipo == "torrente") return 2;
        return 0;
    }

    // L'ACQUA DEL QUADRANTE, col suo colore: mare verde cristallino,
    // lago blu, fiume piu' chiaro. In config "acqua_mare", "acqua_lago",
    // "acqua_fiume" come r,g,b; "acqua_alfa" la trasparenza.
    void DisegnaAcqua(int quale, float cx, float largo, float top, float alt)
    {
        string[] chiavi = new string[] { "acqua_mare", "acqua_lago", "acqua_fiume" };
        string[] dif = new string[] { "45,150,140", "40,80,130", "85,120,150" };
        string[] c = LeggiS(chiavi[quale], dif[quale]).Split(',');
        int r = 40, g = 80, b = 130;
        if (c.Length >= 3) { r = Numero(c[0].Trim()); g = Numero(c[1].Trim()); b = Numero(c[2].Trim()); }
        int a = (int)LeggiF("acqua_alfa", 150f);
        DisegnaRett(cx - largo * 0.5f, top, largo, alt, r, g, b, a);
        // SFUMATA SENZA RIGHE: sopra il colore pieno si stende un PNG
        // nero che va da trasparente in alto a coperto in fondo, pixel
        // per pixel, come la ruota. "acqua_ombra" 0..1 quanto e' scura.
        float om = LeggiF("acqua_ombra", 1f);
        if (om > 0.01f)
            Sprite("img\\hud\\acqua_ombra.png", cx - largo * 0.5f, top, largo, alt);
        // il pelo dell'acqua: lo stesso colore, piu' chiaro
        DisegnaRett(cx - largo * 0.5f, top, largo, 2f,
                    Math.Min(255, r + 110), Math.Min(255, g + 115), Math.Min(255, b + 105), 235);
    }

    void DisegnaFondale(float cx, float largo, float top, float alt)
    {
        int quale = QualeAcqua();
        float fh = LeggiF("fondale_alt", 6f);
        float y = top + alt - fh;
        UnFondale(quale, cx, largo, y, fh);
        // "fondale_tutti=1": gli altri due a sinistra, verso il centro
        // dello schermo, per vederli insieme
        if (LeggiF("fondale_tutti", 0f) > 0.5f)
        {
            int lato = 0, q;
            for (q = 0; q < 3; q++)
            {
                if (q == quale) continue;
                float qx = cx - largo * 1.5f - 50f - lato * (largo + 6f);
                DisegnaAcqua(q, qx, largo, top, alt);
                UnFondale(q, qx, largo, y, fh);
                lato++;
            }
        }
    }

    // un fondale: la striscia di terreno, e SOPRA le alghe che
    // ondeggiano (lago) o i sassi appoggiati (fiume)
    void UnFondale(int quale, float x, float largo, float y, float fh)
    {
        string[] terreni = new string[] { "img\\hud\\fondo_mare.png", "img\\hud\\fondo_lago.png", "img\\hud\\fondo_fiume.png" };
        float x0 = x - largo * 0.5f;
        Sprite(terreni[quale], x0, y, largo, fh);
        if (quale == 0)
        {
            // il mare: due coralli, rosa e violetto
            float ch = LeggiF("coralli_alt", 14f);
            Sprite("img\\hud\\coralli.png", x0, y - ch + 1f, largo, ch);
        }
        else if (quale == 1)
        {
            // il lago: sassolini sul fondo e le alghe che ondeggiano
            float sh = LeggiF("sassi_alt", 7f);
            Sprite("img\\hud\\sassi.png", x0, y - sh + 1f, largo, sh);
            float ah = LeggiF("alghe_alt", 20f);
            float onda = (float)Math.Sin(Game.GameTime * 0.0025) * LeggiF("alghe_onda", 5f);
            SpriteInclinata("img\\hud\\alghe.png", x0, y - ah, largo, ah, onda);
        }
        else if (quale == 2)
        {
            // il fiume: sassi grossi
            float sg = LeggiF("sassi_grandi_alt", 11f);
            Sprite("img\\hud\\sassi_grandi.png", x0, y - sg + 1f, largo, sg);
        }
    }

    void DisegnaGalleggiante(int now, float giu, float scossa)
    {
        // STESSO QUADRANTE DELLO SPINNING, con dentro il montaggio a
        // galleggiante: il pelo dell'acqua in alto, il fondo in basso,
        // il galleggiante che sta a galla e l'amo con l'esca appeso
        // sotto. Il galleggiante si muove come si e' sempre mosso -
        // ondeggia e affonda quando morde - solo che adesso si vede
        // anche dove sta l'esca.
        float cx = QuadX();
        float alt = LeggiF("spin_alt", 62f);
        float top = LeggiF("quadrante_y", 296f);
        float largo = 48f;

        DisegnaAcqua(QualeAcqua(), cx, largo, top, alt);
        DisegnaFondale(cx, largo, top, alt);

        // L'AMO CON L'ESCA, appeso sotto: sta a mezz'acqua, alla
        // profondita' che gli da' il galleggiante ("gall_prof").
        float eh = LeggiF("spin_esca_h", 16f);
        float ah = eh * 1.3f;
        float aw = eh * 1.15f;
        // IN SCALA COL FONDO VERO: l'esca sta a "profondita" metri sotto il
        // galleggiante; se il fondo lo sappiamo, nel quadrante sta in
        // proporzione. Piu' lungo del fondo = tocca terra.
        float prof = LeggiF("gall_prof", 0.62f);
        float fondoQ = (fase == FASE_ACQUA || fase == FASE_ABBOCCA || fase == FASE_LOTTA) ? FondoDellEsca() : -1f;
        float gradiGall = 0f;
        if (fondoQ > 0.05f)
        {
            prof = profondita / fondoQ;
            if (profondita > fondoQ * 1.05f) gradiGall = LeggiF("gall_sdraiato", 80f);
            else if (profondita > fondoQ * 0.9f) gradiGall = 45f;
        }
        if (prof < 0f) prof = 0f;
        if (prof > 1f) prof = 1f;
        float ay = top + 6f + prof * (alt - 12f - ah);
        float sx = 0f;
        if (scossa > 0f)
        {
            float amp = LeggiF("spin_scossa", 3.4f) * scossa;
            sx = (float)Math.Sin(now * 0.055) * amp;
            ay += (float)Math.Sin(now * 0.041) * amp * 0.8f;
        }
        // COL GALLEGGIANTE L'AMO NON SI DISEGNA: si vede il galleggiante e
        // basta ("gall_amo=1" in config lo rimette).
        if (LeggiF("gall_amo", 0f) > 0.5f)
            SpriteInclinata("img\\terminali\\amo_base.png",
                            cx - aw * 0.5f + sx, ay, aw, ah, sx * 2.2f);

        // IL GALLEGGIANTE, a galla sul pelo dell'acqua. Ondeggio e
        // affondata sono quelli di sempre: "giu" e' quanto e' sotto.
        float ondeggio = (float)Math.Sin(now * 0.004) * 2f;
        float gh = 29f;
        float gw = gh * 440f / 175f;      // proporzioni vere della PNG
        float gy = top - gh * 0.62f + ondeggio + giu;
        // sdraiato se il filo e' piu' lungo del fondo, a 45 se lo sfiora
        SpriteInclinata("img\\galleggianti\\galleggiante_base.png",
                        cx - gw * 0.5f, gy, gw, gh, gradiGall);
    }


}
