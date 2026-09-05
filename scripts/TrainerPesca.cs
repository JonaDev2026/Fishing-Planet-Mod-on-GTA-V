// ============================================================
//  PESCA - lo stesso motore del trainer, ma dentro c'e' solo la
//  pesca e le impostazioni. Si apre con F7 o RB+DESTRA.
//  (l'originale, che ha tutto il resto, e' Trainer.cs)
//  V MODS MANAGER - il menu unico: dentro c'e' il trainer e
//  le mod installate, che si agganciano da sole
//  SHVDN3 - stile vecchio C# (niente $"" ne ?. ne lambda)
//
//  APERTURA:  tastiera = F4     |    pad = RB + DPAD-GIU
//  NAVIGA:    frecce / DPAD  (anche NumPad 8/2/4/6)
//  CONFERMA:  INVIO / A      (anche NumPad 5)
//  INDIETRO:  BACKSPACE / B  (anche NumPad 0)
// ============================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

// ---------- una voce di menu ----------
class TItem
{
    public const int ACTION = 0;   // esegue e basta
    public const int TOGGLE = 1;   // ON / OFF
    public const int LIST   = 2;   // scelta fra opzioni (sx/dx)
    public const int NUMBER = 3;   // valore numerico (sx/dx)
    public const int SUB    = 4;   // apre un sottomenu
    public const int HEADER = 5;   // etichetta di sezione, non selezionabile

    public int Kind;
    public string Text;
    public int Id;            // id azione -> switch in DoAction / OnChanged
    public int Sub;           // indice sottomenu (solo SUB)
    public bool On;           // TOGGLE
    public string[] Opts;     // LIST
    public int Sel;           // LIST: indice scelto
    public int Val;           // NUMBER
    public int Min;
    public int Max;
    public int Step;
    public string Img2;       // seconda immagine, accanto alla prima
    public string Hint;       // riga di aiuto in fondo
    public string Data;       // payload libero (es. nome modello veicolo)
    public string TextIt;     // etichetta italiana
    public int Cr, Cg, Cb;    // tinta (0,0,0 = default)
    public bool Tinted;
    public bool SignedValue;  // colora il valore: + verde, - rosso, 0 bianco
    public string Img;        // immagine da mostrare sopra il menu (percorso)
    public string Desc;       // descrizione breve sotto l'immagine
    public string[] OptVals;  // liste delle mod: valore-comando per opzione
    public string[] OptImgs;  // liste delle mod: immagine per opzione
    public string[] OptDescs; // liste delle mod: descrizione per opzione
    public bool FondoPieno;   // tinta come sfondo dell'intera riga, non barretta
    public string Sotto;      // seconda riga piccola sotto il nome (righe icona)
    public int Dr, Dg, Db;    // colore del testo a destra (0,0,0 = quello di sempre)
    public bool DescTinta;
    public int Sr, Sg, Sb;    // colore della riga piccola sotto il nome
    public bool SottoTinta;

    public TItem(int kind, string text, int id)
    {
        Kind = kind;
        Text = text;
        Id = id;
        Sub = -1;
        On = false;
        Opts = null;
        Sel = 0;
        Val = 0;
        Min = 0;
        Max = 100;
        Step = 1;
        Hint = "";
        Data = "";
        TextIt = text;
        Cr = 0; Cg = 0; Cb = 0; Tinted = false;
        SignedValue = false;
    }
}

// ---------- una pagina di menu ----------
class TMenu
{
    public string Title;
    public string TitleIt;
    public int Parent;
    public List<TItem> Items;
    public int Sel;
    public int Top;
    public bool IconRows;      // righe alte con l'immagine a sinistra, niente banner
    public bool Centrato;      // le voci scritte in mezzo alla riga
    public bool HaSotto;       // qualche voce ha il sottotitolo: righe piu' alte
    public string Nota;        // fascia fissa sopra la lista, non scorre
    public string Insegna;     // immagine larga quanto il menu, in cima
    public bool Insegne;       // ogni riga e' un'insegna larga, col testo sotto
    public List<string> Blocco;  // righe di descrizione sotto l'insegna
    public List<string> Pannello;  // riquadro fisso a destra del menu
    public List<string> PannelloSx;  // riquadro fisso a SINISTRA del menu
    public string Titolo;      // titolo centrato sopra la lista
    public int PanSel = -1;    // -1 = cursore nella lista, >=0 = nel riquadro
    public int PanTop;         // prima riga del riquadro che si vede
    public int PanSelSx = -1;  // lo stesso, per il riquadro di sinistra
    public int PanTopSx;
    public List<string> PannelloKey;    // le chiavi del riquadro di destra
    public List<string> PannelloSxKey;  // la chiave di ogni riga: si vede solo
                                        // quando in mezzo e' scelta quella voce
                                        // ("" = si vede sempre)
    public string SxVista = "";         // l'ultima chiave disegnata
    public string DxVista = "";
    public List<string> PannelloGiu;   // riquadro sotto la lista in mezzo
    public List<string> Armatura;      // il montaggio disegnato, sotto la finestra
    public string PanPie = "";         // riga in fondo al riquadro di destra
    public string PanSxPie = "";       // riga in fondo al riquadro di sinistra
    public List<string> Rig;           // le caselle dell'armatura sullo schermo
    public int RigSel = -1;            // -1 = il cursore non e' sull'armatura

    public TMenu(string title, int parent)
    {
        Title = title;
        TitleIt = title;
        Parent = parent;
        Items = new List<TItem>();
        Sel = 0;
        Top = 0;
        IconRows = false;
        HaSotto = false;
        Nota = "";
        Insegna = "";
        Insegne = false;
        Blocco = null;
    }
}

public class TrainerPesca : Script
{
    // ---------- controlli (gruppo 2 = frontend: vale sia tastiera che pad) ----------
    const int C_UP     = 172;
    const int C_DOWN   = 173;
    const int C_LEFT   = 174;
    const int C_RIGHT  = 175;
    const int C_ACCEPT = 176;
    const int C_CANCEL = 177;
    // 176 = A (RDOWN), 177 = B (RRIGHT), 178 = Y (RUP), 179 = X (RLEFT).
    const int C_X      = 179;   // il secondo tasto delle righe dei riquadri
    const int C_Y      = 178;   // il terzo: butta via

    // pad: RB tenuto + DPAD-GIU apre/chiude
    const int C_PAD_RB = 183;

    // ---------- layout ----------
    const float MW = 300f;                  // larghezza della finestra
    static float MX = (1280f - MW) * 0.5f;  // x menu (spostabile da Impostazioni)
    static float MY = 4f;                   // y menu (spostabile da Impostazioni)
    bool spostaFinestra = false;            // modalita' sposta-finestra attiva
    const float HEAD_H = 18f;               // header attaccato al menu
    const float FOOT_H = 14f;
    const float ITEM_H = 18f;  // altezza voce
    const int   MAX_VIS = 12;  // voci visibili

    static readonly string[] DAYS_EN = new string[] {
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    };
    static readonly string[] DAYS_IT = new string[] {
        "Domenica", "Lunedi", "Martedi", "Mercoledi", "Giovedi", "Venerdi", "Sabato"
    };

    // ---------- voci con effetto continuo ----------
    TItem tGod, tNeverWanted, tStamina, tJump, tBreath, tFastRun;
    TItem tSpawnInside, tDelPrev, tMaxMods, tLang, tVehGod, tOnWater, tTopBar;
    TItem tMass, tLimiter, tUnits, tDash;
    bool vehGodWas = false;
    int limiterVeh = 0;
    bool evCurrent = false;      // il mezzo su cui sei e' elettrico
    bool pumping = false;        // pompa o colonnina in funzione
    float pumpDebt = 0f;         // spiccioli non ancora scalati

    // ---------- radio ----------
    TItem tRadio, tRadioMobile;
    List<string> radioName = new List<string>();   // nomi interni delle stazioni
    int radioNext = 0;
    int radioLastVeh = 0;

    // ---------- mod esterne ----------
    // Ogni mod e' un file .cs dentro scripts\<categoria>\<nome>\.
    // Il trainer non sa cosa facciano: legge le cartelle, mette un
    // interruttore per ognuna e scrive mods.ini. Il mod legge quel file
    // e si accende o si spegne da solo.
    static readonly string SCRIPTS_DIR = "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V Enhanced\\scripts";
    // le linee dell'autobus: Los Santos Transit
    static readonly string[] LINEE = new string[] { "A1", "B2", "C3", "M4" };

    // il colore di ogni linea, lo stesso che usa la mod sulla mappa
    static readonly int[,] LINEE_RGB = new int[,] {
        { 90, 170, 255 }, { 120, 220, 150 }, { 245, 210, 90 }, { 245, 130, 130 } };

    static readonly string[] MOD_CAT_DIR = new string[] { "Lavori", "Attivita", "Minigiochi", "Missioni" };
    static readonly string[] MOD_CAT_EN  = new string[] { "Jobs", "Activities", "Minigames", "Missions" };
    static readonly string[] MOD_CAT_IT  = new string[] { "Lavori", "Attivita'", "Minigiochi", "Missioni" };
    List<string> modId = new List<string>();      // "lavori/fuzer"
    List<TItem> modItem = new List<TItem>();
    int modFirstId = 700;
    static readonly float[] MASS_MULT = new float[] {
        1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f, 4.5f, 5f, 5.5f, 6f, 6.5f, 7f, 7.5f, 8f, 8.5f,
        9f, 9.5f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, 100f, 200f, 300f, 400f,
        500f, 600f, 700f, 800f, 900f, 1000f, 2000f, 3000f, 4000f, 5000f, 6000f, 7000f,
        8000f, 9000f, 10000f, 100000f };
    TItem tFreezeTime, tFreezeWeather, tBlackout, tHour, tMinute, tWeather;
    TItem tNoWater;
    TItem tTimeSpeed;
    bool clockTaken = false;
    int nextGameMin = 0;
    TItem tAutoTp;
    int mPlaces = -1;
    List<string> placeRaw = new List<string>();
    TItem tInfAmmo, tNoReload;
    TItem tInvisible, tNoRagdoll, tExplosiveAmmo, tFireAmmo, tExplosiveMelee, tSeatbelt;
    TItem tIgnored, tWalkWater, tSpecial, tAutoClean, tFastSwim;
    TItem tAutoRepair, tAutoFlip, tKeepOn, tMuteSiren;
    TItem tNightVision, tHeatVision, tFullMap, tHideHud, tWind, tPuddles;

    // ---- roba da provare (menu TEST) ----
    TItem tMaxPop, tNoCops, tManyParked, tNoTrains, tNoBoats, tNoGarbage;
    TItem tArmedPeds, tRiot, tAllHateMe, tAllFlee, tPedsGod, tPedsSniper;
    TItem tHotDrivers, tSlowDrivers, tGravity, tTimeScale, tMaxWanted;
    TItem tNoHeli, tNoSwat, tNoRoadBlock;
    TItem tMultiShot, tRapidFire, tBulletTime;
    TItem tDmgGun, tDmgMelee, tDefGun, tDefMelee, tRunSpeed, tPugni, tNoBotte;

    // quanto forte spedisci via quello che colpisci a mani nude
    // velocita' di lancio in metri al secondo: 12 sono un paio di metri,
    // 25 una decina, 40 una ventina abbondante
    static readonly float[] PUGNI = new float[] {
        0f, 6f, 10f, 14f, 18f, 22f, 26f, 30f, 35f, 40f, 50f, 65f };
    static readonly string[] PUGNI_TXT = new string[] {
        "Off", "6", "10", "14", "18", "22", "26", "30", "35", "40", "50", "65" };

    // il native della corsa si ferma a 1.49: oltre si spinge a mano
    static readonly float[] CORSA = new float[] { 1f, 1.25f, 1.49f, 2f, 3f, 4f, 6f, 10f };
    static readonly string[] CORSA_TXT = new string[] {
        "x1", "x1.25", "x1.49", "x2", "x3", "x4", "x6", "x10" };

    static readonly float[] MOLTIPL = new float[] {
        0f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 25f, 50f, 100f, 500f, 1000f, 10000f, 100000f };
    static readonly string[] MOLTIPL_TXT = new string[] {
        "x0", "x0.25", "x0.5", "x1", "x2", "x5", "x10", "x25", "x50",
        "x100", "x500", "x1000", "x10000", "x100000" };
    List<int> testFatti = new List<int>();   // ped gia' sistemati
    TItem tTraffic, tPeds;

    Vector3 lastAutoTp = Vector3.Zero;
    int autoTpNext = 0;

    // 0 = English, 1 = Italiano
    int lang = 0;

    // ---------- veicoli ----------
    const string DATA_DIR = "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V Enhanced\\scripts\\Trainer";
    int mVehicles = -1;
    int mSpawnOpts = -1;
    bool vehBuilt = false;
    Vehicle lastSpawned = null;

    // ---------- veicoli salvati ----------
    TItem tPersist;
    HashSet<int> addonSuper = new HashSet<int>();   // add-on segnati Super in addons.txt
    int mMyVeh = -1;
    int mWard = -1;
    int mModShop = -1;
    int mBody = -1, mMech = -1, mWheels = -1, mLights = -1, mExtras = -1;
    List<string> pvRaw = new List<string>();
    List<int> pvBlip = new List<int>();

    // ---------- benzina ----------
    TItem tFuel, tOilWear, tOdoOn;

    // ---------- limiti di velocita' ----------
    TItem tSpeedLimit, tLimCity, tLimHwy, tLimDirt;
    int speedCheckNext = 0;
    int roadKind = 0;          // 0 = citta', 1 = autostrada, 2 = sterrato/montagna
    float overSince = -1f;
    int fineCooldown = 0;
    int beepNext = 0;
    const int SPEED_MARGIN = 10;      // tolleranza in km/h
    const int OVER_SECONDS = 10;      // quanto puoi restare nel margine prima della multa
    const float PCT_PER_METER = 0.0014286f;  // un pieno ~ 70 km
    const float COST_PER_PCT = 0.9f;

    // ---------- veicoli elettrici ----------
    // La proprieta' IsElectricVehicle di SHVDN in questa build risponde
    // sempre "no" (verificato sonda alla mano: la Voltic dava no), e il
    // volume del serbatoio non e' affidabile (la Neon dichiara 65 come una
    // benzina). Quindi la lista e' nostra: noiosa una volta, precisa sempre.
    // Solo full electric. Dilettante, Khamelion e Imorgon NON stanno qui:
    // sono ibride e vivono nella lista HYBRID.
    static readonly string[] ELECTRIC = new string[] {
        "voltic", "voltic2", "surge", "cyclone", "cyclone2", "tezeract",
        "neon", "raiden", "virtue", "powersurge", "omnisegt",
        "caddy", "caddy2", "caddy3", "airtug", "docktug", "forklift",
        "rcbandito", "minitank",
        "models"                      // Tesla Model S addon
    };

    // ---------- veicoli ibridi ----------
    // Motore termico piu' spinta elettrica: benzina nel serbatoio, ma la
    // spia della terza posizione e' ECO invece della spia motore.
    static readonly string[] HYBRID = new string[] {
        "dilettante", "dilettante2", "khamelion", "imorgon", "t20",
        "turismor", "pfister811", "etr1", "viseris", "osiris"
    };

    // la batteria rende di piu' della benzina: ~100 km invece di 75
    const float PCT_PER_METER_EV = 0.0008333f;   // una carica ~ 120 km
    const float PCT_PER_METER_HY = 0.0010526f;   // un pieno ibrido ~ 95 km
    // e ricaricare costa meno che fare il pieno
    const float COST_PER_PCT_EV = 0.35f;
    // il rifornimento richiede tempo, come nella vita:
    // pieno di benzina in 30 secondi, ricarica completa in 60
    // pieno di benzina in 12 secondi, ricarica completa in 60: la colonnina
    // deve farsi sentire come piu' lenta
    const float PUMP_PER_SEC = 100f / 12f;
    const float CHARGE_PER_SEC = 100f / 60f;
    const float GAS_RADIUS = 20f;

    static readonly float[] GX = new float[] {
        49.4187f, 263.894f, 1039.958f, 1207.260f, 2539.685f, 2679.858f, 2005.055f,
        1687.156f, 1701.314f, 179.857f, -94.4619f, -2554.996f, -1800.375f, -1437.622f,
        -2096.243f, -724.619f, -526.019f, -70.2148f, 265.648f, 819.653f, 1208.951f,
        1181.381f, 620.843f, 2581.321f, 176.631f
    };
    static readonly float[] GY = new float[] {
        2778.793f, 2606.463f, 2671.134f, 2660.175f, 2594.192f, 3263.946f, 3773.887f,
        4929.392f, 6416.028f, 6602.839f, 6419.594f, 2334.40f, 803.661f, -276.747f,
        -320.286f, -935.1631f, -1211.003f, -1761.792f, -1261.309f, -1028.846f, -1402.567f,
        -330.847f, 269.100f, 362.039f, -1562.025f
    };
    static readonly float[] GZ = new float[] {
        58.043f, 44.983f, 39.550f, 37.899f, 37.944f, 55.240f, 32.403f,
        42.078f, 32.763f, 31.868f, 31.489f, 33.078f, 138.651f, 46.207f,
        13.168f, 19.213f, 18.184f, 29.534f, 29.292f, 26.403f, 35.224f,
        69.316f, 103.089f, 108.468f, 29.263f
    };
    int[] gasBlips = null;

    // ---------- fame e sete ----------
    TItem tBody;
    float hunger = 100f;
    float thirst = 100f;
    int lastBodyHour = -1;
    int starveNext = 0;

    // market 24/7 accessibili
    static readonly float[] MKX = new float[] {
        373.55f, 25.75f, -3038.71f, -3241.47f, 547.79f,
        1961.48f, 2678.91f, 1729.21f, -2519.23f
    };
    static readonly float[] MKY = new float[] {
        325.56f, -1346.94f, 585.95f, 1001.14f, 2671.79f,
        3740.69f, 3280.67f, 6414.13f, 2316.93f
    };
    static readonly float[] MKZ = new float[] {
        103.56f, 29.49f, 7.90f, 12.83f, 42.16f,
        32.34f, 55.24f, 35.04f, 33.41f
    };
    int[] mkBlips = null;


    List<string> tankKey = new List<string>();
    List<float> tankVal = new List<float>();

    // ---------- olio motore ----------
    // Cala coi chilometri, non col tempo: un tagliando ogni ~300 km.
    // Sotto il 15% il motore comincia a soffrire davvero.
    List<string> oilKey = new List<string>();
    List<float> oilVal = new List<float>();
    string curOilKey = "";
    float oil = 100f;          // quanto manca al tagliando, in percentuale
    float servM = 0f;         // metri dell'odometro all'ultimo tagliando
    int oilWarnAt = 0;
    int oilSlowVeh = 0;      // veicolo a cui e' stata tolta potenza

    // ---------- icone del cruscotto ----------
    // Sono PNG normali in scripts\Trainer\icone\: CustomSprite li disegna
    // direttamente dal disco, senza doverli impacchettare nel gioco.
    // Se qualcosa non va si spengono da sole e il cruscotto resta come prima.
    bool iconsOk = true;
    bool iconsTried = false;
    bool dictSaid = false;
    // tagliando: benzina ogni 1000 km, ibride 1500, elettriche 2000
    const float SERVICE_M_PETROL = 1000000f;
    const float SERVICE_M_CAMION = 2500000f;   // camion grossi: ogni 2500 km
    const float SERVICE_M_HYBRID = 1500000f;
    const float SERVICE_M_EV = 2000000f;
    const int OIL_SERVICE_COST = 120;   // auto normali

    // La manutenzione costa secondo il mezzo: un camion o una supercar
    // in officina non pagano come un'utilitaria.
    int CostoManutenzione(Vehicle v)
    {
        if (v == null || !v.Exists()) return OIL_SERVICE_COST;
        if (ESuper(v)) return 450;                                   // supercar
        VehicleClass c = v.ClassType;
        if (c == VehicleClass.Commercial || c == VehicleClass.Industrial) return 350;  // camion grossi
        if (c == VehicleClass.SportsClassics) return 300;            // classiche: pezzi rari
        if (c == VehicleClass.Sports || c == VehicleClass.Coupes) return 220;          // sportive e lusso
        if (c == VehicleClass.Motorcycles) return 70;                // moto
        return OIL_SERVICE_COST;
    }
    const int COST_WASH = 20;       // lavaggio al distributore
    const int COST_REPAIR = 350;    // riparazione al distributore

    // ---------- odometro ----------
    // I chilometri li conta il trainer, uno per veicolo: il contachilometri
    // del gioco non esiste. Serve anche a sapere quanto manca al tagliando.
    List<string> odoKey = new List<string>();
    List<float> odoVal = new List<float>();      // metri
    string curOdoKey = "";
    int fermoDa = 0;          // da quando la macchina e' ferma (GameTime)
    int frecciaSxFino = 0;    // freccia automatica: resta accesa fino a...
    int frecceSxWas = -1;     // ultimo stato mandato al gioco
    int frecceDxWas = -1;
    int frecceVeh = 0;        // su quale veicolo
    int ticId = -1;           // suono del tic, con id proprio
    int frecceRepeatAt = 0;   // ogni quanto si ribadisce lo stato al gioco
    int ticAt = 0;            // ultimo tic del suono
    bool frecceFase = true;   // fase del quadratino sul cruscotto
    int frecceSxPend = -1;    // stato in attesa di stabilizzarsi
    int frecceDxPend = -1;
    int freccePendAt = 0;
    int frecceStartAt = 0;    // quando e' partito il lampeggio
    bool inRetro = false;     // manovra in retromarcia in corso

    void StopTic()
    {
        if (ticId < 0) return;
        Function.Call(Hash.STOP_SOUND, ticId);
        Function.Call(Hash.RELEASE_SOUND_ID, ticId);
        ticId = -1;
    }
    int frecciaDxFino = 0;
    bool hyHot = false;       // ibrida: termico entrato, resta finche' non ti fermi
    float odoM = 0f;
    int odoSaveAt = 0;
    float fuel = 100f;
    string curTankKey = "";
    int fuelHelpAt = 0;
    TItem tBlips;
    int trackedIdx = -1;
    int pendingRemove = -1;   // rimozione differita: mai dentro il frame del menu
    bool pendingClear = false;
    bool wasInVeh = false;
    Vehicle lastDriven = null;

    // ---------- stato ----------
    bool open = false;
    bool xGiu = false;          // X era gia' premuto il giro prima
    bool yGiu = false;
    int cur = 0;                       // menu corrente
    List<TMenu> menus = new List<TMenu>();
    bool f5Last = false;
    bool comboLast = false;
    bool rbHeld = false;
    int navNext = 0;                   // anti-ripetizione navigazione

    public TrainerPesca()
    {
        BuildMenus();

        // all'avvio l'orologio del gioco torna sempre libero: se una mod
        // (o una sessione precedente) lo ha lasciato in pausa o in override,
        // il tempo restava fermo per sempre
        Function.Call(Hash.NETWORK_CLEAR_CLOCK_TIME_OVERRIDE);
        Function.Call(Hash.PAUSE_CLOCK, false);

        // I blip del trainer alla chiusura vengono solo nascosti (cancellarli
        // in quel momento fa crashare il gioco), quindi a ogni Insert ne
        // restavano in giro decine di invisibili. Il gioco ha un numero
        // massimo di blip: esaurito quello, nessuna mod riesce piu' a
        // crearne (il cliente di Fuzer non compariva). All'avvio, quando
        // cancellare e' sicuro, si tolgono i nostri blip nascosti rimasti.
        PulisciBlipNascosti();

        Tick += OnTick;
        KeyDown += OnKeyDown;
        Aborted += OnAborted;
        Interval = 0;
    }

    // ============================================================
    //  QUI SI COSTRUISCE IL MENU - aggiungere voci qui
    // ============================================================
    int NewMenu(string title, int parent)
    {
        menus.Add(new TMenu(title, parent));
        return menus.Count - 1;
    }

    // spezza il nome interno in parole: CarbineRifleMk2 -> "Carbine Rifle Mk2"
    string PrettyName(string raw)
    {
        StringBuilder sb = new StringBuilder();
        int i;
        for (i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(raw[i - 1]))
            {
                sb.Append(' ');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    int WeaponGroup(string n)
    {
        string q = n.ToLower();

        if (q.Contains("pistol") || q.Contains("revolver") || q.Contains("snspistol")
            || q.Contains("vintagepistol") || q.Contains("marksmanpistol")
            || q.Contains("stungun") || q.Contains("flaregun") || q.Contains("raypistol")) return 1;

        if (q.Contains("smg") || q.Contains("microsmg") || q.Contains("machinepistol")
            || q.Contains("minismg") || q.Contains("assaultsmg") || q.Contains("combatpdw")) return 2;

        if (q.Contains("shotgun") || q.Contains("bullpupshotgun") || q.Contains("musket")) return 3;

        if (q.Contains("rifle") || q.Contains("carbine") || q.Contains("bullpuprifle")
            || q.Contains("compactrifle") || q.Contains("militaryrifle")) return 4;

        if (q.Contains("sniper") || q.Contains("marksmanrifle") || q.Contains("heavysniper")) return 5;

        if (q.Contains("mg") || q.Contains("minigun") || q.Contains("rpg") || q.Contains("grenadelauncher")
            || q.Contains("railgun") || q.Contains("firework") || q.Contains("homing")
            || q.Contains("compactlauncher") || q.Contains("raycarbine")) return 6;

        if (q.Contains("grenade") || q.Contains("molotov") || q.Contains("stickybomb")
            || q.Contains("bzgas") || q.Contains("smokegrenade") || q.Contains("pipebomb")
            || q.Contains("snowball") || q.Contains("ball") || q.Contains("flare")
            || q.Contains("proximitymine") || q.Contains("petrolcan") || q.Contains("fireextinguisher")
            || q.Contains("parachute") || q.Contains("jerrycan")) return 7;

        if (q.Contains("knife") || q.Contains("bat") || q.Contains("crowbar") || q.Contains("golfclub")
            || q.Contains("hammer") || q.Contains("hatchet") || q.Contains("knuckle")
            || q.Contains("machete") || q.Contains("flashlight") || q.Contains("dagger")
            || q.Contains("bottle") || q.Contains("wrench") || q.Contains("poolcue")
            || q.Contains("nightstick") || q.Contains("battleaxe") || q.Contains("stone")) return 0;

        return 8;
    }



    void BuildWeaponList(int parent)
    {
        string[] gEn = new string[] { "Melee", "Pistols", "SMG", "Shotguns", "Rifles",
                                      "Sniper", "Heavy", "Throwables", "Other" };
        string[] gIt = new string[] { "Corpo a corpo", "Pistole", "Mitragliette", "Fucili a pompa",
                                      "Fucili d'assalto", "Cecchino", "Pesanti", "Lanciabili", "Altro" };

        int[] menu = new int[gEn.Length];
        int i;
        for (i = 0; i < menu.Length; i++) menu[i] = -1;

        Array vals = Enum.GetValues(typeof(WeaponHash));
        for (i = 0; i < vals.Length; i++)
        {
            WeaponHash w = (WeaponHash)vals.GetValue(i);
            if (w == WeaponHash.Unarmed) continue;

            string raw = w.ToString();
            int g = WeaponGroup(raw);

            if (menu[g] < 0)
            {
                menu[g] = NewMenu(gEn[g].ToUpper(), gIt[g].ToUpper(), parent);
                TItem sub = AddSub(parent, gEn[g], gIt[g], menu[g]);
                int ci = g % (PASTEL.Length / 3);
                sub.Cr = PASTEL[ci, 0]; sub.Cg = PASTEL[ci, 1]; sub.Cb = PASTEL[ci, 2];
                sub.Tinted = true;
            }

            string nice = PrettyName(raw);
            TItem it = AddAction(menu[g], nice, nice, 505);
            it.Data = ((int)w).ToString();
        }
    }

    void BuildMenus()
    {
        int root = NewMenu("PESCA", -1);

        // il trainer vero e proprio e' una voce come le altre: dentro ci
        // finiscono giocatore, veicoli, armi, mondo, teleport e varie
        int mTrainer = NewMenu("TRAINER", "TRAINER", root);
        TItem trSub = AddSub(root, "Trainer", "Trainer", mTrainer);
        trSub.Cr = PASTEL[0, 0]; trSub.Cg = PASTEL[0, 1]; trSub.Cb = PASTEL[0, 2];
        trSub.Tinted = true;

        // ---------------- GIOCATORE ----------------
        int mPlayer = NewMenu("PLAYER", "GIOCATORE", mTrainer);
        AddSub(mTrainer, "Player", "Giocatore", mPlayer);

        AddAction(mPlayer, "Heal", "Cura completa", 100);
        AddAction(mPlayer, "Full armour", "Armatura piena", 101);
        tGod         = AddToggle(mPlayer, "Godmode", "Invincibilita", 102, false);
        tNeverWanted = AddToggle(mPlayer, "Never wanted", "Mai ricercato", 103, false);
        AddNumber(mPlayer, "Wanted level", "Livello ricercato", 104, 0, 0, 5, 1);
        tStamina     = AddToggle(mPlayer, "Infinite stamina", "Fiato infinito", 105, false);
        tBreath      = AddToggle(mPlayer, "Infinite breath", "Respiro infinito", 106, false);
        tJump        = AddToggle(mPlayer, "Super jump", "Super salto", 107, false);
        tFastRun     = AddToggle(mPlayer, "Fast run", "Corsa veloce", 108, false);
        tInvisible      = AddToggle(mPlayer, "Invisible", "Invisibile", 111, false);
        tNoRagdoll      = AddToggle(mPlayer, "No ragdoll", "Niente ragdoll", 112, false);
        tSeatbelt       = AddToggle(mPlayer, "Seatbelt", "Cintura di sicurezza", 113, false);
        tExplosiveAmmo  = AddToggle(mPlayer, "Explosive ammo", "Proiettili esplosivi", 114, false);
        tFireAmmo       = AddToggle(mPlayer, "Fire ammo", "Proiettili incendiari", 115, false);
        tExplosiveMelee = AddToggle(mPlayer, "Explosive melee", "Pugno esplosivo", 116, false);

        AddHeader(mPlayer, "- RADIO -", "- RADIO -", 2);
        BuildRadioItems(mPlayer);
        tIgnored    = AddToggle(mPlayer, "Ignored by everyone", "Ignorato da tutti", 117, false);
        tWalkWater  = AddToggle(mPlayer, "Walk underwater", "Cammina sott'acqua", 118, false);
        tSpecial    = AddToggle(mPlayer, "Infinite special ability",
                                "Abilita' speciale infinita", 119, false);
        tAutoClean  = AddToggle(mPlayer, "Always clean", "Sempre pulito", 120, false);
        tFastSwim   = AddToggle(mPlayer, "Fast swim", "Nuoto veloce", 121, false);

        // guardaroba: si riempie quando entri, sul personaggio di adesso
        mWard = NewMenu("WARDROBE", "GUARDAROBA", mPlayer);
        TItem wsub = AddSub(mPlayer, "Wardrobe", "Guardaroba", mWard);
        wsub.Id = 280;

        TItem money = AddList(mPlayer, "Money", "Soldi", 110,
                new string[] { "-100.000", "-10.000", "-1.000", "-100", "-10",
                               "0",
                               "+10", "+100", "+1.000", "+10.000", "+100.000" }, 5);
        money.SignedValue = true;

        // ---------------- VITA REALE (sotto Giocatore) ----------------
        int mReal = NewMenu("REAL LIFE", "VITA REALE", mPlayer);
        TItem rl = AddSub(mPlayer, "Real life", "Vita reale", mReal);
        rl.Cr = PASTEL[2, 0]; rl.Cg = PASTEL[2, 1]; rl.Cb = PASTEL[2, 2]; rl.Tinted = true;

        AddHeader(mReal, "- VEHICLE -", "- VEICOLO -", 3);
        tFuel = AddToggle(mReal, "Fuel / energy consumption",
                          "Consumo benzina ed energia", 260, false);
        tOilWear = AddToggle(mReal, "Wear (maintenance)",
                             "Usura (manutenzione)", 287, false);
        tOdoOn = AddToggle(mReal, "Odometer", "Odometro", 288, true);

        AddHeader(mReal, "- PLAYER -", "- GIOCATORE -", 2);
        tBody = AddToggle(mReal, "Hunger & thirst", "Fame e sete", 261, false);
        AddAction(mReal, "Eat something ($12)", "Mangia qualcosa ($12)", 266);
        AddAction(mReal, "Drink something ($3)", "Bevi qualcosa ($3)", 267);

        AddHeader(mReal, "- ROAD -", "- STRADA -", 5);
        tSpeedLimit = AddToggle(mReal, "Speed limits", "Limiti di velocita", 262, false);
        tLimCity = AddNumber(mReal, "City limit", "Limite citta", 263, 80, 30, 130, 10);
        tLimHwy  = AddNumber(mReal, "Highway limit", "Limite autostrada", 264, 140, 60, 200, 10);
        tLimDirt = AddNumber(mReal, "Dirt road limit", "Limite sterrato", 265, 65, 20, 120, 5);

        // ---------------- VEHICLES ----------------
        mVehicles = NewMenu("VEHICLES", "VEICOLI", mTrainer);
        AddSub(mTrainer, "Vehicles", "Veicoli", mVehicles);

        int mOpts = NewMenu("SPAWN OPTIONS", "OPZIONI SPAWN", mVehicles);
        tSpawnInside = AddToggle(mOpts, "Spawn inside", "Spawn dentro il veicolo", 201, true);
        tDelPrev     = AddToggle(mOpts, "Delete previous", "Elimina il precedente", 202, false);
        tMaxMods     = AddToggle(mOpts, "Max upgrades", "Elaborazione massima", 203, false);
        mSpawnOpts   = mOpts;
        // il contenuto di VEHICLES viene composto in BuildVehicleClasses(): prima le classi, poi le azioni

        // ---------------- WEAPONS ----------------
        int mWeap = NewMenu("WEAPONS", "ARMI", mTrainer);
        AddSub(mTrainer, "Weapons", "Armi", mWeap);

        AddAction(mWeap, "Give all weapons", "Dammi tutte le armi", 500);
        AddAction(mWeap, "Refill ammo", "Ricarica le munizioni", 501);
        tInfAmmo  = AddToggle(mWeap, "Infinite ammo", "Munizioni infinite", 502, false);
        tNoReload = AddToggle(mWeap, "No reload", "Non ricaricare mai", 503, false);
        AddAction(mWeap, "Remove all weapons", "Togli tutte le armi", 504);

        AddHeader(mWeap, "- CATEGORIES -", "- CATEGORIE -", 1);
        BuildWeaponList(mWeap);

        // ---------------- WORLD ----------------
        int mWorld = NewMenu("WORLD", "MONDO", mTrainer);
        AddSub(mTrainer, "World", "Mondo", mWorld);

        AddHeader(mWorld, "- WEATHER -", "- METEO -", 1);
        tWeather = AddList(mWorld, "Weather", "Meteo", 400, new string[] {
            "Extra sunny", "Clear", "Clouds", "Smog", "Foggy", "Overcast",
            "Rain", "Thunder", "Clearing", "Neutral", "Snow", "Blizzard",
            "Snow light", "Christmas", "Halloween" }, 1);
        tFreezeWeather = AddToggle(mWorld, "Freeze weather", "Blocca il meteo", 401, false);
        tBlackout = AddToggle(mWorld, "Blackout", "Blackout", 409, false);
        tNoWater = AddToggle(mWorld, "Remove ocean water", "Togli l'acqua dagli oceani",
                             413, false);

        AddHeader(mWorld, "- TIME -", "- ORA -", 5);
        tHour   = AddNumber(mWorld, "Hour", "Ora", 402, 12, 0, 23, 1);
        tMinute = AddNumber(mWorld, "Minutes", "Minuti", 403, 0, 0, 59, 5);
        tFreezeTime = AddToggle(mWorld, "Freeze time", "Blocca l'ora", 404, false);
        // VELOCITA DEL TEMPO.
        // Di serie un minuto di gioco passa ogni 2 secondi veri. Sotto x1
        // il tempo RALLENTA (x0.25 = un minuto ogni 8 secondi), sopra corre.
        // Prima la lista arrivava a x100000 ma il codice leggeva solo le
        // prime quattro voci: tutto il resto non faceva niente, e rallentare
        // non si poteva.
        tTimeSpeed = AddList(mWorld, "Time speed", "Velocita del tempo", 412,
                             new string[] {
                               "x0.1", "x0.25", "x0.5", "x0.75",
                               "x1",
                               "x1.5", "x2", "x3", "x4", "x6", "x8",
                               "x12", "x16", "x24", "x32", "x60" }, 4);
        AddAction(mWorld, "Dawn", "Alba", 405);
        AddAction(mWorld, "Midday", "Mezzogiorno", 406);
        AddAction(mWorld, "Sunset", "Tramonto", 407);
        AddAction(mWorld, "Night", "Notte", 408);

        AddHeader(mWorld, "- VISION -", "- VISUALE -", 2);
        tNightVision = AddToggle(mWorld, "Night vision", "Visione notturna", 414, false);
        tHeatVision  = AddToggle(mWorld, "Thermal vision", "Visione termica", 415, false);
        tFullMap     = AddToggle(mWorld, "Reveal whole map", "Mappa tutta rivelata", 416, false);
        tHideHud     = AddToggle(mWorld, "Hide HUD", "Nascondi HUD", 417, false);
        AddAction(mWorld, "Unlock restricted areas", "Sblocca le aree militari", 420);
        tWind    = AddNumber(mWorld, "Wind", "Vento", 418, 0, 0, 30, 1);
        tPuddles = AddNumber(mWorld, "Rain puddles", "Pozzanghere", 419, 0, 0, 10, 1);

        AddHeader(mWorld, "- DENSITY -", "- DENSITA' -", 4);
        tTraffic = AddNumber(mWorld, "Traffic", "Traffico", 410, 100, 0, 100, 10);
        tPeds    = AddNumber(mWorld, "Pedestrians", "Pedoni", 411, 100, 0, 100, 10);

        // ---------------- TELEPORT ----------------
        int mTp = NewMenu("TELEPORT", "TELEPORT", mTrainer);
        AddSub(mTrainer, "Teleport", "Teleport", mTp);
        AddAction(mTp, "To waypoint", "Al waypoint", 300);
        AddAction(mTp, "To objective", "All'obiettivo", 301);
        AddAction(mTp, "Save current spot...", "Salva questo punto...", 303);

        mPlaces = NewMenu("MY PLACES", "I MIEI PUNTI", mTp);
        AddSub(mTp, "My places", "I miei punti", mPlaces);
        LoadPlaces();
        BuildPlaces();

        int mKnown = NewMenu("KNOWN PLACES", "LUOGHI NOTI", mTp);
        AddSub(mTp, "Known places", "Luoghi noti", mKnown);
        BuildKnownPlaces(mKnown);

        tAutoTp = AddList(mTp, "Auto teleport", "Teleport automatico", 302,
                          new string[] { "Off", "Waypoint", "Objective" }, 0);

        // ---------------- VARIE ----------------
        int mTest = NewMenu("VARIOUS", "VARIE", mTrainer);
        AddSub(mTrainer, "Various", "Varie", mTest);

        AddHeader(mTest, "- POPULATION -", "- POPOLAZIONE -", 1);
        tMaxPop     = AddToggle(mTest, "Max population", "Popolazione al massimo", 700, false);
        tManyParked = AddToggle(mTest, "More parked cars", "Piu' auto parcheggiate", 702, false);
        tNoCops     = AddToggle(mTest, "No random cops", "Niente poliziotti", 701, false);
        tNoHeli     = AddToggle(mTest, "No police helicopters",
                                "Niente elicotteri della polizia", 719, false);
        tNoSwat     = AddToggle(mTest, "No SWAT / army", "Niente SWAT ed esercito", 720, false);
        tNoRoadBlock = AddToggle(mTest, "No roadblocks", "Niente posti di blocco", 721, false);
        tNoTrains   = AddToggle(mTest, "No trains", "Niente treni", 703, false);
        tNoBoats    = AddToggle(mTest, "No boats", "Niente barche", 704, false);
        tNoGarbage  = AddToggle(mTest, "No garbage trucks", "Niente camion spazzatura", 705, false);

        AddHeader(mTest, "- PEDS -", "- PEDONI -", 3);
        tArmedPeds   = AddToggle(mTest, "Armed peds", "Pedoni armati", 706, false);
        tRiot        = AddToggle(mTest, "Riot", "Rivolta", 707, false);
        tAllHateMe   = AddToggle(mTest, "Everyone attacks me", "Tutti mi attaccano", 708, false);
        tAllFlee     = AddToggle(mTest, "Everyone runs away", "Tutti scappano", 709, false);
        tPedsGod     = AddToggle(mTest, "Invincible peds", "Pedoni immortali", 710, false);
        tPedsSniper  = AddToggle(mTest, "Deadly aim", "Pedoni cecchini", 711, false);

        AddHeader(mTest, "- DRIVING -", "- GUIDA -", 5);
        tHotDrivers  = AddToggle(mTest, "Aggressive drivers", "Guida aggressiva", 712, false);
        tSlowDrivers = AddToggle(mTest, "Clumsy drivers", "Guidatori imbranati", 713, false);

        AddHeader(mTest, "- WEAPONS -", "- ARMI -", 4);
        tMultiShot  = AddToggle(mTest, "Multi shot (5 at once)",
                                "Colpi multipli (5 insieme)", 722, false);
        tRapidFire  = AddToggle(mTest, "Rapid fire", "Fuoco rapido", 723, false);
        tBulletTime = AddToggle(mTest, "Bullet time (while aiming)",
                                "Bullet time (mentre miri)", 724, false);
        tDmgGun   = AddList(mTest, "Bullet damage", "Danno dei proiettili", 725,
                            MOLTIPL_TXT, 3);
        tDmgMelee = AddList(mTest, "Melee strength", "Forza nel corpo a corpo", 726,
                            MOLTIPL_TXT, 3);
        tDefGun   = AddList(mTest, "Bullet resistance", "Resistenza ai proiettili", 727,
                            MOLTIPL_TXT, 3);
        tDefMelee = AddList(mTest, "Melee resistance", "Resistenza ai pugni", 728,
                            MOLTIPL_TXT, 3);

        tRunSpeed = AddList(mTest, "Run speed", "Velocita' di corsa", 729, CORSA_TXT, 0);
        tPugni    = AddList(mTest, "Punch power", "Forza dei pugni", 730, PUGNI_TXT, 0);
        tNoBotte  = AddToggle(mTest, "Immune to punches", "Non senti i pugni", 731, false);

        AddHeader(mTest, "- WORLD -", "- MONDO -", 2);
        tGravity   = AddList(mTest, "Gravity", "Gravita'", 714,
                             new string[] { "Normale", "Bassa", "Lunare", "Zero" }, 0);
        tTimeScale = AddList(mTest, "Game speed", "Velocita' del gioco", 715,
                             new string[] { "x1", "x0.7", "x0.4", "x0.2" }, 0);
        tMaxWanted = AddNumber(mTest, "Max wanted level", "Ricercato massimo", 716, 5, 0, 5, 1);
        AddAction(mTest, "Blow up nearby cars", "Fai saltare le auto vicine", 718);

        // ---------------- DEVELOPER ----------------
        // ---------------- MODS ----------------
        BuildModsMenu(root);

        // ---------------- SETTINGS (sempre ultima voce) ----------------
        int mSet = NewMenu("SETTINGS", "IMPOSTAZIONI", root);
        AddSub(root, "Settings", "Impostazioni", mSet);
        tLang   = AddList(mSet, "Language", "Lingua", 900, new string[] { "English", "Italiano" }, 0);
        tTopBar = AddToggle(mSet, "Header", "Header", 901, true);
        AddAction(mSet, "Move the window", "Sposta la finestra", 905);

        LoadConfig();
        CaricaFinestra();
        if (tLang != null) lang = tLang.Sel;

        SoloPesca();
    }

    // SOLO LA PESCA.
    // Il motore resta intero - le voci del trainer vengono costruite
    // lo stesso, se no meta' del codice che gira a ogni tick andrebbe a
    // cercare roba che non c'e' - ma nella prima pagina ci finisce
    // soltanto la pesca e le impostazioni. Tutto il resto esiste e non
    // si raggiunge.
    int menuPesca = -1;
    string dirPesca = "";
    List<TItem> impTrainer = new List<TItem>();

    void SoloPesca()
    {
        int i, iP = -1, iS = -1;
        for (i = 0; i < menus.Count; i++)
        {
            if (iP < 0 && menus[i].Title == "PESCA" && i != 0) iP = i;
            if (iS < 0 && menus[i].Title == "SETTINGS") iS = i;
        }
        if (iP < 0) return;
        // SI APRE DRITTO SULLA PESCA.
        // Questa mod fa una cosa sola: non ha senso una prima pagina con
        // dentro "Pesca". Premi F7 e sei gia' dentro; indietro da qui
        // chiude la finestra, perche' sopra non c'e' niente.
        menuPesca = iP;
        menus[iP].Parent = -1;
        int qd;
        for (qd = 0; qd < modSubDir.Count; qd++)
            if (modSubDir[qd] != null
                && modSubDir[qd].ToLower().EndsWith("pesca"))
            { dirPesca = modSubDir[qd]; break; }
        menus[0].Items.Clear();
        SpegniRobaDelTrainer();
        // LE IMPOSTAZIONI SONO UNA SOLA.
        // Quelle della finestra - lingua, header, sposta - non stanno
        // piu' per conto loro: si attaccano in fondo alle impostazioni
        // della pesca, ogni volta che quella pagina si rilegge.
        impTrainer.Clear();
        if (iS >= 0)
        {
            int q;
            for (q = 0; q < menus[iS].Items.Count; q++)
                impTrainer.Add(menus[iS].Items[q]);
        }
    }

    string L(string en, string it)
    {
        return lang == 1 ? it : en;
    }

    string Txt(TItem it)
    {
        return lang == 1 ? it.TextIt : it.Text;
    }

    string TitleOf(TMenu m)
    {
        return lang == 1 ? m.TitleIt : m.Title;
    }

    // ============================================================
    //  lista veicoli -> sottomenu per classe (al primo tick)
    // ============================================================
    static readonly string[] VCLASS = new string[] {
        "Compacts", "Sedans", "SUVs", "Coupes", "Muscle", "Sports Classics", "Sports",
        "Super", "Motorcycles", "Off-road", "Industrial", "Utility", "Vans", "Cycles",
        "Boats", "Helicopters", "Planes", "Service", "Emergency", "Military",
        "Commercial", "Trains"
    };

    // palette pastello "Miami Vice"
    static readonly int[,] PASTEL = new int[,] {
        { 255, 133, 192 },   // rosa neon
        { 130, 225, 235 },   // azzurro acqua
        { 170, 235, 190 },   // menta
        { 200, 170, 245 },   // lilla
        { 255, 190, 135 },   // pesca
        { 250, 235, 150 },   // giallo pastello
        { 255, 160, 160 },   // corallo
        { 160, 200, 255 }    // celeste
    };

    // famiglia di colore per classe: mezzi simili, stessa tinta
    //   1 = auto di strada   0 = sportive   2 = due ruote   4 = lavoro/fuoristrada
    //   3 = aria             7 = acqua      6 = soccorso/militari   5 = treni
    static readonly int[] CCOLOR = new int[] {
        1, 1, 1, 1,      // Compacts, Sedans, SUVs, Coupes
        0, 0, 0, 0,      // Muscle, Sports Classics, Sports, Super
        2,               // Motorcycles
        4, 4, 4, 4,      // Off-road, Industrial, Utility, Vans
        2,               // Cycles
        7,               // Boats
        3, 3,            // Helicopters, Planes
        4,               // Service
        6, 6,            // Emergency, Military
        4,               // Commercial
        5                // Trains
    };

    static readonly string[] VCLASS_IT = new string[] {
        "Compatte", "Berline", "SUV", "Coupe", "Muscle", "Sportive classiche", "Sportive",
        "Super", "Moto", "Fuoristrada", "Industriali", "Utilitari", "Furgoni", "Bici",
        "Barche", "Elicotteri", "Aerei", "Servizi", "Emergenza", "Militari",
        "Commerciali", "Treni"
    };

    int ClassFromText(string t)
    {
        if (t == null || t.Length == 0)
        {
            return -1;
        }
        string q = t.Trim().ToLower();

        int i;
        for (i = 0; i < VCLASS.Length; i++)
        {
            if (VCLASS[i].ToLower() == q || VCLASS_IT[i].ToLower() == q)
            {
                return i;
            }
        }
        // tolleranza: singolare/plurale e forme corte
        for (i = 0; i < VCLASS.Length; i++)
        {
            string a = VCLASS[i].ToLower();
            string b = VCLASS_IT[i].ToLower();
            if (a.StartsWith(q) || b.StartsWith(q) || q.StartsWith(a) || q.StartsWith(b))
            {
                return i;
            }
        }
        return -1;
    }

    void BuildPaintMenus(int mPaint)
    {
        string cf = DATA_DIR + "\\colors.txt";
        if (!File.Exists(cf))
        {
            return;
        }

        string[] rows = File.ReadAllLines(cf);

        // 4 destinazioni: primario, secondario, perlato, cerchi
        int[] tgtId = new int[] { 220, 221, 222, 223 };
        string[] tgtEn = new string[] { "Primary", "Secondary", "Pearlescent", "Details" };
        string[] tgtIt = new string[] { "Primario", "Secondario", "Perlato", "Dettagli" };

        int t;
        for (t = 0; t < tgtId.Length; t++)
        {
            int mt = NewMenu(tgtEn[t].ToUpper(), tgtIt[t].ToUpper(), mPaint);
            AddSub(mPaint, tgtEn[t], tgtIt[t], mt);

            List<string> gk = new List<string>();
            List<int> gm = new List<int>();

            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string row = rows[i].Trim();
                if (row.Length == 0 || row.StartsWith("#")) continue;

                string[] f = row.Split('|');
                if (f.Length < 2) continue;

                int idx;
                if (!int.TryParse(f[0].Trim(), out idx)) continue;

                string cname = f[1].Trim();
                string grp = f.Length >= 3 ? f[2].Trim() : "Other";
                if (grp.Length == 0) grp = "Other";

                int g = gk.IndexOf(grp.ToLower());
                if (g < 0)
                {
                    int nm = NewMenu(grp.ToUpper(), grp.ToUpper(), mt);
                    AddSub(mt, grp, grp, nm);
                    gk.Add(grp.ToLower());
                    gm.Add(nm);
                    g = gk.Count - 1;
                }

                TItem ci = AddAction(gm[g], cname, cname, tgtId[t]);
                ci.Data = idx.ToString();
            }
        }
    }

    Vector3 FindObjectiveBlip()
    {
        // gli obiettivi di missione sono blip gialli (colore 5)
        int sprite;
        for (sprite = 1; sprite <= 250; sprite++)
        {
            int b = Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, sprite);
            while (Function.Call<bool>(Hash.DOES_BLIP_EXIST, b))
            {
                int col = Function.Call<int>(Hash.GET_BLIP_COLOUR, b);
                if (col == 5)
                {
                    return Function.Call<Vector3>(Hash.GET_BLIP_INFO_ID_COORD, b);
                }
                b = Function.Call<int>(Hash.GET_NEXT_BLIP_INFO_ID, sprite);
            }
        }
        return Vector3.Zero;
    }

    void TeleportTo(Vector3 dest)
    {
        Ped ped = Game.Player.Character;
        Entity what = ped;
        Vehicle v = ped.CurrentVehicle;
        if (v != null && v.Exists())
        {
            what = v;
        }

        // porta il giocatore in quota e cerca il terreno scendendo
        float[] probe = new float[] {
            1000f, 800f, 650f, 500f, 400f, 320f, 260f, 200f, 160f, 130f,
            100f, 80f, 62f, 50f, 40f, 32f, 25f, 20f, 15f, 10f, 5f, 0f
        };

        Function.Call(Hash.SET_ENTITY_COORDS, what, dest.X, dest.Y, 1000f, false, false, false, true);
        Script.Wait(200);

        int i;
        float groundZ = 0f;
        bool ok = false;
        for (i = 0; i < probe.Length; i++)
        {
            Function.Call(Hash.SET_ENTITY_COORDS, what, dest.X, dest.Y, probe[i], false, false, false, true);
            Script.Wait(40);

            OutputArgument oz = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, dest.X, dest.Y, probe[i], oz, false))
            {
                groundZ = oz.GetResult<float>();
                ok = true;
                break;
            }
        }

        if (!ok)
        {
            // niente terreno: prova il livello dell'acqua
            OutputArgument ow = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_WATER_HEIGHT, dest.X, dest.Y, 100f, ow))
            {
                groundZ = ow.GetResult<float>();
                ok = true;
            }
        }

        if (!ok)
        {
            groundZ = dest.Z;
        }

        Function.Call(Hash.SET_ENTITY_COORDS, what, dest.X, dest.Y, groundZ + 1.0f, false, false, false, true);
        if (v != null && v.Exists())
        {
            Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, v);
        }

        Notification.PostTicker("~g~" + L("Teleported", "Teletrasportato"), false);
    }

    // ---------- anteprima colore mentre scorri ----------
    bool paintPreview = false;
    int savePrim, saveSec, savePearl, saveWheel;
    int lastPreviewMenu = -1;
    int lastPreviewSel = -1;

    bool ReadPaint()
    {
        Vehicle v = TargetVehicle();
        if (v == null || !v.Exists())
        {
            return false;
        }

        OutputArgument a1 = new OutputArgument();
        OutputArgument a2 = new OutputArgument();
        Function.Call(Hash.GET_VEHICLE_COLOURS, v, a1, a2);
        savePrim = a1.GetResult<int>();
        saveSec = a2.GetResult<int>();

        OutputArgument b1 = new OutputArgument();
        OutputArgument b2 = new OutputArgument();
        Function.Call(Hash.GET_VEHICLE_EXTRA_COLOURS, v, b1, b2);
        savePearl = b1.GetResult<int>();
        saveWheel = b2.GetResult<int>();
        return true;
    }

    void RestorePaint()
    {
        Vehicle v = TargetVehicle();
        if (v == null || !v.Exists())
        {
            return;
        }
        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
        Function.Call(Hash.SET_VEHICLE_COLOURS, v, SafeColor(savePrim), SafeColor(saveSec));
        Function.Call(Hash.SET_VEHICLE_EXTRA_COLOURS, v, SafeColor(savePearl), SafeColor(saveWheel));
    }

    void UpdatePaintPreview()
    {
        TMenu m = menus[cur];
        if (m.Items.Count == 0)
        {
            return;
        }

        TItem it = m.Items[m.Sel];
        bool isColor = it.Kind == TItem.ACTION && it.Id >= 220 && it.Id <= 223;

        if (!isColor)
        {
            if (paintPreview)
            {
                RestorePaint();
                paintPreview = false;
            }
            lastPreviewMenu = -1;
            lastPreviewSel = -1;
            return;
        }

        if (!paintPreview)
        {
            if (!ReadPaint())
            {
                return;
            }
            paintPreview = true;
        }

        if (cur == lastPreviewMenu && m.Sel == lastPreviewSel)
        {
            return;
        }
        lastPreviewMenu = cur;
        lastPreviewSel = m.Sel;

        int ci;
        if (int.TryParse(it.Data, out ci))
        {
            ApplyPaint(it.Id - 220, ci);
        }
    }

    // ============================================================
    //  VEICOLI SALVATI  (myvehicles.txt)
    //  formato: modello|targa|prim|sec|perlato|cerchi|x|y|z|heading|nome
    // ============================================================
    string MyVehFile()
    {
        return Path.Combine(DATA_DIR, "myvehicles.txt");
    }

    void LoadMyVehicles()
    {
        pvRaw.Clear();
        try
        {
            if (!File.Exists(MyVehFile())) return;
            string[] l = File.ReadAllLines(MyVehFile());
            int i;
            for (i = 0; i < l.Length; i++)
            {
                string row = l[i].Trim();
                if (row.Length == 0 || row.StartsWith("#")) continue;
                if (row.Split('|').Length < 11) continue;
                pvRaw.Add(row);
            }
        }
        catch (Exception)
        {
        }
    }

    void SaveMyVehicles()
    {
        try
        {
            Directory.CreateDirectory(DATA_DIR);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# I MIEI VEICOLI - salvati dal trainer");
            sb.AppendLine("# hashmodello|targa|primario|secondario|perlato|cerchi|x|y|z|heading|nome|elaborazioni");
            int i;
            for (i = 0; i < pvRaw.Count; i++)
            {
                sb.AppendLine(pvRaw[i]);
            }
            File.WriteAllText(MyVehFile(), sb.ToString());
        }
        catch (Exception)
        {
        }
    }

    string PvField(int idx, int field)
    {
        if (idx < 0 || idx >= pvRaw.Count) return "";
        string[] f = pvRaw[idx].Split('|');
        if (field < 0 || field >= f.Length) return "";
        return f[field].Trim();
    }

    float PvFloat(int idx, int field)
    {
        float r;
        if (float.TryParse(PvField(idx, field), NumberStyles.Float, CultureInfo.InvariantCulture, out r)) return r;
        return 0f;
    }

    // il campo 0 e' l'hash del modello; le righe vecchie hanno un nome
    // le targhe di GTA arrivano riempite di spazi: vanno sempre normalizzate,
    // altrimenti il confronto fallisce e si creano doppioni all'infinito
    string PlateOf(Vehicle v)
    {
        string t = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v);
        if (t == null) return "";
        return t.Trim().ToUpper();
    }

    string PvPlate(int idx)
    {
        return PvField(idx, 1).Trim().ToUpper();
    }

    int PvHash(int idx)
    {
        string f0 = PvField(idx, 0);
        int h;
        if (int.TryParse(f0, out h)) return h;
        return Function.Call<int>(Hash.GET_HASH_KEY, f0);
    }

    int PvInt(int idx, int field)
    {
        int r;
        if (int.TryParse(PvField(idx, field), out r)) return r;
        return 0;
    }

    // ------------------------------------------------------------
    //  GUARDAROBA
    //  Ogni pezzo ha due numeri: il capo e la sua variante di colore.
    //  I massimi si chiedono al gioco sul personaggio che hai adesso.
    // ------------------------------------------------------------
    static readonly int[] WARD_COMP = new int[] { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 2, 0 };
    static readonly string[] WARD_EN = new string[] {
        "Mask", "Torso", "Legs", "Bag", "Shoes", "Accessories", "Undershirt",
        "Body armor", "Decals", "Tops", "Hair", "Face" };
    static readonly string[] WARD_IT = new string[] {
        "Maschera", "Torso", "Pantaloni", "Borsa", "Scarpe", "Accessori", "Maglietta",
        "Giubbotto", "Adesivi", "Capi sopra", "Capelli", "Viso" };

    static readonly int[] WARD_PROP = new int[] { 0, 1, 2, 6, 7 };
    static readonly string[] WARDP_EN = new string[] { "Hat", "Glasses", "Earrings", "Watch", "Bracelet" };
    static readonly string[] WARDP_IT = new string[] { "Cappello", "Occhiali", "Orecchini", "Orologio", "Bracciale" };

    void BuildWardrobe()
    {
        if (mWard < 0) return;

        menus[mWard].Items.Clear();
        menus[mWard].Sel = 0;
        menus[mWard].Top = 0;

        Ped p = Game.Player.Character;
        if (p == null || !p.Exists()) return;

        AddHeader(mWard, "- CLOTHES -", "- VESTITI -", 3);

        int i;
        for (i = 0; i < WARD_COMP.Length; i++)
        {
            int c = WARD_COMP[i];

            int nd = Function.Call<int>(Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS, p, c);
            if (nd <= 0) continue;

            int cd = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, p, c);
            int ct = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, p, c);
            int nt = Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, p, c, cd);
            if (nt < 1) nt = 1;

            TItem d = AddNumber(mWard, WARD_EN[i], WARD_IT[i], 281, cd, 0, nd - 1, 1);
            d.Data = "c|" + c + "|d";

            TItem t = AddNumber(mWard, WARD_EN[i] + " colour", WARD_IT[i] + " colore",
                                281, ct, 0, nt - 1, 1);
            t.Data = "c|" + c + "|t";
        }

        AddHeader(mWard, "- ACCESSORIES -", "- ACCESSORI -", 2);

        for (i = 0; i < WARD_PROP.Length; i++)
        {
            int c = WARD_PROP[i];

            int nd = Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS, p, c);
            if (nd <= 0) continue;

            int cd = Function.Call<int>(Hash.GET_PED_PROP_INDEX, p, c);
            int ct = Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, p, c);
            if (cd < -1) cd = -1;

            int nt = Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS, p, c, cd < 0 ? 0 : cd);
            if (nt < 1) nt = 1;

            // -1 vuol dire "niente": si tiene come primo valore
            TItem d = AddNumber(mWard, WARDP_EN[i], WARDP_IT[i], 281, cd, -1, nd - 1, 1);
            d.Data = "p|" + c + "|d";

            TItem t = AddNumber(mWard, WARDP_EN[i] + " colour", WARDP_IT[i] + " colore",
                                281, ct < 0 ? 0 : ct, 0, nt - 1, 1);
            t.Data = "p|" + c + "|t";
        }
    }

    // applica un pezzo del guardaroba quando cambi il numero
    void ApplyWardrobe(TItem it)
    {
        Ped p = Game.Player.Character;
        if (p == null || !p.Exists()) return;
        if (it.Data == null) return;

        string[] f = it.Data.Split('|');
        if (f.Length < 3) return;

        int c;
        if (!int.TryParse(f[1], out c)) return;

        if (f[0] == "c")
        {
            int d = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, p, c);
            int t = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, p, c);

            if (f[2] == "d") d = it.Val; else t = it.Val;

            int nt = Function.Call<int>(Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS, p, c, d);
            if (nt < 1) nt = 1;
            if (t >= nt) t = 0;

            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, p, c, d, t, 0);
        }
        else
        {
            int d = Function.Call<int>(Hash.GET_PED_PROP_INDEX, p, c);
            int t = Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, p, c);
            if (t < 0) t = 0;

            if (f[2] == "d") d = it.Val; else t = it.Val;

            if (d < 0)
            {
                Function.Call(Hash.CLEAR_PED_PROP, p, c);
                return;
            }

            int nt = Function.Call<int>(Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS, p, c, d);
            if (nt < 1) nt = 1;
            if (t >= nt) t = 0;

            Function.Call(Hash.SET_PED_PROP_INDEX, p, c, d, t, true);
        }
    }

    // ------------------------------------------------------------
    //  LINEE DELL'AUTOBUS: una voce per ogni file linea_*.txt
    // ------------------------------------------------------------
    void BuildMyVehicles()
    {
        if (mMyVeh < 0) return;

        menus[mMyVeh].Items.Clear();
        menus[mMyVeh].Sel = 0;
        menus[mMyVeh].Top = 0;

        int i;
        for (i = 0; i < pvRaw.Count; i++)
        {
            string nm = PvField(i, 10);
            if (nm.Length == 0)
            {
                int hh;
                if (int.TryParse(PvField(i, 0), out hh)) nm = NomeDaHash(hh);
                if (nm.Length == 0) nm = PvField(i, 0);
            }

            int sm = NewMenu(nm.ToUpper(), nm.ToUpper(), mMyVeh);
            AddSub(mMyVeh, nm, nm, sm);

            TItem a1 = AddAction(sm, "Go to vehicle", "Vai al veicolo", 230);
            a1.Data = i.ToString();
            TItem a2 = AddAction(sm, "Bring here", "Portalo qui", 231);
            a2.Data = i.ToString();
            TItem a3 = AddAction(sm, "Remove from list", "Rimuovi dalla lista", 232);
            a3.Data = i.ToString();
        }
    }

    int FindMyVehicle(Vehicle v)
    {
        if (v == null || !v.Exists()) return -1;

        string plate = PlateOf(v);
        int hash = v.Model.Hash;

        int i;
        for (i = 0; i < pvRaw.Count; i++)
        {
            if (PvPlate(i) != plate) continue;
            if (PvHash(i) == hash) return i;
        }
        return -1;
    }

    string ComposeEntry(Vehicle v, string name)
    {
        OutputArgument a1 = new OutputArgument();
        OutputArgument a2 = new OutputArgument();
        Function.Call(Hash.GET_VEHICLE_COLOURS, v, a1, a2);
        OutputArgument b1 = new OutputArgument();
        OutputArgument b2 = new OutputArgument();
        Function.Call(Hash.GET_VEHICLE_EXTRA_COLOURS, v, b1, b2);

        string plate = PlateOf(v);
        Vector3 pp = v.Position;

        return v.Model.Hash + "|" + plate + "|"
             + a1.GetResult<int>() + "|" + a2.GetResult<int>() + "|"
             + b1.GetResult<int>() + "|" + b2.GetResult<int>() + "|"
             + pp.X.ToString("0.000", CultureInfo.InvariantCulture) + "|"
             + pp.Y.ToString("0.000", CultureInfo.InvariantCulture) + "|"
             + pp.Z.ToString("0.000", CultureInfo.InvariantCulture) + "|"
             + v.Heading.ToString("0.0", CultureInfo.InvariantCulture) + "|"
             + name + "|"
             + CollectMods(v);
    }

    void SaveVehicleEntry(Vehicle v, string modelName)
    {
        if (v == null || !v.Exists()) return;

        string label = VehLabel(v.Model.Hash, modelName);
        string line = ComposeEntry(v, label);

        int idx = FindMyVehicle(v);
        if (idx >= 0)
        {
            pvRaw[idx] = line;
        }
        else
        {
            pvRaw.Add(line);
            idx = pvRaw.Count - 1;
        }

        trackedIdx = idx;
        SaveMyVehicles();
        BuildMyVehicles();
    }

    // ============================================================
    //  elaborazioni: le legge dal veicolo e le riscrive identiche
    //  formato compatto:  m<slot>=<idx>;t<slot>=<0|1>;wt=..;tint=..;ts=r.g.b;liv=..;ps=..;bp=..;x<n>=<0|1>
    // ============================================================
    static readonly int[] TOGGLE_SLOTS = new int[] { 17, 18, 19, 20, 21, 22 };

    static readonly string[] SLOT_EN = new string[] {
        "Spoiler", "Front bumper", "Rear bumper", "Side skirts", "Exhaust", "Roll cage",
        "Grille", "Hood", "Left fender", "Right fender", "Roof", "Engine", "Brakes",
        "Transmission", "Horn", "Suspension", "Armour", "Slot 17", "Turbo", "Slot 19",
        "Tyre smoke", "Slot 21", "Xenon lights", "Front wheels", "Rear wheels",
        "Plate holder", "Vanity plate", "Trim design", "Ornaments", "Dashboard",
        "Dials", "Door speakers", "Seats", "Steering wheel", "Shifter", "Plaques",
        "Speakers", "Trunk", "Hydraulics", "Engine block", "Air filter", "Struts",
        "Arch covers", "Aerials", "Trim", "Tank", "Windows", "Slot 47", "Livery"
    };

    static readonly string[] SLOT_IT = new string[] {
        "Spoiler", "Paraurti anteriore", "Paraurti posteriore", "Minigonne", "Scarico", "Roll bar",
        "Griglia", "Cofano", "Parafango sx", "Parafango dx", "Tetto", "Motore", "Freni",
        "Cambio", "Clacson", "Sospensioni", "Corazzatura", "Slot 17", "Turbo", "Slot 19",
        "Fumo gomme", "Slot 21", "Fari allo xeno", "Cerchi anteriori", "Cerchi posteriori",
        "Portatarga", "Targa personalizzata", "Rivestimenti", "Ornamenti", "Cruscotto",
        "Strumenti", "Casse portiere", "Sedili", "Volante", "Leva del cambio", "Targhette",
        "Casse", "Bagagliaio", "Idraulica", "Blocco motore", "Filtro aria", "Montanti",
        "Passaruota", "Antenne", "Finiture", "Serbatoio", "Finestrini", "Slot 47", "Livrea"
    };

    bool IsToggleSlot(int slot)
    {
        int i;
        for (i = 0; i < TOGGLE_SLOTS.Length; i++)
        {
            if (TOGGLE_SLOTS[i] == slot) return true;
        }
        return false;
    }

    string CollectMods(Vehicle v)
    {
        if (v == null || !v.Exists()) return "";

        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
        StringBuilder sb = new StringBuilder();

        int slot;
        for (slot = 0; slot <= 48; slot++)
        {
            if (IsToggleSlot(slot))
            {
                if (Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, v, slot))
                {
                    sb.Append("t" + slot + "=1;");
                }
            }
            else
            {
                int mod = Function.Call<int>(Hash.GET_VEHICLE_MOD, v, slot);
                if (mod >= 0)
                {
                    sb.Append("m" + slot + "=" + mod + ";");
                }
            }
        }

        sb.Append("wt=" + Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE, v) + ";");
        sb.Append("tint=" + Function.Call<int>(Hash.GET_VEHICLE_WINDOW_TINT, v) + ";");
        sb.Append("liv=" + Function.Call<int>(Hash.GET_VEHICLE_LIVERY, v) + ";");
        sb.Append("ps=" + Function.Call<int>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, v) + ";");

        OutputArgument sr = new OutputArgument();
        OutputArgument sg = new OutputArgument();
        OutputArgument sbb = new OutputArgument();
        Function.Call(Hash.GET_VEHICLE_TYRE_SMOKE_COLOR, v, sr, sg, sbb);
        sb.Append("ts=" + sr.GetResult<int>() + "." + sg.GetResult<int>() + "." + sbb.GetResult<int>() + ";");

        int ex;
        for (ex = 1; ex <= 14; ex++)
        {
            if (Function.Call<bool>(Hash.DOES_EXTRA_EXIST, v, ex))
            {
                bool on = Function.Call<bool>(Hash.IS_VEHICLE_EXTRA_TURNED_ON, v, ex);
                sb.Append("x" + ex + "=" + (on ? "1" : "0") + ";");
            }
        }

        return sb.ToString();
    }

    void ApplyMods(Vehicle v, string data)
    {
        if (v == null || !v.Exists()) return;
        if (data == null || data.Length == 0) return;

        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);

        string[] parts = data.Split(';');
        int i;

        // il tipo di cerchio va impostato prima dei cerchi stessi
        for (i = 0; i < parts.Length; i++)
        {
            string q = parts[i].Trim();
            if (!q.StartsWith("wt=")) continue;
            int wt;
            if (int.TryParse(q.Substring(3), out wt))
            {
                Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE, v, wt);
            }
        }

        for (i = 0; i < parts.Length; i++)
        {
            string q = parts[i].Trim();
            if (q.Length < 3) continue;

            int eq = q.IndexOf('=');
            if (eq < 1) continue;

            string key = q.Substring(0, eq);
            string val = q.Substring(eq + 1);

            if (key == "wt")
            {
                continue;
            }
            else if (key == "tint")
            {
                int t;
                if (int.TryParse(val, out t)) Function.Call(Hash.SET_VEHICLE_WINDOW_TINT, v, t);
            }
            else if (key == "liv")
            {
                int t;
                if (int.TryParse(val, out t) && t >= 0) Function.Call(Hash.SET_VEHICLE_LIVERY, v, t);
            }
            else if (key == "ps")
            {
                int t;
                if (int.TryParse(val, out t)) Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, v, t);
            }
            else if (key == "ts")
            {
                string[] rgb = val.Split('.');
                if (rgb.Length == 3)
                {
                    int r, g, b;
                    if (int.TryParse(rgb[0], out r) && int.TryParse(rgb[1], out g) && int.TryParse(rgb[2], out b))
                    {
                        Function.Call(Hash.SET_VEHICLE_TYRE_SMOKE_COLOR, v, r, g, b);
                    }
                }
            }
            else if (key.StartsWith("m"))
            {
                int slot, idx;
                if (int.TryParse(key.Substring(1), out slot) && int.TryParse(val, out idx))
                {
                    // un indice che quel modello non ha fa crashare il gioco
                    int num = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, v, slot);
                    if (idx >= 0 && idx < num)
                    {
                        Function.Call(Hash.SET_VEHICLE_MOD, v, slot, idx, false);
                    }
                }
            }
            else if (key.StartsWith("t"))
            {
                int slot;
                if (int.TryParse(key.Substring(1), out slot))
                {
                    Function.Call(Hash.TOGGLE_VEHICLE_MOD, v, slot, val == "1");
                }
            }
            else if (key.StartsWith("x"))
            {
                int ex;
                if (int.TryParse(key.Substring(1), out ex))
                {
                    Function.Call(Hash.SET_VEHICLE_EXTRA, v, ex, val == "1" ? 0 : 1);
                }
            }
        }
    }

    // a quale gruppo appartiene ogni slot
    //  0 = carrozzeria   1 = meccanica   2 = ruote   3 = luci e altro
    static readonly int[] SLOT_GROUP = new int[] {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,   // 0-10  spoiler ... tetto
        1, 1, 1,                            // 11 motore, 12 freni, 13 cambio
        3,                                  // 14 clacson
        1, 1,                               // 15 sospensioni, 16 corazzatura
        3, 1, 3, 3, 3,                      // 17, 18 turbo, 19, 20 fumo, 21
        3,                                  // 22 xeno
        2, 2,                               // 23-24 cerchi
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 25-36 interni ed estetica
        0,                                  // 37 bagagliaio
        1,                                  // 38 idraulica
        0, 0, 0, 0, 0, 0, 0, 0,             // 39-46
        0,                                  // 47
        0                                   // 48 livrea
    };

    int MenuForGroup(int g)
    {
        if (g == 1) return mMech;
        if (g == 2) return mWheels;
        if (g == 3) return mLights;
        return mBody;
    }

    void BuildModShop()
    {
        if (mModShop < 0) return;

        menus[mModShop].Items.Clear();
        menus[mModShop].Sel = 0;
        menus[mModShop].Top = 0;
        menus[mBody].Items.Clear();
        menus[mMech].Items.Clear();
        menus[mWheels].Items.Clear();
        menus[mLights].Items.Clear();
        menus[mExtras].Items.Clear();

        Vehicle v = TargetVehicle();
        if (v == null || !v.Exists())
        {
            AddAction(mModShop, "No vehicle nearby", "Nessun veicolo vicino", -1);
            return;
        }

        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);

        // ---- tipo di cerchio: sempre in cima al menu ruote ----
        string[] wtn = new string[] {
            "Sport", "Muscle", "Lowrider", "SUV", "Offroad", "Tuner",
            "Bike", "High End", "Benny's Original", "Benny's Bespoke", "Open Wheel",
            "Street", "Track"
        };
        TItem wt = AddList(mWheels, "Wheel type", "Tipo cerchi", 243, wtn, 0);
        int cwt = Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE, v);
        if (cwt >= 0 && cwt < wtn.Length) wt.Sel = cwt;

        // ---- tutti gli slot, ognuno nel suo gruppo ----
        int slot;
        for (slot = 0; slot <= 48; slot++)
        {
            string nameEn = slot < SLOT_EN.Length ? SLOT_EN[slot] : ("Slot " + slot);
            string nameIt = slot < SLOT_IT.Length ? SLOT_IT[slot] : ("Slot " + slot);
            int dest = MenuForGroup(slot < SLOT_GROUP.Length ? SLOT_GROUP[slot] : 0);

            if (IsToggleSlot(slot))
            {
                if (slot != 18 && slot != 20 && slot != 22) continue;
                TItem tg = AddToggle(dest, nameEn, nameIt, 242, false);
                tg.Data = slot.ToString();
                tg.On = Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, v, slot);
                continue;
            }

            int num = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, v, slot);
            if (num <= 0) continue;

            string[] opts = new string[num + 1];
            opts[0] = L("Stock", "Di serie");
            int k;
            for (k = 0; k < num; k++)
            {
                opts[k + 1] = (k + 1).ToString();
            }

            TItem li = AddList(dest, nameEn, nameIt, 241, opts, 0);
            li.Data = slot.ToString();
            int curMod = Function.Call<int>(Hash.GET_VEHICLE_MOD, v, slot);
            li.Sel = (curMod >= 0 && curMod < num) ? curMod + 1 : 0;
        }

        // ---- vetri e livrea ----
        string[] tints = new string[] { "None", "Black", "Dark smoke", "Light smoke", "Limo", "Green" };
        TItem ti = AddList(mLights, "Window tint", "Vetri oscurati", 244, tints, 0);
        int ct = Function.Call<int>(Hash.GET_VEHICLE_WINDOW_TINT, v);
        if (ct >= 0 && ct < tints.Length) ti.Sel = ct;

        int livCount = Function.Call<int>(Hash.GET_VEHICLE_LIVERY_COUNT, v);
        if (livCount > 0)
        {
            string[] livs = new string[livCount + 1];
            livs[0] = L("None", "Nessuna");
            int q;
            for (q = 0; q < livCount; q++)
            {
                livs[q + 1] = (q + 1).ToString();
            }
            TItem lv = AddList(mBody, "Livery", "Livrea", 246, livs, 0);
            int cl = Function.Call<int>(Hash.GET_VEHICLE_LIVERY, v);
            lv.Sel = (cl >= 0 && cl < livCount) ? cl + 1 : 0;
        }

        // ---- extra del modello ----
        int ex;
        for (ex = 1; ex <= 14; ex++)
        {
            if (!Function.Call<bool>(Hash.DOES_EXTRA_EXIST, v, ex)) continue;
            TItem xt = AddToggle(mExtras, "Extra " + ex, "Extra " + ex, 247, false);
            xt.Data = ex.ToString();
            xt.On = Function.Call<bool>(Hash.IS_VEHICLE_EXTRA_TURNED_ON, v, ex);
        }

        // ---- indice del menu officina ----
        AddAction(mModShop, "Max upgrades", "Elaborazione massima", 248);
        AddAction(mModShop, "Back to stock", "Torna di serie", 249);

        AddHeader(mModShop, "- SECTIONS -", "- SEZIONI -", 1);
        if (menus[mBody].Items.Count > 0)
        {
            TItem b1 = AddSub(mModShop, "Bodywork", "Carrozzeria", mBody);
            b1.Cr = PASTEL[1, 0]; b1.Cg = PASTEL[1, 1]; b1.Cb = PASTEL[1, 2]; b1.Tinted = true;
        }
        if (menus[mMech].Items.Count > 0)
        {
            TItem b2 = AddSub(mModShop, "Mechanics", "Meccanica", mMech);
            b2.Cr = PASTEL[0, 0]; b2.Cg = PASTEL[0, 1]; b2.Cb = PASTEL[0, 2]; b2.Tinted = true;
        }
        if (menus[mWheels].Items.Count > 0)
        {
            TItem b3 = AddSub(mModShop, "Wheels", "Ruote", mWheels);
            b3.Cr = PASTEL[2, 0]; b3.Cg = PASTEL[2, 1]; b3.Cb = PASTEL[2, 2]; b3.Tinted = true;
        }
        if (menus[mLights].Items.Count > 0)
        {
            TItem b4 = AddSub(mModShop, "Lights & other", "Luci e altro", mLights);
            b4.Cr = PASTEL[5, 0]; b4.Cg = PASTEL[5, 1]; b4.Cb = PASTEL[5, 2]; b4.Tinted = true;
        }
        if (menus[mExtras].Items.Count > 0)
        {
            TItem b5 = AddSub(mModShop, "Extras", "Extra", mExtras);
            b5.Cr = PASTEL[4, 0]; b5.Cg = PASTEL[4, 1]; b5.Cb = PASTEL[4, 2]; b5.Tinted = true;
        }

        menus[mModShop].Sel = FirstSelectable(mModShop);
    }

    void TouchSaved(Vehicle v)
    {
        int ti = FindMyVehicle(v);
        if (ti < 0) return;

        int keep = trackedIdx;
        trackedIdx = ti;
        UpdateTrackedEntry(v);
        trackedIdx = keep < 0 ? ti : keep;
    }

    void UpdateTrackedEntry(Vehicle v)
    {
        if (trackedIdx < 0 || trackedIdx >= pvRaw.Count) return;
        if (v == null || !v.Exists()) return;

        string[] old = pvRaw[trackedIdx].Split('|');
        if (old.Length < 11) return;

        string name = old[10];

        OutputArgument a1 = new OutputArgument();
        OutputArgument a2 = new OutputArgument();
        Function.Call(Hash.GET_VEHICLE_COLOURS, v, a1, a2);
        OutputArgument b1 = new OutputArgument();
        OutputArgument b2 = new OutputArgument();
        Function.Call(Hash.GET_VEHICLE_EXTRA_COLOURS, v, b1, b2);

        string plate = PlateOf(v);
        Vector3 pp = v.Position;

        pvRaw[trackedIdx] = v.Model.Hash + "|" + plate + "|"
            + a1.GetResult<int>() + "|" + a2.GetResult<int>() + "|"
            + b1.GetResult<int>() + "|" + b2.GetResult<int>() + "|"
            + pp.X.ToString("0.000", CultureInfo.InvariantCulture) + "|"
            + pp.Y.ToString("0.000", CultureInfo.InvariantCulture) + "|"
            + pp.Z.ToString("0.000", CultureInfo.InvariantCulture) + "|"
            + v.Heading.ToString("0.0", CultureInfo.InvariantCulture) + "|"
            + name + "|"
            + CollectMods(v);

        SaveMyVehicles();
    }

    // ============================================================
    //  comparsa "pigra": il veicolo esiste solo quando gli sei vicino.
    //  Entro LAZY_RANGE viene creato, oltre CLEANUP_RANGE viene tolto.
    //  La posizione resta comunque scritta nel file.
    // ============================================================
    const float LAZY_RANGE = 200f;
    const float CLEANUP_RANGE = 500f;
    const int LAZY_INTERVAL = 500;
    int lazyNext = 0;

    void SetBlipName(int blip, string name)
    {
        Function.Call(Hash.BEGIN_TEXT_COMMAND_SET_BLIP_NAME, "STRING");
        Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, name);
        Function.Call(Hash.END_TEXT_COMMAND_SET_BLIP_NAME, blip);
    }

    void ClearBlips()
    {
        int i;
        for (i = 0; i < pvBlip.Count; i++)
        {
            HideBlip(pvBlip[i]);
        }
        pvBlip.Clear();
    }

    void UpdateBlips(Vehicle[] all)
    {
        // allinea la lista dei blip a quella dei veicoli salvati
        while (pvBlip.Count < pvRaw.Count) pvBlip.Add(0);
        while (pvBlip.Count > pvRaw.Count)
        {
            int last = pvBlip.Count - 1;
            HideBlip(pvBlip[last]);
            pvBlip.RemoveAt(last);
        }

        bool on = (tBlips != null && tBlips.On) && (tPersist != null && tPersist.On);

        int i;
        for (i = 0; i < pvRaw.Count; i++)
        {
            if (!on)
            {
                HideBlip(pvBlip[i]);
                continue;
            }

            Vehicle wv = FindWorldVehicle(i, all);

            // posizione: quella vera se il veicolo esiste, altrimenti quella salvata
            Vector3 pos = new Vector3(PvFloat(i, 6), PvFloat(i, 7), PvFloat(i, 8));
            if (wv != null && wv.Exists()) pos = wv.Position;

            // se lo stai guidando il blip non serve: si NASCONDE, non si distrugge.
            // Ricrearlo ogni volta significherebbe rifare il comando di testo per
            // il nome mentre il gioco ne sta usando uno suo, e li' crasha.
            Vehicle mine = Game.Player.Character.CurrentVehicle;
            bool driving = (wv != null && mine != null && wv.Handle == mine.Handle);

            if (pvBlip[i] == 0 || !Function.Call<bool>(Hash.DOES_BLIP_EXIST, pvBlip[i]))
            {
                if (driving) continue;   // niente creazione mentre sei a bordo

                int b = Function.Call<int>(Hash.ADD_BLIP_FOR_COORD, pos.X, pos.Y, pos.Z);

                int hash = PvHash(i);
                int cls = Function.Call<int>(Hash.GET_VEHICLE_CLASS_FROM_NAME, hash);
                int sprite = 225;                       // auto personale
                if (cls == 8 || cls == 13) sprite = 226; // moto / bici
                else if (cls == 14) sprite = 427;        // barca
                else if (cls == 15) sprite = 422;        // elicottero
                else if (cls == 16) sprite = 423;        // aereo

                Function.Call(Hash.SET_BLIP_SPRITE, b, sprite);
                Function.Call(Hash.SET_BLIP_COLOUR, b, 3);
                Function.Call(Hash.SET_BLIP_SCALE, b, 0.75f);
                Function.Call(Hash.SET_BLIP_AS_SHORT_RANGE, b, true);
                SetBlipName(b, PvField(i, 10));

                pvBlip[i] = b;
            }
            else if (driving)
            {
                Function.Call(Hash.SET_BLIP_ALPHA, pvBlip[i], 0);
            }
            else
            {
                ShowBlip(pvBlip[i]);
                Function.Call(Hash.SET_BLIP_COORDS, pvBlip[i], pos.X, pos.Y, pos.Z);
            }
        }
    }

    // ============================================================
    //  BENZINA
    // ============================================================
    int gasMade = 0;

    void MakeGasBlips()
    {
        if (gasBlips != null) return;
        gasBlips = new int[GX.Length];
        gasMade = 0;
    }

    // senza rinomina non ci sono comandi di testo, ma li creiamo lo stesso
    // a piccoli gruppi: e' lavoro sparso invece che un picco in un frame
    void PumpGasBlips()
    {
        if (gasBlips == null) return;
        if (gasMade >= GX.Length) return;

        int made = 0;
        while (gasMade < GX.Length && made < 2)
        {
            int i = gasMade;
            int b = Function.Call<int>(Hash.ADD_BLIP_FOR_COORD, GX[i], GY[i], GZ[i]);
            Function.Call(Hash.SET_BLIP_SPRITE, b, 361);
            Function.Call(Hash.SET_BLIP_COLOUR, b, 1);       // rosso
            Function.Call(Hash.SET_BLIP_SCALE, b, 0.7f);
            Function.Call(Hash.SET_BLIP_AS_SHORT_RANGE, b, false);
            // 3 = solo sulla mappa grande: sul radar sarebbero un casino
            Function.Call(Hash.SET_BLIP_DISPLAY, b, 3);
            gasBlips[i] = b;

            gasMade++;
            made++;
        }
    }


    // ---------- batteria dell'ibrida ----------
    // L'ibrida ha una batteria piccola sua: si scarica quando va in
    // elettrico e si ricarica quando lavora il termico, di piu' se molli
    // il gas. E' separata dal serbatoio.
    List<string> battKey = new List<string>();
    List<float> battVal = new List<float>();
    string curBattKey = "";
    float batt = 100f;

    const float HYB_KM_EV = 2f;      // km di autonomia in solo elettrico
    const float HYB_KM_CHARGE = 6f;  // km per ricaricarla col termico
    const float HYB_KM_REGEN = 1.5f; // km per ricaricarla in rilascio

    float GetBatt(string key)
    {
        int i = battKey.IndexOf(key);
        if (i < 0) return 100f;
        return battVal[i];
    }

    void SetBatt(string key, float val)
    {
        if (key.Length == 0) return;
        int i = battKey.IndexOf(key);
        if (i < 0) { battKey.Add(key); battVal.Add(val); }
        else battVal[i] = val;
    }

    void SaveBatt()
    {
        try
        {
            Directory.CreateDirectory(DATA_DIR);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# batteria delle ibride: modello:targa=percentuale");
            int i;
            for (i = 0; i < battKey.Count; i++)
                sb.AppendLine(battKey[i] + "=" + battVal[i].ToString("0.#", CultureInfo.InvariantCulture));
            File.WriteAllText(Path.Combine(DATA_DIR, "batteria.txt"), sb.ToString());
        }
        catch (Exception) { }
    }

    void LoadBatt()
    {
        try
        {
            string f = Path.Combine(DATA_DIR, "batteria.txt");
            if (!File.Exists(f)) return;

            battKey.Clear();
            battVal.Clear();
            string[] l = File.ReadAllLines(f);
            int i;
            for (i = 0; i < l.Length; i++)
            {
                string row = l[i].Trim();
                if (row.Length == 0 || row.StartsWith("#")) continue;
                int eq = row.LastIndexOf('=');
                if (eq < 1) continue;
                float val;
                if (!float.TryParse(row.Substring(eq + 1), NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out val)) continue;
                battKey.Add(row.Substring(0, eq));
                battVal.Add(val);
            }
        }
        catch (Exception) { }
    }

    string TankKeyOf(Vehicle v)
    {
        if (v == null || !v.Exists()) return "";
        return v.Model.Hash + ":" + Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, v);
    }

    float GetTank(string key)
    {
        int i = tankKey.IndexOf(key);
        if (i < 0) return 100f;
        return tankVal[i];
    }

    void SetTank(string key, float val)
    {
        if (key.Length == 0) return;
        int i = tankKey.IndexOf(key);
        if (i < 0)
        {
            tankKey.Add(key);
            tankVal.Add(val);
        }
        else
        {
            tankVal[i] = val;
        }
    }

    float GetOil(string key)
    {
        int i = oilKey.IndexOf(key);
        if (i < 0) return 100f;
        return oilVal[i];
    }

    void SetOil(string key, float val)
    {
        if (key.Length == 0) return;
        int i = oilKey.IndexOf(key);
        if (i < 0)
        {
            oilKey.Add(key);
            oilVal.Add(val);
        }
        else
        {
            oilVal[i] = val;
        }
    }

    void SaveOil()
    {
        try
        {
            Directory.CreateDirectory(DATA_DIR);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# tagliando: modello:targa=metri odometro all'ultimo servizio");
            int i;
            for (i = 0; i < oilKey.Count; i++)
            {
                sb.AppendLine(oilKey[i] + "=" + oilVal[i].ToString("0.#", CultureInfo.InvariantCulture));
            }
            File.WriteAllText(Path.Combine(DATA_DIR, "oil.txt"), sb.ToString());
        }
        catch (Exception) { }
    }

    void LoadOil()
    {
        try
        {
            string f = Path.Combine(DATA_DIR, "oil.txt");
            if (!File.Exists(f)) return;

            oilKey.Clear();
            oilVal.Clear();

            string[] rows = File.ReadAllLines(f);
            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string row = rows[i].Trim();
                if (row.Length == 0 || row.StartsWith("#")) continue;
                int eq = row.LastIndexOf('=');
                if (eq < 1) continue;
                float val;
                if (!float.TryParse(row.Substring(eq + 1), NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out val)) continue;
                oilKey.Add(row.Substring(0, eq));
                oilVal.Add(val);
            }
        }
        catch (Exception) { }
    }

    // Il tagliando si misura sui chilometri percorsi dall'ultimo, non su un
    // livello a se': l'olio che vedi scendere e' la stessa cosa vista da
    // un'altra parte. Benzina 1000 km, elettriche 2000 (li' non c'e' olio,
    // ma freni, gomme e batteria un controllo lo vogliono lo stesso).
    float ServiceKmLeft(Vehicle v, bool ev)
    {
        float total = IntervalloTagliando(ev);
        float done = odoM - servM;
        if (done < 0f) done = 0f;
        return (total - done) / 1000f;
    }

    // ogni quanti metri va fatto il tagliando sul mezzo su cui sei
    float IntervalloTagliando(bool ev)
    {
        if (ev) return SERVICE_M_EV;

        Vehicle vv = Game.Player.Character.CurrentVehicle;
        if (vv != null && vv.Exists() && IsHybrid(vv)) return SERVICE_M_HYBRID;
        if (vv != null && vv.Exists() && ECamionGrosso(vv)) return SERVICE_M_CAMION;

        return SERVICE_M_PETROL;
    }

    bool ESuper(Vehicle v)
    {
        if (v.ClassType == VehicleClass.Super) return true;
        return addonSuper.Contains(v.Model.Hash);
    }

    // motrici e mezzi industriali: classi Commercial e Industrial del gioco
    bool ECamionGrosso(Vehicle v)
    {
        VehicleClass c = v.ClassType;
        return c == VehicleClass.Commercial || c == VehicleClass.Industrial;
    }

    void UpdateOil(Ped p, Vehicle v, float meters, bool ev)
    {
        string k = TankKeyOf(v);
        if (k != curOilKey)
        {
            if (curOilKey.Length > 0) SetOil(curOilKey, servM);
            curOilKey = k;
            servM = GetOil(k);
        }

        float total = IntervalloTagliando(ev);
        float done = odoM - servM;
        if (done < 0f) { done = 0f; servM = odoM; }

        // l'olio e' la percentuale di tagliando che ti resta
        oil = 100f * (1f - done / total);
        if (oil < 0f) oil = 0f;
        if (oil > 100f) oil = 100f;

        SetOil(curOilKey, servM);

        int now = Game.GameTime;

        // ---- le elettriche non perdono potenza: solo il promemoria ----
        if (ev)
        {
            if (oil <= 0f && now > oilWarnAt + 90000)
            {
                oilWarnAt = now;
                Notification.PostTicker("~y~" + L("Maintenance due", "Manutenzione da fare")
                    + "~s~ - " + ((int)(odoM / 1000f)) + " km", false);
            }
            return;
        }

        // ---- benzina: olio vero, e se lo trascuri la macchina non tira ----
        if (oil < 30f)
        {
            float mult = 0.55f + (oil / 30f) * 0.45f;    // da 1.00 a 0.55
            Function.Call(Hash.MODIFY_VEHICLE_TOP_SPEED, v, mult);
            oilSlowVeh = v.Handle;
        }
        else if (oilSlowVeh == v.Handle)
        {
            Function.Call(Hash.MODIFY_VEHICLE_TOP_SPEED, v, 1f);
            oilSlowVeh = 0;
        }

        if (oil <= 0f)
        {
            float eh = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, v);
            if (eh > 200f && meters > 0f)
            {
                Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, v, eh - meters * 0.05f);
            }
            if (now > oilWarnAt + 30000)
            {
                oilWarnAt = now;
                Notification.PostTicker("~r~" + L("Maintenance overdue!", "Manutenzione scaduta!") + "~s~ "
                    + L("the motor is wearing out - do it now",
                        "il motore si sta rovinando - falla subito"), false);
            }
        }
        else if (oil < 10f && now > oilWarnAt + 60000)
        {
            oilWarnAt = now;
            Notification.PostTicker("~r~" + L("Maintenance overdue", "Manutenzione scaduta") + "~s~ - "
                + L("the engine is losing power", "il motore non tira piu'"), false);
        }
        else if (oil < 25f && now > oilWarnAt + 120000)
        {
            oilWarnAt = now;
            Notification.PostTicker("~y~" + L("Maintenance due soon", "Manutenzione in scadenza")
                + "~s~ - " + ((int)ServiceKmLeft(v, ev)) + " km", false);
        }
    }

    float GetOdo(string key)
    {
        int i = odoKey.IndexOf(key);
        if (i < 0) return 0f;
        return odoVal[i];
    }

    void SetOdo(string key, float val)
    {
        if (key.Length == 0) return;
        int i = odoKey.IndexOf(key);
        if (i < 0) { odoKey.Add(key); odoVal.Add(val); }
        else odoVal[i] = val;
    }

    void SaveOdo()
    {
        try
        {
            Directory.CreateDirectory(DATA_DIR);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# odometro: modello:targa=metri");
            int i;
            for (i = 0; i < odoKey.Count; i++)
            {
                sb.AppendLine(odoKey[i] + "=" + odoVal[i].ToString("0", CultureInfo.InvariantCulture));
            }
            File.WriteAllText(Path.Combine(DATA_DIR, "odo.txt"), sb.ToString());
        }
        catch (Exception) { }
    }

    void LoadOdo()
    {
        try
        {
            string f = Path.Combine(DATA_DIR, "odo.txt");
            if (!File.Exists(f)) return;

            odoKey.Clear();
            odoVal.Clear();

            string[] rows = File.ReadAllLines(f);
            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string row = rows[i].Trim();
                if (row.Length == 0 || row.StartsWith("#")) continue;
                int eq = row.LastIndexOf('=');
                if (eq < 1) continue;
                float val;
                if (!float.TryParse(row.Substring(eq + 1), NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out val)) continue;
                odoKey.Add(row.Substring(0, eq));
                odoVal.Add(val);
            }
        }
        catch (Exception) { }
    }

    void UpdateOdo(Vehicle v, float meters)
    {
        string k = TankKeyOf(v);
        if (k != curOdoKey)
        {
            if (curOdoKey.Length > 0) SetOdo(curOdoKey, odoM);
            curOdoKey = k;
            odoM = GetOdo(k);
        }

        if (meters > 0f) odoM = odoM + meters;
        SetOdo(curOdoKey, odoM);

        // su file ogni mezzo minuto: non ha senso scrivere a ogni frame
        int now = Game.GameTime;
        if (now > odoSaveAt + 30000)
        {
            odoSaveAt = now;
            SaveOdo();
        }
    }

    void SaveTanks()
    {
        try
        {
            Directory.CreateDirectory(DATA_DIR);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# serbatoi: modello:targa=litri%");
            int i;
            for (i = 0; i < tankKey.Count; i++)
            {
                sb.AppendLine(tankKey[i] + "=" + tankVal[i].ToString("0.#", CultureInfo.InvariantCulture));
            }
            File.WriteAllText(Path.Combine(DATA_DIR, "fuel.txt"), sb.ToString());
        }
        catch (Exception)
        {
        }
    }

    void LoadTanks()
    {
        try
        {
            string f = Path.Combine(DATA_DIR, "fuel.txt");
            if (!File.Exists(f)) return;

            tankKey.Clear();
            tankVal.Clear();
            string[] l = File.ReadAllLines(f);
            int i;
            for (i = 0; i < l.Length; i++)
            {
                string row = l[i].Trim();
                if (row.Length == 0 || row.StartsWith("#")) continue;
                int eq = row.LastIndexOf('=');
                if (eq < 1) continue;
                float val;
                if (!float.TryParse(row.Substring(eq + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out val)) continue;
                tankKey.Add(row.Substring(0, eq));
                tankVal.Add(val);
            }
        }
        catch (Exception)
        {
        }
    }

    bool IsHybrid(Vehicle v)
    {
        if (v == null || !v.Exists()) return false;
        int hash = v.Model.Hash;
        int i;
        for (i = 0; i < HYBRID.Length; i++)
        {
            if (Function.Call<int>(Hash.GET_HASH_KEY, HYBRID[i]) == hash) return true;
        }
        return false;
    }

    // elettriche aggiunte a mano: si legge il file del trainer e, se
    // installato, anche quello del mod dei lavori
    List<string> evExtra = new List<string>();
    bool evExtraLetto = false;

    void CaricaEvExtra()
    {
        evExtraLetto = true;
        evExtra.Clear();
        try
        {
            // il file del trainer, piu' quello del mod dei lavori se c'e'
            string[] cand = new string[] {
                Path.Combine(SCRIPTS_DIR, "Trainer\\elettriche.txt"),
                Path.Combine(SCRIPTS_DIR, "Lavori\\Fuzer\\elettriche.txt") };

            int k2;
            for (k2 = 0; k2 < cand.Length; k2++)
            {
                if (!File.Exists(cand[k2])) continue;
                string[] rows = File.ReadAllLines(cand[k2]);
                int i;
                for (i = 0; i < rows.Length; i++)
                {
                    string r = rows[i].Trim().ToLower();
                    if (r.Length > 0 && !r.StartsWith("#")) evExtra.Add(r);
                }
            }
        }
        catch { }
    }

    bool IsElectric(Vehicle v)
    {
        if (v == null || !v.Exists()) return false;

        int hash = v.Model.Hash;
        int i;
        for (i = 0; i < ELECTRIC.Length; i++)
        {
            if (Function.Call<int>(Hash.GET_HASH_KEY, ELECTRIC[i]) == hash) return true;
        }

        if (!evExtraLetto) CaricaEvExtra();
        for (i = 0; i < evExtra.Count; i++)
        {
            string r = evExtra[i];

            int num;
            if (int.TryParse(r, out num))
            {
                if (num == hash) return true;
            }
            else if (Function.Call<int>(Hash.GET_HASH_KEY, r) == hash) return true;
        }
        return false;
    }

    void UpdateFuel(Ped p)
    {
        if (tFuel == null || !tFuel.On) return;

        Vehicle v = p.CurrentVehicle;
        if (v == null || !v.Exists())
        {
            if (curTankKey.Length > 0)
            {
                SetTank(curTankKey, fuel);
                SaveTanks();
                curTankKey = "";
            }
            return;
        }

        // cambio mezzo: ogni veicolo ha il suo serbatoio
        string k = TankKeyOf(v);
        if (k != curTankKey)
        {
            if (curTankKey.Length > 0) SetTank(curTankKey, fuel);
            curTankKey = k;
            fuel = GetTank(k);
        }

        bool evNow = IsElectric(v);
        evCurrent = evNow;

        // tiene allineato l'olio al mezzo su cui sei, anche da fermo
        if (TankKeyOf(v) != curOilKey) UpdateOil(p, v, 0f, evNow);
        if (TankKeyOf(v) != curOdoKey) UpdateOdo(v, 0f);

        bool oilOn = (tOilWear == null || tOilWear.On);
        bool odoOn = (tOdoOn == null || tOdoOn.On);

        bool vehGod = (VehGodMode() == 3) || v.IsInvincible;   // solo Full toglie il consumo

        Ped drv = v.Driver;
        if (drv != null && drv.Handle == p.Handle && fuel > 0f && !vehGod)
        {
            float accel = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, 71);
            float meters = Function.Call<float>(Hash.GET_ENTITY_SPEED, v) * Game.LastFrameTime;

            if (evNow)
            {
                // l'elettrica consuma soprattutto in accelerazione: a gas
                // fermo scorre quasi gratis, col piede giu' beve il doppio
                fuel = fuel - meters * PCT_PER_METER_EV * (0.5f + 1.0f * accel);
            }
            else if (IsHybrid(v))
            {
                // la batteria segue il mezzo su cui sei
                string kb = TankKeyOf(v);
                if (kb != curBattKey) { curBattKey = kb; batt = GetBatt(kb); }

                int kmh2 = (int)(Function.Call<float>(Hash.GET_ENTITY_SPEED, v) * 3.6f);

                // tre fasce: 0-59 elettrico, 60-89 misto, 90 e oltre benzina.
                // A batteria scarica si passa comunque a benzina.
                // Il termico non si spegne appena scendi: una volta acceso
                // continua a girare finche' non cali sotto i 20 km/h.
                int fascia;
                if (kmh2 >= 90 || batt <= 1f) fascia = 2;
                else if (kmh2 >= 60) fascia = 1;
                else if (ibridaFascia > 0 && kmh2 > 20) fascia = 1;
                else fascia = 0;

                ibridaFascia = fascia;
                ibridaTermico = (fascia > 0);

                float dBatt = 100f / (HYB_KM_EV * 1000f);   // scarica piena

                // gas rilasciato: non si chiede potenza, quindi non si
                // consuma. E' il momento in cui la batteria recupera.
                bool rilascio = (accel < 0.05f);
                float freno = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, 72);

                if (fascia == 0)
                {
                    // ECO: solo batteria
                    if (!rilascio)
                        batt = batt - meters * dBatt * (0.6f + 0.8f * accel);
                }
                else if (fascia == 1)
                {
                    // IBRIDO: meta' e meta'
                    if (!rilascio)
                    {
                        batt = batt - meters * dBatt * 0.5f * (0.6f + 0.8f * accel);
                        fuel = fuel - meters * PCT_PER_METER_HY * 0.5f * (0.75f + 0.5f * accel);
                    }
                }
                else
                {
                    // BENZINA: solo termico, e la batteria si ricarica
                    fuel = fuel - meters * PCT_PER_METER_HY * (0.75f + 0.5f * accel);

                    float perM = (accel < 0.05f)
                        ? (100f / (HYB_KM_REGEN * 1000f))
                        : (100f / (HYB_KM_CHARGE * 1000f));
                    batt = batt + meters * perM;
                }

                // recupero in rilascio, e piu' forte in frenata
                if (fascia < 2 && rilascio)
                {
                    float rec = 100f / (HYB_KM_REGEN * 1000f);
                    if (freno > 0.1f) rec = rec * (1f + 4f * freno);
                    batt = batt + meters * rec;
                }

                if (batt > 100f) batt = 100f;
                if (batt < 0f) batt = 0f;

                SetBatt(curBattKey, batt);
            }
            else
            {
                // i camion grossi (motrici, mezzi industriali) hanno il
                // serbatoio doppio: stessa percentuale, il doppio dei km.
                // Le supercar invece bevono: consumo doppio, meta' autonomia.
                float pm = PCT_PER_METER;
                if (ECamionGrosso(v)) pm = pm * 0.5f;
                else if (ESuper(v)) pm = pm * 2f;
                fuel = fuel - meters * pm * (0.75f + 0.5f * accel);
            }

            if (oilOn) UpdateOil(p, v, meters, evNow);
            if (odoOn) UpdateOdo(v, meters);

            if (fuel <= 0f)
            {
                fuel = 0f;
                if (evNow)
                {
                    Notification.PostTicker("~r~" + L("Battery empty!", "Batteria scarica!") + "~s~ "
                        + L("Find a charging point.", "Raggiungi una colonnina."), false);
                }
                else
                {
                    Notification.PostTicker("~r~" + L("Out of fuel!", "Sei rimasto a secco!") + "~s~ "
                        + L("Find a gas station.", "Raggiungi un benzinaio."), false);
                }
            }
        }

        if (fuel <= 0f)
        {
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, v, false, true, true);
        }

        SetTank(curTankKey, fuel);
        UpdateRefuel(p, v);
    }

    void UpdateRefuel(Ped p, Vehicle v)
    {
        bool ev = evCurrent;
        float perPct = ev ? COST_PER_PCT_EV : COST_PER_PCT;
        float rate = ev ? CHARGE_PER_SEC : PUMP_PER_SEC;

        float speed = Function.Call<float>(Hash.GET_ENTITY_SPEED, v);

        // ---- rifornimento in corso ----
        // Si paga mano a mano che sale, non tutto in anticipo: se stacchi
        // a meta' hai pagato meta'.
        if (pumping)
        {
            if (speed > 0.5f || !NearStation(p))
            {
                pumping = false;
                Notification.PostTicker("~y~" + (ev ? L("Charging stopped", "Ricarica interrotta")
                                                   : L("Refuelling stopped", "Rifornimento interrotto")), false);
            }
            else
            {
                float add = rate * Game.LastFrameTime;
                if (fuel + add > 100f) add = 100f - fuel;

                pumpDebt = pumpDebt + add * perPct;
                int pay = (int)pumpDebt;
                if (pay > 0)
                {
                    int money = Game.Player.Money;
                    if (money < pay)
                    {
                        pumping = false;
                        Notification.PostTicker("~r~" + L("Out of money", "Soldi finiti"), false);
                        SetTank(curTankKey, fuel);
                        SaveTanks();
                        return;
                    }
                    Game.Player.Money = money - pay;
                    pumpDebt = pumpDebt - pay;
                }

                fuel = fuel + add;

                if (fuel >= 99.99f)
                {
                    fuel = 100f;
                    pumping = false;
                    Notification.PostTicker("~g~" + (ev ? L("Battery full", "Batteria carica")
                                                        : L("Tank full", "Pieno fatto")), false);
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PURCHASE",
                                  "HUD_LIQUOR_STORE_SOUNDSET", true);
                    SaveTanks();
                }

                SetTank(curTankKey, fuel);
                return;
            }
        }

        if (fuel >= 99.5f) return;
        if (speed > 0.5f) return;
        if (!NearStation(p)) return;

        // il rifornimento ora si fa dal pannello del distributore:
        // niente piu' avviso con la E
    }

    // sei davanti a un minimarket?
    bool NearMarket(Ped p)
    {
        Vector3 pp = p.Position;
        int i;
        for (i = 0; i < MKX.Length; i++)
        {
            float dx = pp.X - MKX[i];
            float dy = pp.Y - MKY[i];
            if (dx * dx + dy * dy < 20f * 20f) return true;
        }
        return false;
    }

    // sei fermo a un distributore? (le colonnine stanno li': stessa piazzola)
    bool NearStation(Ped p)
    {
        Vector3 pp = p.Position;
        int i;
        for (i = 0; i < GX.Length; i++)
        {
            float dx = pp.X - GX[i];
            float dy = pp.Y - GY[i];
            if (dx * dx + dy * dy < GAS_RADIUS * GAS_RADIUS) return true;
        }
        return false;
    }

    // ============================================================
    //  FAME E SETE
    // ============================================================
    int mkMade = 0;

    void MakeMarketBlips()
    {
        if (mkBlips != null) return;
        mkBlips = new int[MKX.Length];
        mkMade = 0;
    }

    void PumpMarketBlips()
    {
        if (mkBlips == null) return;
        if (mkMade >= MKX.Length) return;

        int made = 0;
        while (mkMade < MKX.Length && made < 2)
        {
            int i = mkMade;
            int b = Function.Call<int>(Hash.ADD_BLIP_FOR_COORD, MKX[i], MKY[i], MKZ[i]);
            Function.Call(Hash.SET_BLIP_SPRITE, b, 52);
            Function.Call(Hash.SET_BLIP_COLOUR, b, 0);
            Function.Call(Hash.SET_BLIP_SCALE, b, 0.7f);
            Function.Call(Hash.SET_BLIP_AS_SHORT_RANGE, b, false);
            // 3 = solo sulla mappa grande: sul radar sarebbero un casino
            Function.Call(Hash.SET_BLIP_DISPLAY, b, 3);
            mkBlips[i] = b;

            mkMade++;
            made++;
        }
    }


    // la mod della pesca scrive in cibo.txt quello che hai preso al bar:
    // "fame|sete" per riga. Noi lo leggiamo, lo applichiamo e svuotiamo.
    void MangiatoAlLago()
    {
        string f = "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V Enhanced"
                 + "\\scripts\\Attivita\\Pesca\\cibo.txt";
        try
        {
            if (!File.Exists(f)) return;
            string[] rows = File.ReadAllLines(f);
            if (rows.Length == 0) return;
            File.WriteAllText(f, "");
            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string[] c = rows[i].Trim().Split('|');
                if (c.Length < 2) continue;
                float fa, se;
                if (!float.TryParse(c[0], NumberStyles.Float, CultureInfo.InvariantCulture, out fa)) continue;
                if (!float.TryParse(c[1], NumberStyles.Float, CultureInfo.InvariantCulture, out se)) continue;
                hunger += fa;
                thirst += se;
            }
            if (hunger > 100f) hunger = 100f;
            if (thirst > 100f) thirst = 100f;
            SaveBody();
        }
        catch { }
    }

    void SaveBody()
    {
        try
        {
            Directory.CreateDirectory(DATA_DIR);
            string txt = "hunger=" + hunger.ToString("0.#", CultureInfo.InvariantCulture) + "\r\n"
                       + "thirst=" + thirst.ToString("0.#", CultureInfo.InvariantCulture) + "\r\n";
            File.WriteAllText(Path.Combine(DATA_DIR, "body.txt"), txt);
        }
        catch (Exception)
        {
        }
    }

    void LoadBody()
    {
        try
        {
            string f = Path.Combine(DATA_DIR, "body.txt");
            if (!File.Exists(f)) return;

            string[] l = File.ReadAllLines(f);
            int i;
            for (i = 0; i < l.Length; i++)
            {
                string[] kv = l[i].Split('=');
                if (kv.Length != 2) continue;
                float val;
                if (!float.TryParse(kv[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out val)) continue;
                if (kv[0].Trim() == "hunger") hunger = val;
                if (kv[0].Trim() == "thirst") thirst = val;
            }
        }
        catch (Exception)
        {
        }
    }






    int snackNext = 0;
    int lastHealth = -1;
    int lastMoney = -1;

    void Feed(float food, float drink, int nowS)
    {
        hunger = hunger + food;
        thirst = thirst + drink;
        if (hunger > 100f) hunger = 100f;
        if (thirst > 100f) thirst = 100f;

        lastMoney = Game.Player.Money;
        lastHealth = Game.Player.Character.Health;
        snackNext = nowS + 6000;

        SaveBody();
        Notification.PostTicker("~g~" + L("Fed", "Rifocillato"), false);
    }

    void CheckGameSnacks(Ped p)
    {
        int nowS = Game.GameTime;
        if (nowS < snackNext) return;
        snackNext = nowS + 250;

        // mangiare o bere fa salire la vita di colpo; la rigenerazione
        // naturale invece sale un punto alla volta
        int hp = p.Health;
        if (lastHealth < 0)
        {
            lastHealth = hp;
        }
        else
        {
            int jump = hp - lastHealth;
            lastHealth = hp;

            if (jump >= 8 && jump <= 80)   // sopra gli 80 e' una rinascita, non un panino
            {
                float gain = jump * 1.2f;
                if (gain > 60f) gain = 60f;
                Feed(gain, gain * 0.8f, nowS);
                return;
            }
        }

        // secondo segnale: con la vita gia' piena il cibo non cura, ma lo paghi.
        // Una spesa piccola a piedi e' quasi sempre un distributore o uno snack.
        int money = Game.Player.Money;
        if (lastMoney < 0)
        {
            lastMoney = money;
        }
        else
        {
            int spent = lastMoney - money;
            lastMoney = money;

            if (spent >= 1 && spent <= 25)
            {
                Feed(30f, 35f, nowS);
            }
        }
    }


    bool rechargeOff = false;

    void UpdateBody(Ped p)
    {
        int pidb = Function.Call<int>(Hash.PLAYER_ID);

        if (tBody == null || !tBody.On)
        {
            // sistema spento: ridai al gioco la sua rigenerazione
            if (rechargeOff)
            {
                Function.Call(Hash.SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER, pidb, 1.0f);
                rechargeOff = false;
            }
            return;
        }

        // il gioco puo' curarti normalmente, ma solo fino al livello che
        // fame e sete consentono: cosi' le ferite guariscono e non c'e'
        // nessun tiro alla fune con la barra
        rechargeOff = true;
        float avgNow = (hunger + thirst) * 0.5f;
        if (avgNow < 0f) avgNow = 0f;
        if (avgNow > 100f) avgNow = 100f;

        // il gioco ricarica solo finche' sei sotto il tetto: sopra, la spengo.
        // Cosi' le ferite guariscono ma non oltre quello che la pancia consente.
        int maxH2 = Function.Call<int>(Hash.GET_ENTITY_MAX_HEALTH, p);
        int curH2 = Function.Call<int>(Hash.GET_ENTITY_HEALTH, p);
        int floorH2 = (maxH2 > 150) ? 100 : 0;
        float capNow = floorH2 + (maxH2 - floorH2) * (avgNow / 100f);

        Function.Call(Hash.SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER, pidb,
                      curH2 < capNow ? 1.0f : 0.0f);

        int now = Game.GameTime;

        // consumo legato all'ORA DI GIOCO, sempre, 24 ore su 24
        int gh = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        if (lastBodyHour < 0)
        {
            lastBodyHour = gh;
        }
        else if (gh != lastBodyHour)
        {
            lastBodyHour = gh;

            // in invincibilita' il corpo non consuma
            bool god = (tGod != null && tGod.On) || p.IsInvincible;
            if (god)
            {
                return;
            }

            // se la mod della pesca ci ha lasciato detto che hai mangiato
            // al chiosco, lo contiamo qui
            MangiatoAlLago();

            hunger = hunger - 2.5f;
            thirst = thirst - 3.5f;
            if (hunger < 0f) hunger = 0f;
            if (thirst < 0f) thirst = 0f;

            if (hunger > 0f && hunger < 25f)
            {
                Notification.PostTicker("~o~" + L("You are hungry", "Hai fame"), false);
            }
            if (thirst > 0f && thirst < 25f)
            {
                Notification.PostTicker("~b~" + L("You are thirsty", "Hai sete"), false);
            }

            SaveBody();
        }

        // la vita segue di pari passo la media di fame e sete: al 70% di media
        // hai il 70% di vita. A zero muori. Cosi' il negozio ti vende da mangiare
        // anche quando sei solo un po' affamato.
        if (now > starveNext)
        {
            starveNext = now + 3000;

            bool god2 = (tGod != null && tGod.On) || p.IsInvincible;
            if (!god2)
            {
                float avg = (hunger + thirst) * 0.5f;
                if (avg < 0f) avg = 0f;
                if (avg > 100f) avg = 100f;

                // ATTENZIONE alla scala: per il giocatore la salute va da 100
                // (morto) a 200 (piena). Trattarla come 0-200 significava
                // ridurre a un filo di vita gia' a meta' fame.
                int maxH = Function.Call<int>(Hash.GET_ENTITY_MAX_HEALTH, p);
                int curH = Function.Call<int>(Hash.GET_ENTITY_HEALTH, p);
                int floorH = (maxH > 150) ? 100 : 0;

                float cap = floorH + (maxH - floorH) * (avg / 100f);

                if (curH > cap)
                {
                    Function.Call(Hash.SET_ENTITY_HEALTH, p, (int)cap);
                }
            }
        }

        if (p.IsInVehicle()) return;

        CheckGameSnacks(p);
    }



    // ============================================================
    //  AUTOVELOX
    //  140 in autostrada, 80 sulle altre strade, +10 di tolleranza.
    //  Se resti oltre la tolleranza per 10 secondi filati, multa.
    // ============================================================
    int SpeedLimitNow()
    {
        if (roadKind == 1) return tLimHwy != null ? tLimHwy.Val : 140;
        if (roadKind == 2) return tLimDirt != null ? tLimDirt.Val : 65;
        return tLimCity != null ? tLimCity.Val : 80;
    }

    string RoadLabel()
    {
        if (roadKind == 1) return L("highway", "autostrada");
        if (roadKind == 2) return L("dirt road", "sterrato");
        return L("town", "citta");
    }

    // ---------- contromano ----------

    void UpdateSpeedLimit(Ped p)
    {
        Vehicle v = p.CurrentVehicle;
        if (v == null || !v.Exists() || v.Driver == null || v.Driver.Handle != p.Handle)
        {
            overSince = -1f;
            return;
        }

        // i limiti valgono solo per i veicoli da strada: niente multe su
        // barche, aerei, elicotteri, treni e biciclette
        if (!HaCruscotto(v))
        {
            overSince = -1f;
            return;
        }

        int now = Game.GameTime;

        // che strada e'? si controlla due volte al secondo, non ogni frame
        if (now > speedCheckNext)
        {
            speedCheckNext = now + 500;

            Vector3 pp = v.Position;
            OutputArgument sh = new OutputArgument();
            OutputArgument ch = new OutputArgument();
            Function.Call(Hash.GET_STREET_NAME_AT_COORD, pp.X, pp.Y, pp.Z, sh, ch);

            string street = Function.Call<string>(Hash.GET_STREET_NAME_FROM_HASH_KEY, sh.GetResult<int>());
            if (street == null) street = "";
            string low = street.ToLower();

            bool named = street.Length > 0 && low != "unknown";
            bool onRoad = Function.Call<bool>(Hash.IS_POINT_ON_ROAD, pp.X, pp.Y, pp.Z, v);

            if (low.Contains("freeway") || low.Contains("highway"))
            {
                roadKind = 1;
            }
            else if (!named && !onRoad)
            {
                // solo quando sei davvero fuori strada: niente nome della via
                // E nessuna carreggiata sotto. Bastava una delle due e in campagna
                // dava sterrato anche sull'asfalto.
                roadKind = 2;
            }
            else
            {
                roadKind = 0;
            }
        }

        // senza l'interruttore il cartello si vede lo stesso, ma non succede nulla
        if (tSpeedLimit == null || !tSpeedLimit.On)
        {
            overSince = -1f;
            return;
        }

        int kmh = (int)(Function.Call<float>(Hash.GET_ENTITY_SPEED, v) * 3.6f);
        int limit = SpeedLimitNow();

        // il bip parte a meta' strada verso la multa, non subito
        if (overSince >= 0f && now - overSince >= (OVER_SECONDS * 1000) / 2)
        {
            if (now > beepNext)
            {
                beepNext = now + 600;
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Beep_Red",
                              "DLC_HEIST_HACKING_SNAKE_SOUNDS", true);
            }
        }

        if (kmh <= limit + SPEED_MARGIN)
        {
            overSince = -1f;
            return;
        }

        // oltre la tolleranza: parte il cronometro
        if (overSince < 0f)
        {
            overSince = now;
            return;
        }

        if (now - overSince < OVER_SECONDS * 1000) return;
        if (now < fineCooldown) return;

        // ---- limite superato troppo a lungo ----
        overSince = -1f;
        fineCooldown = now + 30000;

        OnSpeedingCaught(kmh, limit);
    }

    // QUI si decide cosa succede quando ti beccano: per ora solo un avviso.
    // Multa, polizia, punti patente... si aggiunge qui dentro.
    void OnSpeedingCaught(int kmh, int limit)
    {
        // multa: 100 in autostrada, 50 sulle altre strade
        int amount = (roadKind == 1) ? 100 : 50;

        int money = Game.Player.Money;
        if (money < amount) amount = money;
        Game.Player.Money = money - amount;

        Notification.PostTicker("~r~" + L("Speeding ticket", "Multa per eccesso")
            + "~s~  " + kmh + " / " + limit + " km/h  (" + RoadLabel() + ")   -$" + amount, false);
        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "LOSER", "HUD_AWARDS", true);

        // oltre il 50% del limite: la polizia si accorge di te
        if (kmh >= limit + limit / 2)
        {
            int pid = Function.Call<int>(Hash.PLAYER_ID);
            if (Function.Call<int>(Hash.GET_PLAYER_WANTED_LEVEL, pid) < 1)
            {
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, pid, 1, false);
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, pid, false);
            }
            Notification.PostTicker("~r~" + L("Police alerted", "La polizia ti ha visto"), false);
        }
    }

    // eseguita a inizio tick, prima di disegnare qualsiasi cosa
    void ProcessPending()
    {
        if (pendingClear)
        {
            pendingClear = false;
            ClearArea();
            return;
        }

        if (pendingRemove < 0) return;

        int idx = pendingRemove;
        pendingRemove = -1;

        if (idx >= pvRaw.Count) return;

        // 1. l'icona si NASCONDE: REMOVE_BLIP fa crashare questo build del gioco
        if (idx < pvBlip.Count)
        {
            HideBlip(pvBlip[idx]);
            pvBlip.RemoveAt(idx);
        }

        // 2. via il veicolo dal mondo, se non ci sei dentro
        Vehicle wv = FindWorldVehicle(idx);
        Ped ped = Game.Player.Character;
        if (wv != null && wv.Exists() && (ped.CurrentVehicle == null || ped.CurrentVehicle.Handle != wv.Handle))
        {
            wv.IsPersistent = false;
            wv.Delete();
        }

        // 3. via la riga dal file
        pvRaw.RemoveAt(idx);
        if (trackedIdx == idx) trackedIdx = -1;
        else if (trackedIdx > idx) trackedIdx--;

        SaveMyVehicles();
        BuildMyVehicles();

        if (cur == mMyVeh)
        {
            menus[cur].Sel = FirstSelectable(cur);
            menus[cur].Top = 0;
        }

        Notification.PostTicker("~g~" + L("Removed", "Rimosso"), false);
    }

    // cancella i veicoli vuoti entro 60 metri: non tocca quello che guidi
    // ne' quelli con qualcuno a bordo
    void ClearArea()
    {
        Ped ped = Game.Player.Character;
        Vector3 me = ped.Position;
        int mine = (ped.CurrentVehicle != null) ? ped.CurrentVehicle.Handle : 0;

        Vehicle[] all = World.GetAllVehicles();
        int i;
        int killed = 0;

        for (i = 0; i < all.Length; i++)
        {
            Vehicle v = all[i];
            if (v == null || !v.Exists()) continue;
            if (v.Handle == mine) continue;
            if (v.IsSeatFree(VehicleSeat.Driver) == false) continue;

            Vector3 d = v.Position - me;
            if (d.Length() > 60f) continue;

            v.IsPersistent = false;
            v.Delete();
            killed++;
        }

        Notification.PostTicker("~g~" + killed + "~s~ " + L("vehicles removed", "veicoli rimossi"), false);
    }

    void RemoveDuplicates(int idx, Vehicle[] all, Vehicle keep)
    {
        if (keep == null || !keep.Exists()) return;

        int hash = PvHash(idx);
        string plate = PvPlate(idx);

        Ped ped = Game.Player.Character;
        int mine = (ped.CurrentVehicle != null) ? ped.CurrentVehicle.Handle : 0;

        int i;
        int killed = 0;
        for (i = 0; i < all.Length && killed < 4; i++)
        {
            Vehicle v = all[i];
            if (v == null || !v.Exists()) continue;
            if (v.Handle == keep.Handle) continue;
            if (v.Handle == mine) continue;
            if (v.Model.Hash != hash) continue;
            if (PlateOf(v) != plate) continue;

            v.IsPersistent = false;
            v.Delete();
            killed++;
        }
    }

    void LazyVehicles()
    {
        if (tPersist == null || !tPersist.On)
        {
            if (pvBlip.Count > 0) ClearBlips();
            return;
        }
        if (pvRaw.Count == 0)
        {
            if (pvBlip.Count > 0) ClearBlips();
            return;
        }

        int now = Game.GameTime;
        if (now < lazyNext) return;
        lazyNext = now + LAZY_INTERVAL;

        Ped ped = Game.Player.Character;
        if (ped == null || !ped.Exists()) return;
        Vector3 me = ped.Position;
        Vehicle[] all = World.GetAllVehicles();

        UpdateBlips(all);

        int i;
        for (i = 0; i < pvRaw.Count; i++)
        {
            Vector3 sp = new Vector3(PvFloat(i, 6), PvFloat(i, 7), PvFloat(i, 8));
            Vector3 d = sp - me;
            float dist = d.Length();

            Vehicle wv = FindWorldVehicle(i, all);

            // la distanza per la pulizia si misura sul veicolo VERO, non sul
            // punto salvato: teleportandoti col mezzo il punto salvato resta
            // lontano e prima lo cancellavo mentre ce l'avevi accanto
            if (wv != null && wv.Exists())
            {
                Vector3 dv = wv.Position - me;
                dist = dv.Length();
            }

            // se per qualsiasi motivo ne esistono piu' di uno con lo stesso
            // modello e la stessa targa, restano doppioni: se ne tiene uno solo
            RemoveDuplicates(i, all, wv);

            if (dist <= LAZY_RANGE)
            {
                if (wv == null)
                {
                    SpawnSaved(i, false);
                    trackedIdx = -1;
                    return;   // uno per volta, cosi' non blocca il frame
                }
            }
            else if (dist > CLEANUP_RANGE)
            {
                // non toccare quello che stai guidando
                if (wv != null && wv.Exists() && ped.CurrentVehicle != wv)
                {
                    wv.IsPersistent = false;
                    wv.Delete();
                }
            }
        }
    }

    Vehicle FindWorldVehicle(int idx)
    {
        return FindWorldVehicle(idx, World.GetAllVehicles());
    }

    Vehicle FindWorldVehicle(int idx, Vehicle[] all)
    {
        if (idx < 0 || idx >= pvRaw.Count) return null;
        if (all == null) return null;

        int hash = PvHash(idx);
        string plate = PvPlate(idx);

        int i;
        for (i = 0; i < all.Length; i++)
        {
            Vehicle v = all[i];
            if (v == null || !v.Exists()) continue;
            if (v.Model.Hash != hash) continue;
            if (PlateOf(v) != plate) continue;
            return v;
        }
        return null;
    }

    void SpawnSaved(int idx, bool atPlayer)
    {
        if (idx < 0 || idx >= pvRaw.Count) return;

        int hash = PvHash(idx);
        if (!Function.Call<bool>(Hash.IS_MODEL_IN_CDIMAGE, hash) || !Function.Call<bool>(Hash.IS_MODEL_A_VEHICLE, hash))
        {
            Notification.PostTicker("~r~" + L("Invalid model", "Modello non valido") + ":~s~ "
                + PvField(idx, 10), false);
            return;
        }

        Ped ped = Game.Player.Character;

        // se il veicolo esiste gia' nel mondo lo si sposta, non se ne crea un altro
        Vehicle exist = FindWorldVehicle(idx);
        if (exist != null)
        {
            if (atPlayer)
            {
                Vector3 dst = ped.Position + ped.ForwardVector * 5.0f;
                exist.Position = dst;
                exist.Heading = ped.Heading + 90f;
                Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, exist);
                Notification.PostTicker("~g~" + PvField(idx, 10), false);
            }
            exist.IsPersistent = true;
            trackedIdx = idx;
            return;
        }

        Model m = new Model(hash);
        m.Request();
        int waited = 0;
        while (!m.IsLoaded && waited < 4000)
        {
            Script.Wait(50);
            waited += 50;
        }
        if (!m.IsLoaded) return;

        Vector3 pos;
        float head;
        if (atPlayer)
        {
            pos = ped.Position + ped.ForwardVector * 5.0f;
            head = ped.Heading + 90f;
        }
        else
        {
            pos = new Vector3(PvFloat(idx, 6), PvFloat(idx, 7), PvFloat(idx, 8));
            head = PvFloat(idx, 9);
        }

        Vehicle v = World.CreateVehicle(m, pos, head);
        m.MarkAsNoLongerNeeded();
        if (v == null || !v.Exists()) return;

        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
        Function.Call(Hash.SET_VEHICLE_COLOURS, v, SafeColor(PvInt(idx, 2)), SafeColor(PvInt(idx, 3)));
        Function.Call(Hash.SET_VEHICLE_EXTRA_COLOURS, v, SafeColor(PvInt(idx, 4)), SafeColor(PvInt(idx, 5)));
        Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, v, PvPlate(idx));
        ApplyMods(v, PvField(idx, 11));
        Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, v);
        v.IsPersistent = true;

        trackedIdx = idx;

        if (atPlayer)
        {
            Notification.PostTicker("~g~" + PvField(idx, 10), false);
        }
    }

    Vehicle TargetVehicle()
    {
        Ped ped = Game.Player.Character;
        Vehicle v = ped.CurrentVehicle;
        if (v != null && v.Exists())
        {
            return v;
        }

        Vehicle near = World.GetClosestVehicle(ped.Position, 12f);
        if (near != null && near.Exists())
        {
            return near;
        }
        return null;
    }

    static readonly string[] WEATHER_ID = new string[] {
        "EXTRASUNNY", "CLEAR", "CLOUDS", "SMOG", "FOGGY", "OVERCAST",
        "RAIN", "THUNDER", "CLEARING", "NEUTRAL", "SNOW", "BLIZZARD",
        "SNOWLIGHT", "XMAS", "HALLOWEEN"
    };

    // ---------- luoghi ----------
    static readonly string[] KNOWN = new string[] {
        "Casa di Michael|-813.0|179.0|72.2",
        "Casa di Franklin|7.9|539.0|176.0",
        "Roulotte di Trevor|1985.7|3812.2|32.2",
        "Aeroporto LS|-1037.0|-2737.0|20.2",
        "Sandy Shores|1961.0|3740.0|32.3",
        "Paleto Bay|-275.0|6635.0|7.4",
        "Monte Chiliad|501.5|5604.5|797.9",
        "Scritta Vinewood|711.0|1198.0|348.0",
        "Tetto Maze Bank|-75.0|-818.0|326.0",
        "Molo Del Perro|-1850.0|-1231.0|13.0",
        "Carcere|1845.0|2585.0|45.0",
        "Casino|925.0|46.0|80.9",
        "Porto|850.0|-3000.0|5.9",
        "Base militare|-2050.0|3200.0|32.8"
    };

    void BuildKnownPlaces(int menu)
    {
        int i;
        for (i = 0; i < KNOWN.Length; i++)
        {
            string[] f = KNOWN[i].Split('|');
            TItem it = AddAction(menu, f[0], f[0], 306);
            it.Data = f[1] + "|" + f[2] + "|" + f[3];
        }
    }

    string PlacesFile()
    {
        return Path.Combine(DATA_DIR, "places.txt");
    }

    void LoadPlaces()
    {
        placeRaw.Clear();
        try
        {
            if (!File.Exists(PlacesFile())) return;
            string[] l = File.ReadAllLines(PlacesFile());
            int i;
            for (i = 0; i < l.Length; i++)
            {
                string row = l[i].Trim();
                if (row.Length == 0 || row.StartsWith("#")) continue;
                if (row.Split('|').Length < 4) continue;
                placeRaw.Add(row);
            }
        }
        catch (Exception)
        {
        }
    }

    void SavePlaces()
    {
        try
        {
            Directory.CreateDirectory(DATA_DIR);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# I MIEI PUNTI - nome|x|y|z");
            int i;
            for (i = 0; i < placeRaw.Count; i++) sb.AppendLine(placeRaw[i]);
            File.WriteAllText(PlacesFile(), sb.ToString());
        }
        catch (Exception)
        {
        }
    }

    void BuildPlaces()
    {
        if (mPlaces < 0) return;

        menus[mPlaces].Items.Clear();
        menus[mPlaces].Sel = 0;
        menus[mPlaces].Top = 0;

        int i;
        for (i = 0; i < placeRaw.Count; i++)
        {
            string[] f = placeRaw[i].Split('|');
            TItem it = AddAction(mPlaces, f[0], f[0], 304);
            it.Data = f[1] + "|" + f[2] + "|" + f[3];

            TItem rm = AddAction(mPlaces, "   " + L("remove", "rimuovi"), "   " + L("remove", "rimuovi"), 305);
            rm.Data = i.ToString();
        }
    }

    void AutoTeleport()
    {
        if (tAutoTp == null || tAutoTp.Sel == 0) return;

        int now = Game.GameTime;
        if (now < autoTpNext) return;
        autoTpNext = now + 1000;

        Vector3 dest = Vector3.Zero;

        if (tAutoTp.Sel == 1)
        {
            int b = Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, 8);
            if (!Function.Call<bool>(Hash.DOES_BLIP_EXIST, b))
            {
                lastAutoTp = Vector3.Zero;   // waypoint tolto: pronto per il prossimo
                return;
            }
            dest = Function.Call<Vector3>(Hash.GET_BLIP_INFO_ID_COORD, b);
        }
        else
        {
            dest = FindObjectiveBlip();
            if (dest.X == 0f && dest.Y == 0f) return;
        }

        // gia' fatto per questo punto? non insistere
        Vector3 d = dest - lastAutoTp;
        if (d.Length() < 5f) return;

        lastAutoTp = dest;
        autoTpNext = now + 4000;
        TeleportTo(dest);
    }

    // Velocita' del tempo: di serie GTA fa passare un minuto di gioco ogni
    // 2 secondi reali. Fermiamo il suo orologio e lo avanziamo noi al ritmo
    // scelto, come faceva la mod vecchia.
    // i moltiplicatori, nello stesso ordine della lista qui sopra
    static readonly float[] VEL_MOLT = new float[] {
        0.1f, 0.25f, 0.5f, 0.75f,
        1f,
        1.5f, 2f, 3f, 4f, 6f, 8f,
        12f, 16f, 24f, 32f, 60f };
    const int VEL_NORMALE = 4;

    void UpdateClockSpeed()
    {
        int sel = (tTimeSpeed != null) ? tTimeSpeed.Sel : VEL_NORMALE;

        // x1: si ridA' l'orologio al gioco e non ci si pensa piu'
        if (sel == VEL_NORMALE)
        {
            if (clockTaken)
            {
                Function.Call(Hash.PAUSE_CLOCK, false);
                clockTaken = false;
            }
            return;
        }

        if (sel < 0 || sel >= VEL_MOLT.Length) sel = VEL_NORMALE;
        int msPerMin = (int)(2000f / VEL_MOLT[sel]);
        if (msPerMin < 20) msPerMin = 20;

        if (!clockTaken)
        {
            Function.Call(Hash.PAUSE_CLOCK, true);
            clockTaken = true;
            nextGameMin = Game.GameTime + msPerMin;
            return;
        }

        // dopo una pausa lunga non si recupera il tempo perso
        if (Game.GameTime - nextGameMin > 10000)
        {
            nextGameMin = Game.GameTime + msPerMin;
            return;
        }

        while (Game.GameTime >= nextGameMin)
        {
            Function.Call(Hash.ADD_TO_CLOCK_TIME, 0, 1, 0);
            nextGameMin = nextGameMin + msPerMin;
        }
    }

    void SetWeather(int idx)
    {
        if (idx < 0 || idx >= WEATHER_ID.Length) return;
        Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, WEATHER_ID[idx]);
        Function.Call(Hash.SET_WEATHER_TYPE_PERSIST, WEATHER_ID[idx]);
    }

    // ---- avviso delle spie del cruscotto ----
    bool spiaChiaveWas = false;
    int porteAt = 0;
    int porteSnd = -1;
    bool ibridaTermico = false;   // ibrida: termico inserito
    int ibridaFascia = 0;         // 0 eco, 1 ibrido, 2 benzina

    void StopPorte()
    {
        if (porteSnd < 0) return;
        Function.Call(Hash.STOP_SOUND, porteSnd);
        Function.Call(Hash.RELEASE_SOUND_ID, porteSnd);
        porteSnd = -1;
    }
    bool spiaGommaWas = false;
    int spiaAt = 0;

    void Spia()
    {
        // non piu' di un avviso ogni due secondi, altrimenti diventa una raffica
        if (Game.GameTime - spiaAt < 2000) return;
        spiaAt = Game.GameTime;
        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1,
                      "TIMER_STOP", "HUD_MINI_GAME_SOUNDSET", true);
    }

    // ---- acqua degli oceani ----
    // Stessa tecnica del Water Hack di Menyoo: MODIFY_WATER(x, y, raggio,
    // altezza) su tanti punti disposti in cerchi concentrici attorno al
    // giocatore, uno ogni 3,5 metri, con raggio 0 e altezza -800.
    void SvuotaOceano()
    {
        Ped pl = Game.Player.Character;
        if (pl == null || !pl.Exists()) return;

        Vector3 pos = pl.Position;

        const float PASSO   = 3.5f;     // distanza fra un punto e l'altro
        const float RAGGIO  = 650f;     // fin dove si arriva, come Menyoo
        const float ANGOLO  = 13f;      // gradi fra un raggio e l'altro
        const float QUOTA   = -800f;    // dove finisce l'acqua

        Function.Call(Hash.MODIFY_WATER, pos.X, pos.Y, 0f, QUOTA);

        float u, d;
        for (u = 0f; u < 360f; u += ANGOLO)
        {
            float rad = u * 0.0174533f;
            float cs = (float)Math.Cos(rad);
            float sn = (float)Math.Sin(rad);

            for (d = PASSO; d < RAGGIO; d += PASSO)
            {
                Function.Call(Hash.MODIFY_WATER,
                              pos.X + d * cs, pos.Y + d * sn, 0f, QUOTA);
            }
        }
    }

    // ============================================================
    //  MENU TEST - tutta roba da provare
    // ============================================================
    void TickTest()
    {
        Ped p = Game.Player.Character;
        if (p == null || !p.Exists()) return;
        int pid = Game.Player.Handle;

        // ---- popolazione ----
        if (tMaxPop != null && tMaxPop.On)
        {
            Function.Call(Hash.SET_PED_POPULATION_BUDGET, 3);
            Function.Call(Hash.SET_VEHICLE_POPULATION_BUDGET, 3);
            Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, 1f);
            Function.Call(Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, 1f, 1f);
            Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1f);
            Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1f);
            Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1f);
            Function.Call(Hash.SET_AMBIENT_VEHICLE_RANGE_MULTIPLIER_THIS_FRAME, 1f);
        }

        if (tNoCops != null)
        {
            Function.Call(Hash.SET_CREATE_RANDOM_COPS, !tNoCops.On);
            Function.Call(Hash.SET_CREATE_RANDOM_COPS_NOT_ON_SCENARIOS, !tNoCops.On);

            // niente pattuglie chiamate addosso: auto della polizia, rinforzi,
            // pattuglie in attesa e motovedette
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 1, !tNoCops.On);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 6, !tNoCops.On);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 7, !tNoCops.On);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 9, !tNoCops.On);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 10, !tNoCops.On);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 13, !tNoCops.On);

            // i due native fermano solo le nuove nascite: quelli gia' in giro
            // restano, quindi ogni due secondi si tolgono di mezzo
            if (tNoCops.On && Game.GameTime > copNext)
            {
                copNext = Game.GameTime + 2000;

                Ped[] cops = World.GetNearbyPeds(p, 250f);
                int ci;
                for (ci = 0; ci < cops.Length; ci++)
                {
                    Ped c = cops[ci];
                    if (c == null || !c.Exists()) continue;
                    if (c.Handle == p.Handle) continue;

                    int tipo = Function.Call<int>(Hash.GET_PED_TYPE, c);
                    // 6 = poliziotto, 27 = SWAT, 29 = esercito
                    if (tipo != 6 && tipo != 27 && tipo != 29) continue;

                    Vehicle cv2 = c.CurrentVehicle;
                    c.IsPersistent = false;
                    Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, c, true, true);
                    c.Delete();

                    if (cv2 != null && cv2.Exists())
                    {
                        Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, cv2, true, true);
                        cv2.Delete();
                    }
                }
            }
        }

        // servizi di intervento: 2 elicottero polizia, 4 SWAT in auto,
        // 8 posti di blocco, 12 elicottero SWAT, 14 esercito, 13 motovedette
        if (tNoHeli != null)
        {
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 2, !tNoHeli.On);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 12, !tNoHeli.On);
            if (tNoHeli.On)
            {
                Function.Call(Hash.BLOCK_DISPATCH_SERVICE_RESOURCE_CREATION, 2, true);
                Function.Call(Hash.BLOCK_DISPATCH_SERVICE_RESOURCE_CREATION, 12, true);
            }
        }

        if (tNoSwat != null)
        {
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 4, !tNoSwat.On);
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 14, !tNoSwat.On);
        }

        if (tNoRoadBlock != null)
        {
            Function.Call(Hash.ENABLE_DISPATCH_SERVICE, 8, !tNoRoadBlock.On);
        }

        if (tManyParked != null && tManyParked.On)
        {
            Function.Call(Hash.SET_NUMBER_OF_PARKED_VEHICLES, 500);
            Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, 1f);
        }

        if (tNoTrains != null) Function.Call(Hash.SET_RANDOM_TRAINS, !tNoTrains.On);
        if (tNoBoats != null) Function.Call(Hash.SET_RANDOM_BOATS, !tNoBoats.On);
        if (tNoGarbage != null) Function.Call(Hash.SET_GARBAGE_TRUCKS, !tNoGarbage.On);

        // ---- pedoni e guidatori ----
        bool serve = (tArmedPeds != null && tArmedPeds.On)
                  || (tRiot != null && tRiot.On)
                  || (tAllHateMe != null && tAllHateMe.On)
                  || (tAllFlee != null && tAllFlee.On)
                  || (tPedsGod != null && tPedsGod.On)
                  || (tPedsSniper != null && tPedsSniper.On)
                  || (tHotDrivers != null && tHotDrivers.On)
                  || (tSlowDrivers != null && tSlowDrivers.On);

        if (!serve)
        {
            if (testFatti.Count > 0) testFatti.Clear();
        }
        else if (Game.GameTime > testNext)
        {
            testNext = Game.GameTime + 1500;

            if (tRiot != null && tRiot.On)
            {
                int cm = Function.Call<int>(Hash.GET_HASH_KEY, "CIVMALE");
                int cf = Function.Call<int>(Hash.GET_HASH_KEY, "CIVFEMALE");
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, cm, cm);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, cm, cf);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, cf, cm);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, cf, cf);
            }

            Ped[] vicini = World.GetNearbyPeds(p, 120f);
            int i;
            for (i = 0; i < vicini.Length; i++)
            {
                Ped q = vicini[i];
                if (q == null || !q.Exists() || q.IsDead) continue;
                if (q.Handle == p.Handle) continue;
                if (!Function.Call<bool>(Hash.IS_PED_HUMAN, q)) continue;

                bool nuovo = !testFatti.Contains(q.Handle);
                if (nuovo)
                {
                    testFatti.Add(q.Handle);
                    if (testFatti.Count > 400) testFatti.RemoveAt(0);
                }

                if (tPedsGod != null && tPedsGod.On) q.IsInvincible = true;

                if (tArmedPeds != null && tArmedPeds.On && nuovo)
                {
                    int arma = Function.Call<int>(Hash.GET_HASH_KEY,
                        (i % 3 == 0) ? "WEAPON_PISTOL"
                                     : ((i % 3 == 1) ? "WEAPON_MICROSMG" : "WEAPON_BAT"));
                    Function.Call(Hash.GIVE_WEAPON_TO_PED, q, arma, 250, false, true);
                }

                if (tPedsSniper != null && tPedsSniper.On && nuovo)
                {
                    Function.Call(Hash.SET_PED_ACCURACY, q, 100);
                    Function.Call(Hash.SET_PED_COMBAT_ABILITY, q, 2);
                    Function.Call(Hash.SET_PED_SEEING_RANGE, q, 200f);
                    Function.Call(Hash.SET_PED_HEARING_RANGE, q, 200f);
                }

                if (tRiot != null && tRiot.On && nuovo)
                {
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, q, 46, true);
                    Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, q, 0, false);
                    Function.Call(Hash.SET_PED_KEEP_TASK, q, true);

                    int j = (i + 1) % vicini.Length;
                    Ped bersaglio = vicini[j];
                    if (bersaglio != null && bersaglio.Exists() && bersaglio.Handle != q.Handle)
                        Function.Call(Hash.TASK_COMBAT_PED, q, bersaglio, 0, 16);
                }

                if (tAllHateMe != null && tAllHateMe.On && nuovo)
                {
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, q, 46, true);
                    Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, q, 0, false);
                    Function.Call(Hash.SET_PED_KEEP_TASK, q, true);
                    Function.Call(Hash.TASK_COMBAT_PED, q, p, 0, 16);
                }

                if (tAllFlee != null && tAllFlee.On && nuovo)
                {
                    Function.Call(Hash.SET_PED_KEEP_TASK, q, true);
                    Function.Call(Hash.TASK_SMART_FLEE_PED, q, p, 200f, -1, false, false);
                }

                if (q.IsInVehicle())
                {
                    if (tHotDrivers != null && tHotDrivers.On)
                    {
                        Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, q, 1.0f);
                        Function.Call(Hash.SET_DRIVER_ABILITY, q, 1.0f);
                    }
                    else if (tSlowDrivers != null && tSlowDrivers.On)
                    {
                        Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, q, 0.0f);
                        Function.Call(Hash.SET_DRIVER_ABILITY, q, 0.0f);
                    }
                }
            }
        }
    }

    int testNext = 0;
    int copNext = 0;
    int sparoAt = 0;
    int pugnoAt = 0;
    bool bulletTimeOn = false;

    // spinte da dare nei frame successivi al colpo
    List<Ped> spintaPed = new List<Ped>();
    List<Vector3> spintaDir = new List<Vector3>();
    List<int> spintaFrame = new List<int>();

    // ---- trucchi sulle armi (menu TEST) ----
    // Come Menyoo: mentre premi il grilletto si sparano altri proiettili
    // dalla stessa arma, partendo dalla canna verso il centro schermo.
    void TickArmi()
    {
        Ped p = Game.Player.Character;
        if (p == null || !p.Exists() || p.IsDead) return;
        int pid = Game.Player.Handle;

        if (tRunSpeed != null && tRunSpeed.Sel > 0)
        {
            float vel = CORSA[tRunSpeed.Sel];
            float nat = vel > 1.49f ? 1.49f : vel;
            Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, pid, nat);

            // oltre 1.49 il gioco non va: si aggiunge spinta mentre scatti
            if (vel > 1.49f
                && Function.Call<bool>(Hash.IS_PED_ON_FOOT, p)
                && Function.Call<bool>(Hash.IS_PED_SPRINTING, p))
            {
                Vector3 vv = p.Velocity;
                float piatta = (float)Math.Sqrt(vv.X * vv.X + vv.Y * vv.Y);
                float voluta = 7.2f * (vel / 1.49f);   // 7.2 m/s e' lo scatto pieno
                if (piatta > 0.5f && piatta < voluta)
                {
                    float f = voluta / piatta;
                    p.Velocity = new Vector3(vv.X * f, vv.Y * f, vv.Z);
                }
            }
        }

        if (tDmgGun != null)
            Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, pid, MOLTIPL[tDmgGun.Sel]);
        if (tDmgMelee != null)
            Function.Call(Hash.SET_PLAYER_MELEE_WEAPON_DAMAGE_MODIFIER, pid,
                          MOLTIPL[tDmgMelee.Sel], false);
        if (tDefGun != null)
            Function.Call(Hash.SET_PLAYER_WEAPON_DEFENSE_MODIFIER, pid,
                          MOLTIPL[tDefGun.Sel] > 0f ? 1f / MOLTIPL[tDefGun.Sel] : 0f);
        if (tDefMelee != null)
            Function.Call(Hash.SET_PLAYER_MELEE_WEAPON_DEFENSE_MODIFIER, pid,
                          MOLTIPL[tDefMelee.Sel] > 0f ? 1f / MOLTIPL[tDefMelee.Sel] : 0f);

        // ---- le botte degli altri non ti toccano ----
        if (tNoBotte != null)
        {
            // solo il corpo a corpo: proiettili, fuoco ed esplosioni restano
            // come sono, quelli hanno gia' le loro voci
            Function.Call(Hash.SET_ENTITY_PROOFS, p,
                          false, false, false, false, tNoBotte.On, false, false, false);
            Function.Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, p, !tNoBotte.On);
            if (tNoBotte.On)
            {
                // niente barcollate: non ti muovono nemmeno
                Function.Call(Hash.SET_PED_CAN_RAGDOLL, p, false);
                Function.Call(Hash.SET_PLAYER_MELEE_WEAPON_DEFENSE_MODIFIER, pid, 0f);
            }
        }

        // ---- spinte rimaste in coda dal colpo del frame prima ----
        int sp;
        for (sp = spintaPed.Count - 1; sp >= 0; sp--)
        {
            Ped bersaglio = spintaPed[sp];
            if (bersaglio == null || !bersaglio.Exists() || spintaFrame[sp] <= 0)
            {
                spintaPed.RemoveAt(sp);
                spintaDir.RemoveAt(sp);
                spintaFrame.RemoveAt(sp);
                continue;
            }

            // sulle persone: velocita' imposta (non si puo' ignorare) piu' una
            // spinta di appoggio con numeri bassi, che e' quello che vuole il
            // ragdoll per partire davvero
            Vector3 f2 = spintaDir[sp];
            bersaglio.Velocity = f2;
            Function.Call(Hash.APPLY_FORCE_TO_ENTITY, bersaglio, 1,
                          f2.X * 0.6f, f2.Y * 0.6f, f2.Z * 0.6f,
                          0f, 0f, 0f, 0, false, false, true, false, true);

            spintaFrame[sp] = spintaFrame[sp] - 1;
        }

        // ---- pugni pesanti: chi prende una botta parte per aria ----
        if (tPugni != null && tPugni.Sel > 0 && Function.Call<bool>(Hash.IS_PED_ON_FOOT, p))
        {
            float spinta = PUGNI[tPugni.Sel];
            Vector3 mio = p.Position;

            // Il danno non e' un segnale affidabile: dopo due o tre botte il
            // gioco smette di registrarlo (bersaglio gia' a terra o morto).
            // Quindi si guarda direttamente il colpo: tasto attacco premuto,
            // a mani nude o con un'arma da mischia, e chi ti sta davanti parte.
            // "a mani nude" = non hai in mano un'arma da fuoco o da lancio.
            // Leggere l'arma corrente e confrontare i gruppi non funzionava:
            // il native tornava zero e risultavi sempre armato.
            // 2 = armi da fuoco, 4 = da lancio  ->  6 = tutte e due
            bool mani = !Function.Call<bool>(Hash.IS_PED_ARMED, p, 6);

            // 24 attacco, 140/141 mischia leggera e pesante, 263/264 mischia 1 e 2
            bool premuto =
                   Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 24)
                || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, 24)
                || Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 140)
                || Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 141)
                || Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 263)
                || Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 264);

            bool colpoOra = false;
            if (premuto && Game.GameTime - pugnoAt > 250)
            {
                pugnoAt = Game.GameTime;
                colpoOra = mani;
            }

            if (colpoOra)
            {
                Vector3 avanti = p.ForwardVector;
                Ped[] davanti = World.GetNearbyPeds(p, 3f);
                int d1;
                for (d1 = 0; d1 < davanti.Length; d1++)
                {
                    Ped q1 = davanti[d1];
                    if (q1 == null || !q1.Exists()) continue;
                    if (q1.Handle == p.Handle) continue;

                    Vector3 v1 = q1.Position - mio;
                    float l1 = v1.Length();
                    if (l1 < 0.1f || l1 > 2.6f) continue;

                    // solo chi ti sta davanti, non alle spalle
                    float dot = (v1.X * avanti.X + v1.Y * avanti.Y) / l1;
                    if (dot < 0.35f) continue;

                    // uno stesso tizio non va ripreso finche' e' ancora per aria
                    if (spintaPed.Contains(q1)) continue;

                    Function.Call(Hash.SET_PED_TO_RAGDOLL, q1, 5000, 6000, 0, true, true, false);

                    spintaPed.Add(q1);
                    spintaDir.Add(new Vector3(v1.X / l1 * spinta,
                                              v1.Y / l1 * spinta,
                                              spinta * 0.5f));
                    spintaFrame.Add(5);
                }

                // e i mezzi che hai davanti: qui la forza funziona, ma serve
                // un numero molto piu' grande che per una persona
                Vehicle[] vDav = World.GetNearbyVehicles(mio, 4f);
                int d2;
                for (d2 = 0; d2 < vDav.Length; d2++)
                {
                    Vehicle m1 = vDav[d2];
                    if (m1 == null || !m1.Exists()) continue;
                    if (p.CurrentVehicle != null && p.CurrentVehicle.Exists()
                        && m1.Handle == p.CurrentVehicle.Handle) continue;

                    Vector3 w1 = m1.Position - mio;
                    float lw = w1.Length();
                    if (lw < 0.1f || lw > 3.5f) continue;

                    float dotv = (w1.X * avanti.X + w1.Y * avanti.Y) / lw;
                    if (dotv < 0.2f) continue;

                    float fv = spinta * 150f;
                    Function.Call(Hash.APPLY_FORCE_TO_ENTITY, m1, 1,
                                  w1.X / lw * fv, w1.Y / lw * fv, 0.5f * fv,
                                  0f, 0f, 0f, 0, false, false, true, false, true);
                }

            }

            Vehicle[] vicVeh = World.GetNearbyVehicles(mio, 5f);
            int i3;
            for (i3 = 0; i3 < vicVeh.Length; i3++)
            {
                Vehicle q3 = vicVeh[i3];
                if (q3 == null || !q3.Exists()) continue;
                if (!Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY, q3, p, true)) continue;

                Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY, q3);

                Vector3 dv = q3.Position - mio;
                float lv = dv.Length();
                if (lv < 0.1f) continue;
                dv = new Vector3(dv.X / lv, dv.Y / lv, 0.35f);

                // un'auto pesa molto di piu' di una persona
                float sv = spinta * 150f;
                Function.Call(Hash.APPLY_FORCE_TO_ENTITY, q3, 1,
                              dv.X * sv, dv.Y * sv, dv.Z * sv,
                              0f, 0f, 0f, 0, false, false, true, false, true);
            }
        }

        bool multi = (tMultiShot != null && tMultiShot.On);
        bool rapid = (tRapidFire != null && tRapidFire.On);
        bool bt    = (tBulletTime != null && tBulletTime.On);

        bool mira = Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING, pid);

        if (bt)
        {
            if (mira && !bulletTimeOn)
            {
                Function.Call(Hash.SET_TIME_SCALE, 0.2f);
                bulletTimeOn = true;
            }
            else if (!mira && bulletTimeOn)
            {
                Function.Call(Hash.SET_TIME_SCALE, 1.0f);
                bulletTimeOn = false;
            }
        }
        else if (bulletTimeOn)
        {
            Function.Call(Hash.SET_TIME_SCALE, 1.0f);
            bulletTimeOn = false;
        }

        if (!multi && !rapid) return;
        // 24 = INPUT_ATTACK
        if (!Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 0, 24)) return;

        int arma = 0;
        OutputArgument oaW = new OutputArgument();
        if (Function.Call<bool>(Hash.GET_CURRENT_PED_WEAPON, p, oaW, true))
            arma = oaW.GetResult<int>();
        if (arma == 0) return;

        // niente corpo a corpo
        int gruppo = Function.Call<int>(Hash.GET_WEAPONTYPE_GROUP, arma);
        if (gruppo == Function.Call<int>(Hash.GET_HASH_KEY, "GROUP_MELEE")) return;

        int now2 = Game.GameTime;
        int attesa = rapid ? 60 : 140;
        if (now2 - sparoAt < attesa) return;
        sparoAt = now2;

        Vector3 cp = GameplayCamera.Position;
        Vector3 cd = GameplayCamera.Direction;
        Vector3 da = cp + cd * 1.2f;

        int colpi = multi ? 5 : 2;
        int k;
        for (k = 0; k < colpi; k++)
        {
            // i colpi in piu' si aprono un po' a ventaglio
            float sx = (k - (colpi - 1) * 0.5f) * 0.45f;
            Vector3 a = cp + cd * 200f + new Vector3(sx, sx, 0f);

            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS,
                          da.X, da.Y, da.Z, a.X, a.Y, a.Z,
                          150, true, arma, p, true, false, 2000f);
        }
    }

    // quante auto e quanti pedoni ci sono adesso intorno a te: serve per
    // capire se una voce di TEST sta cambiando davvero qualcosa
    void ContaIntorno()
    {
        Ped me3 = Game.Player.Character;
        if (me3 == null || !me3.Exists()) return;

        Vehicle[] vv3 = World.GetNearbyVehicles(me3.Position, 200f);
        Ped[] pp3 = World.GetNearbyPeds(me3, 200f);

        Notification.PostTicker("~b~" + L("VARIOUS", "VARIE") + "~s~  "
            + L("cars", "auto") + ": " + vv3.Length + "   "
            + L("peds", "pedoni") + ": " + pp3.Length + "  (200 m)", false);
    }

    void SetClock(int h, int m)
    {
        if (h < 0) h = 0;
        if (h > 23) h = 23;
        if (m < 0) m = 0;
        if (m > 59) m = 59;

        // NETWORK_OVERRIDE_CLOCK_TIME inchioda l'orologio all'ora impostata
        // finche' non lo si sblocca: per mettere l'ora e lasciare scorrere
        // il tempo si toglie l'override e si usa SET_CLOCK_TIME.
        Function.Call(Hash.NETWORK_CLEAR_CLOCK_TIME_OVERRIDE);
        Function.Call(Hash.SET_CLOCK_TIME, h, m, 0);
    }

    const int MAX_COLOR = 255;   // rete di sicurezza contro valori corrotti nel file, non un limite alle tinte

    int SafeColor(int c)
    {
        if (c < 0) return 0;
        if (c > MAX_COLOR) return 0;
        return c;
    }

    void ApplyPaint(int slot, int colorIdx)
    {
        colorIdx = SafeColor(colorIdx);
        Vehicle v = TargetVehicle();
        if (v == null || !v.Exists())
        {
            Notification.PostTicker("~y~" + L("Not in a vehicle", "Non sei in un veicolo"), false);
            return;
        }

        OutputArgument a1 = new OutputArgument();
        OutputArgument a2 = new OutputArgument();
        Function.Call(Hash.GET_VEHICLE_COLOURS, v, a1, a2);
        int prim = a1.GetResult<int>();
        int sec = a2.GetResult<int>();

        OutputArgument b1 = new OutputArgument();
        OutputArgument b2 = new OutputArgument();
        Function.Call(Hash.GET_VEHICLE_EXTRA_COLOURS, v, b1, b2);
        int pearl = b1.GetResult<int>();
        int wheel = b2.GetResult<int>();

        if (slot == 0) prim = colorIdx;
        if (slot == 1) sec = colorIdx;
        if (slot == 2) pearl = colorIdx;
        if (slot == 3) wheel = colorIdx;

        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
        Function.Call(Hash.SET_VEHICLE_COLOURS, v, SafeColor(prim), SafeColor(sec));
        Function.Call(Hash.SET_VEHICLE_EXTRA_COLOURS, v, SafeColor(pearl), SafeColor(wheel));

        TouchSaved(v);   // se e' un veicolo salvato, memorizza subito la nuova vernice
    }

    // ------------------------------------------------------------
    //  hash del modello -> nome leggibile, ricavato dai nostri file
    //  (vehicles.txt e addons.txt). Serve quando SHVDN non riesce a
    //  leggere il nome dalla memoria del gioco.
    // ------------------------------------------------------------
    List<int> nomeHash = new List<int>();
    List<string> nomeTxt = new List<string>();
    bool nomiLetti = false;

    void CaricaNomiVeicoli()
    {
        nomiLetti = true;
        try
        {
            string fv = Path.Combine(DATA_DIR, "vehicles.txt");
            if (File.Exists(fv))
            {
                string[] rows = File.ReadAllLines(fv);
                int i;
                for (i = 0; i < rows.Length; i++)
                {
                    string r = rows[i].Trim();
                    if (r.Length == 0 || r.StartsWith("#")) continue;
                    nomeHash.Add(Function.Call<int>(Hash.GET_HASH_KEY, r));
                    nomeTxt.Add(r);
                }
            }

            string fa = Path.Combine(DATA_DIR, "addons.txt");
            if (File.Exists(fa))
            {
                string[] rows = File.ReadAllLines(fa);
                int i;
                for (i = 0; i < rows.Length; i++)
                {
                    string r = rows[i].Trim();
                    if (r.Length == 0 || r.StartsWith("#")) continue;

                    string[] f = r.Split('|');
                    if (f.Length < 2) continue;

                    string mdl = f[0].Trim();
                    if (mdl.Length == 0) continue;

                    string lab;
                    if (f.Length >= 4) lab = (f[1].Trim() + " " + f[2].Trim()).Trim();
                    else lab = f[1].Trim();
                    if (lab.Length == 0) lab = mdl;

                    nomeHash.Add(Function.Call<int>(Hash.GET_HASH_KEY, mdl));
                    nomeTxt.Add(lab);
                }
            }
        }
        catch { }
    }

    string NomeDaHash(int hash)
    {
        if (!nomiLetti) CaricaNomiVeicoli();
        int i;
        for (i = 0; i < nomeHash.Count; i++)
        {
            if (nomeHash[i] == hash) return nomeTxt[i];
        }
        return "";
    }

    string VehLabel(int hash, string fallback)
    {
        string lbl = Function.Call<string>(Hash.GET_DISPLAY_NAME_FROM_VEHICLE_MODEL, hash);
        string nice = Game.GetLocalizedString(lbl);
        if (nice == null || nice.Length == 0 || nice == "NULL")
        {
            nice = NomeDaHash(hash);
            if (nice.Length == 0) nice = fallback;
        }
        return nice;
    }

    void BuildVehicleClasses()
    {
        vehBuilt = true;

        string file = DATA_DIR + "\\vehicles.txt";
        if (!File.Exists(file))
        {
            Notification.PostTicker("~r~vehicles.txt " + L("not found", "non trovato"), false);
            return;
        }

        int[] classMenu = new int[VCLASS.Length];
        int i;
        for (i = 0; i < classMenu.Length; i++)
        {
            classMenu[i] = -1;
        }

        // tutte le categorie stanno dentro una sola voce "Spawna veicolo"
        int mSpawn = NewMenu("SPAWN VEHICLE", "SPAWNA VEICOLO", mVehicles);

        // ---- ADD-ONS: MARCA > CATEGORIA > MODELLO ----
        List<string> addonName = new List<string>();
        List<string> addonLabel = new List<string>();
        List<int> addonClass = new List<int>();
        List<string> addonClassText = new List<string>();

        int mAddons = -1;
        List<string> brandKey = new List<string>();
        List<int> brandMenu = new List<int>();
        List<string> bcKey = new List<string>();
        List<int> bcMenu = new List<int>();
        List<string> customName = new List<string>();
        List<int> customMenu = new List<int>();

        string addFile = DATA_DIR + "\\addons.txt";
        if (File.Exists(addFile))
        {
            string[] al = File.ReadAllLines(addFile);
            int k;
            for (k = 0; k < al.Length; k++)
            {
                string row = al[k].Trim();
                if (row.Length == 0 || row.StartsWith("#")) continue;

                // formato:  modello | Marca | Nome | Categoria
                string[] f = row.Split('|');
                int fi;
                for (fi = 0; fi < f.Length; fi++)
                {
                    f[fi] = f[fi].Trim();
                }

                string an = f[0];
                if (an.Length == 0) continue;

                string brand = "";
                string mname = "";
                string ctext = "";

                if (f.Length == 2)
                {
                    mname = f[1];
                }
                else if (f.Length == 3)
                {
                    mname = f[1];
                    ctext = f[2];
                }
                else if (f.Length >= 4)
                {
                    brand = f[1];
                    mname = f[2];
                    ctext = f[3];
                }

                int ah = Function.Call<int>(Hash.GET_HASH_KEY, an);
                if (!Function.Call<bool>(Hash.IS_MODEL_IN_CDIMAGE, ah)) continue;
                if (!Function.Call<bool>(Hash.IS_MODEL_A_VEHICLE, ah)) continue;

                // le mod auto spesso non hanno la classe giusta nei loro
                // file: se in addons.txt la categoria dice Super, per noi
                // e' una supercar (consumo doppio) qualunque classe abbia
                if (ctext.ToLower().StartsWith("super")) addonSuper.Add(ah);

                if (mname.Length == 0)
                {
                    mname = VehLabel(ah, an);
                }

                // etichetta completa per le classi generali
                string full = brand.Length > 0 ? brand + " " + mname : mname;

                if (mAddons < 0)
                {
                    mAddons = NewMenu("ADD-ONS", "ADD-ONS", mSpawn);
                    TItem sa = AddSub(mSpawn, "Add-ons", "Add-ons", mAddons);
                    sa.Cr = PASTEL[0, 0]; sa.Cg = PASTEL[0, 1]; sa.Cb = PASTEL[0, 2];
                    sa.Tinted = true;
                }

                // livello 1: la marca
                string bshow = brand.Length > 0 ? brand : L("Other", "Altro");
                string bk = bshow.ToLower();
                int bi = brandKey.IndexOf(bk);
                if (bi < 0)
                {
                    int bm = NewMenu(bshow.ToUpper(), bshow.ToUpper(), mAddons);
                    TItem bs = AddSub(mAddons, bshow, bshow, bm);
                    int bc = brandKey.Count % (PASTEL.Length / 3);
                    bs.Cr = PASTEL[bc, 0]; bs.Cg = PASTEL[bc, 1]; bs.Cb = PASTEL[bc, 2];
                    bs.Tinted = true;

                    brandKey.Add(bk);
                    brandMenu.Add(bm);
                    bi = brandKey.Count - 1;
                }

                // livello 2: la categoria dentro la marca
                int cidx = ClassFromText(ctext);
                string cshow = ctext.Length > 0 ? ctext : L("Other", "Altro");
                if (cidx >= 0)
                {
                    cshow = lang == 1 ? VCLASS_IT[cidx] : VCLASS[cidx];
                }

                string ck = bk + ">" + cshow.ToLower();
                int ci = bcKey.IndexOf(ck);
                if (ci < 0)
                {
                    int cm = NewMenu(cshow.ToUpper(), cshow.ToUpper(), brandMenu[bi]);
                    AddSub(brandMenu[bi], cshow, cshow, cm);
                    bcKey.Add(ck);
                    bcMenu.Add(cm);
                    ci = bcKey.Count - 1;
                }

                // livello 3: il modello
                TItem ai = AddAction(bcMenu[ci], mname, mname, 210);
                ai.Data = an;

                addonName.Add(an);
                addonLabel.Add(full);
                addonClass.Add(cidx);
                addonClassText.Add(ctext);
            }
        }

        string[] lines = File.ReadAllLines(file);

        // PASSATA 1: quali classi esistono davvero
        bool[] hasClass = new bool[VCLASS.Length];
        for (i = 0; i < lines.Length; i++)
        {
            string nm0 = lines[i].Trim();
            if (nm0.Length == 0 || nm0.StartsWith("#")) continue;

            int h0 = Function.Call<int>(Hash.GET_HASH_KEY, nm0);
            if (!Function.Call<bool>(Hash.IS_MODEL_IN_CDIMAGE, h0)) continue;
            if (!Function.Call<bool>(Hash.IS_MODEL_A_VEHICLE, h0)) continue;

            int c0 = Function.Call<int>(Hash.GET_VEHICLE_CLASS_FROM_NAME, h0);
            if (c0 < 0 || c0 >= VCLASS.Length) c0 = 0;
            hasClass[c0] = true;
        }

        // gli add-on possono aggiungere classi che i veicoli base non usano
        int ap;
        for (ap = 0; ap < addonClass.Count; ap++)
        {
            int ac0 = addonClass[ap];
            if (ac0 >= 0 && ac0 < VCLASS.Length) hasClass[ac0] = true;
        }

        // ---- ELETTRICI: una categoria propria, in cima ----
        // Lista nostra (vedi ELECTRIC): la proprieta' del gioco non funziona
        // in questa build. I modelli restano anche nella loro classe.
        int mEv = NewMenu("ELECTRIC", "ELETTRICI", mSpawn);
        TItem evSub = AddSub(mSpawn, "Electric", "Elettrici", mEv);
        evSub.Cr = PASTEL[2, 0]; evSub.Cg = PASTEL[2, 1]; evSub.Cb = PASTEL[2, 2];
        evSub.Tinted = true;

        int ev;
        int evFound = 0;
        for (ev = 0; ev < ELECTRIC.Length; ev++)
        {
            string en = ELECTRIC[ev];
            int eh = Function.Call<int>(Hash.GET_HASH_KEY, en);
            if (!Function.Call<bool>(Hash.IS_MODEL_IN_CDIMAGE, eh)) continue;
            if (!Function.Call<bool>(Hash.IS_MODEL_A_VEHICLE, eh)) continue;

            TItem eit = AddAction(mEv, VehLabel(eh, en), 210);
            eit.Data = en;
            evFound++;
        }

        if (evFound == 0)
        {
            AddHeader(mEv, "- NONE FOUND -", "- NESSUNO TROVATO -", 0);
        }

        // ---- IBRIDI: categoria propria, subito sotto gli elettrici ----
        int mHy = NewMenu("HYBRID", "IBRIDI", mSpawn);
        TItem hySub = AddSub(mSpawn, "Hybrid", "Ibridi", mHy);
        hySub.Cr = PASTEL[2, 0]; hySub.Cg = PASTEL[2, 1]; hySub.Cb = PASTEL[2, 2];
        hySub.Tinted = true;

        int hy;
        int hyFound = 0;
        for (hy = 0; hy < HYBRID.Length; hy++)
        {
            string hn = HYBRID[hy];
            int hh = Function.Call<int>(Hash.GET_HASH_KEY, hn);
            if (!Function.Call<bool>(Hash.IS_MODEL_IN_CDIMAGE, hh)) continue;
            if (!Function.Call<bool>(Hash.IS_MODEL_A_VEHICLE, hh)) continue;

            TItem hit = AddAction(mHy, VehLabel(hh, hn), 210);
            hit.Data = hn;
            hyFound++;
        }

        if (hyFound == 0)
        {
            AddHeader(mHy, "- NONE FOUND -", "- NESSUNO TROVATO -", 0);
        }

        // crea i sottomenu nell'ordine ufficiale delle classi
        for (i = 0; i < VCLASS.Length; i++)
        {
            if (!hasClass[i]) continue;

            classMenu[i] = NewMenu(VCLASS[i].ToUpper(), VCLASS_IT[i].ToUpper(), mSpawn);
            TItem cs = AddSub(mSpawn, VCLASS[i], VCLASS_IT[i], classMenu[i]);
            int cg = CCOLOR[i];
            cs.Cr = PASTEL[cg, 0]; cs.Cg = PASTEL[cg, 1]; cs.Cb = PASTEL[cg, 2];
            cs.Tinted = true;
        }

        // PASSATA 2: i veicoli dentro la loro classe
        int found = 0;
        for (i = 0; i < lines.Length; i++)
        {
            string name = lines[i].Trim();
            if (name.Length == 0 || name.StartsWith("#"))
            {
                continue;
            }

            int hash = Function.Call<int>(Hash.GET_HASH_KEY, name);
            if (!Function.Call<bool>(Hash.IS_MODEL_IN_CDIMAGE, hash))
            {
                continue;
            }
            if (!Function.Call<bool>(Hash.IS_MODEL_A_VEHICLE, hash))
            {
                continue;
            }

            int cls = Function.Call<int>(Hash.GET_VEHICLE_CLASS_FROM_NAME, hash);
            if (cls < 0 || cls >= VCLASS.Length)
            {
                cls = 0;
            }

            if (classMenu[cls] < 0) continue;

            string nice = VehLabel(hash, name);

            TItem it = AddAction(classMenu[cls], nice, 210);
            it.Data = name;
            found++;
        }

        // ---- add-on anche dentro la loro classe ----
        int ax;
        for (ax = 0; ax < addonName.Count; ax++)
        {
            int acl = addonClass[ax];

            // categoria personalizzata (es. "Audi", "BMW"): creata al volo
            if (acl < 0)
            {
                string ct = addonClassText[ax];
                if (ct.Length == 0)
                {
                    continue;
                }

                int ck = customName.IndexOf(ct.ToLower());
                if (ck < 0)
                {
                    int nm = NewMenu(ct.ToUpper(), ct.ToUpper(), mSpawn);
                    TItem sc = AddSub(mSpawn, ct, ct, nm);
                    int cc = (customName.Count + 2) % (PASTEL.Length / 3);
                    sc.Cr = PASTEL[cc, 0]; sc.Cg = PASTEL[cc, 1]; sc.Cb = PASTEL[cc, 2];
                    sc.Tinted = true;

                    customName.Add(ct.ToLower());
                    customMenu.Add(nm);
                    ck = customName.Count - 1;
                }

                TItem cit = AddAction(customMenu[ck], addonLabel[ax], addonLabel[ax], 210);
                cit.Data = addonName[ax];
                continue;
            }

            if (acl >= VCLASS.Length)
            {
                continue;
            }

            if (classMenu[acl] < 0) continue;

            TItem ci3 = AddAction(classMenu[acl], addonLabel[ax], addonLabel[ax], 210);
            ci3.Data = addonName[ax];
        }

        // ---- sezione azioni, sotto alle classi ----
        AddHeader(mVehicles, "- SPAWN -", "- SPAWN -", 0);
        TItem sv = AddSub(mVehicles, "Spawn vehicle", "Spawna veicolo", mSpawn);
        sv.Cr = PASTEL[1, 0]; sv.Cg = PASTEL[1, 1]; sv.Cb = PASTEL[1, 2];
        sv.Tinted = true;
        AddAction(mVehicles, "Spawn by name...", "Spawn per nome...", 200);
        AddSub(mVehicles, "Spawn options", "Opzioni spawn", mSpawnOpts);

        AddHeader(mVehicles, "- CURRENT VEHICLE -", "- VEICOLO ATTUALE -", 3);
        AddAction(mVehicles, "Repair", "Ripara", 205);
        AddAction(mVehicles, "Clean", "Pulisci", 206);
        AddAction(mVehicles, "Delete", "Elimina", 204);
        AddAction(mVehicles, "Clear area", "Pulisci l'area", 211);
        AddAction(mVehicles, "Flip upright", "Rimettila dritta", 212);
        AddAction(mVehicles, "Engine on/off", "Motore acceso/spento", 213);
        int mPorte = NewMenu("DOORS", "PORTE", mVehicles);
        AddSub(mVehicles, "Doors", "Porte", mPorte);

        AddAction(mPorte, "Open all", "Apri tutte", 214);
        AddAction(mPorte, "Close all", "Chiudi tutte", 215);

        AddHeader(mPorte, "- SINGLE DOOR -", "- SINGOLA -", 3);
        TItem pd0 = AddAction(mPorte, "Front left", "Anteriore sinistra", 286); pd0.Data = "0";
        TItem pd1 = AddAction(mPorte, "Front right", "Anteriore destra", 286); pd1.Data = "1";
        TItem pd2 = AddAction(mPorte, "Rear left", "Posteriore sinistra", 286); pd2.Data = "2";
        TItem pd3 = AddAction(mPorte, "Rear right", "Posteriore destra", 286); pd3.Data = "3";
        TItem pd4 = AddAction(mPorte, "Hood", "Cofano", 286); pd4.Data = "4";
        TItem pd5 = AddAction(mPorte, "Boot", "Baule", 286); pd5.Data = "5";
        AddAction(mVehicles, "Lock / unlock", "Blocca / sblocca", 216);
        AddAction(mVehicles, "Set plate...", "Cambia targa...", 217);
        tVehGod  = AddList(mVehicles, "Invincible", "Invincibile", 207,
                           new string[] { "Off", "Engine", "Body", "Full" }, 0);
        tOnWater = AddToggle(mVehicles, "Drive on water", "Guida sull'acqua", 208, false);
        AddAction(mVehicles, "Maintenance", "Manutenzione", 268);
        AddAction(mVehicles, "Refuel / recharge", "Fai il pieno / ricarica", 270);
        tLimiter = AddToggle(mVehicles, "Speed limiter", "Limitatore di velocita'", 219, false);
        tAutoRepair = AddToggle(mVehicles, "Auto repair", "Riparazione automatica", 220, false);
        tAutoFlip   = AddToggle(mVehicles, "Auto flip", "Si raddrizza da sola", 221, false);
        tKeepOn     = AddToggle(mVehicles, "Keep engine & lights on",
                                "Motore e luci sempre accesi", 222, false);
        tMuteSiren  = AddToggle(mVehicles, "Mute siren", "Sirena silenziosa", 223, false);
        AddToggle(mVehicles, "Boat anchor", "Ancora della barca", 271, false);
        AddAction(mVehicles, "Get into closest vehicle", "Entra nel veicolo piu' vicino", 224);
        tDash = AddList(mVehicles, "Dashboard", "Cruscotto", 285,
                        new string[] { "Off", "Semplice", "Grafico" }, 1);
        tUnits   = AddList(mVehicles, "Units", "Unita' di misura", 269,
                           new string[] { "km/h", "mph" }, 0);
        tMass    = AddList(mVehicles, "Mass multiplier", "Moltiplicatore massa", 218,
                           new string[] {
                               "x1", "x1.5", "x2", "x2.5", "x3", "x3.5", "x4", "x4.5",
                               "x5", "x5.5", "x6", "x6.5", "x7", "x7.5", "x8", "x8.5",
                               "x9", "x9.5", "x10", "x20", "x30", "x40", "x50", "x60",
                               "x70", "x80", "x90", "x100", "x200", "x300", "x400",
                               "x500", "x600", "x700", "x800", "x900", "x1000", "x2000",
                               "x3000", "x4000", "x5000", "x6000", "x7000", "x8000",
                               "x9000", "x10000", "x100000" }, 0);

        AddHeader(mVehicles, "- MY VEHICLES -", "- I MIEI VEICOLI -", 2);
        tPersist = AddToggle(mVehicles, "Persistent", "Persistenti", 209, false);
        tBlips   = AddToggle(mVehicles, "Map blips", "Blip sulla mappa", 250, true);

        mMyVeh = NewMenu("MY VEHICLES", "I MIEI VEICOLI", mVehicles);
        AddSub(mVehicles, "My vehicles", "I miei veicoli", mMyVeh);
        LoadMyVehicles();
        BuildMyVehicles();

        mModShop = NewMenu("MOD SHOP", "OFFICINA", mVehicles);
        TItem ms = AddSub(mVehicles, "Mod shop", "Officina", mModShop);
        ms.Id = 240;   // ricostruisce il menu sul veicolo attuale prima di entrare

        // i sottomenu si creano una volta sola e si svuotano a ogni riapertura
        mBody   = NewMenu("BODYWORK", "CARROZZERIA", mModShop);
        mMech   = NewMenu("MECHANICS", "MECCANICA", mModShop);
        mWheels = NewMenu("WHEELS", "RUOTE", mModShop);
        mLights = NewMenu("LIGHTS & OTHER", "LUCI E ALTRO", mModShop);
        mExtras = NewMenu("EXTRAS", "EXTRA", mModShop);

        int mPaint = NewMenu("PAINT", "VERNICE", mVehicles);
        AddSub(mVehicles, "Paint", "Vernice", mPaint);
        BuildPaintMenus(mPaint);

        menus[mVehicles].Sel = FirstSelectable(mVehicles);

        // le voci create qui (Invincibile, Guida sull'acqua, ...) esistono solo ora:
        // ricarico la config perche' prendano il loro stato salvato
        LoadConfig();

        // benzinai e market: icone fisse, create una volta sola e mai piu' toccate
        MakeGasBlips();
        MakeMarketBlips();

        LoadTanks();
        LoadBatt();
        LoadOil();
        LoadOdo();
        LoadBody();
        if (tFreezeWeather != null && tFreezeWeather.On && tWeather != null)
        {
            SetWeather(tWeather.Sel);
        }

        Notification.PostTicker("~g~" + found + "~s~ " + L("vehicles loaded", "veicoli caricati"), false);
    }

    // ============================================================
    //  QUI SI SCRIVE COSA FA OGNI VOCE
    // ============================================================
    // azione o input di una mod esterna: scrive il comando nel suo comandi.txt
    void AzioneMod(TItem it)
    {
        string[] dd = (it.Data == null) ? null : it.Data.Split('*');
        if (dd == null || dd.Length < 2) return;

        string testo = "";
        if (it.Id == MOD_INPUT)
        {
            testo = Game.GetUserInput("");
            if (testo == null) return;
            testo = testo.Trim();
            if (testo.Length == 0) return;
            ComandoMod(dd[0], dd[1] + "=" + testo);
        }
        else
        {
            ComandoMod(dd[0], dd[1]);
        }

        Notification.PostTicker("~g~" + it.Text + "~s~ " + L("done", "fatto"), false);
    }

    void DoAction(TItem it)
    {
        if (it.Id == MOD_LIST)
        {
            string[] dl = (it.Data == null) ? null : it.Data.Split('*');
            if (dl != null && dl.Length >= 2 && it.OptVals != null
                && it.Sel >= 0 && it.Sel < it.OptVals.Length)
                ComandoMod(dl[0], dl[1] + "_" + it.OptVals[it.Sel].Trim());
            return;
        }
        // le azioni delle mod esterne arrivano qui (sono voci "azione")
        if (it.Id == MOD_ACTION || it.Id == MOD_INPUT) { AzioneMod(it); return; }

        Ped p = Game.Player.Character;
        int pid = Function.Call<int>(Hash.PLAYER_ID);

        switch (it.Id)
        {
            case 100:
                p.Health = p.MaxHealth;
                Function.Call(Hash.SET_PED_ARMOUR, p, Function.Call<int>(Hash.GET_PLAYER_MAX_ARMOUR, pid));
                Notification.PostTicker("~g~" + L("Healed", "Curato"), false);
                break;

            case 101:
                Function.Call(Hash.SET_PED_ARMOUR, p, Function.Call<int>(Hash.GET_PLAYER_MAX_ARMOUR, pid));
                Notification.PostTicker("~g~" + L("Full armour", "Armatura piena"), false);
                break;

            case 110:
                {
                    int[] amounts = new int[] { -100000, -10000, -1000, -100, -10,
                                                 0,
                                                 10, 100, 1000, 10000, 100000 };
                    int amt = amounts[it.Sel];
                    if (amt == 0)
                    {
                        break;
                    }
                    int money = Game.Player.Money + amt;
                    if (money < 0) money = 0;
                    Game.Player.Money = money;

                    string sign = amt < 0 ? "~r~-$" : "~g~+$";
                    int abs = amt < 0 ? -amt : amt;
                    Notification.PostTicker(sign + abs.ToString("N0", CultureInfo.InvariantCulture), false);
                }
                break;

            case 212:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, v);
                    }
                }
                break;

            case 213:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        bool on = Function.Call<bool>(Hash.GET_IS_VEHICLE_ENGINE_RUNNING, v);
                        Function.Call(Hash.SET_VEHICLE_ENGINE_ON, v, !on, true, true);
                    }
                }
                break;

            case 214:
            case 215:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        int d;
                        for (d = 0; d <= 5; d++)
                        {
                            if (it.Id == 214) Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, v, d, false, false);
                            else Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, v, d, false);
                        }
                    }
                }
                break;

            case 286:
                {
                    Vehicle v = TargetVehicle();
                    if (v == null || !v.Exists()) break;

                    int d;
                    if (!int.TryParse(it.Data, out d)) break;

                    // apre se e' chiusa, chiude se e' aperta
                    if (Function.Call<float>(Hash.GET_VEHICLE_DOOR_ANGLE_RATIO, v, d) > 0.02f)
                        Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, v, d, false);
                    else
                        Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, v, d, false, false);
                }
                break;

            case 216:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        int st = Function.Call<int>(Hash.GET_VEHICLE_DOOR_LOCK_STATUS, v);
                        int newSt = (st == 2) ? 1 : 2;
                        Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, v, newSt);
                        Notification.PostTicker(newSt == 2
                            ? "~g~" + L("Locked", "Bloccato")
                            : "~g~" + L("Unlocked", "Sbloccato"), false);
                    }
                }
                break;

            case 905:
                spostaFinestra = true;
                Notification.PostTicker("~g~" + L("Move with arrows/dpad, confirm with select",
                    "Sposta con frecce/croce, conferma con seleziona"), false);
                break;

            case 903:
                radioNext = 0;
                ApplyRadio(Game.Player.Character.CurrentVehicle);
                break;

            case 904:
                ApplyMobileRadio();
                Notification.PostTicker((it.On ? "~g~" : "~y~")
                    + L("Radio on foot", "Radio a piedi") + "~s~ "
                    + (it.On ? L("on", "accesa") : L("off", "spenta")), false);
                break;

            case 270:
                {
                    Vehicle v = p.CurrentVehicle;
                    if (v == null || !v.Exists())
                    {
                        Notification.PostTicker("~y~" + L("Get in a vehicle first", "Sali su un veicolo"), false);
                        break;
                    }
                    if (fuel >= 99.5f)
                    {
                        Notification.PostTicker("~y~" + (evCurrent
                            ? L("Battery already full", "Batteria gia' carica")
                            : L("Tank already full", "Serbatoio gia' pieno")), false);
                        break;
                    }

                    float perPct = evCurrent ? COST_PER_PCT_EV : COST_PER_PCT;
                    int cost = (int)((100f - fuel) * perPct) + 1;

                    if (Game.Player.Money < cost)
                    {
                        Notification.PostTicker("~r~" + L("Not enough money", "Soldi insufficienti")
                            + " ($" + cost + ")", false);
                        break;
                    }

                    Game.Player.Money = Game.Player.Money - cost;
                    fuel = 100f;
                    SetTank(curTankKey, fuel);
                    SaveTanks();

                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PURCHASE",
                                  "HUD_LIQUOR_STORE_SOUNDSET", true);
                    Notification.PostTicker("~g~" + (evCurrent
                        ? L("Battery full", "Batteria carica")
                        : L("Tank full", "Pieno fatto")) + "~s~ -$" + cost, false);
                }
                break;

            case 268:
                {
                    Vehicle v = p.CurrentVehicle;
                    if (v == null || !v.Exists())
                    {
                        Notification.PostTicker("~y~" + L("Get in a vehicle first", "Sali su un veicolo"), false);
                        break;
                    }
                    if (oil > 95f)
                    {
                        Notification.PostTicker("~y~" + L("Maintenance not needed yet", "Manutenzione non ancora necessaria"), false);
                        break;
                    }
                    int costoTg = CostoManutenzione(v);
                    if (Game.Player.Money < costoTg)
                    {
                        Notification.PostTicker("~r~" + L("Not enough money", "Soldi insufficienti")
                            + " ($" + costoTg + ")", false);
                        break;
                    }

                    Game.Player.Money = Game.Player.Money - costoTg;

                    // il tagliando azzera il contachilometri del servizio
                    curOilKey = TankKeyOf(v);
                    servM = odoM;
                    oil = 100f;
                    SetOil(curOilKey, servM);
                    SaveOil();

                    // col tagliando il motore torna a posto e riprende potenza
                    Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, v, 1000f);
                    Function.Call(Hash.MODIFY_VEHICLE_TOP_SPEED, v, 1f);
                    oilSlowVeh = 0;
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PURCHASE",
                                  "HUD_LIQUOR_STORE_SOUNDSET", true);
                    Notification.PostTicker("~g~" + L("Maintenance done", "Manutenzione fatta")
                        + "~s~ -$" + costoTg, false);
                }
                break;

            case 219:
                {
                    Vehicle v = Game.Player.Character.CurrentVehicle;
                    if (!it.On && v != null && v.Exists())
                    {
                        Function.Call(Hash.SET_ENTITY_MAX_SPEED, v, 500f);
                        limiterVeh = 0;
                    }
                    Notification.PostTicker((it.On ? "~g~" : "~y~")
                        + L("Speed limiter", "Limitatore") + "~s~ "
                        + (it.On ? L("on", "acceso") : L("off", "spento")), false);
                }
                break;

            case 218:
                {
                    Vehicle v = Game.Player.Character.CurrentVehicle;
                    if (v != null && v.Exists())
                    {
                        ApplyMass(v);
                        Notification.PostTicker("~g~" + L("Mass", "Massa") + "~s~ "
                            + Txt(it) + " " + it.Opts[it.Sel], false);
                    }
                    else
                    {
                        Notification.PostTicker("~y~" + L("Get in a vehicle first",
                            "Sali su un veicolo"), false);
                    }
                }
                break;

            case 217:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        string txt = Game.GetUserInput("");
                        if (txt != null && txt.Trim().Length > 0)
                        {
                            Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, v, txt.Trim().ToUpper());
                            TouchSaved(v);
                        }
                    }
                }
                break;

            case 500:
                {
                    Array vals = Enum.GetValues(typeof(WeaponHash));
                    int i;
                    int given = 0;
                    for (i = 0; i < vals.Length; i++)
                    {
                        WeaponHash w = (WeaponHash)vals.GetValue(i);
                        if (w == WeaponHash.Unarmed) continue;
                        Function.Call(Hash.GIVE_WEAPON_TO_PED, p, (int)w, 9999, false, false);
                        given++;
                    }
                    Notification.PostTicker("~g~" + given + " " + L("weapons", "armi"), false);
                }
                break;

            case 501:
                {
                    Array vals = Enum.GetValues(typeof(WeaponHash));
                    int i;
                    for (i = 0; i < vals.Length; i++)
                    {
                        WeaponHash w = (WeaponHash)vals.GetValue(i);
                        if (w == WeaponHash.Unarmed) continue;
                        if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, p, (int)w, false)) continue;
                        Function.Call(Hash.SET_PED_AMMO, p, (int)w, 9999);
                    }
                    Notification.PostTicker("~g~" + L("Ammo refilled", "Munizioni ricaricate"), false);
                }
                break;

            case 505:
                {
                    int wh;
                    if (int.TryParse(it.Data, out wh))
                    {
                        Function.Call(Hash.GIVE_WEAPON_TO_PED, p, wh, 9999, false, true);
                        Function.Call(Hash.SET_CURRENT_PED_WEAPON, p, wh, true);
                        Notification.PostTicker("~g~" + Txt(it), false);
                    }
                }
                break;

            case 504:
                Function.Call(Hash.REMOVE_ALL_PED_WEAPONS, p, true);
                Notification.PostTicker("~g~" + L("Weapons removed", "Armi rimosse"), false);
                break;

            case 200:
                {
                    string typed = Game.GetUserInput("");
                    if (typed != null && typed.Trim().Length > 0)
                    {
                        SpawnVehicle(typed.Trim());
                    }
                }
                break;

            case 204:
                {
                    Vehicle v = TargetVehicle();
                    if (v == null || !v.Exists())
                    {
                        Notification.PostTicker("~y~" + L("Not in a vehicle", "Non sei in un veicolo"), false);
                    }
                    else
                    {
                        v.Delete();
                        Notification.PostTicker("~g~" + L("Vehicle deleted", "Veicolo eliminato"), false);
                    }
                }
                break;

            case 205:
                {
                    Vehicle v = TargetVehicle();
                    if (v == null || !v.Exists())
                    {
                        Notification.PostTicker("~y~" + L("Not in a vehicle", "Non sei in un veicolo"), false);
                    }
                    else
                    {
                        v.Repair();
                        Notification.PostTicker("~g~" + L("Repaired", "Riparato"), false);
                    }
                }
                break;

            case 206:
                {
                    Vehicle v = TargetVehicle();
                    if (v == null || !v.Exists())
                    {
                        Notification.PostTicker("~y~" + L("Not in a vehicle", "Non sei in un veicolo"), false);
                    }
                    else
                    {
                        Function.Call(Hash.SET_VEHICLE_DIRT_LEVEL, v, 0.0f);
                        Notification.PostTicker("~g~" + L("Cleaned", "Pulito"), false);
                    }
                }
                break;

            case 210:
                SpawnVehicle(it.Data);
                break;

            case 211:
                pendingClear = true;   // eseguita al prossimo tick, fuori dal disegno
                break;

            case 224:
                {
                    Ped me = Game.Player.Character;
                    Vehicle[] vicini = World.GetNearbyVehicles(me.Position, 30f);
                    Vehicle best = null;
                    float bd = 9999f;
                    int vi;
                    for (vi = 0; vi < vicini.Length; vi++)
                    {
                        Vehicle vv = vicini[vi];
                        if (vv == null || !vv.Exists()) continue;
                        float dd = (vv.Position - me.Position).Length();
                        if (dd < bd) { bd = dd; best = vv; }
                    }
                    if (best == null)
                    {
                        Notification.PostTicker("~r~" + L("No vehicle nearby",
                            "Nessun veicolo vicino"), false);
                        break;
                    }
                    Function.Call(Hash.SET_PED_INTO_VEHICLE, me, best, -1);
                }
                break;

            case 718:
                {
                    Ped me2 = Game.Player.Character;
                    Vehicle[] vic = World.GetNearbyVehicles(me2.Position, 60f);
                    int vk;
                    for (vk = 0; vk < vic.Length; vk++)
                    {
                        Vehicle vv2 = vic[vk];
                        if (vv2 == null || !vv2.Exists()) continue;
                        if (me2.CurrentVehicle != null && me2.CurrentVehicle.Exists()
                            && vv2.Handle == me2.CurrentVehicle.Handle) continue;

                        Function.Call(Hash.ADD_EXPLOSION, vv2.Position.X, vv2.Position.Y,
                                      vv2.Position.Z, 4, 1f, true, false, 1f);
                    }
                }
                break;

            case 420:
                // come Menyoo: si ferma lo script che ti caccia dalle basi
                Function.Call(Hash.TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME, "restrictedareas");
                Notification.PostTicker("~g~" + L("Restricted areas unlocked",
                    "Aree militari sbloccate"), false);
                break;

            case 266:
            case 267:
                {
                    int cost = (it.Id == 266) ? 12 : 3;
                    int money = Game.Player.Money;
                    if (money < cost)
                    {
                        Notification.PostTicker("~r~" + L("Not enough money", "Non hai abbastanza soldi"), false);
                        break;
                    }

                    Game.Player.Money = money - cost;
                    if (it.Id == 266)
                    {
                        hunger = hunger + 60f;
                        thirst = thirst + 20f;
                    }
                    else
                    {
                        thirst = thirst + 55f;
                    }
                    if (hunger > 100f) hunger = 100f;
                    if (thirst > 100f) thirst = 100f;

                    snackNext = Game.GameTime + 8000;
                    lastMoney = Game.Player.Money;
                    SaveBody();
                    Notification.PostTicker("~g~" + Txt(it) + "~s~  -$" + cost, false);
                }
                break;

            case 300:
                {
                    int b = Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, 8);
                    if (!Function.Call<bool>(Hash.DOES_BLIP_EXIST, b))
                    {
                        Notification.PostTicker("~y~" + L("No waypoint set", "Nessun waypoint impostato"), false);
                    }
                    else
                    {
                        Vector3 wp = Function.Call<Vector3>(Hash.GET_BLIP_INFO_ID_COORD, b);
                        TeleportTo(wp);
                    }
                }
                break;

            case 301:
                {
                    Vector3 ob = FindObjectiveBlip();
                    if (ob.X == 0f && ob.Y == 0f)
                    {
                        Notification.PostTicker("~y~" + L("No objective found", "Nessun obiettivo trovato"), false);
                    }
                    else
                    {
                        TeleportTo(ob);
                    }
                }
                break;

            case 248:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        MaxUpgrades(v);
                        TouchSaved(v);
                        BuildModShop();
                    }
                }
                break;

            case 249:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
                        int sl;
                        for (sl = 0; sl <= 48; sl++)
                        {
                            if (IsToggleSlot(sl)) Function.Call(Hash.TOGGLE_VEHICLE_MOD, v, sl, false);
                            else Function.Call(Hash.REMOVE_VEHICLE_MOD, v, sl);
                        }
                        TouchSaved(v);
                        BuildModShop();
                        Notification.PostTicker("~g~" + L("Back to stock", "Tornato di serie"), false);
                    }
                }
                break;

            case 303:
                {
                    string nm = Game.GetUserInput("");
                    if (nm != null && nm.Trim().Length > 0)
                    {
                        Vector3 pp = p.Position;
                        placeRaw.Add(nm.Trim() + "|"
                            + pp.X.ToString("0.00", CultureInfo.InvariantCulture) + "|"
                            + pp.Y.ToString("0.00", CultureInfo.InvariantCulture) + "|"
                            + pp.Z.ToString("0.00", CultureInfo.InvariantCulture));
                        SavePlaces();
                        BuildPlaces();
                        Notification.PostTicker("~g~" + L("Spot saved", "Punto salvato"), false);
                    }
                }
                break;

            case 306:
                {
                    string[] f = it.Data.Split('|');
                    if (f.Length >= 3)
                    {
                        float x, y2, z;
                        if (float.TryParse(f[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                         && float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y2)
                         && float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                        {
                            TeleportTo(new Vector3(x, y2, z));
                        }
                    }
                }
                break;

            case 305:
                {
                    int k;
                    if (int.TryParse(it.Data, out k) && k >= 0 && k < placeRaw.Count)
                    {
                        placeRaw.RemoveAt(k);
                        SavePlaces();
                        BuildPlaces();
                        cur = mPlaces;
                        menus[cur].Sel = FirstSelectable(cur);
                        menus[cur].Top = 0;
                    }
                }
                break;

            case 405:
            case 406:
            case 407:
            case 408:
                {
                    int h = 6;
                    if (it.Id == 406) h = 12;
                    if (it.Id == 407) h = 19;
                    if (it.Id == 408) h = 1;

                    if (tHour != null) tHour.Val = h;
                    if (tMinute != null) tMinute.Val = 0;
                    SetClock(h, 0);
                    SaveConfig();
                }
                break;

            case 230:
                {
                    int idx;
                    if (int.TryParse(it.Data, out idx))
                    {
                        Vector3 d = new Vector3(PvFloat(idx, 6), PvFloat(idx, 7), PvFloat(idx, 8));
                        TeleportTo(d);
                        if (FindWorldVehicle(idx) == null)
                        {
                            SpawnSaved(idx, false);
                        }
                    }
                }
                break;

            case 231:
                {
                    int idx;
                    if (int.TryParse(it.Data, out idx))
                    {
                        SpawnSaved(idx, true);
                    }
                }
                break;

            case 232:
                {
                    int idx;
                    if (int.TryParse(it.Data, out idx) && idx >= 0 && idx < pvRaw.Count)
                    {
                        // si esce subito dal sottomenu e la rimozione vera
                        // avviene al prossimo tick, fuori dal disegno
                        pendingRemove = idx;
                        cur = mMyVeh;
                        menus[cur].Sel = FirstSelectable(cur);
                        menus[cur].Top = 0;
                    }
                }
                break;

            case 220:
            case 221:
            case 222:
            case 223:
                {
                    int ci;
                    if (int.TryParse(it.Data, out ci))
                    {
                        ApplyPaint(it.Id - 220, ci);
                        ReadPaint();          // la tinta scelta diventa la nuova base
                        paintPreview = true;
                        Notification.PostTicker("~g~" + Txt(it), false);
                    }
                }
                break;

            case 0:
                // voce senza comando (per esempio un attrezzo ancora bloccato):
                // non deve dire niente, e' solo da guardare
                break;

            default:
                Notification.PostTicker("~y~" + L("Not implemented yet", "Non ancora implementata") + "~s~ (id " + it.Id + ")", false);
                break;
        }
    }

    // toggle / list / number: chiamata a ogni cambio di valore
    void OnChanged(TItem it)
    {
        if (it.Id == MOD_LIST)
        {
            if (it.OptImgs != null && it.Sel >= 0 && it.Sel < it.OptImgs.Length) it.Img = it.OptImgs[it.Sel];
            if (it.OptDescs != null && it.Sel >= 0 && it.Sel < it.OptDescs.Length) it.Desc = it.OptDescs[it.Sel];
            return;
        }
        int pid = Function.Call<int>(Hash.PLAYER_ID);
        SaveConfig();

        // le mod esterne non hanno un case: si salva mods.ini e basta
        if (HandleModToggle(it.Id)) return;

        if (it.Id == 281)
        {
            ApplyWardrobe(it);
            return;
        }

        if (it.Id == MOD_SET)
        {
            string[] dd = (it.Data == null) ? null : it.Data.Split('*');
            if (dd == null || dd.Length < 2) return;

            string val;
            if (it.Kind == TItem.TOGGLE) val = it.On ? "1" : "0";
            else if (it.Kind == TItem.LIST) val = it.Opts[it.Sel];
            else val = it.Val.ToString(CultureInfo.InvariantCulture);

            ScriviCfgMod(dd[0], dd[1], val);
            return;
        }

        if (it.Id == MOD_ACTION || it.Id == MOD_INPUT) { AzioneMod(it); return; }

        switch (it.Id)
        {
            case 102:
                Game.Player.Character.IsInvincible = it.On;
                break;

            case 103:
                Function.Call(Hash.SET_MAX_WANTED_LEVEL, it.On ? 0 : 5);
                if (!it.On) Notification.PostTicker("~y~" + L("Wanted level restored", "Ricercato riattivato"), false);
                break;

            case 104:
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, pid, it.Val, false);
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, pid, false);
                break;

            case 207:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists() && !it.On)
                    {
                        v.IsInvincible = false;
                        v.CanTiresBurst = true;
                        v.IsFireProof = false;
                    }
                }
                break;

            case 241:
                {
                    Vehicle v = TargetVehicle();
                    int slot;
                    if (v != null && v.Exists() && int.TryParse(it.Data, out slot))
                    {
                        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
                        Function.Call(Hash.SET_VEHICLE_MOD, v, slot, it.Sel - 1, false);
                        TouchSaved(v);
                    }
                }
                break;

            case 242:
                {
                    Vehicle v = TargetVehicle();
                    int slot;
                    if (v != null && v.Exists() && int.TryParse(it.Data, out slot))
                    {
                        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
                        Function.Call(Hash.TOGGLE_VEHICLE_MOD, v, slot, it.On);
                        TouchSaved(v);
                    }
                }
                break;

            case 243:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
                        Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE, v, it.Sel);
                        TouchSaved(v);
                    }
                }
                break;

            case 244:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        Function.Call(Hash.SET_VEHICLE_WINDOW_TINT, v, it.Sel);
                        TouchSaved(v);
                    }
                }
                break;

            case 246:
                {
                    Vehicle v = TargetVehicle();
                    if (v != null && v.Exists())
                    {
                        Function.Call(Hash.SET_VEHICLE_LIVERY, v, it.Sel - 1);
                        TouchSaved(v);
                    }
                }
                break;

            case 247:
                {
                    Vehicle v = TargetVehicle();
                    int ex;
                    if (v != null && v.Exists() && int.TryParse(it.Data, out ex))
                    {
                        Function.Call(Hash.SET_VEHICLE_EXTRA, v, ex, it.On ? 0 : 1);
                        TouchSaved(v);
                    }
                }
                break;

            case 260:
                if (!it.On) SaveTanks();
                break;

            case 261:
                if (!it.On) SaveBody();
                break;

            case 400:
                SetWeather(it.Sel);
                break;

            case 401:
                if (!it.On)
                {
                    Function.Call(Hash.CLEAR_WEATHER_TYPE_PERSIST);
                    Function.Call(Hash.CLEAR_OVERRIDE_WEATHER);
                }
                else if (tWeather != null)
                {
                    SetWeather(tWeather.Sel);
                }
                break;

            case 402:
            case 403:
                if (tHour != null && tMinute != null)
                {
                    SetClock(tHour.Val, tMinute.Val);
                }
                break;

            case 404:
                if (!it.On)
                {
                    Function.Call(Hash.NETWORK_CLEAR_CLOCK_TIME_OVERRIDE);
                }
                break;

            case 409:
                Function.Call(Hash.SET_ARTIFICIAL_LIGHTS_STATE, it.On);
                break;

            case 700:
            case 702:
                if (it.Id == 702)
                    Function.Call(Hash.SET_NUMBER_OF_PARKED_VEHICLES, it.On ? 500 : -1);
                ContaIntorno();
                break;

            case 710:
                if (!it.On)
                {
                    // si spengono le invincibilita' gia' date
                    Ped[] vv = World.GetNearbyPeds(Game.Player.Character, 150f);
                    int vi2;
                    for (vi2 = 0; vi2 < vv.Length; vi2++)
                    {
                        if (vv[vi2] != null && vv[vi2].Exists()) vv[vi2].IsInvincible = false;
                    }
                    testFatti.Clear();
                }
                break;

            case 706:
            case 707:
            case 708:
            case 709:
            case 711:
            case 712:
            case 713:
                testFatti.Clear();
                break;

            case 714:
                Function.Call(Hash.SET_GRAVITY_LEVEL, it.Sel);
                break;

            case 715:
                {
                    float ts = 1f;
                    if (it.Sel == 1) ts = 0.7f;
                    else if (it.Sel == 2) ts = 0.4f;
                    else if (it.Sel == 3) ts = 0.2f;
                    Function.Call(Hash.SET_TIME_SCALE, ts);
                }
                break;

            case 716:
                Function.Call(Hash.SET_MAX_WANTED_LEVEL, it.Val);
                break;

            case 414:
                Function.Call(Hash.SET_NIGHTVISION, it.On);
                break;

            case 415:
                Function.Call(Hash.SET_SEETHROUGH, it.On);
                break;

            case 416:
                Function.Call(Hash.SET_MINIMAP_HIDE_FOW, it.On);
                break;

            case 417:
                if (!it.On)
                {
                    Function.Call(Hash.DISPLAY_HUD, true);
                    Function.Call(Hash.DISPLAY_RADAR, true);
                }
                break;

            case 418:
                Function.Call(Hash.SET_WIND_SPEED, it.Val * 0.1f);
                break;

            case 223:
                if (!it.On)
                {
                    Vehicle mv = Game.Player.Character.CurrentVehicle;
                    if (mv != null && mv.Exists())
                        Function.Call(Hash.SET_VEHICLE_HAS_MUTED_SIRENS, mv, false);
                }
                break;

            case 271:
                {
                    // l'ancora: solo per le barche, tiene ferma quella su cui sei
                    Vehicle ba = Game.Player.Character.CurrentVehicle;
                    if (ba != null && ba.Exists() && ba.ClassType == VehicleClass.Boats)
                    {
                        if (it.On)
                        {
                            if (!Function.Call<bool>(Hash.CAN_ANCHOR_BOAT_HERE, ba))
                            {
                                it.On = false;
                                Notification.PostTicker("~y~" + L("Can't anchor here", "Qui non si puo' ancorare"), false);
                                break;
                            }
                            Function.Call(Hash.SET_BOAT_ANCHOR, ba, true);
                            // il comando che la tiene ferma davvero anche col
                            // giocatore a bordo (SHVDN non ha il nome, si usa il codice)
                            Function.Call((Hash)0xE3EBAAE484798530uL, ba, true);
                            bool presa = Function.Call<bool>(Hash.IS_BOAT_ANCHORED, ba);
                            Notification.PostTicker(presa
                                ? ("~g~" + L("Anchor down", "Ancora calata"))
                                : ("~y~" + L("Anchor didn't take - stop the boat", "L'ancora non ha preso - ferma la barca")), false);
                            if (!presa) { Function.Call((Hash)0xE3EBAAE484798530uL, ba, false); it.On = false; }
                        }
                        else
                        {
                            Function.Call((Hash)0xE3EBAAE484798530uL, ba, false);
                            Function.Call(Hash.SET_BOAT_ANCHOR, ba, false);
                            Notification.PostTicker("~y~" + L("Anchor up", "Ancora levata"), false);
                        }
                    }
                    else if (it.On)
                    {
                        it.On = false;
                        Notification.PostTicker("~y~" + L("Get on a boat first", "Prima sali su una barca"), false);
                    }
                }
                break;

            case 413:
                if (!it.On)
                {
                    // ricarica il file dell'acqua di serie: il mare torna
                    Function.Call(Hash.LOAD_GLOBAL_WATER_FILE, 0);
                }
                break;

            case 900:
                lang = it.Sel;
                break;

            default:
                break;
        }
    }

    void SpawnVehicle(string name)
    {
        int hash = Function.Call<int>(Hash.GET_HASH_KEY, name);
        if (!Function.Call<bool>(Hash.IS_MODEL_IN_CDIMAGE, hash) || !Function.Call<bool>(Hash.IS_MODEL_A_VEHICLE, hash))
        {
            Notification.PostTicker("~r~" + L("Invalid model", "Modello non valido") + ":~s~ " + name, false);
            return;
        }

        Model m = new Model(hash);
        m.Request();
        int waited = 0;
        while (!m.IsLoaded && waited < 4000)
        {
            Script.Wait(50);
            waited += 50;
        }
        if (!m.IsLoaded)
        {
            Notification.PostTicker("~r~" + L("Load timeout", "Timeout caricamento") + ":~s~ " + name, false);
            return;
        }

        Ped ped = Game.Player.Character;
        Vector3 pos = ped.Position + ped.ForwardVector * 5.0f;
        Vehicle v = World.CreateVehicle(m, pos, ped.Heading + 90f);
        m.MarkAsNoLongerNeeded();

        if (v == null || !v.Exists())
        {
            Notification.PostTicker("~r~" + L("Spawn failed", "Spawn fallito") + ":~s~ " + name, false);
            return;
        }

        if (tDelPrev != null && tDelPrev.On && lastSpawned != null && lastSpawned.Exists() && lastSpawned != v)
        {
            lastSpawned.Delete();
        }
        lastSpawned = v;

        v.PlaceOnGround();
        v.IsPersistent = true;
        Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, v);

        if (tMaxMods != null && tMaxMods.On)
        {
            MaxUpgrades(v);
        }

        if (tSpawnInside != null && tSpawnInside.On)
        {
            ped.SetIntoVehicle(v, VehicleSeat.Driver);
        }

        if (tPersist != null && tPersist.On)
        {
            SaveVehicleEntry(v, name);
        }

        Notification.PostTicker("~g~Spawn:~s~ " + name, false);
    }

    void MaxUpgrades(Vehicle v)
    {
        Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
        int i;
        for (i = 0; i <= 16; i++)
        {
            int max = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, v, i);
            if (max > 0)
            {
                Function.Call(Hash.SET_VEHICLE_MOD, v, i, max - 1, false);
            }
        }
        Function.Call(Hash.TOGGLE_VEHICLE_MOD, v, 18, true);   // turbo
        Function.Call(Hash.SET_VEHICLE_WINDOW_TINT, v, 1);
    }

    // ---------- config persistente: salva OGNI voce con uno stato ----------
    void SalvaFinestra()
    {
        try
        {
            File.WriteAllText(Path.Combine(DATA_DIR, "finestra_pesca.txt"),
                MX.ToString("0", CultureInfo.InvariantCulture) + "|"
                + MY.ToString("0", CultureInfo.InvariantCulture));
        }
        catch { }
    }

    void CaricaFinestra()
    {
        try
        {
            string f = Path.Combine(DATA_DIR, "finestra_pesca.txt");
            if (!File.Exists(f)) return;
            string[] q = File.ReadAllText(f).Trim().Split('|');
            float x, y;
            if (q.Length >= 2
                && float.TryParse(q[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                && float.TryParse(q[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
            {
                if (x >= 0f && x <= 1280f - MW) MX = x;
                if (y >= 0f && y <= 500f) MY = y;
            }
        }
        catch { }
    }

    void SaveConfig()
    {
        try
        {
            Directory.CreateDirectory(DATA_DIR);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Impostazioni del trainer - salvate in automatico");
            sb.AppendLine("# formato: idvoce=valore");

            int mi;
            for (mi = 0; mi < menus.Count; mi++)
            {
                int ii;
                for (ii = 0; ii < menus[mi].Items.Count; ii++)
                {
                    TItem it = menus[mi].Items[ii];
                    if (it.Id <= 0)
                    {
                        continue;
                    }

                    if (it.Kind == TItem.TOGGLE)
                    {
                        sb.AppendLine(it.Id + "=" + (it.On ? "1" : "0"));
                    }
                    else if (it.Kind == TItem.LIST)
                    {
                        sb.AppendLine(it.Id + "=" + it.Sel);
                    }
                    else if (it.Kind == TItem.NUMBER)
                    {
                        sb.AppendLine(it.Id + "=" + it.Val);
                    }
                }
            }

            File.WriteAllText(Path.Combine(DATA_DIR, "config_pesca.ini"), sb.ToString());
        }
        catch (Exception)
        {
        }
    }

    void LoadConfig()
    {
        try
        {
            string f = Path.Combine(DATA_DIR, "config_pesca.ini");
            if (!File.Exists(f))
            {
                return;
            }

            string[] lines = File.ReadAllLines(f);
            int i;
            for (i = 0; i < lines.Length; i++)
            {
                string row = lines[i].Trim();
                if (row.Length == 0 || row.StartsWith("#")) continue;

                string[] kv = row.Split('=');
                if (kv.Length != 2) continue;

                int id;
                int val;
                if (!int.TryParse(kv[0].Trim(), out id)) continue;
                if (!int.TryParse(kv[1].Trim(), out val)) continue;

                int mi;
                for (mi = 0; mi < menus.Count; mi++)
                {
                    int ii;
                    for (ii = 0; ii < menus[mi].Items.Count; ii++)
                    {
                        TItem it = menus[mi].Items[ii];
                        if (it.Id != id) continue;

                        if (it.Kind == TItem.TOGGLE)
                        {
                            it.On = (val == 1);
                        }
                        else if (it.Kind == TItem.LIST)
                        {
                            if (it.Opts != null && val >= 0 && val < it.Opts.Length) it.Sel = val;
                        }
                        else if (it.Kind == TItem.NUMBER)
                        {
                            if (val >= it.Min && val <= it.Max) it.Val = val;
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
        }
    }

    // effetti continui: girano a ogni frame, anche a menu chiuso
    // "Massa": in questa build la handling non e' scrivibile (letta 14.900.000
    // invece di ~1900 kg, l'offset di SHVDN non combacia con Enhanced).
    // Quindi l'effetto si fa con la fisica: le auto che tocchi vengono
    // spinte via con un impulso proporzionale al moltiplicatore e alla
    // tua velocita'. Il risultato a schermo e' quello di un'auto pesantissima.
    int massNext = 0;

    // 0 = off, 1 = solo motore, 2 = solo carrozzeria, 3 = tutto
    // ============================================================
    //  MOD ESTERNE
    //  scripts\Lavori\Uber\Uber.cs        -> voce "Uber" sotto Jobs
    //  scripts\Minigiochi\Drift\Drift.cs  -> voce "Drift" sotto Minigames
    //  Il trainer scrive solo Trainer\mods.ini: "lavori/uber=1".
    // ============================================================
    string ModsFile()
    {
        return Path.Combine(DATA_DIR, "mods.ini");
    }

    // Le cose del trainer che partono accese - odometro, blip delle auto -
    // qui vanno spente: le disegna gia' il trainer normale, che gira
    // insieme a questo. Se restano accese le vedi doppie.
    void SpegniRobaDelTrainer()
    {
        if (tOdoOn != null) tOdoOn.On = false;
        if (tBlips != null) tBlips.On = false;
    }

    void BuildModsMenu(int root)
    {
        int mMods = NewMenu("MODS", "MODS", root);
        TItem sub = AddSub(root, "Mods", "Mods", mMods);
        sub.Cr = PASTEL[2, 0]; sub.Cg = PASTEL[2, 1]; sub.Cb = PASTEL[2, 2];
        sub.Tinted = true;

        int c;
        int nextId = modFirstId;
        for (c = 0; c < MOD_CAT_DIR.Length; c++)
        {
            string dir = Path.Combine(SCRIPTS_DIR, MOD_CAT_DIR[c]);
            string[] subs;
            try
            {
                if (!Directory.Exists(dir)) continue;
                subs = Directory.GetDirectories(dir);
            }
            catch { continue; }
            if (subs.Length == 0) continue;

            Array.Sort(subs);

            bool header = false;
            int i;
            for (i = 0; i < subs.Length; i++)
            {
                // dentro la cartella ci deve essere almeno un .cs, altrimenti
                // e' una cartella di dati e non un mod
                string[] cs;
                try { cs = Directory.GetFiles(subs[i], "*.cs"); }
                catch { continue; }
                if (cs.Length == 0) continue;

                string name = Path.GetFileName(subs[i]);

                if (!header)
                {
                    AddHeader(mMods, "- " + MOD_CAT_EN[c].ToUpper() + " -",
                                     "- " + MOD_CAT_IT[c].ToUpper() + " -", c % 4);
                    header = true;
                }

                // ogni mod ha il suo sottomenu: dentro c'e' l'interruttore
                // e, se il mod le ha, le sue impostazioni
                int mOne = NewMenu(name.ToUpper(), name.ToUpper(), mMods);
                AddSub(mMods, name, name, mOne);

                // Se il menu.txt della mod contiene la riga "sempre_attiva",
                // la mod non ha un interruttore: e' accesa e basta. La voce
                // "Attivo" non viene disegnata, ma l'oggetto esiste lo stesso
                // perche' mods.ini e il resto del trainer ci contano.
                bool senzaInt = false;
                try
                {
                    string fchk = Path.Combine(subs[i], "menu.txt");
                    if (File.Exists(fchk))
                    {
                        string[] rchk = File.ReadAllLines(fchk);
                        int q;
                        for (q = 0; q < rchk.Length; q++)
                            if (rchk[q].Trim().ToLower() == "sempre_attiva") senzaInt = true;
                    }
                }
                catch { }

                TItem t;
                if (senzaInt)
                {
                    t = new TItem(TItem.TOGGLE, "Active", nextId);
                    t.TextIt = "Attivo";
                    t.On = true;
                    modSempre.Add(MOD_CAT_DIR[c].ToLower() + "/" + name.ToLower());
                }
                else
                {
                    t = AddToggle(mOne, "Active", "Attivo", nextId, false);
                }
                modId.Add(MOD_CAT_DIR[c].ToLower() + "/" + name.ToLower());
                modItem.Add(t);
                nextId++;

                // il pannello della mod: lo descrive lei nel suo menu.txt
                BuildPannelloMod(mOne, subs[i]);

            }
        }

        if (modId.Count == 0)
        {
            AddHeader(mMods, "- NO MODS INSTALLED -", "- NESSUN MOD INSTALLATO -", 0);
        }

        LoadModsIni();
    }

    // ============================================================
    //  PANNELLO DELLE MOD
    //  Ogni mod descrive le sue voci in menu.txt, dentro la sua cartella.
    //  Il trainer non sa niente di cosa fa la mod: legge il file, disegna
    //  le voci e le risposte le scrive dove la mod se le aspetta.
    //
    //  Righe ammesse (separatore |):
    //    titolo|- LINEE -
    //    azione|Registra fermata qui|registra_fermata
    //    input|Crea nuova linea...|nuova_linea
    //    toggle|Campanello alla fermata|campanello|1
    //    lista|Tipo di lavoro|tipo|Tutti,Corse,Consegne|0
    //    lista_file|Linea in servizio|linea|linea_*.txt
    //    numero|Prezzo per fermata|prezzo|3|1|20|1
    //
    //  toggle, lista e numero finiscono nel config.ini della mod;
    //  azione e input finiscono in comandi.txt, che la mod legge e svuota.
    // ============================================================
    // i sottomenu delle mod riempiti da file (direttiva "sottofile")
    // mod dichiarate "sempre_attiva": niente interruttore nel menu
    List<string> modSempre = new List<string>();

    List<int> modSubMenu = new List<int>();
    List<string> modSubFile = new List<string>();
    List<string> modSubDir = new List<string>();
    List<DateTime> modSubStamp = new List<DateTime>();
    int modSubNext = 0;

    void RicaricaSottofile(int k)
    {
        int menu = modSubMenu[k];
        TMenu m = menus[menu];
        int vSel = m.Sel, vTop = m.Top;   // il cursore non deve saltare a ogni ricarica
        // e nemmeno quello dei due riquadri: se sposti un pezzo la pagina
        // si riscrive, e il cursore deve restare dov'era
        int vPan = m.PanSel, vPanT = m.PanTop;
        int vPanS = m.PanSelSx, vPanST = m.PanTopSx;
        int vRig = m.RigSel;
        m.Items.Clear();
        m.Sel = 0;
        m.Top = 0;
        m.IconRows = false;
        m.Centrato = false;
        m.HaSotto = false;
        m.Nota = "";
        m.Insegna = "";
        m.Insegne = false;
        m.Blocco = null;
        m.Pannello = null;
        m.PannelloKey = null;
        m.PannelloSx = null;
        m.Titolo = "";
        m.PanSel = -1;
        m.PanTop = 0;
        m.PanSelSx = -1;
        m.PanTopSx = 0;
        m.PannelloSxKey = null;
        m.PannelloGiu = null;
        m.Armatura = null;
        m.PanPie = "";
        m.PanSxPie = "";
        m.Rig = null;

        string[] righe = null;
        try
        {
            if (File.Exists(modSubFile[k]))
            {
                modSubStamp[k] = File.GetLastWriteTimeUtc(modSubFile[k]);
                righe = File.ReadAllLines(modSubFile[k]);
            }
        }
        catch { }

        if (righe == null || righe.Length == 0)
        {
            AddHeader(menu, "(empty)", "(vuoto)", 0);
            return;
        }

        int i;
        for (i = 0; i < righe.Length; i++)
        {
            string r = righe[i].Trim();
            // il cancelletto vale come commento solo se la riga non ha campi:
            // le misure degli ami ("#4/0|...") cominciano con # e sono dati
            if (r.Length == 0) continue;
            if (r[0] == '#' && r.IndexOf('|') < 0) continue;
            string[] c = r.Split('|');
            string testo = c[0].Trim();
            string cmd = (c.Length > 1) ? c[1].Trim() : "";
            string img = (c.Length > 2) ? c[2].Trim() : "";
            string desc = (c.Length > 3) ? c[3].Trim() : "";

            // riga da sola "icone": questo menu disegna l'immagine dentro la
            // riga, a sinistra, e non mette il banner in cima
            if (testo == "icone" && c.Length == 1)
            {
                m.IconRows = true;
                continue;
            }

            // riga da sola "centra": le voci di questo menu si scrivono in
            // mezzo alla riga invece che a sinistra
            if (testo == "centra" && c.Length == 1)
            {
                m.Centrato = true;
                continue;
            }

            // riga "nota|testo": una fascia fissa sopra la lista, che non
            // scorre mai, per ricordare il tasto da premere
            if (testo == "nota" && c.Length > 1)
            {
                m.Nota = c[1].Trim();
                continue;
            }

            // riga da sola "insegne": ogni voce di questo menu si disegna
            // come un'insegna larga quanto il menu, col nome e la
            // descrizione sotto. La usa la pesca per i banner dei tornei.
            if (testo == "insegne" && c.Length == 1)
            {
                m.Insegne = true;
                continue;
            }

            // riga "insegna|file.png": un'immagine larga quanto il menu,
            // in cima alla pagina, con la descrizione che le scorre sotto.
            // La usa la pesca per i banner dei tornei.
            if (testo == "insegna" && c.Length > 1)
            {
                string ip = c[1].Trim();
                if (ip.Length > 0) m.Insegna = Path.Combine(modSubDir[k], ip);
                continue;
            }

            // riga "testo|...": una riga di descrizione. Non e' una voce
            // del menu: si disegna nel riquadro sotto l'insegna, va a capo
            // da sola e non si puo' selezionare. Serve alle schede, dove
            // il testo e' testo e non una lista di bottoni.
            if (testo == "testo" && c.Length > 1)
            {
                if (m.Blocco == null) m.Blocco = new List<string>();
                m.Blocco.Add(c[1]);
                continue;
            }

            // riga "pannello|testo": una riga del riquadro fisso a destra.
            // La mod ne scrive quante ne vuole, in ordine.
            // riga "titolo_finestra|testo": il titolo centrato in cima
            if (testo == "titolo_finestra" && c.Length > 1)
            {
                m.Titolo = c[1].Trim();
                continue;
            }

            // riga "pannello_sx|testo": uguale a "pannello", ma il riquadro
            // sta a SINISTRA della finestra invece che a destra.
            // "pannello_sx|..."   riga sempre visibile
            // "pannello_sx_k|chiave|..."  riga che si vede solo quando in
            // mezzo e' scelta la voce con quel comando: cosi' il riquadro
            // di sinistra cambia insieme alla categoria, senza aspettare
            // che la mod riscriva la pagina.
            if ((testo == "pannello_sx" || testo == "pannello_sx_k") && c.Length > 1)
            {
                if (m.PannelloSx == null) m.PannelloSx = new List<string>();
                if (m.PannelloSxKey == null) m.PannelloSxKey = new List<string>();
                int b = (testo == "pannello_sx_k") ? 1 : 0;
                string chiave = (b == 1) ? c[1].Trim() : "";
                if (c.Length <= 1 + b) continue;
                string rs = c[1 + b].Trim();
                if (c.Length > 2 + b)
                {
                    string isx = c[2 + b].Trim();
                    if (isx.Length > 0) isx = Path.Combine(modSubDir[k], isx);
                    rs += "\u0001" + isx;
                }
                if (c.Length > 3 + b) rs += "\u0001" + c[3 + b].Trim();
                if (c.Length > 4 + b) rs += "\u0001" + c[4 + b].Trim();
                if (c.Length > 5 + b) rs += "\u0001" + c[5 + b].Trim();
                if (c.Length > 6 + b) rs += "\u0001" + c[6 + b].Trim();
                if (c.Length > 7 + b) rs += "\u0001" + c[7 + b].Trim();
                if (c.Length > 8 + b) rs += "\u0001" + c[8 + b].Trim();
                if (c.Length > 9 + b) rs += "\u0001" + c[9 + b].Trim();
                if (c.Length > 10 + b) rs += "\u0001" + c[10 + b].Trim();
                m.PannelloSx.Add(rs);
                m.PannelloSxKey.Add(chiave);
                continue;
            }

            // riga "rig|x|y|larghezza|altezza|comando": una casella
            // dell'armatura, con le coordinate dove la mod la disegna.
            // Andando ancora a destra il cursore ci finisce sopra, e X
            // smonta il pezzo.
            if (testo == "rig" && c.Length > 5)
            {
                if (m.Rig == null) m.Rig = new List<string>();
                m.Rig.Add(c[1].Trim() + "\u0001" + c[2].Trim() + "\u0001"
                          + c[3].Trim() + "\u0001" + c[4].Trim() + "\u0001"
                          + c[5].Trim() + "\u0001"
                          + ((c.Length > 6) ? c[6].Trim() : "") + "\u0001"
                          + ((c.Length > 7) ? c[7].Trim() : ""));
                continue;
            }

            // "pannello_pie|..." e "pannello_sx_pie|...": la riga che sta
            // in FONDO al riquadro, come un piede di pagina. Non scorre.
            if ((testo == "pannello_pie" || testo == "pannello_sx_pie")
                && c.Length > 1)
            {
                string rf = c[1].Trim();
                if (c.Length > 2) rf += "\u0001" + c[2].Trim();
                if (c.Length > 3) rf += "\u0001" + c[3].Trim();
                if (c.Length > 4) rf += "\u0001" + c[4].Trim();
                if (c.Length > 5) rf += "\u0001" + c[5].Trim();
                if (c.Length > 6) rf += "\u0001" + c[6].Trim();
                if (testo == "pannello_pie") m.PanPie = rf;
                else m.PanSxPie = rf;
                continue;
            }

            // riga "armatura|img|dati": il montaggio disegnato sotto la
            // finestra, come nell'HUD. La prima riga e' la canna, poi i
            // pezzi dal basso in su: mulinello, lenza, amo, galleggiante.
            if (testo == "armatura" && c.Length > 1)
            {
                if (m.Armatura == null) m.Armatura = new List<string>();
                string ra = c[1].Trim();
                if (ra.Length > 0) ra = Path.Combine(modSubDir[k], ra);
                ra += "\u0001" + ((c.Length > 2) ? c[2].Trim() : "");
                m.Armatura.Add(ra);
                continue;
            }

            // riga "pannello_giu|...": il riquadro SOTTO la lista in mezzo.
            // Stessi campi degli altri, ma non si sceglie: si guarda.
            if (testo == "pannello_giu" && c.Length > 1)
            {
                if (m.PannelloGiu == null) m.PannelloGiu = new List<string>();
                string rg = c[1].Trim();
                if (c.Length > 2)
                {
                    string igx = c[2].Trim();
                    if (igx.Length > 0) igx = Path.Combine(modSubDir[k], igx);
                    rg += "\u0001" + igx;
                }
                if (c.Length > 3) rg += "\u0001" + c[3].Trim();
                if (c.Length > 4) rg += "\u0001" + c[4].Trim();
                if (c.Length > 5) rg += "\u0001" + c[5].Trim();
                if (c.Length > 6) rg += "\u0001" + c[6].Trim();
                m.PannelloGiu.Add(rg);
                continue;
            }

            // come a sinistra: "pannello_k|chiave|..." si vede solo quando
            // in mezzo e' scelta la voce con quel nome
            if ((testo == "pannello" || testo == "pannello_k") && c.Length > 1)
            {
                if (m.Pannello == null) m.Pannello = new List<string>();
                if (m.PannelloKey == null) m.PannelloKey = new List<string>();
                int bd = (testo == "pannello_k") ? 1 : 0;
                string chd = (bd == 1) ? c[1].Trim() : "";
                if (c.Length <= 1 + bd) continue;
                string rp = c[1 + bd].Trim();
                if (c.Length > 2 + bd)
                {
                    string ipx = c[2 + bd].Trim();
                    if (ipx.Length > 0) ipx = Path.Combine(modSubDir[k], ipx);
                    rp += "\u0001" + ipx;
                }
                if (c.Length > 3 + bd) rp += "\u0001" + c[3 + bd].Trim();
                if (c.Length > 4 + bd) rp += "\u0001" + c[4 + bd].Trim();
                if (c.Length > 5 + bd) rp += "\u0001" + c[5 + bd].Trim();
                if (c.Length > 6 + bd) rp += "\u0001" + c[6 + bd].Trim();
                if (c.Length > 7 + bd) rp += "\u0001" + c[7 + bd].Trim();
                if (c.Length > 8 + bd) rp += "\u0001" + c[8 + bd].Trim();
                if (c.Length > 9 + bd) rp += "\u0001" + c[9 + bd].Trim();
                if (c.Length > 10 + bd) rp += "\u0001" + c[10 + bd].Trim();
                m.Pannello.Add(rp);
                m.PannelloKey.Add(chd);
                continue;
            }

            if (testo == "lista" && c.Length >= 6)
            {
                string etL = cmd;                       // c[1]
                string cmdL = img;                      // c[2]
                string[] valsL = c[3].Trim().Split(';');
                string[] optsL = c[4].Trim().Split(';');
                int selL;
                int.TryParse(c[5].Trim(), out selL);
                if (selL < 0 || selL >= optsL.Length) selL = 0;
                TItem li = AddList(menu, etL, etL, MOD_LIST, optsL, selL);
                li.Data = modSubDir[k] + "*" + cmdL;
                li.OptVals = valsL;
                if (c.Length > 6 && c[6].Trim().Length > 0)
                {
                    string[] imgsL = c[6].Trim().Split(';');
                    int q3;
                    for (q3 = 0; q3 < imgsL.Length; q3++)
                        imgsL[q3] = Path.Combine(modSubDir[k], imgsL[q3].Trim());
                    li.OptImgs = imgsL;
                    if (selL < imgsL.Length) li.Img = imgsL[selL];
                }
                if (c.Length > 7 && c[7].Trim().Length > 0)
                {
                    li.OptDescs = c[7].Trim().Split(';');
                    if (selL < li.OptDescs.Length) li.Desc = li.OptDescs[selL];
                }
                continue;
            }

            if (testo == "sottofile" && c.Length > 2 && img.Length > 0)
            {
                // riga "sottofile|Etichetta|file.txt": una categoria che
                // apre un altro sottomenu riempito da quel file
                string fsub = Path.Combine(modSubDir[k], img);
                int trovato = -1, q2;
                for (q2 = 0; q2 < modSubFile.Count; q2++)
                    if (modSubFile[q2] == fsub) { trovato = q2; break; }
                TItem vsub;
                if (trovato < 0)
                {
                    int sm2 = NewMenu(cmd.ToUpper(), cmd.ToUpper(), menu);
                    vsub = AddSub(menu, cmd, cmd, sm2);
                    modSubMenu.Add(sm2);
                    modSubFile.Add(fsub);
                    modSubDir.Add(modSubDir[k]);
                    modSubStamp.Add(DateTime.MinValue);
                    RicaricaSottofile(modSubMenu.Count - 1);
                }
                else vsub = AddSub(menu, cmd, cmd, modSubMenu[trovato]);
                if (c.Length > 3 && vsub != null && c[3].Trim().Length > 0)
                {
                    string[] col2 = c[3].Trim().Split(',');
                    int cr8, cg8, cb8;
                    if (col2.Length >= 3 && int.TryParse(col2[0].Trim(), out cr8)
                        && int.TryParse(col2[1].Trim(), out cg8) && int.TryParse(col2[2].Trim(), out cb8))
                    {
                        vsub.Cr = cr8; vsub.Cg = cg8; vsub.Cb = cb8;
                        vsub.Tinted = true;
                        vsub.FondoPieno = true;
                    }
                }
                if (c.Length > 4 && vsub != null && c[4].Trim().Length > 0)
                    vsub.Img = Path.Combine(modSubDir[k], c[4].Trim());
                if (c.Length > 5 && vsub != null && c[5].Trim().Length > 0)
                    vsub.Desc = c[5].Trim();
                // settimo campo: la seconda riga piccola sotto il nome,
                // come nelle righe normali. Senza questa la descrizione
                // lunga finiva a destra e si sovrapponeva al nome.
                if (c.Length > 6 && vsub != null && c[6].Trim().Length > 0)
                {
                    vsub.Sotto = c[6].Trim();
                    m.HaSotto = true;
                }
                continue;
            }

            // il trattino apre un'intestazione solo se la riga non ha campi:
            // una voce vera ha sempre le barrette
            if (testo.StartsWith("- ") && c.Length == 1)
            {
                AddHeader(menu, testo, testo, 1);
                continue;
            }

            TItem it;
            if (cmd.Length > 0 && cmd != "niente")
            {
                it = AddAction(menu, testo, testo, MOD_ACTION);
                it.Data = modSubDir[k] + "*" + cmd;
            }
            else
            {
                it = AddAction(menu, testo, testo, 0);
            }
            if (img.Length > 0)
            {
                // due immagini in una: "scatola.png+disegno.png".
                // La seconda si disegna accanto alla prima.
                int piu = img.IndexOf('+');
                if (piu > 0)
                {
                    string i1 = img.Substring(0, piu).Trim();
                    string i2 = img.Substring(piu + 1).Trim();
                    if (i1.Length > 0) it.Img = Path.Combine(modSubDir[k], i1);
                    if (i2.Length > 0) it.Img2 = Path.Combine(modSubDir[k], i2);
                }
                else it.Img = Path.Combine(modSubDir[k], img);
            }
            if (desc.Length > 0) it.Desc = desc;
            // sesto campo: la riga piccola sotto il nome
            if (c.Length > 5 && c[5].Trim().Length > 0)
            {
                it.Sotto = c[5].Trim();
                m.HaSotto = true;
            }
            // settimo campo: il colore del testo a destra, "r,g,b"
            if (c.Length > 6 && c[6].Trim().Length > 0)
            {
                string[] cd = c[6].Trim().Split(',');
                int dr9, dg9, db9;
                if (cd.Length >= 3 && int.TryParse(cd[0].Trim(), out dr9)
                    && int.TryParse(cd[1].Trim(), out dg9)
                    && int.TryParse(cd[2].Trim(), out db9))
                {
                    it.Dr = dr9; it.Dg = dg9; it.Db = db9;
                    it.DescTinta = true;
                }
            }
            // ottavo campo: il colore della riga piccola sotto il nome
            if (c.Length > 7 && c[7].Trim().Length > 0)
            {
                string[] cs = c[7].Trim().Split(',');
                int sr9, sg9, sb9;
                if (cs.Length >= 3 && int.TryParse(cs[0].Trim(), out sr9)
                    && int.TryParse(cs[1].Trim(), out sg9)
                    && int.TryParse(cs[2].Trim(), out sb9))
                {
                    it.Sr = sr9; it.Sg = sg9; it.Sb = sb9;
                    it.SottoTinta = true;
                }
            }
            if (c.Length > 4)
            {
                string[] col = c[4].Trim().Split(',');
                int cr9, cg9, cb9;
                if (col.Length >= 3 && int.TryParse(col[0].Trim(), out cr9)
                    && int.TryParse(col[1].Trim(), out cg9) && int.TryParse(col[2].Trim(), out cb9))
                {
                    it.Cr = cr9; it.Cg = cg9; it.Cb = cb9;
                    it.Tinted = true;
                    it.FondoPieno = true;
                }
            }
        }
        // LE IMPOSTAZIONI DELLA FINESTRA vanno in fondo a questa pagina.
        // La pagina la riscrive la mod a ogni giro e qui dentro si
        // ripulisce tutto, quindi si riattaccano ogni volta. Sono gli
        // stessi oggetti di sempre: quello che accendi resta acceso.
        if (impTrainer.Count > 0 && modSubFile[k] != null
            && modSubFile[k].EndsWith("impostazioni_voci.txt"))
        {
            AddHeader(menu, "- WINDOW -", "- FINESTRA -", 2);
            int qi;
            for (qi = 0; qi < impTrainer.Count; qi++)
                m.Items.Add(impTrainer[qi]);
        }
        if (m.Items.Count == 0) AddHeader(menu, "(empty)", "(vuoto)", 0);
        if (vSel > 0 && m.Items.Count > 0)
        {
            m.Sel = (vSel < m.Items.Count) ? vSel : m.Items.Count - 1;
            m.Top = (vTop < m.Items.Count) ? vTop : 0;
        }
        // i due riquadri tornano dov'erano. Se la riga non c'e' piu'
        // - l'ultimo pezzo spostato - si prende la prima buona.
        if (vPan >= 0 && m.Pannello != null && m.Pannello.Count > 0)
        {
            m.PanSel = PanAttiva(m, vPan) ? vPan : PanPrima(m);
            m.PanTop = vPanT;
            if (m.PanSel >= 0 && m.PanSel < m.PanTop) m.PanTop = m.PanSel;
        }
        if (vRig >= 0 && m.Rig != null && m.Rig.Count > 0)
            m.RigSel = (vRig < m.Rig.Count) ? vRig : m.Rig.Count - 1;
        if (vPanS >= 0 && m.PannelloSx != null && m.PannelloSx.Count > 0)
        {
            m.PanSelSx = SxAttiva(m, vPanS) ? vPanS : SxPrima(m);
            m.PanTopSx = vPanST;
            if (m.PanSelSx >= 0 && m.PanSelSx < m.PanTopSx) m.PanTopSx = m.PanSelSx;
        }
    }

    void PumpSottofile()
    {
        if (Game.GameTime < modSubNext) return;
        modSubNext = Game.GameTime + 1000;
        int k;

        // LA PAGINA CHE E' APERTA ADESSO.
        // "apri" arriva una volta sola, e la mod non saprebbe mai quando
        // chiudi. Questo invece arriva ogni secondo finche' la pagina sta
        // aperta: la mod puo' far vedere qualcosa solo mentre ci sei.
        if (open)
        {
            int kv2;
            for (kv2 = 0; kv2 < modSubMenu.Count; kv2++)
            {
                if (modSubMenu[kv2] != cur) continue;
                ComandoMod(modSubDir[kv2], "vedi "
                           + Path.GetFileName(modSubFile[kv2]));
                break;
            }
        }
        for (k = 0; k < modSubMenu.Count; k++)
        {
            try
            {
                if (!File.Exists(modSubFile[k])) continue;
                DateTime st = File.GetLastWriteTimeUtc(modSubFile[k]);
                if (st != modSubStamp[k]) RicaricaSottofile(k);
            }
            catch { }
        }

        // e le etichette del menu.txt di ogni mod
        for (k = 0; k < modPanMenu.Count; k++)
        {
            try
            {
                if (!File.Exists(modPanFile[k])) continue;
                DateTime st2 = File.GetLastWriteTimeUtc(modPanFile[k]);
                if (st2 != modPanStamp[k]) AggiornaEtichetteMod(k);
            }
            catch { }
        }
    }

    const int MOD_ACTION = 800;   // azione semplice
    const int MOD_INPUT  = 801;   // azione che chiede un testo
    const int MOD_SET    = 802;   // toggle / lista / numero -> config.ini
    const int MOD_LIST   = 803;   // lista dei sottofile: sx/dx sceglie, A conferma

    // ETICHETTE VIVE.
    // menu.txt viene letto una volta sola, quando si costruiscono i menu
    // delle mod. Ma una mod puo' voler cambiare il NOME di una sua voce
    // mentre giochi ("Inizia a pescare" che diventa "Torna a pescare").
    // Qui non si ricostruisce niente: si riscrivono solo i testi e le
    // descrizioni delle voci gia' create, nell'ordine del file. Cosi' i
    // sottomenu, la selezione e lo scorrimento restano dove sono.
    List<int> modPanMenu = new List<int>();
    List<string> modPanFile = new List<string>();
    List<DateTime> modPanStamp = new List<DateTime>();

    // QUANDO IL MENU CAMBIA FORMA, non basta riscrivere le etichette.
    // La pesca toglie e rimette la voce "Inizia a pescare" a seconda di
    // dove sei: se qui ci limitiamo a rinominare quello che c'e' gia',
    // quando una riga sparisce le altre restano al posto sbagliato e ti
    // ritrovi il nome di un lago sul banner di un altro. Allora la
    // pagina si rifa': i sottomenu gia' aperti si riusano - si
    // riconoscono dal file - quindi non si accumula niente.
    void RicostruisciPannelloMod(int k)
    {
        int menu = modPanMenu[k];
        if (menu < 0 || menu >= menus.Count) return;
        string fm = modPanFile[k];
        string cartella = Path.GetDirectoryName(fm);
        string[] righe;
        try
        {
            modPanStamp[k] = File.GetLastWriteTimeUtc(fm);
            righe = File.ReadAllLines(fm);
        }
        catch { return; }

        TMenu m = menus[menu];
        // gli interruttori delle mod si tengono: non li descrive menu.txt
        List<TItem> tenuti = new List<TItem>();
        int qt;
        for (qt = 0; qt < m.Items.Count; qt++)
            if (m.Items[qt].Kind == TItem.TOGGLE) tenuti.Add(m.Items[qt]);
        int vSel = m.Sel;
        m.Items.Clear();
        for (qt = 0; qt < tenuti.Count; qt++) m.Items.Add(tenuti[qt]);

        int i;
        for (i = 0; i < righe.Length; i++)
        {
            string r = righe[i].Trim();
            if (r.Length == 0) continue;
            if (r[0] == '#' && r.IndexOf('|') < 0) continue;
            string[] c = r.Split('|');
            string tipo = c[0].Trim().ToLower();
            if (c.Length < 2) continue;
            string testo = c[1].Trim();
            if (tipo == "titolo") { AddHeader(menu, testo, testo, 1); continue; }
            if (c.Length < 3) continue;
            string chiave = c[2].Trim();
            if (tipo == "azione" || tipo == "input")
            {
                TItem a = AddAction(menu, testo, testo,
                                    tipo == "input" ? MOD_INPUT : MOD_ACTION);
                a.Data = cartella + "*" + chiave;
                continue;
            }
            if (tipo == "sottofile")
            {
                string fsub = Path.Combine(cartella, chiave);
                int trovato = -1, k3;
                for (k3 = 0; k3 < modSubFile.Count; k3++)
                    if (modSubFile[k3] == fsub) { trovato = k3; break; }
                int sm;
                if (trovato < 0)
                {
                    sm = NewMenu(testo.ToUpper(), testo.ToUpper(), menu);
                    modSubMenu.Add(sm);
                    modSubFile.Add(fsub);
                    modSubDir.Add(cartella);
                    modSubStamp.Add(DateTime.MinValue);
                    RicaricaSottofile(modSubMenu.Count - 1);
                }
                else sm = modSubMenu[trovato];
                // il titolo della pagina segue il nome di adesso: se no in
                // fondo alla finestra resta scritto il lago di prima
                menus[sm].Title = testo.ToUpper();
                menus[sm].TitleIt = testo.ToUpper();
                menus[sm].Parent = menu;
                TItem vs = AddSub(menu, testo, testo, sm);
                if (vs != null && c.Length > 4 && c[4].Trim().Length > 0)
                    vs.Img = Path.Combine(cartella, c[4].Trim());
                if (vs != null && c.Length > 5 && c[5].Trim().Length > 0)
                    vs.Desc = c[5].Trim();
                continue;
            }
        }
        if (m.Items.Count == 0) AddHeader(menu, "(empty)", "(vuoto)", 0);
        if (vSel >= m.Items.Count) vSel = m.Items.Count - 1;
        if (vSel < 0) vSel = 0;
        m.Sel = vSel;
        m.Top = 0;
    }

    void AggiornaEtichetteMod(int k)
    {
        try
        {
            modPanStamp[k] = File.GetLastWriteTimeUtc(modPanFile[k]);
            string[] righe = File.ReadAllLines(modPanFile[k]);
            // due elenchi separati: i sottomenu con i sottomenu, le
            // azioni con le azioni. Mescolarli disallineava tutto appena
            // la mod aggiungeva o toglieva una riga.
            List<string> et = new List<string>();
            List<string> de = new List<string>();
            List<string> fi = new List<string>();
            List<string> im = new List<string>();
            List<string> etA = new List<string>();
            List<string> deA = new List<string>();
            int i;
            for (i = 0; i < righe.Length; i++)
            {
                string r = righe[i].Trim();
                if (r.Length == 0) continue;
                if (r[0] == '#' && r.IndexOf('|') < 0) continue;
                string[] c = r.Split('|');
                if (c.Length < 3) continue;
                string tp0 = c[0].Trim().ToLower();
                if (tp0 == "sottofile")
                {
                    et.Add(c[1].Trim());
                    de.Add((c.Length > 5) ? c[5].Trim() : "");
                    fi.Add(c[2].Trim());
                    im.Add((c.Length > 4) ? c[4].Trim() : "");
                }
                else if (tp0 == "azione")
                {
                    etA.Add(c[1].Trim());
                    deA.Add((c.Length > 3) ? c[3].Trim() : "");
                }
            }
            int menu = modPanMenu[k];
            if (menu < 0 || menu >= menus.Count) return;
            // il file ha piu' o meno voci di quante ne abbiamo a schermo:
            // e' cambiata la forma del menu, non solo i nomi
            int nSub = 0, nAz = 0, qz;
            for (qz = 0; qz < menus[menu].Items.Count; qz++)
            {
                if (menus[menu].Items[qz].Kind == TItem.SUB) nSub++;
                else if (menus[menu].Items[qz].Kind == TItem.ACTION) nAz++;
            }
            if (nSub != et.Count || nAz != etA.Count)
            {
                RicostruisciPannelloMod(k);
                return;
            }
            int q = 0, qa = 0, ii;
            for (ii = 0; ii < menus[menu].Items.Count; ii++)
            {
                TItem it = menus[menu].Items[ii];

                if (it.Kind == TItem.ACTION)
                {
                    if (qa < etA.Count)
                    {
                        if (etA[qa].Length > 0) { it.Text = etA[qa]; it.TextIt = etA[qa]; }
                        if (deA[qa].Length > 0) it.Desc = deA[qa];
                        qa++;
                    }
                    continue;
                }
                if (it.Kind != TItem.SUB) continue;
                if (q >= et.Count) continue;
                if (et[q].Length > 0) { it.Text = et[q]; it.TextIt = et[q]; }
                if (de[q].Length > 0) it.Desc = de[q];

                // E ANCHE L'IMMAGINE.
                // La pesca cambia il banner della prima voce a seconda
                // dell'acqua su cui ti trovi: se qui si aggiornava solo
                // il nome, restava il banner del posto dov'eri quando hai
                // caricato la mod.
                if (q < im.Count && im[q].Length > 0)
                {
                    string cartI = Path.GetDirectoryName(modPanFile[k]);
                    it.Img = Path.Combine(cartI, im[q]);
                }

                // e anche il FILE che apre: se la mod cambia l'ordine o
                // sostituisce una voce, il nome nuovo su un file vecchio
                // farebbe aprire la pagina sbagliata.
                if (fi[q].Length > 0)
                {
                    string cart = Path.GetDirectoryName(modPanFile[k]);
                    string nuovo = Path.Combine(cart, fi[q]);
                    int k3;
                    for (k3 = 0; k3 < modSubMenu.Count; k3++)
                    {
                        if (modSubMenu[k3] != it.Sub) continue;
                        if (modSubFile[k3] != nuovo)
                        {
                            modSubFile[k3] = nuovo;
                            modSubStamp[k3] = DateTime.MinValue;
                        }
                        break;
                    }
                }
                q++;
            }
        }
        catch { }
    }

    void BuildPannelloMod(int menu, string cartella)
    {
        string fm = Path.Combine(cartella, "menu.txt");
        if (!File.Exists(fm)) return;

        // da qui in poi le etichette di questo menu si tengono aggiornate
        modPanMenu.Add(menu);
        modPanFile.Add(fm);
        try { modPanStamp.Add(File.GetLastWriteTimeUtc(fm)); }
        catch { modPanStamp.Add(DateTime.MinValue); }

        string[] righe;
        try { righe = File.ReadAllLines(fm); }
        catch { return; }

        int i;
        for (i = 0; i < righe.Length; i++)
        {
            string r = righe[i].Trim();
            // il cancelletto vale come commento solo se la riga non ha campi:
            // le misure degli ami ("#4/0|...") cominciano con # e sono dati
            if (r.Length == 0) continue;
            if (r[0] == '#' && r.IndexOf('|') < 0) continue;

            string[] c = r.Split('|');
            string tipo = c[0].Trim().ToLower();
            if (c.Length < 2) continue;
            string testo = c[1].Trim();

            if (tipo == "titolo")
            {
                AddHeader(menu, testo, testo, 1);
                continue;
            }

            if (c.Length < 3) continue;
            string chiave = c[2].Trim();

            if (tipo == "azione" || tipo == "input")
            {
                TItem a = AddAction(menu, testo, testo,
                                    tipo == "input" ? MOD_INPUT : MOD_ACTION);
                a.Data = cartella + "*" + chiave;
                continue;
            }

            if (tipo == "interruttore")
            {
                // rinomina l'interruttore Attivo della mod e gli mette
                // immagine e descrizione: "interruttore|Etichetta|img|desc"
                int qi;
                for (qi = 0; qi < menus[menu].Items.Count; qi++)
                {
                    TItem tg2 = menus[menu].Items[qi];
                    if (tg2.Kind != TItem.TOGGLE) continue;
                    if (testo.Length > 0) { tg2.Text = testo; tg2.TextIt = testo; }
                    if (c.Length > 2 && c[2].Trim().Length > 0)
                        tg2.Img = Path.Combine(cartella, c[2].Trim());
                    if (c.Length > 3 && c[3].Trim().Length > 0) tg2.Desc = c[3].Trim();
                    break;
                }
                continue;
            }

            if (tipo == "sottofile")
            {
                // sottomenu riempito da un file che la mod riscrive quando
                // vuole: ogni riga "etichetta|comando|immagine". Il trainer
                // lo ricostruisce da solo quando il file cambia.
                int sm = NewMenu(testo.ToUpper(), testo.ToUpper(), menu);
                TItem vs = AddSub(menu, testo, testo, sm);
                // campi facoltativi: "sottofile|Etichetta|file|colore|immagine|descrizione"
                if (vs != null && c.Length > 4 && c[4].Trim().Length > 0)
                    vs.Img = Path.Combine(cartella, c[4].Trim());
                if (vs != null && c.Length > 5 && c[5].Trim().Length > 0)
                    vs.Desc = c[5].Trim();
                modSubMenu.Add(sm);
                modSubFile.Add(Path.Combine(cartella, chiave));
                modSubDir.Add(cartella);
                modSubStamp.Add(DateTime.MinValue);
                RicaricaSottofile(modSubMenu.Count - 1);
                continue;
            }

            if (tipo == "toggle")
            {
                bool on = (c.Length > 3 && c[3].Trim() == "1");
                string val = LeggiCfgMod(cartella, chiave);
                if (val.Length > 0) on = (val == "1");

                TItem tg = AddToggle(menu, testo, testo, MOD_SET, on);
                tg.Data = cartella + "*" + chiave;
                continue;
            }

            if (tipo == "lista" || tipo == "lista_file")
            {
                string[] opts;

                if (tipo == "lista_file")
                {
                    // le opzioni sono i file che corrispondono al modello,
                    // senza prefisso ne' estensione: linea_A1.txt -> A1
                    string modello = (c.Length > 3) ? c[3].Trim() : "*.txt";
                    string prefisso = modello;
                    int st = prefisso.IndexOf('*');
                    prefisso = (st > 0) ? prefisso.Substring(0, st) : "";

                    List<string> tr = new List<string>();
                    try
                    {
                        string[] ff = Directory.GetFiles(cartella, modello);
                        Array.Sort(ff);
                        int k;
                        for (k = 0; k < ff.Length; k++)
                        {
                            string n = Path.GetFileNameWithoutExtension(ff[k]);
                            if (prefisso.Length > 0 && n.StartsWith(prefisso))
                                n = n.Substring(prefisso.Length);
                            if (n.Length > 0) tr.Add(n);
                        }
                    }
                    catch { }

                    if (tr.Count == 0) continue;
                    opts = tr.ToArray();
                }
                else
                {
                    if (c.Length < 4) continue;
                    opts = c[3].Split(',');
                    int k;
                    for (k = 0; k < opts.Length; k++) opts[k] = opts[k].Trim();
                }

                int sel = 0;
                string vv = LeggiCfgMod(cartella, chiave);
                int kk;
                for (kk = 0; kk < opts.Length; kk++)
                {
                    if (opts[kk] == vv) { sel = kk; break; }
                }
                if (vv.Length == 0 && c.Length > 4)
                {
                    int d;
                    if (int.TryParse(c[4].Trim(), out d) && d >= 0 && d < opts.Length) sel = d;
                }

                TItem li = AddList(menu, testo, testo, MOD_SET, opts, sel);
                li.Data = cartella + "*" + chiave;
                continue;
            }

            if (tipo == "numero")
            {
                int val = 0, min = 0, max = 100, step = 1;
                if (c.Length > 3) int.TryParse(c[3].Trim(), out val);
                if (c.Length > 4) int.TryParse(c[4].Trim(), out min);
                if (c.Length > 5) int.TryParse(c[5].Trim(), out max);
                if (c.Length > 6) int.TryParse(c[6].Trim(), out step);

                string vs = LeggiCfgMod(cartella, chiave);
                if (vs.Length > 0) int.TryParse(vs, out val);

                TItem nu = AddNumber(menu, testo, testo, MOD_SET, val, min, max, step);
                nu.Data = cartella + "*" + chiave;
                continue;
            }
        }
    }

    string LeggiCfgMod(string cartella, string chiave)
    {
        try
        {
            string f = Path.Combine(cartella, "config.ini");
            if (!File.Exists(f)) return "";
            string[] rows = File.ReadAllLines(f);
            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string r = rows[i].Trim();
                // il cancelletto vale come commento solo se la riga non ha campi:
            // le misure degli ami ("#4/0|...") cominciano con # e sono dati
            if (r.Length == 0) continue;
            if (r[0] == '#' && r.IndexOf('|') < 0) continue;
                int eq = r.IndexOf('=');
                if (eq <= 0) continue;
                if (r.Substring(0, eq).Trim().ToLower() != chiave.ToLower()) continue;
                return r.Substring(eq + 1).Trim();
            }
        }
        catch { }
        return "";
    }

    void ScriviCfgMod(string cartella, string chiave, string valore)
    {
        try
        {
            string f = Path.Combine(cartella, "config.ini");
            List<string> righe = new List<string>();
            bool trovata = false;

            if (File.Exists(f))
            {
                string[] rows = File.ReadAllLines(f);
                int i;
                for (i = 0; i < rows.Length; i++)
                {
                    string r = rows[i];
                    string t = r.Trim();
                    int eq = t.IndexOf('=');
                    if (t.Length > 0 && t[0] != '#' && eq > 0
                        && t.Substring(0, eq).Trim().ToLower() == chiave.ToLower())
                    {
                        righe.Add(chiave + "=" + valore);
                        trovata = true;
                    }
                    else righe.Add(r);
                }
            }

            if (!trovata) righe.Add(chiave + "=" + valore);

            Directory.CreateDirectory(cartella);
            File.WriteAllLines(f, righe.ToArray());
        }
        catch { }
    }

    // le azioni si passano alla mod scrivendo una riga in comandi.txt:
    // e' lei che la esegue e poi svuota il file
    // Il comando porta con se' dove eri e come eri girato quando l'hai
    // premuto ("... @x|y|z|heading"): la mod potrebbe leggerlo piu' tardi,
    // e i comandi "registra qui" devono usare il posto del clic, non
    // quello della lettura.
    void ComandoMod(string cartella, string comando)
    {
        try
        {
            Ped p = Game.Player.Character;
            string dove = "";
            if (p != null && p.Exists())
            {
                Vector3 q = p.Position;
                Vehicle v = p.CurrentVehicle;
                float h = (v != null && v.Exists()) ? v.Heading : p.Heading;
                dove = " @" + q.X.ToString("0.00", CultureInfo.InvariantCulture)
                     + "|" + q.Y.ToString("0.00", CultureInfo.InvariantCulture)
                     + "|" + q.Z.ToString("0.00", CultureInfo.InvariantCulture)
                     + "|" + h.ToString("0.00", CultureInfo.InvariantCulture);
            }
            // ALCUNI COMANDI CHIUDONO LA FINESTRA.
            // "Inizia a pescare" ti mette la canna in mano: restare col
            // menu aperto davanti non ha senso, e per toglierlo dovevi
            // premere indietro cinque volte. Lo decide la mod: se il
            // comando comincia con "!", la finestra si chiude. Il "!"
            // si toglie prima di scriverlo, alla mod arriva pulito.
            bool chiudi = comando.StartsWith("!");
            if (chiudi) comando = comando.Substring(1);
            Directory.CreateDirectory(cartella);
            File.AppendAllText(Path.Combine(cartella, "comandi.txt"), comando + dove + "\r\n");
            if (chiudi) open = false;
        }
        catch { }
    }

    // stato salvato: e' mods.ini a comandare, non config.ini,
    // cosi' il mod e il trainer leggono sempre la stessa cosa
    void LoadModsIni()
    {
        try
        {
            string f = ModsFile();
            if (!File.Exists(f)) { SaveModsIni(); return; }

            string[] rows = File.ReadAllLines(f);
            int i, k;
            for (i = 0; i < rows.Length; i++)
            {
                string r = rows[i].Trim();
                // il cancelletto vale come commento solo se la riga non ha campi:
            // le misure degli ami ("#4/0|...") cominciano con # e sono dati
            if (r.Length == 0) continue;
            if (r[0] == '#' && r.IndexOf('|') < 0) continue;
                int eq = r.IndexOf('=');
                if (eq < 1) continue;
                string id = r.Substring(0, eq).Trim().ToLower();
                string val = r.Substring(eq + 1).Trim();

                for (k = 0; k < modId.Count; k++)
                {
                    // le mod senza interruttore restano accese comunque:
                    // le accende e le spegne quello che fanno, non il menu
                    if (modId[k] == id && !modSempre.Contains(modId[k]))
                        modItem[k].On = (val == "1");
                }
            }
        }
        catch { }
    }



    void SaveModsIni()
    {
        try
        {
            Directory.CreateDirectory(DATA_DIR);
            StringBuilder sb = new StringBuilder();
            sb.Append("# acceso/spento delle mod esterne - lo scrive il trainer\r\n");
            sb.Append("# formato: categoria/nome=1\r\n");
            int i;
            for (i = 0; i < modId.Count; i++)
            {
                sb.Append(modId[i] + "=" + (modItem[i].On ? "1" : "0") + "\r\n");
            }
            File.WriteAllText(ModsFile(), sb.ToString());
        }
        catch { }
    }

    // true se l'id della voce e' di un mod: allora si salva mods.ini
    bool HandleModToggle(int id)
    {
        if (id < modFirstId || id >= modFirstId + modId.Count) return false;
        SaveModsIni();
        int k = id - modFirstId;
        Notification.PostTicker((modItem[k].On ? "~g~" : "~y~") + modItem[k].Text
            + "~s~ " + (modItem[k].On ? L("on", "acceso") : L("off", "spento")), false);
        return true;
    }

    // ============================================================
    //  RADIO
    //  Le stazioni si chiedono al gioco invece di scriverle a mano:
    //  cosi' ci sono anche quelle nuove di Enhanced e quelle dei mod.
    // ============================================================
    void BuildRadioItems(int menu)
    {
        // NIENTE native qui dentro: questo codice gira mentre lo script
        // nasce, e il gioco puo' non essere ancora pronto a rispondere.
        // La lista vera si riempie al primo tick, dentro ApplyRadio.
        tRadio = AddList(menu, "Radio station", "Stazione radio", 903,
                         new string[] { "Off" }, 0);
        tRadioMobile = AddToggle(menu, "Radio on foot", "Radio a piedi", 904, false);
    }

    // rilegge dal config.ini la stazione scelta, ora che la lista e' vera
    void RestoreSavedRadio()
    {
        try
        {
            string f = Path.Combine(DATA_DIR, "config_pesca.ini");
            if (!File.Exists(f)) return;
            string[] rows = File.ReadAllLines(f);
            int i;
            for (i = 0; i < rows.Length; i++)
            {
                string r = rows[i].Trim();
                if (!r.StartsWith("903=")) continue;
                int val;
                if (int.TryParse(r.Substring(4).Trim(), out val))
                {
                    if (tRadio != null && tRadio.Opts != null
                        && val >= 0 && val < tRadio.Opts.Length)
                    {
                        tRadio.Sel = val;
                    }
                }
                return;
            }
        }
        catch { }
    }

    void FillRadioList()
    {
        radioName.Clear();
        List<string> shown = new List<string>();
        shown.Add("Off");
        shown.Add(L("Free", "Libera"));

        int n = Function.Call<int>(Hash.GET_NUM_UNLOCKED_RADIO_STATIONS);
        int i;
        for (i = 0; i < n; i++)
        {
            string id = Function.Call<string>(Hash.GET_RADIO_STATION_NAME, i);
            if (id == null || id.Length == 0 || id == "OFF") continue;
            radioName.Add(id);
            shown.Add(RadioLabel(id));
        }

        if (radioName.Count > 0 && tRadio != null)
        {
            int keep = tRadio.Sel;
            tRadio.Opts = shown.ToArray();
            if (keep >= tRadio.Opts.Length) keep = 0;
            tRadio.Sel = keep;
        }
    }

    // dal nome interno della stazione al dizionario del suo logo:
    // RADIO_01_CLASS_ROCK -> radio_01class_rock provato prima, poi radio_01class
    string RadioDict(string id)
    {
        if (id == null) return "";
        string t = id.ToLower();
        if (!t.StartsWith("radio_")) return "";

        // radio_01_class_rock -> radio_01class_rock
        int p1 = t.IndexOf('_', 6);
        if (p1 > 0) t = t.Substring(0, p1) + t.Substring(p1 + 1);

        // il dizionario vero e' la forma corta: radio_01class
        int p2 = t.IndexOf('_', 6);
        if (p2 > 0) t = t.Substring(0, p2);
        return t;
    }

    // un colore pastello fisso per ogni stazione: dipende dal nome,
    // quindi la stessa radio ha sempre lo stesso colore
    static readonly Color[] RADIO_PASTEL = new Color[] {
        Color.FromArgb(255, 150, 230, 170),   // verde
        Color.FromArgb(255, 150, 200, 245),   // azzurro
        Color.FromArgb(255, 245, 175, 195),   // rosa
        Color.FromArgb(255, 250, 215, 140),   // ambra
        Color.FromArgb(255, 200, 175, 245),   // lilla
        Color.FromArgb(255, 150, 230, 225),   // acqua
        Color.FromArgb(255, 245, 200, 150),   // pesca
        Color.FromArgb(255, 220, 235, 150)    // lime
    };

    // i colori seguono l'ordine delle stazioni: prima stazione il primo
    // colore, seconda il secondo, e dopo l'ottava si ricomincia
    Color PastelFor(string id)
    {
        if (id == null || id.Length == 0) return Color.FromArgb(255, 255, 255, 255);
        int i = radioName.IndexOf(id);
        if (i < 0) return Color.FromArgb(255, 255, 255, 255);
        return RADIO_PASTEL[i % RADIO_PASTEL.Length];
    }

    // dal nome interno al nome leggibile, se il gioco ce l'ha
    string RadioLabel(string id)
    {
        string lab = Game.GetLocalizedString(id);
        if (lab != null && lab.Length > 0 && lab != "NULL") return lab;

        // ripiego: RADIO_01_CLASS_ROCK -> CLASS ROCK
        string t = id;
        if (t.StartsWith("RADIO_") && t.Length > 9) t = t.Substring(9);
        return t.Replace("_", " ");
    }

    // stazione scelta: 0 = spenta, altrimenti indice in radioName
    void ApplyRadio(Vehicle v)
    {
        if (tRadio == null) return;

        // all'avvio dello script il gioco puo' non aver ancora pronto
        // l'elenco delle stazioni: in quel caso si riempie al primo giro utile
        if (radioName.Count == 0)
        {
            FillRadioList();
            if (radioName.Count == 0) return;

            // la config e' stata letta quando la lista aveva solo "Off":
            // il valore salvato era stato scartato, si rilegge adesso
            RestoreSavedRadio();
        }

        int now = Game.GameTime;
        int vh = (v != null && v.Exists()) ? v.Handle : 0;

        // si interviene quando cambi veicolo, e comunque ogni due secondi:
        // il gioco ogni tanto rimette la sua stazione preferita
        if (vh == radioLastVeh && now < radioNext) return;
        radioLastVeh = vh;
        radioNext = now + 2000;

        int sel = tRadio.Sel;

        if (sel <= 0)
        {
            if (v != null && v.Exists())
            {
                Function.Call(Hash.SET_VEHICLE_RADIO_ENABLED, v, false);
            }
            Function.Call(Hash.SET_RADIO_TO_STATION_NAME, "OFF");
            return;
        }

        // 'Libera': il trainer non tocca piu' niente, la cambi tu nel gioco
        if (sel == 1)
        {
            if (v != null && v.Exists())
            {
                Function.Call(Hash.SET_VEHICLE_RADIO_ENABLED, v, true);
            }
            return;
        }

        int idx = sel - 2;
        if (idx >= radioName.Count) return;

        if (v != null && v.Exists())
        {
            Function.Call(Hash.SET_VEHICLE_RADIO_ENABLED, v, true);
        }

        string want = radioName[idx];
        string cur = Function.Call<string>(Hash.GET_PLAYER_RADIO_STATION_NAME);
        if (cur != want)
        {
            Function.Call(Hash.SET_RADIO_TO_STATION_NAME, want);
        }
    }

    // radio anche a piedi
    void ApplyMobileRadio()
    {
        if (tRadioMobile == null) return;
        bool want = tRadioMobile.On;

        Function.Call(Hash.SET_MOBILE_RADIO_ENABLED_DURING_GAMEPLAY, want);
        Function.Call(Hash.SET_AUDIO_FLAG, "MobileRadioInGame", want);
        Function.Call(Hash.SET_MOBILE_PHONE_RADIO_STATE, want);
    }

    // ============================================================
    //  LIMITATORE DI VELOCITA' (come l'ISA delle auto moderne)
    //  Legge il limite della strada su cui sei e non ti lascia andare
    //  oltre: il gas resta tuo, e' la macchina che non spinge di piu'.
    // ============================================================
    void ApplyLimiter(Vehicle v)
    {
        bool want = (tLimiter != null && tLimiter.On);

        if (v == null || !v.Exists()) { limiterVeh = 0; return; }
        if (v.Driver == null || v.Driver.Handle != Game.Player.Character.Handle) return;

        if (!want)
        {
            // spento: si toglie il tetto sul veicolo su cui sei, sempre,
            // perche' se il limitatore e' stato spento fuori dall'auto il
            // tetto restava attaccato e la macchina non superava i 93 km/h
            Function.Call(Hash.SET_ENTITY_MAX_SPEED, v, 500f);
            limiterVeh = 0;
            return;
        }

        limiterVeh = v.Handle;

        // Due km/h sotto il cartello: cosi' non fa scattare la multa
        // proprio mentre ti sta trattenendo.
        float target = (float)SpeedLimitNow() - 2f;
        if (target < 5f) target = 5f;

        float kmh = Function.Call<float>(Hash.GET_ENTITY_SPEED, v) * 3.6f;

        // Il limitatore vero non frena e non mette un tetto alla fisica:
        // taglia il gas. Cosi' il cambio sale di marcia come sempre e non
        // resti in fuorigiri. Il freno e la retromarcia restano tuoi.
        if (kmh > target)
        {
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 71, true);   // acceleratore
        }

        // rete di sicurezza per le discese: un tetto largo, 15 km/h sopra,
        // che non entra mai in gioco in piano e non tocca le marce
        Function.Call(Hash.SET_ENTITY_MAX_SPEED, v, (target + 15f) / 3.6f);
    }

    int VehGodMode()
    {
        if (tVehGod == null) return 0;
        return tVehGod.Sel;
    }

    void ApplyMass(Vehicle v)
    {
        if (v == null || !v.Exists()) return;
        if (tMass == null) return;

        int sel = tMass.Sel;
        if (sel < 0 || sel >= MASS_MULT.Length) return;

        float mult = MASS_MULT[sel];
        if (mult <= 1f) return;              // x1 e sotto: il gioco fa da se'

        int now = Game.GameTime;
        if (now < massNext) return;
        massNext = now + 50;

        Vector3 me = v.Position;
        float mySpeed = v.Speed;
        if (mySpeed < 1f) mySpeed = 1f;

        // quanto spinge: cresce col moltiplicatore ma senza esplodere
        // il tipo di forza 1 e' gia' scalato sulla massa del bersaglio:
        // bastano numeri piccoli. x1 = zero spinta, poi si sale a mezzi passi.
        float power = (mult - 1f) * 1.2f;

        Vehicle[] near = World.GetNearbyVehicles(me, 7f);
        int i;
        for (i = 0; i < near.Length; i++)
        {
            Vehicle o = near[i];
            if (o == null || !o.Exists()) continue;
            if (o.Handle == v.Handle) continue;

            // solo quelle che stai davvero toccando
            if (!Function.Call<bool>(Hash.IS_ENTITY_TOUCHING_ENTITY, v, o)) continue;

            Vector3 d = o.Position - me;
            d.Z = 0f;
            float len = d.Length();
            if (len < 0.01f) continue;
            d = d / len;

            float force = power * (1f + mySpeed / 25f);

            Function.Call(Hash.APPLY_FORCE_TO_ENTITY, o, 1,
                          d.X * force, d.Y * force, force * 0.08f,
                          0f, 0f, 0f, 0, false, true, true, false, true);
        }
    }

    void ApplyToggles(bool busy)
    {
        Ped p = Game.Player.Character;
        if (p == null || !p.Exists())
        {
            return;
        }
        int pid = Function.Call<int>(Hash.PLAYER_ID);

        if (tGod != null && tGod.On && !p.IsInvincible)
        {
            p.IsInvincible = true;
        }

        if (tNeverWanted != null && tNeverWanted.On)
        {
            if (Function.Call<int>(Hash.GET_PLAYER_WANTED_LEVEL, pid) != 0)
            {
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, pid, 0, false);
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, pid, false);
            }
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
        }

        if (tStamina != null && tStamina.On)
        {
            Function.Call(Hash.RESET_PLAYER_STAMINA, pid);
        }

        if (tBreath != null && tBreath.On)
        {
            Function.Call(Hash.SET_PED_MAX_TIME_UNDERWATER, p, 1000.0f);
        }

        if (tJump != null && tJump.On)
        {
            Function.Call(Hash.SET_SUPER_JUMP_THIS_FRAME, pid);
        }

        if (tInvisible != null)
        {
            Function.Call(Hash.SET_ENTITY_VISIBLE, p, !tInvisible.On, false);
        }

        if (tNoRagdoll != null)
        {
            Function.Call(Hash.SET_PED_CAN_RAGDOLL, p, !tNoRagdoll.On);
        }

        if (tSeatbelt != null)
        {
            // flag 32: non essere sbalzato dal parabrezza
            Function.Call(Hash.SET_PED_CONFIG_FLAG, p, 32, !tSeatbelt.On);
        }

        if (tIgnored != null)
        {
            Function.Call(Hash.SET_EVERYONE_IGNORE_PLAYER, pid, tIgnored.On);
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, pid, tIgnored.On);
        }

        if (tWalkWater != null && tWalkWater.On)
        {
            if (Function.Call<bool>(Hash.IS_ENTITY_IN_WATER, p))
            {
                Function.Call(Hash.SET_PED_CONFIG_FLAG, p, 65, false);
                Function.Call(Hash.SET_PED_CONFIG_FLAG, p, 66, false);
                Function.Call(Hash.SET_PED_CONFIG_FLAG, p, 168, false);
            }
        }

        if (tSpecial != null && tSpecial.On)
        {
            Function.Call(Hash.ENABLE_SPECIAL_ABILITY, pid, true, 0);
            Function.Call(Hash.SET_SPECIAL_ABILITY_MULTIPLIER, 1.5f);
            Function.Call(Hash.SPECIAL_ABILITY_FILL_METER, pid, true, 0);
        }

        if (tAutoClean != null && tAutoClean.On)
        {
            Function.Call(Hash.CLEAR_PED_BLOOD_DAMAGE, p);
            Function.Call(Hash.RESET_PED_VISIBLE_DAMAGE, p);
            Function.Call(Hash.CLEAR_PED_WETNESS, p);
        }

        if (tFastSwim != null)
        {
            Function.Call(Hash.SET_SWIM_MULTIPLIER_FOR_PLAYER, pid,
                          tFastSwim.On ? 1.49f : 1.0f);
        }

        if (tExplosiveAmmo != null && tExplosiveAmmo.On)
        {
            Function.Call(Hash.SET_EXPLOSIVE_AMMO_THIS_FRAME, pid);
        }

        if (tFireAmmo != null && tFireAmmo.On)
        {
            Function.Call(Hash.SET_FIRE_AMMO_THIS_FRAME, pid);
        }

        if (tExplosiveMelee != null && tExplosiveMelee.On)
        {
            Function.Call(Hash.SET_EXPLOSIVE_MELEE_THIS_FRAME, pid);
        }

        if (tTraffic != null && tTraffic.Val < 100)
        {
            float m = tTraffic.Val / 100f;
            Function.Call(Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, m);
            Function.Call(Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, m);
            Function.Call(Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, m);
        }

        if (tPeds != null && tPeds.Val < 100)
        {
            float m = tPeds.Val / 100f;
            Function.Call(Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, m);
            Function.Call(Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, m, m);
        }

        if (tNoReload != null)
        {
            Function.Call(Hash.SET_PED_INFINITE_AMMO_CLIP, p, tNoReload.On);
        }

        if (tInfAmmo != null && tInfAmmo.On)
        {
            OutputArgument wa = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_CURRENT_PED_WEAPON, p, wa, true))
            {
                int wh = wa.GetResult<int>();
                if (wh != 0) Function.Call(Hash.SET_PED_AMMO, p, wh, 9999);
            }
        }

        if (busy) return;

        Vehicle cv = p.CurrentVehicle;

        // uscito dal veicolo -> salva dove l'hai lasciato
        if (wasInVeh && (cv == null || !cv.Exists()))
        {
            if (lastDriven != null && lastDriven.Exists())
            {
                if (trackedIdx >= 0)
                {
                    UpdateTrackedEntry(lastDriven);
                }
                else if (tPersist != null && tPersist.On)
                {
                    SaveVehicleEntry(lastDriven, "");
                    Notification.PostTicker("~g~" + L("Vehicle saved", "Veicolo salvato"), false);
                }
            }
            wasInVeh = false;
        }

        if (cv != null && cv.Exists())
        {
            lastDriven = cv;

            // salendo NON si scrive nulla: il veicolo in quel momento sta
            // ancora venendo agganciato dal gioco e interrogarlo lo fa crashare.
            // Ci si limita a capire se e' gia' nella lista.
            if (!wasInVeh)
            {
                wasInVeh = true;
                trackedIdx = FindMyVehicle(cv);
            }
        }

        ApplyRadio(cv);
        ApplyMobileRadio();
        ApplyLimiter(cv);

        if (cv != null && cv.Exists())
        {
            ApplyMass(cv);

            int gmode = VehGodMode();
            if (gmode > 0)
            {
                // 1 = solo meccanica: il motore non si rompe e non prende
                //     fuoco, ma la macchina si ammacca e puo' morire
                if (gmode == 1 || gmode == 3)
                {
                    Function.Call(Hash.SET_VEHICLE_ENGINE_CAN_DEGRADE, cv, false);
                    Function.Call(Hash.SET_DISABLE_VEHICLE_PETROL_TANK_DAMAGE, cv, true);
                    Function.Call(Hash.SET_DISABLE_VEHICLE_PETROL_TANK_FIRES, cv, true);
                    Function.Call(Hash.SET_DISABLE_VEHICLE_ENGINE_FIRES, cv, true);
                    Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, cv, 1000f);
                    Function.Call(Hash.SET_VEHICLE_PETROL_TANK_HEALTH, cv, 1000f);
                }

                // 2 = solo carrozzeria: non si ammacca, gomme e ruote reggono,
                //     ma il motore si consuma come sempre
                if (gmode == 2 || gmode == 3)
                {
                    Function.Call(Hash.SET_VEHICLE_CAN_BE_VISIBLY_DAMAGED, cv, false);
                    Function.Call(Hash.SET_VEHICLE_STRONG, cv, true);
                    Function.Call(Hash.SET_VEHICLE_HAS_STRONG_AXLES, cv, true);
                    Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST, cv, false);
                    Function.Call(Hash.SET_VEHICLE_WHEELS_CAN_BREAK, cv, false);
                    Function.Call(Hash.SET_VEHICLE_BODY_HEALTH, cv, 1000f);
                    cv.CanTiresBurst = false;
                }

                // 3 = tutto: immune a qualunque danno
                if (gmode == 3)
                {
                    cv.IsInvincible = true;
                    cv.IsFireProof = true;
                    Function.Call(Hash.SET_ENTITY_INVINCIBLE, cv, true);
                    Function.Call(Hash.SET_ENTITY_CAN_BE_DAMAGED, cv, false);
                    Function.Call(Hash.SET_ENTITY_PROOFS, cv,
                                  true, true, true, true, true, true, true, true);
                    if (cv.Health < cv.MaxHealth) cv.Health = cv.MaxHealth;
                }
            }
            else if (vehGodWas)
            {
                // spento: si rimette il veicolo com'era
                cv.IsInvincible = false;
                cv.CanTiresBurst = true;
                cv.IsFireProof = false;
                Function.Call(Hash.SET_ENTITY_INVINCIBLE, cv, false);
                Function.Call(Hash.SET_ENTITY_CAN_BE_DAMAGED, cv, true);
                Function.Call(Hash.SET_ENTITY_PROOFS, cv,
                              false, false, false, false, false, false, false, false);
                Function.Call(Hash.SET_VEHICLE_CAN_BE_VISIBLY_DAMAGED, cv, true);
                Function.Call(Hash.SET_VEHICLE_STRONG, cv, false);
                Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST, cv, true);
                Function.Call(Hash.SET_VEHICLE_WHEELS_CAN_BREAK, cv, true);
                Function.Call(Hash.SET_VEHICLE_ENGINE_CAN_DEGRADE, cv, true);
                Function.Call(Hash.SET_DISABLE_VEHICLE_PETROL_TANK_DAMAGE, cv, false);
                Function.Call(Hash.SET_DISABLE_VEHICLE_PETROL_TANK_FIRES, cv, false);
                Function.Call(Hash.SET_DISABLE_VEHICLE_ENGINE_FIRES, cv, false);
            }
            vehGodWas = (gmode > 0);

            if (tAutoRepair != null && tAutoRepair.On)
            {
                if (cv.EngineHealth < 990f || cv.BodyHealth < 990f)
                {
                    Function.Call(Hash.SET_VEHICLE_FIXED, cv);
                    Function.Call(Hash.SET_VEHICLE_DIRT_LEVEL, cv, 0f);
                }
            }

            if (tAutoFlip != null && tAutoFlip.On)
            {
                if (Function.Call<bool>(Hash.IS_ENTITY_UPSIDEDOWN, cv)
                    && Function.Call<float>(Hash.GET_ENTITY_SPEED, cv) < 2f)
                {
                    Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, cv);
                }
            }

            if (tKeepOn != null && tKeepOn.On)
            {
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, cv, true, true, false);
                Function.Call(Hash.SET_VEHICLE_LIGHTS, cv, 2);
            }

            if (tMuteSiren != null && tMuteSiren.On)
            {
                Function.Call(Hash.SET_VEHICLE_HAS_MUTED_SIRENS, cv, true);
            }

            if (tOnWater != null && tOnWater.On)
            {
                OutputArgument oa = new OutputArgument();
                if (Function.Call<bool>(Hash.GET_WATER_HEIGHT, cv.Position.X, cv.Position.Y, cv.Position.Z, oa))
                {
                    float wz = oa.GetResult<float>();
                    if (cv.Position.Z < wz + 1.5f)
                    {
                        Vector3 pp = cv.Position;
                        cv.Position = new Vector3(pp.X, pp.Y, wz + 0.55f);
                        Vector3 vel = cv.Velocity;
                        if (vel.Z < 0f)
                        {
                            cv.Velocity = new Vector3(vel.X, vel.Y, 0f);
                        }
                    }
                }
            }
        }

        AutoTeleport();

        if (tFreezeTime != null && tFreezeTime.On && tHour != null && tMinute != null)
        {
            Function.Call(Hash.NETWORK_OVERRIDE_CLOCK_TIME, tHour.Val, tMinute.Val, 0);
        }
        else
        {
            UpdateClockSpeed();
        }

        if (tBlackout != null && tBlackout.On)
        {
            Function.Call(Hash.SET_ARTIFICIAL_LIGHTS_STATE, true);
        }

        if (tNoWater != null && tNoWater.On)
        {
            SvuotaOceano();
        }

        TickTest();
        TickArmi();

        if (tHideHud != null && tHideHud.On)
        {
            Function.Call(Hash.DISPLAY_HUD, false);
            Function.Call(Hash.DISPLAY_RADAR, false);
        }

        if (tPuddles != null && tPuddles.Val > 0)
        {
            Function.Call(Hash.SET_RAIN, tPuddles.Val * 0.1f);
        }

        if (tFastRun != null)
        {
            Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, pid, tFastRun.On ? 1.49f : 1.0f);
        }
    }

    // ============================================================
    //  helper per aggiungere voci
    // ============================================================
    TItem AddHeader(int menu, string en, string it, int ci)
    {
        TItem x = new TItem(TItem.HEADER, en, -1);
        x.TextIt = it;
        x.Cr = PASTEL[ci, 0]; x.Cg = PASTEL[ci, 1]; x.Cb = PASTEL[ci, 2];
        x.Tinted = true;
        menus[menu].Items.Add(x);
        return x;
    }

    // versioni bilingui: (inglese, italiano)
    TItem AddAction(int menu, string en, string it, int id)
    {
        TItem x = AddAction(menu, en, id); x.TextIt = it; return x;
    }

    TItem AddToggle(int menu, string en, string it, int id, bool on)
    {
        TItem x = AddToggle(menu, en, id, on); x.TextIt = it; return x;
    }

    TItem AddNumber(int menu, string en, string it, int id, int val, int min, int max, int step)
    {
        TItem x = AddNumber(menu, en, id, val, min, max, step); x.TextIt = it; return x;
    }

    TItem AddList(int menu, string en, string it, int id, string[] opts, int sel)
    {
        TItem x = AddList(menu, en, id, opts, sel); x.TextIt = it; return x;
    }

    TItem AddSub(int menu, string en, string it, int sub)
    {
        TItem x = AddSub(menu, en, sub); x.TextIt = it; return x;
    }

    int NewMenu(string en, string it, int parent)
    {
        int k = NewMenu(en, parent); menus[k].TitleIt = it; return k;
    }

    TItem AddAction(int menu, string text, int id)
    {
        TItem it = new TItem(TItem.ACTION, text, id);
        menus[menu].Items.Add(it);
        return it;
    }

    TItem AddToggle(int menu, string text, int id, bool on)
    {
        TItem it = new TItem(TItem.TOGGLE, text, id);
        it.On = on;
        menus[menu].Items.Add(it);
        return it;
    }

    TItem AddList(int menu, string text, int id, string[] opts, int sel)
    {
        TItem it = new TItem(TItem.LIST, text, id);
        it.Opts = opts;
        it.Sel = sel;
        menus[menu].Items.Add(it);
        return it;
    }

    TItem AddNumber(int menu, string text, int id, int val, int min, int max, int step)
    {
        TItem it = new TItem(TItem.NUMBER, text, id);
        it.Val = val;
        it.Min = min;
        it.Max = max;
        it.Step = step;
        menus[menu].Items.Add(it);
        return it;
    }

    TItem AddSub(int menu, string text, int sub)
    {
        TItem it = new TItem(TItem.SUB, text, -1);
        it.Sub = sub;
        menus[menu].Items.Add(it);
        return it;
    }

    // ============================================================
    //  loop
    // ============================================================
    // vero mentre il personaggio sta entrando o uscendo da un veicolo,
    // o mentre il gioco sta caricando: in quella finestra non tocchiamo
    // nulla che riguardi i veicoli, e' li' che il gioco crashava
    bool VehicleBusy()
    {
        if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return true;

        Ped p = Game.Player.Character;
        if (p == null || !p.Exists()) return true;

        if (Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, p)) return true;
        if (Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, p, true)
            && !Function.Call<bool>(Hash.IS_PED_SITTING_IN_ANY_VEHICLE, p)) return true;

        return false;
    }

    // ============================================================
    //  DISTRIBUTORE: il pannello dei servizi
    //  Quando sei fermo in una stazione compaiono le tre voci: pieno,
    //  tagliando e pasto dell'autogrill. Si scelgono col tastierino.
    // ============================================================
    int staAt = 0;

    void PannelloStazione()
    {
        // resta visibile anche col menu aperto: e' un pannello dell'auto,
        // non del trainer
        Ped p = Game.Player.Character;
        if (p == null || !p.Exists() || p.IsDead) return;
        if (!NearStation(p)) return;

        // il distributore serve solo se sei su un mezzo: a piedi non c'e'
        // niente da rifornire, lavare o riparare
        Vehicle v = p.CurrentVehicle;
        if (v == null || !v.Exists()) return;
        if (Function.Call<float>(Hash.GET_ENTITY_SPEED, v) > 2f) return;

        // ---- prezzi ----
        float perPct = evCurrent ? COST_PER_PCT_EV : COST_PER_PCT;
        int costoPieno = (int)((100f - fuel) * perPct) + 1;
        if (fuel >= 99.5f) costoPieno = 0;


        // ---- riquadro: accanto al cruscotto, sul lato destro ----
        bool serveTagliando = (ServiceKmLeft(v, evCurrent) <= 0f);

        float w = 118f;
        float h = 91f;                        // come il cruscotto: 17 spie + 74
        float x = 640f + 125f + 10f;          // il cruscotto finisce a 765
        float y = 720f - h - 8f;              // stesso fondo del cruscotto

        float engPctSta = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, v) / 10f;
        float bodyPctSta = Function.Call<float>(Hash.GET_VEHICLE_BODY_HEALTH, v) / 10f;

        DrawRect(x, y, w, 15f, 0, 0, 0, 195);
        DrawText(L("STATION", "DISTRIBUTORE"), x + 7f, y + 2f, 0.18f,
                 Color.FromArgb(255, 120, 190, 255));

        DrawRect(x, y + 15f, w, h - 15f, 0, 0, 0, 160);

        Color bianco = Color.FromArgb(255, 235, 235, 240);
        Color grigio = Color.FromArgb(255, 160, 163, 172);

        float ry3 = y + 17f;

        float sporco = Function.Call<float>(Hash.GET_VEHICLE_DIRT_LEVEL, v);
        bool serveLavaggio = (sporco > 1f);
        bool serveRiparo = (engPctSta < 99f || bodyPctSta < 99f);

        bool pienoDaFare = (costoPieno > 0);

        // il tasto 1 si TIENE premuto: sale finche' vuoi e paghi solo quello
        // che metti, cosi' funziona anche con pochi soldi in tasca
        string voce1 = evCurrent ? L("Charge", "Ricarica") : L("Fuel", "Benzina");
        if (pumping) voce1 = voce1 + " ...";
        else if (pienoDaFare) voce1 = voce1 + L(" (hold)", " (tieni)");

        VocePannello(x, ref ry3, "1", voce1,
                     pienoDaFare ? costoPieno : 0, pienoDaFare);

        VocePannello(x, ref ry3, "2", L("Meal", "Pasto"), 15, true);
        VocePannello(x, ref ry3, "3", L("Maintenance", "Manutenzione"),
                     CostoManutenzione(v), serveTagliando);
        VocePannello(x, ref ry3, "4", L("Wash", "Lavaggio"), COST_WASH, serveLavaggio);
        VocePannello(x, ref ry3, "5", L("Repair", "Riparazione"), COST_REPAIR, serveRiparo);

        // ---- tasto 1 tenuto premuto: benzina o ricarica in corso ----
        bool tieni = Game.IsKeyPressed(Keys.NumPad1);
        if (tieni && pienoDaFare && !pumping)
        {
            pumping = true;
            pumpDebt = 0f;
        }
        else if (!tieni && pumping)
        {
            pumping = false;
            SetTank(curTankKey, fuel);
            SaveTanks();
        }

        // ---- gli altri tasti ----
        if (Game.GameTime - staAt < 400) return;

        // TASTO 0: svuota il serbatoio o la batteria. Non e' scritto nel
        // pannello: serve solo per provare il rifornimento.
        if (Game.IsKeyPressed(Keys.NumPad0))
        {
            staAt = Game.GameTime;
            fuel = 2f;
            SetTank(curTankKey, fuel);
            SaveTanks();
            Notification.PostTicker("~y~" + (evCurrent
                ? L("Battery emptied", "Batteria scaricata")
                : L("Tank emptied", "Serbatoio svuotato")), false);
            return;
        }

        if (Game.IsKeyPressed(Keys.NumPad2))
        {
            staAt = Game.GameTime;
            FaiPasto();
        }
        else if (Game.IsKeyPressed(Keys.NumPad3))
        {
            staAt = Game.GameTime;
            // se l'olio e' ancora buono il tasto non fa niente: non si spende
            if (serveTagliando) FaiTagliando();
        }
        else if (Game.IsKeyPressed(Keys.NumPad4))
        {
            staAt = Game.GameTime;
            if (serveLavaggio) FaiLavaggio(v);
        }
        else if (Game.IsKeyPressed(Keys.NumPad5))
        {
            staAt = Game.GameTime;
            if (serveRiparo) FaiRiparazione(v);
        }
    }

    void FaiPieno()
    {
        Ped p = Game.Player.Character;
        Vehicle v = (p != null && p.Exists()) ? p.CurrentVehicle : null;
        if (v == null || !v.Exists())
        {
            Notification.PostTicker("~y~" + L("Get in a vehicle first", "Sali su un veicolo"), false);
            return;
        }
        if (fuel >= 99.5f)
        {
            Notification.PostTicker("~y~" + (evCurrent
                ? L("Battery already full", "Batteria gia' carica")
                : L("Tank already full", "Serbatoio gia' pieno")), false);
            return;
        }

        float perPct = evCurrent ? COST_PER_PCT_EV : COST_PER_PCT;
        int cost = (int)((100f - fuel) * perPct) + 1;

        if (Game.Player.Money < cost)
        {
            Notification.PostTicker("~r~" + L("Not enough money", "Soldi insufficienti")
                + " ($" + cost + ")", false);
            return;
        }

        Game.Player.Money = Game.Player.Money - cost;
        fuel = 100f;
        SetTank(curTankKey, fuel);
        SaveTanks();

        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PURCHASE",
                      "HUD_LIQUOR_STORE_SOUNDSET", true);
        Notification.PostTicker("~g~" + (evCurrent
            ? L("Battery full", "Batteria carica")
            : L("Tank full", "Pieno fatto")) + "~s~ -$" + cost, false);
    }

    void FaiTagliando()
    {
        Ped p = Game.Player.Character;
        Vehicle v = (p != null && p.Exists()) ? p.CurrentVehicle : null;
        if (v == null || !v.Exists())
        {
            Notification.PostTicker("~y~" + L("Get in a vehicle first", "Sali su un veicolo"), false);
            return;
        }
        if (oil > 95f)
        {
            Notification.PostTicker("~y~" + L("Maintenance not needed yet",
                "Manutenzione non ancora necessaria"), false);
            return;
        }
        int costoTg = CostoManutenzione(v);
        if (Game.Player.Money < costoTg)
        {
            Notification.PostTicker("~r~" + L("Not enough money", "Soldi insufficienti")
                + " ($" + costoTg + ")", false);
            return;
        }

        Game.Player.Money = Game.Player.Money - costoTg;

        curOilKey = TankKeyOf(v);
        servM = odoM;
        oil = 100f;
        SetOil(curOilKey, servM);
        SaveOil();

        Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, v, 1000f);
        Function.Call(Hash.MODIFY_VEHICLE_TOP_SPEED, v, 1f);
        oilSlowVeh = 0;

        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PURCHASE",
                      "HUD_LIQUOR_STORE_SOUNDSET", true);
        Notification.PostTicker("~g~" + L("Maintenance done", "Manutenzione fatta")
            + "~s~ -$" + costoTg, false);
    }

    // una riga dei pannelli: numero, voce e prezzo in verde
    void VocePannello(float x, ref float y, string tasto, string testo,
                      int prezzo, bool attiva)
    {
        Color giallo = attiva ? Color.FromArgb(255, 250, 210, 90)
                              : Color.FromArgb(70, 250, 210, 90);
        Color bianco = attiva ? Color.FromArgb(255, 235, 235, 240)
                              : Color.FromArgb(70, 200, 203, 210);
        Color verde = attiva ? Color.FromArgb(255, 130, 225, 180)
                             : Color.FromArgb(70, 130, 225, 180);

        DrawText(tasto, x + 7f, y, 0.18f, giallo);
        DrawText(testo, x + 17f, y, 0.18f, bianco);

        if (prezzo > 0)
        {
            float wt = TextWidth(testo, 0.18f);
            DrawText("$" + prezzo, x + 17f + wt + 6f, y, 0.18f, verde);
        }

        y = y + 13f;
    }

    // Stesso riquadro del distributore, ma davanti a un minimarket: qui si
    // compra da mangiare e da bere.
    void PannelloMarket()
    {
        Ped p = Game.Player.Character;
        if (p == null || !p.Exists() || p.IsDead) return;
        if (!NearMarket(p)) return;

        Vehicle v = p.CurrentVehicle;
        if (v != null && v.Exists()
            && Function.Call<float>(Hash.GET_ENTITY_SPEED, v) > 2f) return;

        float w = 118f;
        float h = 91f;                        // come il cruscotto
        float x = 640f + 125f + 10f;
        float y = 720f - h - 8f;

        Color bianco = Color.FromArgb(255, 235, 235, 240);
        Color giallo = Color.FromArgb(255, 250, 210, 90);

        DrawRect(x, y, w, 15f, 0, 0, 0, 195);
        DrawText(L("SHOP", "MINIMARKET"), x + 7f, y + 2f, 0.18f,
                 Color.FromArgb(255, 120, 190, 255));
        DrawRect(x, y + 15f, w, h - 15f, 0, 0, 0, 160);

        float ry4 = y + 17f;
        VocePannello(x, ref ry4, "1", L("Sandwich", "Panino"), 12, true);
        VocePannello(x, ref ry4, "2", L("Drink", "Bibita"), 3, true);
        VocePannello(x, ref ry4, "3", L("Full meal", "Pasto"), 15, true);

        if (Game.GameTime - staAt < 400) return;

        if (Game.IsKeyPressed(Keys.NumPad1))
        {
            staAt = Game.GameTime;
            Compra(12, 60f, 20f, L("Sandwich", "Panino"));
        }
        else if (Game.IsKeyPressed(Keys.NumPad2))
        {
            staAt = Game.GameTime;
            Compra(3, 0f, 55f, L("Drink", "Bibita"));
        }
        else if (Game.IsKeyPressed(Keys.NumPad3))
        {
            staAt = Game.GameTime;
            Compra(15, 70f, 60f, L("Full meal", "Pasto completo"));
        }
    }

    // acquisto di cibo o bevande: prezzo, quanta fame e quanta sete toglie
    void Compra(int costo, float fame, float sete, string nome)
    {
        if (Game.Player.Money < costo)
        {
            Notification.PostTicker("~r~" + L("Not enough money", "Soldi insufficienti"), false);
            return;
        }

        Game.Player.Money = Game.Player.Money - costo;
        hunger = hunger + fame;
        thirst = thirst + sete;
        if (hunger > 100f) hunger = 100f;
        if (thirst > 100f) thirst = 100f;

        snackNext = Game.GameTime + 8000;
        lastMoney = Game.Player.Money;

        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PURCHASE",
                      "HUD_LIQUOR_STORE_SOUNDSET", true);
        Notification.PostTicker("~g~" + nome + "~s~ -$" + costo, false);
    }

    void FaiLavaggio(Vehicle v)
    {
        if (v == null || !v.Exists()) return;
        if (Game.Player.Money < COST_WASH)
        {
            Notification.PostTicker("~r~" + L("Not enough money", "Soldi insufficienti")
                + " ($" + COST_WASH + ")", false);
            return;
        }

        Game.Player.Money = Game.Player.Money - COST_WASH;
        Function.Call(Hash.SET_VEHICLE_DIRT_LEVEL, v, 0f);
        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PURCHASE",
                      "HUD_LIQUOR_STORE_SOUNDSET", true);
        Notification.PostTicker("~g~" + L("Washed", "Lavato") + "~s~ -$" + COST_WASH, false);
    }

    void FaiRiparazione(Vehicle v)
    {
        if (v == null || !v.Exists()) return;
        if (Game.Player.Money < COST_REPAIR)
        {
            Notification.PostTicker("~r~" + L("Not enough money", "Soldi insufficienti")
                + " ($" + COST_REPAIR + ")", false);
            return;
        }

        Game.Player.Money = Game.Player.Money - COST_REPAIR;
        Function.Call(Hash.SET_VEHICLE_FIXED, v);
        Function.Call(Hash.SET_VEHICLE_DEFORMATION_FIXED, v);
        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PURCHASE",
                      "HUD_LIQUOR_STORE_SOUNDSET", true);
        Notification.PostTicker("~g~" + L("Repaired", "Riparato") + "~s~ -$" + COST_REPAIR, false);
    }

    void FaiPasto()
    {
        Compra(15, 70f, 60f, L("Meal", "Pasto"));
    }

    // ============================================================
    //  RIQUADRO DI SINISTRA: il navigatore
    //  Stessa forma di quello del distributore, dall'altra parte del
    //  cruscotto: strada, zona e distanza alla destinazione.
    // ============================================================
    // taglia una scritta troppo lunga per il riquadro
    string Corto(string s, int max)
    {
        if (s == null) return "";
        if (s.Length <= max) return s;
        if (max <= 1) return s.Substring(0, max);
        return s.Substring(0, max - 1) + ".";
    }

    void PannelloNavigatore()
    {
        Ped p = Game.Player.Character;
        if (p == null || !p.Exists() || p.IsDead) return;

        Vehicle v = p.CurrentVehicle;
        if (v == null || !v.Exists()) return;

        int modo = (tDash != null) ? tDash.Sel : 1;
        if (modo == 0) return;

        // il riquadro compare solo quando c'e' una rotta sulla minimappa:
        // waypoint tuo, missione del gioco o blip di una nostra mod
        ScanTurn();
        RottaMetri = turnHaRotta ? turnClearDist : -1f;
        RottaMetriTempo = Game.GameTime;
        if (!turnHaRotta) return;

        float w = 118f;
        float h = 91f;
        float x = 640f - 125f - 10f - w;      // il cruscotto inizia a 515
        float y = 720f - h - 8f;

        Color bianco = Color.FromArgb(255, 235, 235, 240);

        bool mi = UseMiles();

        // ---- intestazione: DESTINAZIONE a sinistra, distanza alla meta a destra ----
        float dm = turnClearDist;
        string dtxt;
        if (mi)
        {
            float miglia = dm / 1609.344f;
            dtxt = (miglia >= 0.1f)
                   ? miglia.ToString("0.00", CultureInfo.InvariantCulture) + " mi"
                   : (((int)(dm * 1.09361f)) + " yd");
        }
        else
        {
            dtxt = (dm >= 1000f)
                   ? (dm / 1000f).ToString("0.00", CultureInfo.InvariantCulture) + " km"
                   : (((int)dm) + " m");
        }

        DrawRect(x, y, w, 15f, 0, 0, 0, 195);
        DrawText(L("DESTINATION", "DESTINAZIONE"), x + 7f, y + 2f, 0.18f, bianco);
        DrawTextRight(dtxt, x + w - 7f, y + 2f, 0.18f, ColoreRotta());
        DrawRect(x, y + 15f, w, h - 15f, 0, 0, 0, 160);

        // ---- manovra: freccia 24 px e metri 0.34, centrati insieme ----
        string arrow = "dritto.png";
        if (turnCache.Length > 0)
        {
            string lc = turnCache.ToLower();
            bool inv = !(lc == "sinistra" || lc == "left" || lc == "destra" || lc == "right");
            if (inv) arrow = "inversione.png";
            else if (turnDistCache <= 80f)
                arrow = (lc == "sinistra" || lc == "left") ? "sinistra.png" : "destra.png";
        }

        float td = (turnCache.Length > 0) ? turnDistCache : turnClearDist;
        string tt = mi ? (((int)(td * 1.09361f)) + " yd") : (((int)td) + " m");

        float wTt = TextWidth(tt, 0.34f);
        float wGr = 24f + 6f + wTt;
        float gx = x + (w - wGr) * 0.5f;
        DrawIcon(arrow, gx + 12f, y + 30f, 24f, Color.FromArgb(255, 245, 245, 250));
        DrawText(tt, gx + 30f, y + 23f, 0.34f, bianco);

        // riga sottile di separazione, 1 px
        DrawRect(x + 7f, y + 51f, w - 14f, 1f, 255, 255, 255, 40);

        // ---- strada e zona in cui ti trovi, piu' grandi: via 0.24, zona 0.19 ----
        Vector3 pos = p.Position;
        OutputArgument h1 = new OutputArgument();
        OutputArgument h2 = new OutputArgument();
        Function.Call(Hash.GET_STREET_NAME_AT_COORD, pos.X, pos.Y, pos.Z, h1, h2);
        string via = Function.Call<string>(Hash.GET_STREET_NAME_FROM_HASH_KEY,
                                           h1.GetResult<int>());
        if (via == null) via = "";

        string zona = Game.GetLocalizedString(
                       Function.Call<string>(Hash.GET_NAME_OF_ZONE, pos.X, pos.Y, pos.Z));
        if (zona == null) zona = "";

        DrawText(Corto(via, 16), x + 7f, y + 55f, 0.24f, bianco);
        DrawText(Corto(zona, 19), x + 7f, y + 72f, 0.19f, ColoreRotta());   // stesso colore della linea
    }

    // IL BATTITO DELLA MOD.
    // Pesca.cs riscrive vivo.txt ogni due secondi con la sua versione e
    // l'ora del gioco. Se il file non c'e' o non cambia da un po', la
    // mod non sta girando.
    string versionePesca = "";
    int vivoQuando = 0;
    bool vivoOk = false;
    string vivoTesto = "";

    bool PescaViva()
    {
        int ora = Game.GameTime;
        if (ora - vivoQuando < 2500) return vivoOk;
        vivoQuando = ora;
        try
        {
            int k;
            for (k = 0; k < modSubDir.Count; k++)
            {
                string f = Path.Combine(modSubDir[k], "vivo.txt");
                if (!File.Exists(f)) continue;
                string t = File.ReadAllText(f).Trim();
                if (t == vivoTesto) { vivoOk = false; return false; }  // fermo
                vivoTesto = t;
                string[] c = t.Split('|');
                if (c.Length > 0) versionePesca = c[0].Trim();
                vivoOk = true;
                return true;
            }
        }
        catch { }
        vivoOk = false;
        return false;
    }

    void OnTick(object sender, EventArgs e)
    {
        // QUESTO E' IL TRAINER DELLA PESCA E BASTA.
        // Nasce come copia di quello grande, ma benzinai, minimarket,
        // navigatore, blip, carburante, cruscotto e tachimetro non
        // c'entrano niente con la pesca: qui non girano proprio. Il
        // codice resta - e' lo stesso file - ma non lo chiama nessuno.
        ProcessPending();
        PumpSottofile();
        HandleOpenClose();

        if (tTopBar == null || tTopBar.On)
        {
            DrawHeader(MX, MY, MW);

            // a menu chiuso, sotto l'header, si dice come si apre: se no
            // l'indicazione la leggi solo quando l'hai gia' aperto
            if (!open)
            {
                // sotto le barre di fame e sete, quando ci sono (12 px l'una)
                float hy = MY + HEAD_H;
                if (tBody != null && tBody.On) hy = hy + 24f;
                DrawRect(MX, hy, MW, 10f, 0, 0, 0, 150);
                // LA MOD C'E' O NON C'E'.
                // La pesca scrive vivo.txt ogni due secondi: se quel file
                // e' vecchio o non c'e', la mod non sta girando - di solito
                // perche' non ha compilato - e il trainer lo dice in rosso
                // invece di lasciarti a premere tasti che non fanno niente.
                if (PescaViva())
                {
                    DrawText("PESCA " + versionePesca, MX + 9f, hy + 1f, 0.15f,
                             Color.FromArgb(255, 120, 190, 255));
                    DrawTextRight(L("F7 / RB+RIGHT", "F7 / RB+DESTRA"),
                                  MX + MW - 9f, hy + 1f, 0.15f,
                                  Color.FromArgb(255, 170, 170, 185));
                }
                else
                {
                    DrawText("PESCA", MX + 9f, hy + 1f, 0.15f,
                             Color.FromArgb(255, 235, 90, 80));
                    DrawTextRight(L("MOD NOT COMPILED", "MOD NON COMPILATA"),
                                  MX + MW - 9f, hy + 1f, 0.15f,
                                  Color.FromArgb(255, 235, 90, 80));
                }
            }
        }


        if (!open)
        {
            return;
        }

        BlockGameControls();
        HandleNavigation();
        UpdatePaintPreview();
        DrawMenu();
    }

    // Allo scarico del dominio NON si usa REMOVE_BLIP: nasconde e basta,
    // come fa Grocery. Rimuovere blip qui fa crashare il gioco.
    // cancella i blip invisibili dei nostri tipi (auto personali, benzinai,
    // minimarket) lasciati da una sessione precedente degli script
    void PulisciBlipNascosti()
    {
        try
        {
            Blip[] tutti = World.GetAllBlips();
            int tolti = 0;
            int i;
            for (i = 0; i < tutti.Length; i++)
            {
                Blip b = tutti[i];
                if (b == null || !b.Exists()) continue;
                int sp = Function.Call<int>(Hash.GET_BLIP_SPRITE, b.Handle);
                if (sp != 225 && sp != 226 && sp != 427 && sp != 422 && sp != 423
                    && sp != 361 && sp != 52 && sp != 40) continue;
                if (Function.Call<int>(Hash.GET_BLIP_ALPHA, b.Handle) != 0) continue;
                b.Delete();
                tolti++;
            }
            if (tolti > 0)
                Notification.PostTicker("~y~V Mods Manager~s~ " + L("cleaned up", "puliti")
                    + " " + tolti + " blip", false);
        }
        catch (Exception) { }
    }

    void HideBlip(int b)
    {
        if (b == 0) return;
        Function.Call(Hash.SET_BLIP_ALPHA, b, 0);
        Function.Call(Hash.SET_BLIP_DISPLAY, b, 0);   // fuori da minimappa e mappa
    }

    void ShowBlip(int b)
    {
        if (b == 0) return;
        Function.Call(Hash.SET_BLIP_ALPHA, b, 255);
        Function.Call(Hash.SET_BLIP_DISPLAY, b, 2);
    }

    void OnAborted(object sender, EventArgs e)
    {
        try
        {
            SaveTanks();
        SaveBatt();
            SaveOil();
            SaveOdo();
            SaveBody();
        }
        catch (Exception)
        {
        }

        int i;
        for (i = 0; i < pvBlip.Count; i++)
        {
            HideBlip(pvBlip[i]);
        }
        if (gasBlips != null)
        {
            for (i = 0; i < gasBlips.Length; i++)
            {
                HideBlip(gasBlips[i]);
            }
        }
        if (mkBlips != null)
        {
            for (i = 0; i < mkBlips.Length; i++)
            {
                HideBlip(mkBlips[i]);
            }
        }

        if (clockTaken)
        {
            Function.Call(Hash.PAUSE_CLOCK, false);
        }
        Function.Call(Hash.NETWORK_CLEAR_CLOCK_TIME_OVERRIDE);

    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        // niente qui: F4 gestito a tick per funzionare anche col pad collegato
    }

    // LA RUOTA DELLA PESCA CHIUDE IL MENU.
    // Quando la mod apre la ruota (LB) scrive "chiudi.txt" nella sua
    // cartella: se il menu e' aperto si chiude, cosi' non si pestano i
    // piedi sugli stessi tasti e la ruota ha lo schermo libero.
    void ChiudiSeChiesto()
    {
        try
        {
            int k;
            for (k = 0; k < modSubDir.Count; k++)
            {
                string f = Path.Combine(modSubDir[k], "chiudi.txt");
                if (!File.Exists(f)) continue;
                try { File.Delete(f); } catch { }
                open = false;
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "BACK", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
                return;
            }
        }
        catch { }
    }

    // LA RUOTA APRE UNA PAGINA: la mod scrive "apri.txt" col nome del
    // file della pagina (casa_voci.txt = equipaggiamento). Si apre il
    // menu direttamente su quella, e alla mod si manda "apri" come se ci
    // fossi entrato tu.
    void ApriSeChiesto()
    {
        try
        {
            int k;
            for (k = 0; k < modSubDir.Count; k++)
            {
                string f = Path.Combine(modSubDir[k], "apri.txt");
                if (!File.Exists(f)) continue;
                string nome = File.ReadAllText(f).Trim();
                try { File.Delete(f); } catch { }
                // "file|voce": dopo il nome, la voce su cui mettere il cursore
                string voce = "";
                int barra = nome.IndexOf('|');
                if (barra >= 0) { voce = nome.Substring(barra + 1).Trim(); nome = nome.Substring(0, barra).Trim(); }
                int ks;
                for (ks = 0; ks < modSubMenu.Count; ks++)
                {
                    if (Path.GetFileName(modSubFile[ks]) != nome) continue;
                    open = true;
                    cur = modSubMenu[ks];
                    menus[cur].Sel = FirstSelectable(cur);
                    menus[cur].Top = 0;
                    if (voce.Length > 0)
                    {
                        int iv;
                        for (iv = 0; iv < menus[cur].Items.Count; iv++)
                            if (menus[cur].Items[iv].Text == voce) { menus[cur].Sel = iv; break; }
                        if (menus[cur].Sel >= MAX_VIS) menus[cur].Top = menus[cur].Sel - MAX_VIS + 1;
                    }
                    ComandoMod(modSubDir[ks], "apri " + nome);
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
                    return;
                }
                return;
            }
        }
        catch { }
    }

    void HandleOpenClose()
    {
        if (open) ChiudiSeChiesto();
        else ApriSeChiesto();
        // --- tastiera: F7 ---
        // Questo e' il trainer della pesca: sta accanto a quello normale,
        // che tiene F4 e RB+GIU. Qui F7 e RB+DESTRA, cosi' i due non si
        // pestano i piedi e le finestre le sposti dove vuoi.
        bool f5 = Game.IsKeyPressed(Keys.F7);
        if (f5 && !f5Last)
        {
            Toggle();
        }
        f5Last = f5;

        // --- pad: RB tenuto + DPAD-GIU ---
        rbHeld = Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 2, C_PAD_RB)
              || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 2, C_PAD_RB);
        bool dpadDown = Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 2, C_RIGHT)
                     || Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 2, C_RIGHT);
        bool combo = rbHeld && dpadDown;

        if (combo && !comboLast)
        {
            Toggle();
        }
        comboLast = combo;
    }

    void Toggle()
    {
        open = !open;
        if (open)
        {
            cur = (menuPesca >= 0) ? menuPesca : 0;
            menus[cur].Sel = FirstSelectable(cur);
            menus[cur].Top = 0;
        }
        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
    }

    void BlockGameControls()
    {
        // col menu aperto il gioco non deve fare NIENTE:
        // niente cambio personaggio, niente ruota armi, niente telefono
        int[] blocked = new int[] {
            24, 25, 47, 257, 263, 264, 140, 141, 142,   // attacco / mira / armi / mischia
            22, 23, 75, 27, 37, 44, 45, 80, 199, 200,   // salto, entra/esci, telefono, armi, pausa
            172, 173, 174, 175, 176, 177,               // frecce: le usiamo noi
            19, 20, 21, 29, 36, 48, 56, 73, 74, 82, 83, 84, 85,   // cambio personaggio, mappa, tuffo
            157, 158, 159, 160, 161, 162, 163, 164, 165,          // armi rapide 1..9
            166, 167, 168, 169, 170, 171,               // F5..F10 e simili
            243, 288, 289, 311, 344,                    // ~, F1, F2, replay, telefono
            244, 245, 246, 247, 248,                    // menu interazione e chat
            290, 291, 292, 293, 294, 295, 296, 297, 298, 299,   // registratore replay
            300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310,
            320, 321, 322, 323,                         // pausa e menu frontend
            0, 1, 2, 3, 4, 5, 6                         // sguardo e rotazione camera
        };
        int i;
        for (i = 0; i < blocked.Length; i++)
        {
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, blocked[i], true);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 1, blocked[i], true);
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 2, blocked[i], true);
        }
    }

    bool Pressed(int control)
    {
        return Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 2, control)
            || Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 2, control);
    }

    bool Held(int control)
    {
        return Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 2, control)
            || Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 2, control);
    }

    bool IsSelectable(int menu, int idx)
    {
        return menus[menu].Items[idx].Kind != TItem.HEADER;
    }

    int FirstSelectable(int menu)
    {
        int i;
        for (i = 0; i < menus[menu].Items.Count; i++)
        {
            if (IsSelectable(menu, i)) return i;
        }
        return 0;
    }

    // le righe del riquadro su cui si puo' andare: quelle con un comando
    // un numero scritto in una riga del riquadro
    static float PanNum(string t)
    {
        float v;
        if (t != null && float.TryParse(t.Trim(),
                System.Globalization.NumberStyles.Float,
                CultureInfo.InvariantCulture, out v)) return v;
        return 0f;
    }

    static string PanCampo(string riga, int q)
    {
        string[] z = riga.Split('\u0001');
        return (q < z.Length) ? z[q] : "";
    }

    // gli stessi conti, ma su un riquadro qualunque: serve per quello
    // di sinistra, che ha la sua lista e il suo cursore
    // il colore della riga di intestazione di un riquadro: lo scrive la
    // mod nel sesto campo, "r,g,b". Se non c'e' resta quello di sempre.
    static Color PanColore(string riga, Color se)
    {
        string t = PanCampo(riga, 5).Trim();
        if (t.Length == 0) return se;
        string[] z = t.Split(',');
        int r9, g9, b9;
        if (z.Length >= 3 && int.TryParse(z[0].Trim(), out r9)
            && int.TryParse(z[1].Trim(), out g9) && int.TryParse(z[2].Trim(), out b9))
            return Color.FromArgb(255, r9, g9, b9);
        return se;
    }

    // la voce scelta nella lista in mezzo: e' il suo TESTO che decide
    // cosa si vede nel riquadro di sinistra. Il testo e non il comando,
    // cosi' le voci in mezzo non hanno bisogno di fare niente.
    static string SxChiave(TMenu m)
    {
        if (m.Sel < 0 || m.Sel >= m.Items.Count) return "";
        string t = m.Items[m.Sel].Text;
        return (t == null) ? "" : t.Trim();
    }

    static bool SxSiVede(TMenu m, int i)
    {
        if (m.PannelloSx == null || i < 0 || i >= m.PannelloSx.Count) return false;
        if (m.PannelloSxKey == null || i >= m.PannelloSxKey.Count) return true;
        string ch = m.PannelloSxKey[i];
        if (ch.Length == 0) return true;
        return ch == SxChiave(m);
    }

    static bool DxSiVede(TMenu m, int i)
    {
        if (m.Pannello == null || i < 0 || i >= m.Pannello.Count) return false;
        if (m.PannelloKey == null || i >= m.PannelloKey.Count) return true;
        string ch = m.PannelloKey[i];
        if (ch.Length == 0) return true;
        return ch == SxChiave(m);
    }

    static bool SxAttiva(TMenu m, int i)
    {
        if (!SxSiVede(m, i)) return false;
        return PanCampo(m.PannelloSx[i], 3).Length > 0;
    }

    static int SxPrima(TMenu m)
    {
        int i;
        if (m.PannelloSx == null) return -1;
        for (i = 0; i < m.PannelloSx.Count; i++) if (SxAttiva(m, i)) return i;
        return -1;
    }

    static int SxDopo(TMenu m, int da, int dir)
    {
        if (m.PannelloSx == null) return -1;
        int i = da + dir;
        while (i >= 0 && i < m.PannelloSx.Count)
        {
            if (SxAttiva(m, i)) return i;
            i += dir;
        }
        return -1;
    }

    // LA RIGA DI INTESTAZIONE DI UN RIQUADRO.
    // Sta in mezzo alla riga, in maiuscolo, piu' piccola. Il testo si
    // puo' spezzare in pezzi con la tilde, e ogni pezzo prende il suo
    // colore dalla lista scritta nel sesto campo, "r,g,b;r,g,b;...".
    // Serve per le righe dei tasti: "X ARMA" blu, "/" bianco, "DISARMA"
    // rosso. Se i colori finiscono si ripete l'ultimo.
    void CaptionRiquadro(float bx, float by, string riga, string testo, Color se)
    {
        string[] pezzi = testo.ToUpper().Split('~');
        string[] col = PanCampo(riga, 5).Split(';');

        float tot = 0f;
        int i;
        for (i = 0; i < pezzi.Length; i++) tot += TextWidth(pezzi[i], 0.185f);

        float x0 = bx + (MW - tot) * 0.5f;
        Color ult = se;
        for (i = 0; i < pezzi.Length; i++)
        {
            if (i < col.Length) ult = ColoreScritto(col[i], ult);
            DrawText(pezzi[i], x0, by + 5f, 0.185f, ult);
            x0 += TextWidth(pezzi[i], 0.185f);
        }
    }

    static Color ColoreScritto(string t, Color se)
    {
        if (t == null) return se;
        t = t.Trim();
        if (t.Length == 0) return se;
        string[] z = t.Split(',');
        int r9, g9, b9;
        if (z.Length >= 3 && int.TryParse(z[0].Trim(), out r9)
            && int.TryParse(z[1].Trim(), out g9) && int.TryParse(z[2].Trim(), out b9))
            return Color.FromArgb(255, r9, g9, b9);
        return se;
    }

    static bool PanAttivaL(List<string> p, int i)
    {
        if (p == null || i < 0 || i >= p.Count) return false;
        return PanCampo(p[i], 3).Length > 0;
    }

    static int PanPrimaL(List<string> p)
    {
        int i;
        if (p == null) return -1;
        for (i = 0; i < p.Count; i++) if (PanAttivaL(p, i)) return i;
        return -1;
    }

    static int PanDopoL(List<string> p, int da, int dir)
    {
        if (p == null) return -1;
        int i = da + dir;
        while (i >= 0 && i < p.Count)
        {
            if (PanAttivaL(p, i)) return i;
            i += dir;
        }
        return -1;
    }

    static bool PanAttiva(TMenu m, int i)
    {
        if (!DxSiVede(m, i)) return false;
        return PanCampo(m.Pannello[i], 3).Length > 0;
    }

    static int PanPrima(TMenu m)
    {
        int i;
        if (m.Pannello == null) return -1;
        for (i = 0; i < m.Pannello.Count; i++) if (PanAttiva(m, i)) return i;
        return -1;
    }

    static int PanDopo(TMenu m, int da, int dir)
    {
        int i = da + dir;
        while (i >= 0 && i < m.Pannello.Count)
        {
            if (PanAttiva(m, i)) return i;
            i += dir;
        }
        return -1;
    }

    // Il colore lo decide il TIPO di valore, non chi scrive la riga:
    //   quantita' (x25)      rosa
    //   chili (kg)           verde
    //   misure (mm, m, #4/0) azzurro
    //   tutto il resto       giallo tenue
    static Color ColoreValore(string s, bool sel)
    {
        string b = s.ToLower();
        bool qta = b.StartsWith("x") && b.Length > 1 && char.IsDigit(b[1]);
        bool kg = b.EndsWith(" kg") || b.EndsWith("kg");
        bool mis = b.EndsWith(" mm") || b.EndsWith(" m") || b.IndexOf('#') >= 0;
        if (sel)
        {
            if (qta) return Color.FromArgb(255, 130, 40, 80);
            if (kg) return Color.FromArgb(255, 30, 90, 60);
            if (mis) return Color.FromArgb(255, 30, 70, 105);
            return Color.FromArgb(255, 95, 80, 25);
        }
        if (qta) return Color.FromArgb(255, 245, 150, 195);
        if (kg) return Color.FromArgb(255, 140, 225, 175);
        if (mis) return Color.FromArgb(255, 130, 200, 245);
        // i punti esperienza hanno il loro azzurro, sempre lo stesso
        if (b.EndsWith("xp")) return Color.FromArgb(255, 130, 200, 245);
        return Color.FromArgb(255, 235, 210, 130);
    }

    void MoveSel(TMenu m, int dir)
    {
        int n = m.Items.Count;
        if (n == 0) return;
        int guard = 0;
        do
        {
            m.Sel = (m.Sel + dir + n) % n;
            guard++;
        }
        while (m.Items[m.Sel].Kind == TItem.HEADER && guard <= n);
    }

    void HandleNavigation()
    {
        if (rbHeld)
        {
            return;
        }

        if (spostaFinestra)
        {
            float passo = 4f;
            if (Held(C_UP)    || Game.IsKeyPressed(Keys.NumPad8)) MY -= passo;
            if (Held(C_DOWN)  || Game.IsKeyPressed(Keys.NumPad2)) MY += passo;
            if (Held(C_LEFT)  || Game.IsKeyPressed(Keys.NumPad4)) MX -= passo;
            if (Held(C_RIGHT) || Game.IsKeyPressed(Keys.NumPad6)) MX += passo;
            if (MX < 0f) MX = 0f;
            if (MX > 1280f - MW) MX = 1280f - MW;
            if (MY < 0f) MY = 0f;
            if (MY > 500f) MY = 500f;
            if (Pressed(C_ACCEPT) || Pressed(C_CANCEL))
            {
                spostaFinestra = false;
                SalvaFinestra();
                Notification.PostTicker("~g~" + L("Window position saved", "Posizione della finestra salvata"), false);
            }
            return;
        }

        TMenu m = menus[cur];
        int n = m.Items.Count;
        int now = Game.GameTime;

        // --- su / giu (con auto-ripetizione se tenuto premuto) ---
        bool up   = Pressed(C_UP)   || Game.IsKeyPressed(Keys.NumPad8);
        bool down = Pressed(C_DOWN) || Game.IsKeyPressed(Keys.NumPad2);
        bool upHeld   = Held(C_UP);
        bool downHeld = Held(C_DOWN);

        // se il cursore e' sull'armatura, su e giu' cambiano pezzo
        if (m.RigSel >= 0 && m.Rig != null)
        {
            if ((up || down || upHeld || downHeld) && now >= navNext)
            {
                navNext = now + ((up || down) ? 160 : 110);
                Beep();
                int r2 = m.RigSel + ((up || upHeld) ? -1 : 1);
                if (r2 >= 0 && r2 < m.Rig.Count) m.RigSel = r2;
            }
            else if (!(up || down || upHeld || downHeld)) navNext = 0;
        }
        // se il cursore e' nel riquadro di SINISTRA, su e giu' si muovono li'
        else if (m.PanSelSx >= 0)
        {
            if ((up || down || upHeld || downHeld) && now >= navNext)
            {
                navNext = now + ((up || down) ? 160 : 110);
                Beep();
                int p3 = SxDopo(m, m.PanSelSx, (up || upHeld) ? -1 : 1);
                if (p3 >= 0) m.PanSelSx = p3;
            }
            else if (!(up || down || upHeld || downHeld)) navNext = 0;

            if (m.PanSelSx < m.PanTopSx) m.PanTopSx = m.PanSelSx;
            if (m.PanSelSx >= m.PanTopSx + MAX_VIS) m.PanTopSx = m.PanSelSx - MAX_VIS + 1;
        }
        // se il cursore e' nel riquadro di destra, su e giu' si muovono li'
        else if (m.PanSel >= 0)
        {
            if ((up || down || upHeld || downHeld) && now >= navNext)
            {
                navNext = now + ((up || down) ? 160 : 110);
                Beep();
                int p2 = PanDopo(m, m.PanSel, (up || upHeld) ? -1 : 1);
                if (p2 >= 0) m.PanSel = p2;
            }
            else if (!(up || down || upHeld || downHeld)) navNext = 0;

            if (m.PanSel >= 0)
            {
                if (m.PanSel < m.PanTop) m.PanTop = m.PanSel;
                if (m.PanSel >= m.PanTop + MAX_VIS) m.PanTop = m.PanSel - MAX_VIS + 1;
            }
        }
        else if (n > 0)
        {
            if (up || down)
            {
                if (now >= navNext)
                {
                    MoveSel(m, up ? -1 : 1);
                    navNext = now + 160;
                    Beep();
                }
            }
            else if (upHeld || downHeld)
            {
                if (now >= navNext)
                {
                    MoveSel(m, upHeld ? -1 : 1);
                    navNext = now + 110;
                    Beep();
                }
            }
            else
            {
                navNext = 0;
            }

            // scroll
            if (m.Sel < m.Top) m.Top = m.Sel;
            if (m.Sel >= m.Top + MAX_VIS) m.Top = m.Sel - MAX_VIS + 1;
        }

        // --- indietro ---
        if (Pressed(C_CANCEL) || Game.IsKeyPressed(Keys.NumPad0))
        {
            if (paintPreview)
            {
                RestorePaint();
                paintPreview = false;
                lastPreviewMenu = -1;
                lastPreviewSel = -1;
            }

            if (menus[cur].Parent >= 0)
            {
                cur = menus[cur].Parent;
            }
            else
            {
                open = false;
            }
            Beep();
            return;
        }

        if (n == 0)
        {
            return;
        }

        TItem it = m.Items[m.Sel];

        // --- sinistra / destra ---
        bool left  = Pressed(C_LEFT)  || Game.IsKeyPressed(Keys.NumPad4);
        bool right = Pressed(C_RIGHT) || Game.IsKeyPressed(Keys.NumPad6);

        // destra entra nel riquadro, sinistra torna nella lista: cosi' si
        // passa da una colonna all'altra in un colpo, da qualunque riga
        if (right && m.PanSel < 0 && m.PanSelSx < 0 && PanPrima(m) >= 0)
        {
            m.PanSel = PanPrima(m);
            m.PanTop = 0;
            Beep();
            return;
        }
        // ancora a destra: dal riquadro si va sull'armatura, sulla canna
        if (right && m.PanSel >= 0 && m.RigSel < 0
            && m.Rig != null && m.Rig.Count > 0)
        {
            m.RigSel = 0;
            Beep();
            return;
        }
        if (left && m.RigSel >= 0)
        {
            m.RigSel = -1;
            Beep();
            return;
        }
        if (left && m.PanSel >= 0)
        {
            m.PanSel = -1;
            Beep();
            return;
        }
        // e dall'altra parte, uguale: sinistra entra nel riquadro di
        // sinistra, destra torna nella lista in mezzo
        if (left && m.PanSelSx < 0 && m.PanSel < 0 && SxPrima(m) >= 0)
        {
            m.PanSelSx = SxPrima(m);
            m.PanTopSx = 0;
            Beep();
            return;
        }
        if (right && m.PanSelSx >= 0)
        {
            m.PanSelSx = -1;
            Beep();
            return;
        }

        if (left || right)
        {
            if (it.Kind == TItem.LIST && it.Opts != null && it.Opts.Length > 0)
            {
                int k = it.Opts.Length;
                it.Sel = left ? (it.Sel + k - 1) % k : (it.Sel + 1) % k;
                OnChanged(it);
                Beep();
            }
            else if (it.Kind == TItem.NUMBER)
            {
                it.Val = left ? it.Val - it.Step : it.Val + it.Step;
                if (it.Val < it.Min) it.Val = it.Min;
                if (it.Val > it.Max) it.Val = it.Max;
                OnChanged(it);
                Beep();
            }
        }

        // --- X dentro i riquadri: il secondo comando della riga ---
        // la tastiera dice "premuto" a ogni fotogramma finche' tieni giu'
        // il tasto: senza questo fermo, X armava e disarmava trenta volte
        // al secondo e finiva a caso
        bool xOra = Pressed(C_X) || Game.IsKeyPressed(Keys.X);
        bool xColpo = xOra && !xGiu;
        xGiu = xOra;
        bool yOra = Pressed(C_Y) || Game.IsKeyPressed(Keys.Y);
        bool yColpo = yOra && !yGiu;
        yGiu = yOra;
        if (xColpo || yColpo)
        {
            int campo = xColpo ? 6 : 9;
            string cx = "";
            if (m.RigSel >= 0 && m.Rig != null && m.RigSel < m.Rig.Count)
                cx = xColpo ? PanCampo(m.Rig[m.RigSel], 4) : "";
            else
            if (m.PanSelSx >= 0 && SxSiVede(m, m.PanSelSx))
                cx = PanCampo(m.PannelloSx[m.PanSelSx], campo);
            else if (m.PanSel >= 0 && DxSiVede(m, m.PanSel))
                cx = PanCampo(m.Pannello[m.PanSel], campo);
            if (cx.Length > 0)
            {
                int kx;
                for (kx = 0; kx < modSubMenu.Count; kx++)
                {
                    if (modSubMenu[kx] != cur) continue;
                    ComandoMod(modSubDir[kx], cx);
                    break;
                }
                return;
            }
        }

        // --- conferma ---
        if (Pressed(C_ACCEPT) || Game.IsKeyPressed(Keys.NumPad5))
        {
            // A dentro il riquadro di sinistra: il comando della riga scelta
            if (m.PanSelSx >= 0 && SxAttiva(m, m.PanSelSx))
            {
                int ks;
                for (ks = 0; ks < modSubMenu.Count; ks++)
                {
                    if (modSubMenu[ks] != cur) continue;
                    ComandoMod(modSubDir[ks], PanCampo(m.PannelloSx[m.PanSelSx], 3));
                    break;
                }
                return;
            }

            // A dentro il riquadro di destra: il comando della riga scelta
            if (m.PanSel >= 0 && PanAttiva(m, m.PanSel))
            {
                int kk;
                for (kk = 0; kk < modSubMenu.Count; kk++)
                {
                    if (modSubMenu[kk] != cur) continue;
                    ComandoMod(modSubDir[kk], PanCampo(m.Pannello[m.PanSel], 3));
                    break;
                }
                return;
            }

            if (it.Kind == TItem.SUB && it.Sub >= 0)
            {
                if (it.Id == 240)
                {
                    BuildModShop();
                }
                if (it.Id == 280)
                {
                    BuildWardrobe();
                }
                cur = it.Sub;
                menus[cur].Sel = FirstSelectable(cur);
                menus[cur].Top = 0;

                // Se e' il sottomenu di una mod, glielo diciamo: certe mod
                // devono reagire quando apri una loro pagina (la pesca, per
                // dirne una, ripone la canna quando entri nell'armatura).
                int ks;
                for (ks = 0; ks < modSubMenu.Count; ks++)
                {
                    if (modSubMenu[ks] != cur) continue;
                    ComandoMod(modSubDir[ks], "apri "
                               + Path.GetFileName(modSubFile[ks]));
                    break;
                }
            }
            else if (it.Kind == TItem.TOGGLE)
            {
                it.On = !it.On;
                OnChanged(it);
            }
            else if (it.Kind == TItem.ACTION || it.Kind == TItem.LIST)
            {
                DoAction(it);
            }
            Beep();
        }
    }

    void Beep()
    {
        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
    }

    // ============================================================
    //  disegno
    // ============================================================
    // ============================================================
    //  LA TEMPERATURA
    //  GTA una temperatura non ce l'ha: quella qui sotto ce la
    //  calcoliamo noi dall'ora, dal meteo e da quanto sei in alto.
    //  Sono numeri NOSTRI, inventati per il gioco, non presi dal wiki
    //  di Fishing Planet ne' da nessun dato reale.
    //    - la curva del giorno: minimo verso le 4 di notte, massimo
    //      verso le 16, circa sette gradi in mezzo;
    //    - il meteo: col sole si sale, con pioggia e neve si scende;
    //    - l'altitudine: sopra i cinquanta metri, mezzo grado ogni cento.
    // ============================================================
    float TemperaturaAria()
    {
        int hh = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        int mi = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
        float ora = hh + mi / 60f;

        float fase = (ora - 4f) / 24f * 6.2831853f;
        float curva = -(float)Math.Cos(fase);          // -1 alle 4, +1 alle 16
        float t = 20f + 7f * curva;

        t += GradiDelMeteo();

        try
        {
            float z = Game.Player.Character.Position.Z;
            if (z > 50f) t -= (z - 50f) * 0.006f;
        }
        catch { }

        return t;
    }

    // che tempo fa adesso, col nome che usa il gioco (EXTRASUNNY, RAIN...)
    string MeteoOra()
    {
        try
        {
            int h = Function.Call<int>(Hash.GET_PREV_WEATHER_TYPE_HASH_NAME);
            int i;
            for (i = 0; i < WEATHER_ID.Length; i++)
                if (Game.GenerateHash(WEATHER_ID[i]) == h) return WEATHER_ID[i];
        }
        catch { }
        return "CLEAR";
    }

    float GradiDelMeteo()
    {
        string m = MeteoOra();
        if (m == "EXTRASUNNY") return 4f;
        if (m == "CLEAR") return 2f;
        if (m == "CLEARING") return 1f;
        if (m == "SMOG") return 1f;
        if (m == "CLOUDS") return 0f;
        if (m == "OVERCAST") return -1f;
        if (m == "FOGGY") return -2f;
        if (m == "RAIN") return -4f;
        if (m == "THUNDER") return -5f;
        if (m == "SNOWLIGHT") return -10f;
        if (m == "SNOW") return -12f;
        if (m == "XMAS") return -12f;
        if (m == "BLIZZARD") return -15f;
        return 0f;
    }

    // le iconine bianche sono quelle di Fishing Planet, in scripts\\Trainer\\icone
    string IconaMeteo()
    {
        int hh = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        bool notte = (hh < 6 || hh >= 20);
        string m = MeteoOra();
        string n;

        if (m == "EXTRASUNNY" || m == "CLEAR" || m == "CLEARING")
            n = notte ? "night_clear_moon" : "sunny";
        else if (m == "CLOUDS" || m == "SMOG")
            n = notte ? "night_pcloudy" : "pcloudy";
        else if (m == "OVERCAST" || m == "FOGGY")
            n = notte ? "night_cloudy" : "cloudy";
        else if (m == "RAIN" || m == "THUNDER")
            n = notte ? "night_rainy" : "rainy";
        else if (m == "SNOW" || m == "SNOWLIGHT" || m == "BLIZZARD" || m == "XMAS")
            n = notte ? "night_snowy" : "snowy";
        else
            n = notte ? "night_clear" : "sunny";

        return "meteo_" + n + ".png";
    }

    // UNA RIGA DELLA MOD NELL'HEADER.
    // Alla mod basta scrivere header.txt nella sua cartella: quando sei
    // dentro un suo menu, quel testo compare in alto dopo la temperatura.
    // La pesca ci mette il livello e i punti esperienza, cosi' li vedi
    // anche quando non stai pescando.
    string modHeadDir = "";
    string modHeadTxt = "";
    DateTime modHeadStamp = DateTime.MinValue;

    string TestaDellaMod()
    {
        string dir = "";
        int i;
        for (i = 0; i < modSubMenu.Count; i++)
            if (modSubMenu[i] == cur) { dir = modSubDir[i]; break; }
        // QUESTA E' LA MOD DELLA PESCA: livello e XP si vedono SEMPRE.
        // Nel trainer normale la riga della mod compare solo quando sei
        // dentro una sua pagina, e ha senso: le mod sono tante. Qui ce
        // n'e' una sola, quindi la sua riga sta in alto e basta.
        if (dir.Length == 0) dir = dirPesca;
        if (dir.Length == 0) { modHeadDir = ""; return ""; }
        try
        {
            string f = Path.Combine(dir, "header.txt");
            if (!File.Exists(f)) { modHeadDir = ""; return ""; }
            DateTime st = File.GetLastWriteTimeUtc(f);
            if (dir != modHeadDir || st != modHeadStamp)
            {
                modHeadDir = dir;
                modHeadStamp = st;
                modHeadTxt = File.ReadAllText(f).Trim();
                if (modHeadTxt.Length > 40) modHeadTxt = modHeadTxt.Substring(0, 40);
            }
            return modHeadTxt;
        }
        catch { }
        return "";
    }

    // barra di stato: sempre a schermo, anche a menu chiuso
    void DrawHeader(float x, float y, float w)
    {
        DrawRect(x, y, w, HEAD_H, 0, 0, 0, 185);

        int hh = Function.Call<int>(Hash.GET_CLOCK_HOURS);
        int mm = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
        int dw = Function.Call<int>(Hash.GET_CLOCK_DAY_OF_WEEK);
        if (dw < 0 || dw > 6) dw = 0;

        // il giorno a tre lettere: lascia spazio ai punti XP
        string giorno = (lang == 1 ? DAYS_IT[dw] : DAYS_EN[dw]);
        if (giorno.Length > 3) giorno = giorno.Substring(0, 3);
        string left = hh.ToString("00") + ":" + mm.ToString("00") + "  " + giorno;
        DrawText(left, x + 9f, y + 3f, 0.25f, Color.FromArgb(255, 235, 235, 240));

        // dopo il giorno: l'icona del meteo e la temperatura
        float lw = TextWidth(left, 0.25f);
        float ix = x + 9f + lw + 14f;
        DrawIcon(IconaMeteo(), ix, y + HEAD_H * 0.5f, 14f,
                 Color.FromArgb(255, 235, 235, 240));
        string gradi = ((int)Math.Round(TemperaturaAria())).ToString() + "\u00B0C";
        DrawText(gradi, ix + 11f, y + 3f, 0.25f, Color.FromArgb(255, 245, 205, 80));

        // e dopo la temperatura, la riga della mod che hai aperto
        // "Liv. 2   1060 XP": il livello in giallo come la temperatura,
        // gli XP in blu. Si spezza sui tre spazi.
        string suo = TestaDellaMod();
        if (suo.Length > 0)
        {
            float sx = ix + 11f + TextWidth(gradi, 0.25f) + 14f;
            int sp3 = suo.IndexOf("   ");
            if (sp3 > 0)
            {
                string liv = suo.Substring(0, sp3);
                string xp = suo.Substring(sp3 + 3);
                // prima gli XP (blu), poi il livello (giallo) alla loro destra
                DrawText(xp, sx, y + 3f, 0.25f, Color.FromArgb(255, 130, 200, 245));
                DrawText(liv, sx + TextWidth(xp + "   ", 0.25f), y + 3f,
                         0.25f, Color.FromArgb(255, 245, 205, 80));
            }
            else
                DrawText(suo, sx, y + 3f, 0.25f, Color.FromArgb(255, 130, 200, 245));
        }

        DrawTextRight("$" + Game.Player.Money.ToString("N0", CultureInfo.InvariantCulture),
                      x + w - 9f, y + 3f, 0.25f, Color.FromArgb(255, 130, 225, 180));
    }

    void DrawBar(float px, float pw, float y, float lH, string label, float pct,
                 int gr, int gg, int gb)
    {
        DrawBar(px, pw, y, lH, label, pct, gr, gg, gb, Color.FromArgb(255, 205, 205, 220));
    }

    void DrawBar(float px, float pw, float y, float lH, string label, float pct,
                 int gr, int gg, int gb, Color labCol)
    {
        DrawRect(px, y, pw, lH, 0, 0, 0, 150);

        float f01 = pct / 100f;
        if (f01 < 0f) f01 = 0f;
        if (f01 > 1f) f01 = 1f;

        int br = gr, bg = gg, bb = gb;
        if (f01 < 0.2f) { br = 245; bg = 145; bb = 165; }  // rosa tenue di allarme

        float barX = px + 56f;
        float barW = pw - 90f;
        float barY = y + (lH - 4f) * 0.5f;

        DrawRect(barX, barY, barW, 4f, 255, 255, 255, 30);              // binario
        DrawRect(barX, barY, barW * f01, 4f, br, bg, bb, 245);          // riempimento
        DrawRect(barX, barY + 4f, barW * f01, 1f, br, bg, bb, 90);      // riflesso sotto

        DrawText(label, px + 6f, y + 0.5f, 0.20f, labCol);
        DrawTextRight(((int)pct) + "%", px + pw - 5f, y + 0.5f, 0.20f, Color.FromArgb(255, br, bg, bb));
    }

    // ============================================================
    //  CRUSCOTTO SEMPLICE
    //  Niente texture: solo rettangoli e testo, quindi funziona appena
    //  installi lo script. Velocita', marcia, carburante con autonomia,
    //  odometro e le spie che contano.
    // ============================================================
    void DashSemplice(Vehicle v)
    {
        // Il tipo di mezzo e i suoi valori si rileggono SUBITO dal veicolo su
        // cui sei: appena cambi auto le spie devono cambiare con te, non un
        // istante dopo con quelle del mezzo di prima.
        evCurrent = IsElectric(v);

        string kDash = TankKeyOf(v);
        if (kDash != curTankKey) fuel = GetTank(kDash);
        if (kDash != curBattKey) batt = GetBatt(kDash);

        float w = 250f;
        float h = 74f;          // 46 cruscotto + 14 radio + 14 messaggi
        float x = 640f - w * 0.5f;      // centrato in basso
        float y = 720f - h - 8f;

        bool mi = UseMiles();
        float spd = Function.Call<float>(Hash.GET_ENTITY_SPEED, v);
        int vel = (int)(spd * (mi ? 2.23694f : 3.6f));

        Color bianco = Color.FromArgb(255, 240, 240, 245);
        Color grigio = Color.FromArgb(255, 160, 163, 172);

        // lo sfondo resta sempre pulito
        DrawRect(x, y, w, h, 0, 0, 0, 150);

        // ---- velocita': e' il numero a diventare rosso se superi il limite ----
        int limite = SpeedLimitNow();
        int kmhReale = (int)(spd * 3.6f);

        Color colVel = bianco;
        if (kmhReale > limite + SPEED_MARGIN) colVel = Color.FromArgb(255, 245, 70, 70);
        else if (kmhReale > limite) colVel = Color.FromArgb(255, 250, 165, 90);

        DrawText(vel.ToString(), x + 10f, y + 3f, 0.57f, colVel);
        DrawText(mi ? "mph" : "km/h", x + 10f, y + 31f, 0.20f, grigio);

        // ---- cartello del limite, fra velocita' e marcia ----
        {
            float lx = x + 78f;          // centro del cartello
            float ly = y + 6f;

            // lampeggia insieme al bip: stesso mezzo secondo di ritmo
            bool bip = (overSince >= 0f
                        && Game.GameTime - overSince >= (OVER_SECONDS * 1000) / 2);
            bool acceso = bip && ((Game.GameTime % 1200) < 600);

            // quando suona il bip il cartello lampeggia: appare e sparisce,
            // niente fondo rosso, il suono basta a capire
            if (!bip || acceso)
            {
                DrawTextCenter(limite.ToString(), lx, ly + 1f, 0.34f, bianco);
                DrawTextCenter(L("LIMIT", "LIMITE"), lx, ly + 19f, 0.15f, grigio);
            }
        }

        // ---- marcia, con quella prima e quella dopo ai lati ----
        {
            int hi = v.HighGear;
            int gr = v.CurrentGear;
            bool auto2 = (hi <= 1) || evCurrent;

            string marcia;
            if (auto2)
            {
                if (gr <= 0) marcia = "R";
                else if (Function.Call<bool>(Hash.GET_IS_VEHICLE_ENGINE_RUNNING, v)) marcia = "D";
                else marcia = "P";
            }
            else marcia = (gr <= 0) ? "R" : gr.ToString();

            string gL = "";
            string gR = "";
            if (auto2)
            {
                gL = "N";
                gR = (marcia == "D") ? "P" : "D";
            }
            else
            {
                gL = (gr <= 1) ? "N" : (gr - 1).ToString();
                if (gr <= 0) gR = "1";
                else if (gr + 1 <= hi) gR = (gr + 1).ToString();
            }

            float gx = x + 122f;
            float gy = y + 8f;

            // la folle rossa, la retro ocra, il resto bianco
            Color cLato = Color.FromArgb(128, 255, 255, 255);
            Color cRossa = Color.FromArgb(128, 255, 90, 90);
            Color cOcra = Color.FromArgb(128, 220, 170, 60);

            if (gL.Length > 0)
                DrawTextCenter(gL, gx - 13f, gy + 6f, 0.23f,
                               (gL == "N") ? cRossa : ((gL == "R") ? cOcra : cLato));

            if (gR.Length > 0)
                DrawTextCenter(gR, gx + 13f, gy + 6f, 0.23f,
                               (gR == "N") ? cRossa : ((gR == "R") ? cOcra : cLato));

            Color cCentro = bianco;
            if (marcia == "N") cCentro = Color.FromArgb(255, 255, 90, 90);
            else if (marcia == "R") cCentro = Color.FromArgb(255, 220, 170, 60);

            DrawTextCenter(marcia, gx, gy, 0.45f, cCentro);
        }

        // ---- carburante, energia o batteria dell'ibrida ----
        bool ibrida = IsHybrid(v) && !evCurrent;

        // ibrida: 0 eco (batteria), 1 ibrido (media dei due), 2 benzina
        bool ibridaEco = ibrida && ibridaFascia == 0;
        bool ibridaMix = ibrida && ibridaFascia == 1;

        float pct;
        if (ibridaEco) pct = batt;
        else if (ibridaMix) pct = (batt + fuel) * 0.5f;
        else pct = fuel;
        if (pct < 0f) pct = 0f;
        if (pct > 100f) pct = 100f;

        float autKm;
        if (evCurrent) autKm = pct / 100f * 120f;
        else if (IsHybrid(v)) autKm = pct / 100f * 95f;
        else autKm = pct / 100f * (ECamionGrosso(v) ? 140f : (ESuper(v) ? 35f : 70f));

        Color colE;
        if (evCurrent || ibridaEco) colE = Color.FromArgb(255, 90, 220, 120);
        else if (ibridaMix) colE = Color.FromArgb(255, 205, 140, 255);   // viola
        else colE = Color.FromArgb(255, 255, 165, 60);
        if (pct < 15f) colE = Color.FromArgb(255, 245, 90, 90);

        float bx = x + 146f;
        float bw2 = 94f;

        if (ibrida)
        {
            // ibrida: in alto a sinistra i km di autonomia, a destra le tre
            // percentuali sempre in vista - batteria, media, benzina
            string modoSu;
            if (ibridaEco) modoSu = L("BATTERY", "BATTERIA");
            else if (ibridaMix) modoSu = L("HYBRID", "IBRIDO");
            else modoSu = L("FUEL", "BENZINA");
            // solo la percentuale della modalita' in corso: piu' pulito
            DrawText(modoSu, bx, y + 4f, 0.18f, grigio);
            DrawTextRight(((int)pct) + "%", x + w - 10f, y + 4f, 0.18f, colE);
        }
        else
        {
            DrawText(evCurrent ? L("BATTERY", "BATTERIA") : L("FUEL", "BENZINA"),
                     bx, y + 4f, 0.18f, grigio);
            DrawTextRight(((int)pct) + "%", x + w - 10f, y + 4f, 0.18f, colE);
        }

        DrawRect(bx, y + 17f, bw2, 5f, 255, 255, 255, 40);
        DrawRect(bx, y + 17f, bw2 * (pct / 100f), 5f,
                 colE.R, colE.G, colE.B, 235);

        if (ibrida)
        {
            // autonomia della modalita' in cui stai andando:
            //  ECO      quanto fai con la sola batteria
            //  IBRIDO   consumi dimezzati, quindi il doppio per entrambi,
            //           e vale il piu' corto dei due
            //  BENZINA  quanto fai col serbatoio
            float kmBatt = batt / 100f * HYB_KM_EV;
            float kmFuel = fuel / 100f * 95f;

            float autoIb;
            if (ibridaEco) autoIb = kmBatt;
            else if (ibridaMix)
            {
                float a = kmBatt * 2f;
                float b = kmFuel * 2f;
                autoIb = (a < b) ? a : b;
            }
            else autoIb = kmFuel;

            float autoIbM = mi ? autoIb * 0.621371f : autoIb;

            // sotto i dieci chilometri si mostra un decimale, se no in eco
            // leggeresti sempre 1 km o 0 km
            string txtAut = (autoIbM < 10f)
                ? autoIbM.ToString("0.0", CultureInfo.InvariantCulture)
                : ((int)autoIbM).ToString(CultureInfo.InvariantCulture);

            DrawText(txtAut + (mi ? " mi" : " km"), bx, y + 25f, 0.17f, colE);
        }
        else
        {
            float aut = mi ? autKm * 0.621371f : autKm;
            DrawText(((int)aut) + (mi ? " mi" : " km"), bx, y + 25f, 0.17f, colE);
        }

        // ---- odometro: sei cifre con gli zeri davanti, poi i metri in rosso ----
        float tot = mi ? (odoM / 1000f) * 0.621371f : (odoM / 1000f);
        if (tot < 0f) tot = 0f;

        int interi = (int)tot;
        int dec = (int)((tot - interi) * 10f);
        if (dec < 0) dec = 0;
        if (dec > 9) dec = 9;

        string cifre = interi.ToString("000000", CultureInfo.InvariantCulture);
        string unita = mi ? " mi" : " km";
        string metri = dec.ToString(CultureInfo.InvariantCulture);

        float odoS = 0.17f;
        float odoY = y + 25f;
        float odoR = x + w - 10f;                       // bordo destro

        float wU = TextWidth(unita, odoS);
        float wM = TextWidth(metri, odoS);
        float wC = TextWidth(cifre, odoS);

        DrawText(cifre, odoR - wU - wM - wC, odoY, odoS, grigio);
        DrawText(metri, odoR - wU - wM, odoY, odoS, Color.FromArgb(255, 255, 90, 90));
        DrawText(unita, odoR - wU, odoY, odoS, grigio);

        // ---- riga delle icone, sopra il pannello ----
        // frecce agli estremi, poi le spie una dopo l'altra da sinistra
        bool fSx, fDx;
        GestisciFrecce(v, out fSx, out fDx);

        float faseFr = frecceFase ? 1f : 0f;
        float isz = 13f;                    // lato delle icone
        float ih = 17f;                     // altezza della barra delle spie
        float iy = y - ih * 0.5f;           // centro della riga

        // barra scura dietro le spie, larga come il pannello
        DrawRect(x, y - ih, w, ih, 0, 0, 0, 150);

        // Le spie si mettono prima in fila in una lista, poi si distribuiscono
        // tutte alla stessa distanza fra le due frecce: cosi' restano
        // equidistanti anche quando cambia il numero di icone.
        List<string> spie = new List<string>();
        List<float> spieSz = new List<float>();   // dimensione di ognuna

        // la freccia sinistra e' la prima icona della fila
        spie.Add((fSx && frecceFase) ? "freccia_sx.png" : "freccia_sx_off.png");
        spieSz.Add(isz);

        spie.Add(v.AreLightsOn || v.AreHighBeamsOn ? "fari_on.png" : "fari_off.png");
        spieSz.Add(isz);
        spie.Add(v.AreHighBeamsOn ? "abb_on.png" : "abb_off.png");
        spieSz.Add(isz);
        spie.Add((tLimiter != null && tLimiter.On) ? "limiter_on.png" : "limiter_off.png");
        spieSz.Add(isz);

        // gomme
        bool gomma = false;
        int[] ruote = new int[] { 0, 1, 4, 5 };
        int wi;
        for (wi = 0; wi < ruote.Length; wi++)
        {
            if (Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST, v, ruote[wi], false)
                || Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST, v, ruote[wi], true))
            { gomma = true; break; }
        }
        spie.Add(gomma ? "tyres_on.png" : "tyres_off.png");
        spieSz.Add(isz);

        // porte
        string[] sigleIt = new string[] { "AS", "AD", "PS", "PD", "COF", "BAU" };
        string[] sigleEn = new string[] { "FL", "FR", "RL", "RR", "HD", "TR" };

        string porte = "";
        int di;
        for (di = 0; di < 6; di++)
        {
            if (Function.Call<float>(Hash.GET_VEHICLE_DOOR_ANGLE_RATIO, v, di) <= 0.02f) continue;
            if (porte.Length > 0) porte = porte + " ";
            porte = porte + (lang == 1 ? sigleIt[di] : sigleEn[di]);
        }
        bool moto = EMoto(v);
        bool porta = (porte.Length > 0) && !moto;
        if (!moto)
        {
            spie.Add(porta ? "doors_open.png" : "doors_closed.png");
            spieSz.Add(isz + 3f);
        }

        // avviso a ciclo finche' non chiudi
        if (porta)
        {
            if (Game.GameTime - porteAt > 2500)
            {
                porteAt = Game.GameTime;
                StopPorte();
                porteSnd = Function.Call<int>(Hash.GET_SOUND_ID);
                Function.Call(Hash.PLAY_SOUND_FRONTEND, porteSnd,
                              "CHALLENGE_UNLOCKED", "HUD_AWARDS", true);
            }
        }
        else
        {
            porteAt = 0;
            StopPorte();
        }

        float bodyPct = Function.Call<float>(Hash.GET_VEHICLE_BODY_HEALTH, v) / 10f;
        float engPct = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, v) / 10f;
        bool chiave = (oil < 15f) || (bodyPct < 85f) || (engPct < 85f);
        bool motoreGira = Function.Call<bool>(Hash.GET_IS_VEHICLE_ENGINE_RUNNING, v);

        if (evCurrent) { spie.Add("elettric_on.png"); spieSz.Add(isz + 3f); }
        else if (ibrida)
        {
            spie.Add(ibridaEco ? "eco_on.png" : "eco_off.png");
            spieSz.Add(isz + 3f);
        }

        if (evCurrent || ibrida)
        {
            float carica = evCurrent ? fuel : batt;
            spie.Add(carica < 15f ? "energia_on.png" : "energia_off.png");
            spieSz.Add(isz);
        }

        if (!evCurrent)
        {
            spie.Add(fuel < 15f ? "benzina_on.png" : "benzina_off.png");
            spieSz.Add(isz - 3f);

            spie.Add((!motoreGira || engPct < 70f) ? "motore_on.png" : "motore_off.png");
            spieSz.Add(isz);

            spie.Add(motoreGira ? "batteria_off.png" : "batteria_on.png");
            spieSz.Add(isz);
        }

        spie.Add(chiave ? "wrench.png" : "wrench_off.png");
        spieSz.Add(isz - 3f);

        // e la freccia destra e' l'ultima
        spie.Add((fDx && frecceFase) ? "freccia_dx.png" : "freccia_dx_off.png");
        spieSz.Add(isz);

        // distribuzione a passo uguale fra le due frecce
        float spDa = x + 9f;
        float spA = x + w - 9f;
        int spN = spie.Count;
        float spPasso = (spN > 1) ? (spA - spDa) / (spN - 1) : 0f;

        int si2;
        for (si2 = 0; si2 < spN; si2++)
        {
            DashIcona(spie[si2], (spN > 1) ? (spDa + spPasso * si2) : (x + w * 0.5f),
                      iy, spieSz[si2]);
        }

        // lo stato del motore serve alla striscia in basso
        bool motoreAcceso = Function.Call<bool>(Hash.GET_IS_VEHICLE_ENGINE_RUNNING, v);
        if (!motoreAcceso) ibridaTermico = false;

        // ---- striscia della radio ----
        {
            float ry2 = y + 46f;
            DrawRect(x, ry2, w, 14f, 0, 0, 0, 60);

            string rid = Function.Call<string>(Hash.GET_PLAYER_RADIO_STATION_NAME);
            bool rOn = (rid != null && rid.Length > 0 && rid != "OFF");

            if (rOn)
                DrawTextCenter(RadioLabel(rid), x + w * 0.5f, ry2 + 2f, 0.19f, PastelFor(rid));
            else
                DrawTextCenter(L("radio off", "radio spenta"), x + w * 0.5f, ry2 + 2f, 0.19f,
                               Color.FromArgb(140, 255, 255, 255));
        }

        // ---- striscia dei messaggi: si alternano ogni dieci secondi ----
        {
            float ty = y + 60f;
            DrawRect(x, ty, w, 14f, 0, 0, 0, 60);

            List<string> msg = new List<string>();
            List<Color> col = new List<Color>();

            float km = ServiceKmLeft(v, evCurrent);
            if (km <= 0f)
            {
                msg.Add(L("MAINTENANCE DUE", "MANUTENZIONE SCADUTA"));
                col.Add(Color.FromArgb(255, 245, 90, 90));
            }
            else
            {
                msg.Add(L("MAINTENANCE IN ", "MANUTENZIONE FRA ") + ((int)km) + " km");
                col.Add(Color.FromArgb(255, 250, 210, 90));
            }

            if (!Function.Call<bool>(Hash.GET_IS_VEHICLE_ENGINE_RUNNING, v))
            {
                msg.Add(L("ENGINE OFF", "MOTORE SPENTO"));
                col.Add(Color.FromArgb(255, 150, 153, 160));
            }
            else
            {
                int ep = (int)engPct;
                if (ep < 0) ep = 0;
                if (ep > 100) ep = 100;
                msg.Add((evCurrent ? L("E-MOTOR ", "MOTORE ELETTRICO ")
                                   : L("ENGINE ", "MOTORE ")) + ep + "%");
                // verde finche' sta bene, arancione se danneggiato, rosso se messo male
                if (ep >= 85) col.Add(Color.FromArgb(255, 130, 225, 180));
                else if (ep >= 50) col.Add(Color.FromArgb(255, 250, 165, 90));
                else col.Add(Color.FromArgb(255, 245, 90, 90));
            }

            // dieci secondi a messaggio, con un secondo di dissolvenza
            int ms = Game.GameTime % (msg.Count * 10000);
            int idx = ms / 10000;
            int dentro = ms % 10000;

            float a = 1f;
            if (dentro < 500) a = dentro / 500f;
            else if (dentro > 9500) a = (10000 - dentro) / 500f;

            // a sinistra il nome del modello (fisso, grigio), poi il messaggio
            string modello = Game.GetLocalizedString(v.DisplayName);
            if (modello == null || modello.Length == 0 || modello == "NULL") modello = v.DisplayName;
            modello = modello.ToUpper();
            DrawText(modello, x + 8f, ty + 2f, 0.19f, Color.FromArgb(255, 150, 153, 160));
            float wMod = TextWidth(modello, 0.19f);

            Color cc = col[idx];
            DrawText(msg[idx], x + 8f + wMod + 8f, ty + 2f, 0.19f,
                     Color.FromArgb((int)(255 * a), cc.R, cc.G, cc.B));
        }

    }

    // ============================================================
    //  FRECCE
    //  Le comanda la mod: quattro frecce in retromarcia, da fermo col
    //  motore acceso da mezzo minuto o con la sirena; altrimenti freccia
    //  automatica quando sterzi. Restituisce lo stato acceso/spento del
    //  lampeggio, cosi' lo puo' disegnare qualunque cruscotto.
    // ============================================================
    void GestisciFrecce(Vehicle v, out bool sxOn, out bool dxOn)
    {
        sxOn = false;
        dxOn = false;
        if (v == null || !v.Exists()) { StopTic(); return; }

        int nowF = Game.GameTime;
        bool motoreOn = Function.Call<bool>(Hash.GET_IS_VEHICLE_ENGINE_RUNNING, v);
        float spdF = Function.Call<float>(Hash.GET_ENTITY_SPEED, v);

        // La retromarcia NON si legge da CurrentGear: dopo l'aggiornamento del
        // gioco quel valore ogni tanto e' sballato e faceva accendere e
        // spegnere le quattro frecce da un frame all'altro. Si guarda invece
        // se l'auto si sta muovendo all'indietro davvero.
        Vector3 vel = v.Velocity;
        Vector3 fwd = v.ForwardVector;
        float avanti = vel.X * fwd.X + vel.Y * fwd.Y;

        // appena indietreggi si accendono le quattro frecce e RESTANO finche'
        // non riparti in avanti: da fermo la manovra e' ancora in corso
        if (avanti < -0.6f) inRetro = true;
        else if (avanti > 1.5f) inRetro = false;

        bool retro = inRetro;

        if (spdF > 0.5f || !motoreOn) fermoDa = nowF;

        bool sirena = Function.Call<bool>(Hash.IS_VEHICLE_SIREN_ON, v);
        bool haz = motoreOn && (retro || sirena || (nowF - fermoDa > 30000));
        // Il lampeggio lo fa il gioco: noi diciamo solo "accesa" o "spenta",
        // una volta sola quando cambia. Ripeterglielo (o accendere e spegnere
        // noi ogni mezzo secondo) gli faceva ripartire il ciclo da capo, ed
        // era quello lo scatto irregolare della lampada.
        bool sxAttiva, dxAttiva;
        if (haz)
        {
            sxAttiva = true;
            dxAttiva = true;
        }
        else
        {
            float sterzo = v.SteeringAngle;   // gradi, + a sinistra
            if (sterzo > 22f) { frecciaSxFino = nowF + 2000; frecciaDxFino = 0; }
            if (sterzo < -22f) { frecciaDxFino = nowF + 2000; frecciaSxFino = 0; }

            sxAttiva = (nowF < frecciaSxFino);
            dxAttiva = (nowF < frecciaDxFino);
        }

        sxOn = sxAttiva;
        dxOn = dxAttiva;

        // Il native si chiama SOLO quando lo stato cambia. Ripeterlo a ogni
        // frame faceva sfarfallare le luci vere e le mandava fuori passo
        // rispetto ai quadrati sul cruscotto.
        int vh = v.Handle;
        if (vh != frecceVeh)
        {
            frecceVeh = vh;
            StopTic();
            frecceSxWas = -1;
            frecceDxWas = -1;
        }

        // Prima di mandare qualcosa al gioco, lo stato deve reggere un quarto
        // di secondo: cosi' un singolo frame ballerino non fa saltare la
        // lampada.
        int nsxWant = sxOn ? 1 : 0;
        int ndxWant = dxOn ? 1 : 0;

        if (nsxWant != frecceSxPend || ndxWant != frecceDxPend)
        {
            frecceSxPend = nsxWant;
            frecceDxPend = ndxWant;
            freccePendAt = nowF;
        }

        bool stabile = (nowF - freccePendAt > 250);

        int nsx = stabile ? nsxWant : frecceSxWas;
        int ndx = stabile ? ndxWant : frecceDxWas;
        if (nsx < 0) nsx = 0;
        if (ndx < 0) ndx = 0;

        bool sxAtt = (nsx == 1);
        bool dxAtt = (ndx == 1);

        // il ciclo del lampeggio nasce quando la freccia si accende
        if (sxAtt || dxAtt)
        {
            if (frecceStartAt == 0) frecceStartAt = nowF;
        }
        else frecceStartAt = 0;

        // Lampeggiamo NOI: la lampada la accendiamo e la spegniamo col nostro
        // ritmo, cosi' e' per forza in fase col quadratino del cruscotto.
        // PROVA: nessun lampeggio nostro, lampada tenuta accesa e basta.
        // Se lampeggia lo stesso, e' il gioco a farlo e il ritmo non e'
        // nostro; se resta fissa, il lampeggio era tutto nostro.
        sxOn = sxAtt;
        dxOn = dxAtt;

        bool accesaOra = false;

        // Lo stato si applica a OGNI frame. Il gioco ogni tanto si riprende
        // le luci: cosi' non ha il tempo di farlo, perche' il frame dopo
        // riscriviamo noi. Il lampeggio e' nostro, quindi non si interrompe
        // nessun ciclo del gioco.
        int csx = sxOn ? 1 : 0;
        int cdx = dxOn ? 1 : 0;

        Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, v, 1, sxOn);
        Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, v, 0, dxOn);

        frecceSxWas = csx;
        frecceDxWas = cdx;

        // il tic della freccia, una volta al secondo finche' e' inserita
        if ((sxAttiva || dxAttiva) && nowF - ticAt > 1000)
        {
            ticAt = nowF;
            accesaOra = true;
        }

        // il quadratino del cruscotto pulsa agganciato al tic: mezzo secondo
        // acceso dal momento del suono, che e' in passo con la lampada
        frecceFase = (nowF - ticAt) < 500;

        if (accesaOra)
        {
            StopTic();
            ticId = Function.Call<int>(Hash.GET_SOUND_ID);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, ticId, "HACKING_CLICK", 0, true);
        }

        // freccia spenta: si chiude anche il suono
        if (!sxAttiva && !dxAttiva) StopTic();

    }

    // messaggio dell'area di destra: allineato al bordo destro, e ogni
    // messaggio in piu' si mette sopra il precedente
    void DashMsg(float xr, ref float y, string testo, int r, int g, int b)
    {
        float w = TextWidth(testo, 0.17f) + 10f;
        DrawRect(xr - w, y, w, 11f, 0, 0, 0, 150);
        DrawText(testo, xr - w + 5f, y + 1f, 0.17f, Color.FromArgb(255, r, g, b));
        y = y - 13f;
    }

    // spia allineata a destra: si sposta verso sinistra a ogni etichetta
    void DashSpiaDx(ref float xr, float y, string testo, int r, int g, int b)
    {
        float w = TextWidth(testo, 0.17f) + 10f;
        DrawRect(xr - w, y, w, 11f, 0, 0, 0, 150);
        DrawText(testo, xr - w + 5f, y + 1f, 0.17f, Color.FromArgb(255, r, g, b));
        xr = xr - w - 4f;
    }

    // Icona PNG presa da scripts\Trainer\icone. Non serve nessun RPF:
    // il file si legge dal disco. Le PNG si disegnano sopra rettangoli e
    // testi, quindi vanno messe in uno spazio loro.
    bool iconeOk = true;

    void DashIcona(string file, float cx, float cy, float size)
    {
        if (!iconeOk) return;
        try
        {
            string path = Path.Combine(DATA_DIR, "icone\\" + file);
            if (!File.Exists(path)) return;

            CustomSprite sp = new CustomSprite(path,
                new SizeF(size, size),
                new PointF(cx - size * 0.5f, cy - size * 0.5f),
                Color.FromArgb(255, 255, 255, 255));
            sp.ScaledDraw();
        }
        catch (Exception)
        {
            iconeOk = false;
        }
    }

    // una spia del cruscotto semplice: etichetta su fondo scuro
    void DashSpia(ref float x, float y, string testo, int r, int g, int b)
    {
        float w = TextWidth(testo, 0.17f) + 10f;
        DrawRect(x, y, w, 11f, 0, 0, 0, 150);
        DrawText(testo, x + 5f, y + 1f, 0.17f, Color.FromArgb(255, r, g, b));
        x = x + w + 4f;
    }

    bool HaCruscotto(Vehicle v)
    {
        VehicleClass c = v.ClassType;
        if (c == VehicleClass.Boats) return false;
        if (c == VehicleClass.Planes) return false;
        if (c == VehicleClass.Helicopters) return false;
        if (c == VehicleClass.Trains) return false;
        if (c == VehicleClass.Cycles) return false;
        return true;
    }

    bool EMoto(Vehicle v)
    {
        return v.ClassType == VehicleClass.Motorcycles;
    }

    void DrawSpeedo()
    {

        Ped p = Game.Player.Character;
        if (p == null || !p.Exists()) return;

        Vehicle v = p.CurrentVehicle;
        if (v == null || !v.Exists()) return;

        // il cruscotto e' da auto: niente su barche, aerei, elicotteri,
        // treni e biciclette. Le moto ce l'hanno (senza la spia porte).
        if (!HaCruscotto(v)) return;

        // 0 spento, 1 semplice (disegnato), 2 grafico (serve cruscotto.ytd)
        int modo = (tDash != null) ? tDash.Sel : 1;
        if (modo == 0) return;
        if (modo == 1) { DashSemplice(v); return; }

        int kmh = (int)(Function.Call<float>(Hash.GET_ENTITY_SPEED, v) * 3.6f);

        Color c = Color.FromArgb(255, 255, 255, 255);   // il numero resta sempre bianco

        // il cruscotto c'e' sempre: gli interruttori decidono solo se
        // la benzina cala davvero e se scatta la multa
        bool limitOn = true;

        float bw = 44f;
        float gap = 6f;

        float groupW = limitOn ? (bw * 2f + gap) : bw;
        float gx0 = 640f - groupW * 0.5f;

        // ============================================================
        //  CRUSCOTTO: lo sfondo e' il tuo PNG, disegnato come sprite.
        //  Proporzioni originali 1517x663, cioe' 2.288 a 1: si mantiene
        //  quel rapporto per non deformarlo.
        // ============================================================
        float cH = 150f;          // altezza voluta
        float cW = cH * 2.288f;   // larghezza di conseguenza (1517x663)
        float cX = 640f;                     // centro schermo
        float cY = 720f - cH * 0.5f - 4f;    // appoggiato al bordo basso

        // PROVA 2: si chiede la texture ma NON si disegna. Se non crasha,
        // il problema e' il disegno; se crasha, e' il caricamento.
        // Sfondo del cruscotto: texture installata nel gioco (cruscotto.ytd).
        // La disegna il gioco, quindi tutto quello che viene dopo ci va SOPRA.
        // Sfondo del cruscotto dal .ytd installato nel gioco
        // Sfondo del cruscotto: texture dentro cruscotto.ytd, installata nel
        // gioco. La disegna il gioco, quindi navigatore e numeri ci vanno
        // SOPRA. Il .ytd va creato in formato Enhanced (Resource Version 5):
        // OpenIV e CodeWalker 46 scrivono la 13 e il gioco crasha.
        Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, "cruscotto", false);
        if (Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, "cruscotto"))
        {
            string tex = v.AreLightsOn ? "cruscotto_night" : "cruscotto2";

            Function.Call(Hash.DRAW_SPRITE, "cruscotto", tex,
                          cX / 1280f, cY / 720f, cW / 1280f, cH / 720f,
                          0f, 255, 255, 255, 255, false);
        }

        // riferimenti della scocca, per posizionare tutto il resto:
        //   bordo sinistro  cX - cW/2       bordo destro  cX + cW/2
        //   bordo alto      cY - cH/2       bordo basso   cY + cH/2
        float cTop = cY - cH * 0.5f;
        float cLeft = cX - cW * 0.5f;

        // ---- spie, dalle texture dentro cruscotto.ytd ----
        // Disegnate dal gioco come lo sfondo, quindi restano sullo stesso
        // piano e l'ordine lo decide il codice.
        {
            int now2 = Game.GameTime;
            if (now2 > lightsNext)
            {
                lightsNext = now2 + 250;
                lightsLatched = v.AreLightsOn;
                beamsLatched = v.AreHighBeamsOn;
            }

            float isz = 16f;          // lato di ogni spia
            float iszP = 13f;         // benzina
            float iszF = 11f;         // fari e abbaglianti
            float iszM = 11f;         // spia motore
            float igap = 16f;         // distanza fra una spia e l'altra
            float iy = cTop + 17f;    // quanto sotto il bordo alto
            float ix = cX - igap * 2f + 3f;   // cinque spie, equidistanti

            DrawTex(lightsLatched ? "fari_on" : "fari_off", ix, iy, iszF);
            ix = ix + igap;

            DrawTex(beamsLatched ? "abb_on" : "abb_off", ix, iy, iszF);
            ix = ix + igap;

            float engH = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, v);
            float bodH = Function.Call<float>(Hash.GET_VEHICLE_BODY_HEALTH, v);
            bool mortaH = (engH <= 0f || bodH <= 0f || v.IsDead);
            bool acceso = Function.Call<bool>(Hash.GET_IS_VEHICLE_ENGINE_RUNNING, v);

            if (evCurrent)
            {
                // elettrica: accesa quando e' in funzione
                DrawTex((acceso && !mortaH) ? "elettric_on" : "elettric_off", ix, iy, isz);
            }
            else if (IsHybrid(v))
            {
                // ibrida: fino a 60 km/h va il solo elettrico (ECO verde);
                // sopra i 60 entra il termico e ci resta finche' non ti fermi
                float hyKmh = Function.Call<float>(Hash.GET_ENTITY_SPEED, v) * 3.6f;
                if (hyKmh > 60f) hyHot = true;
                else if (hyKmh <= 20f) hyHot = false;

                if (!acceso || mortaH)
                    DrawTex("eco_off", ix, iy, isz);
                else if (hyHot)
                    DrawTex("eco_off", ix, iy, isz);
                else
                    DrawTex("eco_on", ix, iy, isz);
            }
            else
            {
                // benzina: spia motore accesa a motore FERMO, guasto,
                // oppure con la salute del motore sotto il 70%
                DrawTex((!acceso || engH < 700f || mortaH) ? "motore_on" : "motore_off",
                        ix, iy, iszM);
            }
            ix = ix + igap;

            // batteria: accesa a motore spento, spenta a motore acceso
            DrawTex((!acceso || mortaH) ? "batteria_on" : "batteria_off", ix, iy, iszM);
            ix = ix + igap;

            // carburante: si accende in riserva
            bool riserva = (fuel < 15f);
            if (evCurrent)
            {
                DrawTex(riserva ? "energia_on" : "energia_off", ix, iy, iszP);
            }
            else
            {
                DrawTex(riserva ? "benzina_on" : "benzina_off", ix, iy, iszP - 4f);
            }

        }


        // ------------------------------------------------------------
        // Il vecchio contenuto (quadrati, colonnine, spie, odometro) resta
        // nelle sue funzioni ma non viene piu' disegnato: la scocca va
        // riempita da capo, elemento per elemento. Per riaccenderlo basta
        // rimettere le tre chiamate qui sotto.
        //   DrawFuelStrip(v, gx0, groupW, ry);
        //   DrawLightsIndicator(v, gx0, groupW, ry);
        // ------------------------------------------------------------

        // ---- numero velocita' a sinistra, percentuale a destra ----
        {
            Color cNum = Color.FromArgb(255, 255, 255, 255);
            int spd = UseMiles() ? (int)(kmh * 0.621371f) : kmh;
            DrawTextCenter(spd.ToString(), cX - cW * 0.28f - 3f, cY - 14f, 0.50f, cNum);

            int pct = (int)fuel;
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            DrawTextCenter(pct.ToString(), cX + cW * 0.28f + 6f, cY - 14f, 0.50f, cNum);

            // chiave inglese: tagliando da fare o carrozzeria sotto l'80%,
            // a destra del numero della velocita'
            float bodyPct = Function.Call<float>(Hash.GET_VEHICLE_BODY_HEALTH, v) / 10f;
            float engPct = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, v) / 10f;
            float tankPct = Function.Call<float>(Hash.GET_VEHICLE_PETROL_TANK_HEALTH, v) / 10f;
            bool serve = (oil < 15f) || (bodyPct < 85f)
                         || (engPct < 85f) || (tankPct < 85f);
            {
                float wX = cX + cW * 0.28f + 6f + 26f;
                float wY = cY + 0f;
                float wS = 10f;
                // quando serve il tagliando respira: si accende e si spegne
                int wA = 255;
                if (serve)
                {
                    float t = (Game.GameTime % 1600) / 1600f;
                    float k = 0.5f - 0.5f * (float)Math.Cos(t * 6.28318f);
                    wA = (int)(80f + 175f * k);
                }

                Function.Call(Hash.DRAW_SPRITE, "cruscotto",
                              serve ? "wrench" : "wrench_off",
                              wX / 1280f, wY / 720f, wS / 1280f, wS / 720f,
                              -20f, 255, 255, 255, wA, false);
            }

            // gomme: a sinistra del numero della benzina
            bool gommaKo = false;
            int[] wIdx = new int[] { 0, 1, 4, 5 };
            int wI;
            for (wI = 0; wI < wIdx.Length; wI++)
            {
                if (Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST, v, wIdx[wI], false)
                    || Function.Call<bool>(Hash.IS_VEHICLE_TYRE_BURST, v, wIdx[wI], true))
                {
                    gommaKo = true;
                    break;
                }
            }
            DrawTex(gommaKo ? "tyres_on" : "tyres_off",
                    cX + cW * 0.28f + 6f - 26f, cY + 0f, 11f);

            // avviso acustico: quando una spia si accende (non mentre resta
            // accesa) suona una volta sola
            if (serve && !spiaChiaveWas) Spia();
            if (gommaKo && !spiaGommaWas) Spia();
            spiaChiaveWas = serve;
            spiaGommaWas = gommaKo;

            // frecce, ai margini della fila delle spie
            float frW = 10f;
            float frY = cTop + 16f;          // stessa altezza delle spie
            float frDx2 = 45f;               // quanto lontano dal centro

            bool frSx, frDx;
            GestisciFrecce(v, out frSx, out frDx);

            DrawTex(frSx ? "freccia_sx" : "freccia_sx_off",
                    cX - frDx2, frY, frW);
            DrawTex(frDx ? "freccia_dx" : "freccia_dx_off",
                    cX + frDx2 + 5f, frY, frW);

            // porte: sopra il numero della benzina
            bool aperta = false;
            int dI;
            for (dI = 0; dI < 6; dI++)
            {
                if (Function.Call<float>(Hash.GET_VEHICLE_DOOR_ANGLE_RATIO, v, dI) > 0.02f)
                {
                    aperta = true;
                    break;
                }
            }
            DrawTex(aperta ? "doors_open" : "doors_closed",
                    cX + cW * 0.28f + 6f, cY - 22f, 16f);

            // porta aperta: avviso che si ripete finche' non la chiudi
            if (aperta)
            {
                if (Game.GameTime - porteAt > 2500)
                {
                    porteAt = Game.GameTime;
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1,
                                  "CHALLENGE_UNLOCKED", "HUD_AWARDS", true);
                }
            }
            else
            {
                porteAt = 0;
            }

            // limitatore di velocita', sopra il numero della velocita'
            bool limAcceso = (tLimiter != null && tLimiter.On);
            DrawTex(limAcceso ? "limiter_on" : "limiter_off",
                    cX - cW * 0.28f - 3f, cY - 22f, 13f);

            // etichette piccole sotto i due numeri
            DrawTextCenter(UseMiles() ? "mph" : "km/h",
                           cX - cW * 0.28f - 3f, cY + 10f, 0.15f, cNum);
            DrawTextCenter(evCurrent ? L("energy", "energia") : L("fuel", "benzina"),
                           cX + cW * 0.28f + 6f, cY + 10f, 0.15f, cNum);

            // autonomia residua, sotto la scritta: arancione a benzina,
            // verde in elettrico
            float autKm;
            Color autCol;
            if (evCurrent)
            {
                autKm = fuel / 100f * 120f;
                autCol = Color.FromArgb(255, 90, 220, 120);
            }
            else if (IsHybrid(v))
            {
                autKm = fuel / 100f * 95f;
                autCol = Color.FromArgb(255, 255, 160, 60);
            }
            else
            {
                autKm = fuel / 100f * (ECamionGrosso(v) ? 140f : (ESuper(v) ? 35f : 70f));
                autCol = Color.FromArgb(255, 255, 160, 60);
            }

            // GUIDA DI POSIZIONAMENTO: gli archi su cui gireranno le lancette.
            // raggio e angoli si cambiano qui.
            // archi di riferimento spenti: per rivederli togli le //
            // DrawArc(cX - cW * 0.28f - 3f, cY + 2f, 40f, -130f, 130f, 100f, 2f,
            //         Color.FromArgb(255, 0, 255, 255), Color.FromArgb(90, 255, 255, 255));
            // DrawArc(cX + cW * 0.28f + 6f, cY + 2f, 40f, -130f, 130f, 100f, 2f,
            //         Color.FromArgb(255, 0, 255, 255), Color.FromArgb(90, 255, 255, 255));

            // lancette: girano da -130 a +130 gradi (0 = ore 12)
            // col cruscotto night (fari accesi) verde pastello, altrimenti bianca
            Color ndW = v.AreLightsOn
                        ? Color.FromArgb(255, 150, 230, 170)
                        : Color.FromArgb(255, 255, 255, 255);
            Color ndF = autCol;
            DrawNeedle(cX - cW * 0.28f - 3f, cY + 2f, 36f, 46f,
                       -130f + 260f * Clamp01(kmh / 285f), 1.2f, ndW);
            DrawNeedle(cX + cW * 0.28f + 6f, cY + 2f, 36f, 46f,
                       -130f + 260f * Clamp01(fuel / 100f), 1.2f, ndF);

            string autTxt = UseMiles()
                            ? (((int)(autKm * 0.621371f)) + " mi")
                            : (((int)autKm) + " km");
            DrawTextCenter(autTxt, cX + cW * 0.28f + 6f, cY + 18f, 0.17f, autCol);
        }

        // ---- odometro: sempre visibile, non dipende dal navigatore ----
        {
            int odoN = UseMiles() ? (int)(odoM / 1609.344f) : (int)(odoM / 1000f);
            if (odoN < 0) odoN = 0;
            if (odoN > 999999) odoN = 999999;
            // decimo di km (o di miglio): la cifra rossa dell'odometro
            float odoUnit = UseMiles() ? (odoM / 1609.344f) : (odoM / 1000f);
            int odoDec = (int)((odoUnit - (float)odoN) * 10f);
            if (odoDec < 0) odoDec = 0;
            if (odoDec > 9) odoDec = 9;

            string odoInt = odoN.ToString("000000", CultureInfo.InvariantCulture);
            string odoSuf = UseMiles() ? " mi" : " km";
            float odoY = cTop + 45.5f;
            float odoX = cX + 38f;
            float odoS = 0.17f;

            Color odoW = Color.FromArgb(255, 255, 255, 255);
            Color odoR = Color.FromArgb(255, 255, 90, 90);

            float wSuf = TextWidth(odoSuf, odoS);

            // sfondo scuro semitrasparente, stretto attorno al testo
            float wTot = TextWidth(odoInt + odoDec.ToString() + odoSuf, odoS);
            DrawRect(odoX - wTot - 2f, odoY + 1f, wTot + 4f, 7f, 0, 0, 0, 89);

            // le sette cifre sono un unico testo, cosi' restano attaccate;
            // l'ultima si ridisegna sopra in rosso, nello stesso punto
            // giorno della settimana in tre lettere e orario, sopra l'odometro
            int gDow = Function.Call<int>(Hash.GET_CLOCK_DAY_OF_WEEK);
            int gH = Function.Call<int>(Hash.GET_CLOCK_HOURS);
            int gM = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
            if (gDow < 0) gDow = 0;
            if (gDow > 6) gDow = 6;

            // 0 = domenica
            string[] gg = L("SUN MON TUE WED THU FRI SAT",
                            "DOM LUN MAR MER GIO VEN SAB").Split(' ');

            string dtTxt = gg[gDow] + "   "
                           + gH.ToString("00", CultureInfo.InvariantCulture) + ":"
                           + gM.ToString("00", CultureInfo.InvariantCulture);
            // sotto il cerchio della benzina, in fondo al quadrante destro
            DrawTextCenter(dtTxt, cX + cW * 0.28f + 6f, cY + 41f, odoS,
                           Color.FromArgb(204, 255, 255, 255));

            DrawTextRight(odoSuf, odoX, odoY, odoS, odoW);
            DrawTextRight(odoInt + odoDec.ToString(), odoX - wSuf, odoY, odoS, odoW);
            DrawTextRight(odoDec.ToString(), odoX - wSuf, odoY, odoS, odoR);
        }

        // ---- nome della strada, 4 px sotto il navigatore ----
        {
            Vector3 mp = p.Position;
            OutputArgument sh = new OutputArgument();
            OutputArgument ch = new OutputArgument();
            Function.Call(Hash.GET_STREET_NAME_AT_COORD, mp.X, mp.Y, mp.Z, sh, ch);
            string via = Function.Call<string>(Hash.GET_STREET_NAME_FROM_HASH_KEY,
                                               sh.GetResult<int>());
            if (via != null && via.Length > 0)
            {
                DrawTextCenter(via, cX, cTop + 90f, 0.17f,
                               Color.FromArgb(230, 255, 255, 255));
            }
        }

        // ---- radio: nome della stazione, sotto il navigatore ----
        {
            string rid = Function.Call<string>(Hash.GET_PLAYER_RADIO_STATION_NAME);
            bool rOn = (rid != null && rid.Length > 0 && rid != "OFF");

            float rdX = cX;
            float rdY = cTop + 126f;

            string rdTxt;
            Color rdCol;
            if (rOn)
            {
                rdTxt = RadioLabel(rid);
                rdCol = PastelFor(rid);
            }
            else
            {
                rdTxt = L("radio off", "radio spenta");
                rdCol = Color.FromArgb(128, 255, 255, 255);
            }
            DrawTextCenter(rdTxt, rdX, rdY, 0.20f, rdCol);
        }

        // ---- marcia ----
        {
            int gr = v.CurrentGear;
            int hi = v.HighGear;

            // un rapporto solo, o elettrica: si mostrano le lettere P R N D
            bool grAuto = (hi <= 1) || evCurrent;

            string grTxt;
            if (grAuto)
            {
                if (gr <= 0) grTxt = "R";
                else if (Function.Call<bool>(Hash.GET_IS_VEHICLE_ENGINE_RUNNING, v)) grTxt = "D";
                else grTxt = "P";
            }
            else
            {
                if (gr <= 0) grTxt = "R";
                else grTxt = gr.ToString();
            }

            float grX = cX - cW * 0.28f - 3f;
            float grY = cY + 38f;

            // ai lati la marcia sotto e quella sopra, piu' piccole
            Color grSide = Color.FromArgb(128, 255, 255, 255);

            string grL = "";
            string grR = "";
            if (grAuto)
            {
                // con la presa diretta a sinistra c'e' sempre la folle
                grL = "N";
                if (grTxt == "D") grR = "P";
                else grR = "D";
            }
            else
            {
                // sinistra: la marcia sotto; sotto la prima e in retro la folle
                if (gr <= 1) grL = "N";
                else grL = (gr - 1).ToString();

                // destra: la marcia sopra; in retro a destra c'e' la prima
                if (gr <= 0) grR = "1";
                else if (gr + 1 <= hi) grR = (gr + 1).ToString();
            }

            // la folle e' rossa, la retro giallo ocra, le marce bianche
            Color grRed = Color.FromArgb(grSide.A, 255, 90, 90);

            if (grL.Length > 0)
                DrawTextCenter(grL, grX - 11f, grY + 3f, 0.23f,
                               grL == "N" ? grRed : grSide);
            if (grR.Length > 0)
                DrawTextCenter(grR, grX + 11f, grY + 3f, 0.23f, grSide);

            // sotto la scritta km/h, nell'apertura del semicerchio
            Color grCur;
            if (grTxt == "R") grCur = Color.FromArgb(255, 220, 170, 60);
            else grCur = Color.FromArgb(255, 255, 255, 255);
            DrawTextCenter(grTxt, grX, grY, 0.35f, grCur);
        }

        // ---- limite della strada, cartello bianco ----
        {
            int lim = SpeedLimitNow();
            if (UseMiles()) lim = (int)(lim * 0.621371f);

            float sgX = cX - 24f;           // centro X del cartello
            float sgY = cTop + 45f;         // centro Y del cartello
            float sgR = 11f;                // raggio

            // da quando parte il bip (meta' del tempo prima della multa)
            // il cartello lampeggia
            bool sgBip = (overSince >= 0f &&
                          Game.GameTime - overSince >= (OVER_SECONDS * 1000) / 2);
            // lampeggio: si scambiano sfondo e testo, rosso con scritte bianche
            bool sgInv = (sgBip && (Game.GameTime % 600) < 300);

            if (sgInv)
            {
                DrawRect(sgX - sgR, sgY - sgR, sgR * 2f, sgR * 2f, 200, 30, 30, 255);
                DrawTextCenter(lim.ToString(), sgX, sgY - 11f, 0.29f,
                               Color.FromArgb(255, 255, 255, 255));
                DrawTextCenter(L("LIMIT", "LIMITE"), sgX, sgY + 2f, 0.15f,
                               Color.FromArgb(255, 255, 255, 255));
            }
            else
            {
                // sfondo trasparente, testo bianco
                DrawTextCenter(lim.ToString(), sgX, sgY - 11f, 0.29f,
                               Color.FromArgb(255, 255, 255, 255));
                DrawTextCenter(L("LIMIT", "LIMITE"), sgX, sgY + 2f, 0.15f,
                               Color.FromArgb(255, 255, 255, 255));
            }
        }

        DrawNavPanel(cX, cW, cTop);
    }

    // spia dei fari: un quadratino sotto la barra, a sinistra.
    // Lo stato viene tenuto fermo per qualche decimo di secondo perche'
    // la native lampeggia da sola e faceva sfarfallare la spia.
    bool lightsLatched = false;
    bool beamsLatched = false;
    int lightsNext = 0;

    // Fila delle spie, in alto dentro il pannello:
    //   anabbaglianti | abbaglianti | motore (o ECO) ...... olio/tagliando
    // Le icone hanno gia' la loro versione accesa e spenta: si sceglie il
    // file, non si colora niente.
    void DrawLightsIndicator(Vehicle v, float gx0, float groupW, float ry)
    {
        int now = Game.GameTime;
        if (now > lightsNext)
        {
            lightsNext = now + 250;
            lightsLatched = v.AreLightsOn;
            beamsLatched = v.AreHighBeamsOn;
        }

        float sz = 16f;
        float icy = ry - 15f;
        Color pieno = Color.FromArgb(255, 255, 255, 255);

        DrawIcon(lightsLatched ? "fari_on.png" : "fari_off.png", gx0 + 10f, icy, sz, pieno);
        DrawIcon(beamsLatched ? "abb_on.png" : "abb_off.png", gx0 + 31f, icy, sz, pieno);

        float eng = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, v);
        float bod = Function.Call<float>(Hash.GET_VEHICLE_BODY_HEALTH, v);
        bool morta = (eng <= 0f || bod <= 0f || v.IsDead);

        if (evCurrent)
        {
            // elettrica: la spia ECO e' verde finche' il mezzo e' vivo
            DrawIcon(morta ? "eco_off.png" : "eco_on.png", gx0 + 52f, icy, sz, pieno);
        }
        else
        {
            // benzina: la spia motore si accende quando il motore soffre
            bool warn = (eng < 400f || morta);
            DrawIcon(warn ? "motore_on.png" : "motore_off.png", gx0 + 52f, icy, sz, pieno);
        }

        // tagliando: la spia dell'olio, in fondo a destra
        DrawIcon((oil < 15f) ? "olio_warn.png" : "olio_on.png",
                 gx0 + groupW - 10f, icy, sz, pieno);
    }

    // avviso contromano: riquadro rosso sopra il cruscotto, con i secondi


    // Direzione del punto rispetto al muso del veicolo.
    // Niente bussole: si confronta il vettore "avanti" con quello verso la meta.
    // Punto lungo la ROTTA vera del navigatore, a tot metri da te.
    // Firma trovata a tentativi: (uscita, true, metri, false)
    // Un punto della linea GPS a 'meters' metri da te. Il primo e il terzo
    // argomento scelgono il tipo di rotta (waypoint / blip): la
    // documentazione non e' chiara, quindi si provano tutte e quattro le
    // combinazioni, tenendo a mente l'ultima che ha funzionato.
    int routeTipo = 0;
    bool RoutePoint(float meters, out Vector3 result)
    {
        bool[] p1 = new bool[] { true, false, true, false };
        bool[] p3 = new bool[] { false, false, true, true };
        int t;
        for (t = 0; t < 4; t++)
        {
            int k = (routeTipo + t) % 4;
            OutputArgument o = new OutputArgument();
            bool ok = Function.Call<bool>(Hash.GET_POS_ALONG_GPS_TYPE_ROUTE, o, p1[k], meters, p3[k]);
            if (!ok) continue;
            Vector3 r = o.GetResult<Vector3>();
            if (r.X == 0f && r.Y == 0f) continue;
            routeTipo = k;
            result = r;
            return true;
        }
        result = Vector3.Zero;
        return false;
    }

    // La prossima svolta lungo la rotta: si campiona la strada in avanti
    // ogni 20 metri fino a 500 e si cerca il primo punto dove piega.
    // Il calcolo e' pesante, quindi si aggiorna 3 volte al secondo.
    string turnCache = "";
    float turnDistCache = 0f;
    float turnClearDist = 0f;   // metri alla fine della linea (la meta)

    // per le altre mod (es. Camionista): metri di strada fino alla meta
    // secondo il navigatore, con l'ora di gioco dell'ultima lettura.
    // -1 = nessuna rotta. Si leggono via reflection dall'esterno.
    public static float RottaMetri = -1f;
    public static int RottaMetriTempo = 0;
    bool turnFine = false;      // true se la linea e' stata letta fino in fondo
    bool turnHaRotta = false;   // c'e' una linea sulla minimappa
    int turnSalti = 0;          // letture di fila molto diverse dalla precedente
    Vector3 turnMeta = Vector3.Zero;   // dove finisce la linea
    Color rottaCol = Color.FromArgb(255, 235, 235, 240);
    int rottaColNext = 0;

    // Curva agganciata: una volta vista, resta quella finche' non l'hai
    // davvero passata. Senza questo, arrivando a ridosso dell'incrocio il
    // navigatore saltava gia' alla svolta successiva e ti mandava fuori.
    bool turnLatched = false;
    Vector3 turnPos = Vector3.Zero;
    string turnDir = "";
    float turnMinDist = 99999f;
    int turnNext = 0;

    string turnPending = "";
    float turnPendingDist = 0f;
    Vector3 turnPendingPos = Vector3.Zero;
    int turnAgree = 0;

    // il nome della via in un punto, come numero: serve solo a confrontare
    int StreetAt(Vector3 pos)
    {
        OutputArgument sh = new OutputArgument();
        OutputArgument ch = new OutputArgument();
        Function.Call(Hash.GET_STREET_NAME_AT_COORD, pos.X, pos.Y, pos.Z, sh, ch);
        return sh.GetResult<int>();
    }

    void ScanTurn()
    {
        int now = Game.GameTime;
        if (now < turnNext) return;
        turnNext = now + 300;

        Ped meP = Game.Player.Character;
        Vector3 mePos = (meP != null && meP.Exists()) ? meP.Position : Vector3.Zero;

        // ---- la linea della minimappa, fino in fondo ----
        // (waypoint, missione o blip di una mod: RoutePoint prova entrambe)
        // passi da 10 m nel primo km, da 25 m oltre, fino a 10 km. Oltre la
        // meta il gioco restituisce sempre lo stesso punto: li' ci si ferma.
        const int MAXP = 700;          // 500 passi da 10 m + 200 da 25 m = 10 km
        Vector3[] pts = new Vector3[MAXP + 1];
        float[] dst = new float[MAXP + 1];
        int n = 0;
        int i;
        float along = 3f;
        bool fineRotta = false;
        for (i = 0; i <= MAXP; i++)
        {
            Vector3 q;
            if (!RoutePoint(along, out q)) { fineRotta = true; break; }
            if (n > 0)
            {
                float ddx = q.X - pts[n - 1].X;
                float ddy = q.Y - pts[n - 1].Y;
                if (ddx * ddx + ddy * ddy < 4f) { fineRotta = true; break; }
            }
            pts[n] = q;
            dst[n] = along;
            n++;
            along = along + ((along < 5000f) ? 10f : 25f);
        }

        turnHaRotta = (n > 0);
        if (n > 0) turnMeta = pts[n - 1];
        float nuovaMeta = (n > 0) ? dst[n - 1] : 0f;

        if (n == 0)
        {
            // linea non leggibile: c'e' comunque una rotta di un blip?
            bool trovata = Function.Call<bool>(Hash.GET_GPS_BLIP_ROUTE_FOUND);
            float lung = trovata ? Function.Call<float>(Hash.GET_GPS_BLIP_ROUTE_LENGTH) : 0f;
            if (trovata && lung > 0f)
            {
                turnHaRotta = true;
                turnClearDist = lung;
                turnFine = false;
                turnLatched = false;
                turnCache = "";
                turnDistCache = 0f;
                return;
            }
        }

        // Mentre il gioco ricalcola la rotta la linea per un attimo e' piu'
        // corta o diversa: una lettura che salta di oltre il 15% rispetto
        // a prima si accetta solo se la lettura dopo la conferma.
        if (turnClearDist > 0f && nuovaMeta > 0f)
        {
            float diff = nuovaMeta - turnClearDist;
            if (diff < 0f) diff = -diff;
            if (diff > turnClearDist * 0.15f && diff > 100f)
            {
                turnSalti++;
                if (turnSalti < 2) return;      // tieni i numeri di prima
            }
        }
        turnSalti = 0;
        turnClearDist = nuovaMeta;
        turnFine = fineRotta;

        // ---- curva gia' agganciata: si tiene finche' non la passi ----
        // Resta valida finche' la linea passa ancora da quel punto (se la
        // rotta e' stata ricalcolata e non ci passa piu', si molla).
        // La distanza e' misurata LUNGO la linea, non in linea d'aria.
        if (turnLatched)
        {
            Vector3 dv = turnPos - mePos;
            dv.Z = 0f;
            float dcur = dv.Length();

            if (dcur < turnMinDist) turnMinDist = dcur;

            // l'hai passata quando ti sei avvicinato e poi ti allontani
            bool passata = (turnMinDist < 28f && dcur > turnMinDist + 10f);

            int kv = -1;
            float bestD = 99999f;
            int k;
            for (k = 0; k < n; k++)
            {
                float ex = pts[k].X - turnPos.X;
                float ey = pts[k].Y - turnPos.Y;
                float e2 = ex * ex + ey * ey;
                if (e2 < bestD) { bestD = e2; kv = k; }
            }
            bool sullaLinea = (kv >= 0 && bestD < 30f * 30f);
            if (!sullaLinea) passata = true;

            // la rotta puo' essere stata ricalcolata e passare dritta da
            // quel punto: se li' la linea non piega piu', la svolta non c'e'
            if (!passata && kv >= 4 && kv + 8 < n)
            {
                float ax2 = pts[kv].X - pts[kv - 4].X;
                float ay2 = pts[kv].Y - pts[kv - 4].Y;
                float bx2 = pts[kv + 8].X - pts[kv + 4].X;
                float by2 = pts[kv + 8].Y - pts[kv + 4].Y;
                float al2 = (float)Math.Sqrt(ax2 * ax2 + ay2 * ay2);
                float bl2 = (float)Math.Sqrt(bx2 * bx2 + by2 * by2);
                if (al2 > 5f && bl2 > 5f)
                {
                    float dot2 = (ax2 / al2) * (bx2 / bl2) + (ay2 / al2) * (by2 / bl2);
                    if (dot2 > 0.85f) passata = true;    // meno di ~30 gradi: dritto
                }
            }

            if (!passata)
            {
                // distanza lungo la linea fino al punto vero della svolta,
                // non solo fino al campione piu' vicino (sarebbe a scatti di 10 m)
                float dl = dst[kv];
                if (kv > 0 && kv + 1 < n)
                {
                    float tx = pts[kv + 1].X - pts[kv - 1].X;
                    float ty = pts[kv + 1].Y - pts[kv - 1].Y;
                    float tl = (float)Math.Sqrt(tx * tx + ty * ty);
                    if (tl > 1f)
                        dl += ((turnPos.X - pts[kv].X) * tx + (turnPos.Y - pts[kv].Y) * ty) / tl;
                }
                if (dl < 0f) dl = 0f;
                turnCache = turnDir;
                turnDistCache = dl;
                return;
            }

            turnLatched = false;
            turnCache = "";
            turnDir = "";
            turnPending = "";
            turnAgree = 0;
        }

        // ---- sei girato dall'altra parte? ----
        // Prima di cercare le svolte: se la rotta parte alle tue spalle,
        // qualunque svolta piu' avanti non conta niente. Devi invertire.
        if (n >= 3 && meP != null && meP.Exists())
        {
            Entity ent = (meP.CurrentVehicle != null && meP.CurrentVehicle.Exists())
                         ? (Entity)meP.CurrentVehicle : (Entity)meP;

            Vector3 fw = ent.ForwardVector;
            Vector3 rt = pts[2] - mePos;
            fw.Z = 0f; rt.Z = 0f;

            float fl2 = fw.Length();
            float rl2 = rt.Length();

            if (fl2 > 0.01f && rl2 > 3f)
            {
                float dotf = (fw.X / fl2) * (rt.X / rl2) + (fw.Y / fl2) * (rt.Y / rl2);
                if (dotf < -0.5f)          // rotta oltre 120 gradi dal muso
                {
                    turnLatched = false;
                    turnCache = L("U-turn", "inversione");
                    turnDistCache = 0f;
                    turnPending = "";
                    turnAgree = 0;
                    return;
                }
            }
        }

        string found = "";
        float foundDist = 0f;
        Vector3 foundPos = Vector3.Zero;

        // quanto in la' si e' riusciti a guardare: se non c'e' nessuna
        // svolta, e' la lunghezza del dritto che hai davanti

        // si confrontano due tratti LUNGHI 40 metri, distanti fra loro:
        // le sterzate dentro la corsia non li muovono, una svolta si'
        if (n >= 12)
        {
            for (i = 4; i < n - 8; i++)
            {
                float ax = pts[i].X - pts[i - 4].X;
                float ay = pts[i].Y - pts[i - 4].Y;
                float bx = pts[i + 8].X - pts[i + 4].X;
                float by = pts[i + 8].Y - pts[i + 4].Y;

                float al = (float)Math.Sqrt(ax * ax + ay * ay);
                float bl = (float)Math.Sqrt(bx * bx + by * by);
                if (al < 5f || bl < 5f) continue;

                ax /= al; ay /= al;
                bx /= bl; by /= bl;

                float dot = ax * bx + ay * by;
                float cross = ax * by - ay * bx;
                float ang = (float)(Math.Atan2(cross, dot) * 180.0 / Math.PI);

                // L'angolo da solo non basta: una strada tutta curve piega
                // di brutto senza che tu debba svoltare da nessuna parte.
                // Un navigatore vero annuncia una svolta solo quando CAMBI
                // STRADA. Quindi si guarda il nome della via prima e dopo:
                // se e' lo stesso, e' solo una curva della stessa strada.
                if (ang < 150f && ang > -150f)
                {
                    if (StreetAt(pts[i - 3]) == StreetAt(pts[i + 3])) continue;
                }

                if (ang > 150f || ang < -150f) found = L("U-turn", "inversione");
                else if (ang > 45f) found = L("left", "sinistra");
                else if (ang < -45f) found = L("right", "destra");
                else continue;

                foundDist = dst[i];
                foundPos = pts[i];
                break;
            }
        }

        // stabilita': una svolta nuova deve confermarsi due volte di fila
        // prima di comparire, cosi' non lampeggia a ogni sterzata
        if (found == turnCache)
        {
            turnAgree = 0;
            if (found.Length > 0)
            {
                turnDistCache = foundDist;
                if (!turnLatched && foundPos.X != 0f)
                {
                    turnLatched = true;
                    turnPos = foundPos;
                    turnDir = found;
                    turnMinDist = 99999f;
                }
            }
            return;
        }

        if (found == turnPending)
        {
            turnAgree++;
        }
        else
        {
            turnPending = found;
            turnPendingDist = foundDist;
            turnPendingPos = foundPos;
            turnAgree = 1;
            return;
        }

        if (turnAgree >= 2)
        {
            turnCache = turnPending;
            turnDistCache = foundDist;
            turnAgree = 0;

            // da qui in poi quella curva e' "sua": la si segue col punto
            // sulla mappa, non ricalcolandola a ogni giro
            if (turnCache.Length > 0 && turnPendingPos.X != 0f)
            {
                turnLatched = true;
                turnPos = turnPendingPos;
                turnDir = turnCache;
                turnMinDist = 99999f;
            }
        }
    }

    string DirectionTo(Vector3 target)
    {
        Ped p = Game.Player.Character;
        Entity e = (p.CurrentVehicle != null && p.CurrentVehicle.Exists())
                   ? (Entity)p.CurrentVehicle : (Entity)p;

        Vector3 f = e.ForwardVector;
        Vector3 d = target - e.Position;

        float fl = (float)Math.Sqrt(f.X * f.X + f.Y * f.Y);
        float dl = (float)Math.Sqrt(d.X * d.X + d.Y * d.Y);
        if (fl < 0.001f || dl < 0.001f) return L("ahead", "dritto");

        float fx = f.X / fl, fy = f.Y / fl;
        float dx = d.X / dl, dy = d.Y / dl;

        float dot = fx * dx + fy * dy;            // 1 = davanti, -1 = dietro
        float cross = fx * dy - fy * dx;          // segno = da che lato

        float ang = (float)(Math.Atan2(cross, dot) * 180.0 / Math.PI);

        if (ang > 135f || ang < -135f) return L("U-turn", "inversione");
        if (ang > 30f) return L("left", "sinistra");
        if (ang < -30f) return L("right", "destra");
        return L("ahead", "dritto");
    }

    float navDist = 0f;
    int navDistNext = 0;

    // colore del blip -> colore del testo, come lo vedi sulla mappa
    Color BlipColour(int c)
    {
        if (c == 1) return Color.FromArgb(255, 224, 80, 80);      // rosso
        if (c == 2) return Color.FromArgb(255, 120, 215, 130);    // verde
        if (c == 3) return Color.FromArgb(255, 110, 185, 235);    // blu
        if (c == 5) return Color.FromArgb(255, 245, 215, 100);    // giallo
        if (c == 17) return Color.FromArgb(255, 245, 175, 110);   // arancio
        if (c == 27 || c == 48) return Color.FromArgb(255, 245, 130, 205); // rosa
        if (c == 83 || c == 84) return Color.FromArgb(255, 245, 130, 205);
        return Color.FromArgb(255, 235, 235, 240);
    }

    // Il colore della rotta attiva: viola se in fondo alla linea c'e' il
    // tuo waypoint, altrimenti il colore del blip che sta li' (missione,
    // fermata del bus, cliente...). Si ricontrolla ogni mezzo secondo.
    Color ColoreRotta()
    {
        int now = Game.GameTime;
        if (now < rottaColNext) return rottaCol;
        rottaColNext = now + 500;

        Color trovato = Color.FromArgb(255, 235, 235, 240);
        try
        {
            int bw = Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, 8);
            if (Function.Call<bool>(Hash.DOES_BLIP_EXIST, bw))
            {
                Vector3 c = Function.Call<Vector3>(Hash.GET_BLIP_INFO_ID_COORD, bw);
                float ddx = c.X - turnMeta.X, ddy = c.Y - turnMeta.Y;
                if (ddx * ddx + ddy * ddy < 60f * 60f)
                {
                    rottaCol = Color.FromArgb(255, 190, 110, 240);   // viola del waypoint
                    return rottaCol;
                }
            }

            Blip[] tutti = World.GetAllBlips();
            int i;
            float best = 60f * 60f;
            for (i = 0; i < tutti.Length; i++)
            {
                Blip b = tutti[i];
                if (b == null || !b.Exists()) continue;
                Vector3 c = b.Position;
                float ddx = c.X - turnMeta.X, ddy = c.Y - turnMeta.Y;
                float d2 = ddx * ddx + ddy * ddy;
                if (d2 < best)
                {
                    best = d2;
                    trovato = BlipColour(Function.Call<int>(Hash.GET_BLIP_COLOUR, b.Handle));
                }
            }
        }
        catch (Exception) { }
        rottaCol = trovato;
        return rottaCol;
    }

    // cerca un blip vero su quel punto e ne prende il colore
    Color ColourAtPoint(Vector3 p)
    {
        int[] sprites = new int[] { 280, 1, 225, 358, 361 };
        int si;
        for (si = 0; si < sprites.Length; si++)
        {
            int b = Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, sprites[si]);
            int guard = 0;
            while (Function.Call<bool>(Hash.DOES_BLIP_EXIST, b) && guard < 40)
            {
                guard++;
                Vector3 c = Function.Call<Vector3>(Hash.GET_BLIP_INFO_ID_COORD, b);
                Vector3 d = c - p;
                d.Z = 0f;
                if (d.Length() < 40f)
                {
                    return BlipColour(Function.Call<int>(Hash.GET_BLIP_COLOUR, b));
                }
                b = Function.Call<int>(Hash.GET_NEXT_BLIP_INFO_ID, sprites[si]);
            }
        }

        // nessun blip li': e' un waypoint tuo, rosa come lo disegna il gioco
        return Color.FromArgb(255, 245, 130, 205);
    }

    // Navigatore: segue il waypoint che metti tu sulla mappa.
    void DrawNavPanel(float gx0, float groupW, float ry)
    {
        Ped p = Game.Player.Character;

        bool have = false;
        Vector3 tgt = Vector3.Zero;
        Color col = Color.FromArgb(255, 235, 235, 240);

        // 2. il waypoint: l'unica rotta di cui il gioco ci dice anche le svolte
        if (!have)
        {
            int b = Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, 8);
            if (Function.Call<bool>(Hash.DOES_BLIP_EXIST, b))
            {
                have = true;
                tgt = Function.Call<Vector3>(Hash.GET_BLIP_INFO_ID_COORD, b);

                // il colore non e' fisso: e' quello del blip che sta su quel
                // punto. Giallo mentre vai a prendere, verde verso la meta,
                // rosa se e' solo un waypoint tuo.
                col = ColourAtPoint(tgt);
            }
        }

        if (!have) return;

        // testo del navigatore sempre bianco
        col = Color.FromArgb(255, 255, 255, 255);

        int now = Game.GameTime;
        if (now > navDistNext)
        {
            navDistNext = now + 500;
            Vector3 me2 = p.Position;
            navDist = Function.Call<float>(Hash.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS,
                                           me2.X, me2.Y, me2.Z, tgt.X, tgt.Y, tgt.Z);
        }

        float dm = navDist;
        if (dm <= 0f) dm = (tgt - p.Position).Length();

        string dtxt;
        if (UseMiles())
        {
            float mi = dm / 1609.344f;
            if (mi >= 1f) dtxt = mi.ToString("0.00", CultureInfo.InvariantCulture) + " mi";
            else dtxt = ((int)(dm * 1.09361f)) + " yd";
        }
        else
        {
            if (dm >= 1000f) dtxt = (dm / 1000f).ToString("0.00", CultureInfo.InvariantCulture) + " km";
            else dtxt = ((int)dm) + " m";
        }

        // Navigatore: comanda la prossima svolta. La freccia grande e i
        // metri grandi dicono quando gira; la distanza alla meta' resta
        // piccola nella riga di intestazione, che serve solo a inquadrare.
        // dentro la scocca, al centro: 'gx0' qui e' il centro X del cruscotto
        // e 'groupW' la sua larghezza (vedi la chiamata in DrawHud)
        float w = 82f;
        float x = gx0 - w * 0.5f;
        float hHead = 16f;
        float hBig = 34f;
        float y = ry + 66f;              // sotto la fila delle spie

        ScanTurn();

        // ---- riga di intestazione: la meta' ----
        DrawTextRight(dtxt, x + w - 3f, y - 6.5f, 0.21f, col);
        y = y + hHead;

        // ---- riga grande: freccia e metri alla svolta ----
        string arrow = "dritto.png";
        if (turnCache.Length > 0)
        {
            string lc = turnCache.ToLower();
            if (lc == "sinistra" || lc == "left") arrow = "sinistra.png";
            else if (lc == "destra" || lc == "right") arrow = "destra.png";
            else arrow = "inversione.png";
        }

        // (il disegno vero avviene sotto, dopo aver deciso quale freccia)

        // quanto manca alla svolta: e' questo il numero che guardi guidando
        // La svolta si annuncia solo quando sei vicino: oltre gli 80 metri
        // resta la freccia dritta, come fa il navigatore del gioco.
        // I metri sono sempre quelli della prossima svolta e scendono mentre
        // ti avvicini. La freccia della direzione compare invece solo negli
        // ultimi 80 metri: prima resta dritta, come fa il navigatore del gioco.
        bool svoltaVicina = (turnCache.Length > 0 && turnDistCache <= 80f);
        string arrow2 = svoltaVicina ? arrow : "dritto.png";

        float td = (turnCache.Length > 0) ? turnDistCache : turnClearDist;
        string tt = UseMiles()
                    ? (((int)(td * 1.09361f)) + " yd")
                    : (((int)td) + " m");
        DrawIcon(arrow2, x + 20f, y + hBig * 0.5f - 21f, 22f,
                 Color.FromArgb(255, 245, 245, 250));

        DrawTextRight(tt, x + w - 4f, y - 10f, 0.32f, col);

    }



    // Due colonnine verticali ai lati dei quadrati, alte quanto loro:
    // a sinistra il carburante (arancione) o la batteria (verde pastello),
    // a destra l'olio motore. Si riempiono dal basso come i livelli veri.
    // disegna un PNG della cartella icone, colorato come vogliamo noi.
    // Al primo errore si spengono tutte: meglio senza icone che senza gioco.
    // disegna una texture del nostro dizionario cruscotto.ytd,
    // quadrata, centrata sul punto dato (coordinate 1280x720)
    // true quando nel menu veicolo e' scelto mph
    bool UseMiles()
    {
        return (tUnits != null && tUnits.Sel == 1);
    }

    void DrawTex(string name, float cx, float cy, float size)
    {
        Function.Call(Hash.DRAW_SPRITE, "cruscotto", name,
                      cx / 1280f, cy / 720f, size / 1280f, size / 720f,
                      0f, 255, 255, 255, 255, false);
    }

    void DrawIcon(string file, float cx, float cy, float size, Color col)
    {
        DrawIcon(file, cx, cy, size, col, size, size);
    }

    void DrawIcon(string file, float cx, float cy, float size, Color col, float w, float h)
    {
        if (!iconsOk) return;

        try
        {
            string path = Path.Combine(DATA_DIR, "icone\\" + file);
            if (!File.Exists(path))
            {
                if (!iconsTried)
                {
                    iconsTried = true;
                    Notification.PostTicker("~y~" + L("Icon not found", "Icona non trovata")
                        + ":~s~ " + file, false);
                }
                return;
            }

            CustomSprite sp = new CustomSprite(path,
                new SizeF(w, h),
                new PointF(cx - w * 0.5f, cy - h * 0.5f),
                col);
            sp.Draw();
        }
        catch (Exception)
        {
            iconsOk = false;
        }
    }

    void DrawFuelStrip(Vehicle v, float gx0, float groupW, float ry)
    {
        float bh = 40f;         // stessa altezza dei quadrati
        float bwv = 6f;         // spessore della colonnina
        float top = ry - 4f;

        float f01 = fuel / 100f;
        if (f01 < 0f) f01 = 0f;
        if (f01 > 1f) f01 = 1f;

        // ---- sinistra: carburante o batteria ----
        float lxv = gx0 - bwv - 4f;

        DrawRect(lxv, top, bwv, bh, 0, 0, 0, 150);

        // icona ai piedi della colonnina: si accende quando sei in riserva
        Color pieno2 = Color.FromArgb(255, 255, 255, 255);
        if (evCurrent)
        {
            DrawIcon((f01 < 0.15f) ? "batteria_on.png" : "batteria_off.png",
                     lxv + bwv * 0.5f, top + bh + 10f, 14f, pieno2);
        }
        else
        {
            DrawIcon((f01 < 0.15f) ? "benzina_on.png" : "benzina_off.png",
                     lxv + bwv * 0.5f, top + bh + 10f, 14f, pieno2);
        }
        if (evCurrent)
        {
            DrawRect(lxv, top + bh * (1f - f01), bwv, bh * f01, 165, 225, 185, 245);
        }
        else
        {
            int r = 235, g = 165, b = 60;                        // arancione: benzina
            if (f01 < 0.12f) { r = 245; g = 120; b = 120; }      // riserva: rosso
            DrawRect(lxv, top + bh * (1f - f01), bwv, bh * f01, r, g, b, 245);
        }

        // ---- destra: olio motore ----
        // ---- odometro, sotto i quadrati ----
        // Solo i chilometri di questo veicolo, centrati. Il tagliando lo
        // ricordano le notifiche, non una scritta fissa sul cruscotto.
        // niente fondo: solo i chilometri, con il contorno per leggerli
        // anche sull'asfalto chiaro
        float oy = ry + 40f;
        int km = (int)(odoM / 1000f);
        DrawTextCenterOutline(km.ToString() + " km", gx0 + groupW * 0.5f, oy, 0.26f,
                              Color.FromArgb(255, 240, 240, 245));

        float rxv = gx0 + groupW + 4f;

        // ---- destra: la vita del veicolo ----
        // Non piu' l'olio (il tagliando lo dicono la chiave e le notifiche):
        // qui c'e' quanto e' malmesso il mezzo, carrozzeria e motore insieme.
        float eng = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, v) / 1000f;
        float bod = Function.Call<float>(Hash.GET_VEHICLE_BODY_HEALTH, v) / 1000f;
        float h01 = (eng < bod) ? eng : bod;
        if (h01 < 0f) h01 = 0f;
        if (h01 > 1f) h01 = 1f;

        DrawRect(rxv, top, bwv, bh, 0, 0, 0, 150);

        DrawIcon("cuore.png", rxv + bwv * 0.5f, top + bh + 10f, 13f,
                 (h01 < 0.2f) ? Color.FromArgb(255, 245, 120, 120)
                              : Color.FromArgb(255, 120, 190, 245));

        int hr = 90, hg = 175, hb = 245;                     // blu
        if (h01 < 0.2f) { hr = 245; hg = 120; hb = 120; }
        else if (h01 < 0.45f) { hr = 250; hg = 210; hb = 130; }
        DrawRect(rxv, top + bh * (1f - h01), bwv, bh * h01, hr, hg, hb, 245);
    }

    // ============================================================
    //  IMMAGINE VEICOLO / ARMA nel menu: sotto l'header, larga
    //  quanto il menu; se il PNG manca non occupa spazio.
    //  Veicoli: Trainer\\auto\\<modello>.png (780x440)
    //  Armi:    Trainer\\armi\\<nome>.png    (780x310)
    // ============================================================
    string pvSchedaModello = "";
    bool pvSchedaImg = false;
    string paSchedaId = "";
    bool paSchedaImg = false;
    string paSchedaFile = "";

    float PannelloVeicolo(TItem it, float y)
    {
        string modello = (it.Data != null) ? it.Data.Trim().ToLower() : "";
        if (modello.Length == 0) return 0f;
        if (modello != pvSchedaModello)
        {
            pvSchedaModello = modello;
            pvSchedaImg = false;
            try { pvSchedaImg = File.Exists(Path.Combine(DATA_DIR, "auto\\" + modello + ".png")); }
            catch { }
        }
        if (!pvSchedaImg) return 0f;

        float iw = MW - 10f;
        float ih = iw * 440f / 780f;
        float H = ih + 6f;
        DrawRect(MX, y, MW, H, 52, 56, 62, 235);
        try
        {
            CustomSprite sp = new CustomSprite(
                Path.Combine(DATA_DIR, "auto\\" + modello + ".png"),
                new SizeF(iw, ih), new PointF(MX + 5f, y + 3f));
            sp.Draw();
        }
        catch { pvSchedaImg = false; return 0f; }
        return H;
    }


    // ---- pannello laterale della scheda pesce -------------------------
    // a sinistra scorre la lista, a destra sta l'immagine con sotto
    // la scheda intera, andando a capo: niente piu' puntini
    List<string> ACapo(string t, float size, float maxW)
    {
        List<string> righe = new List<string>();
        if (t == null || t.Length == 0) return righe;
        string[] par = t.Split(' ');
        string riga = "";
        int i;
        for (i = 0; i < par.Length; i++)
        {
            string p = par[i];
            if (p.Length == 0) continue;
            string prova = (riga.Length == 0) ? p : riga + " " + p;
            if (TextWidth(prova, size) <= maxW) riga = prova;
            else
            {
                if (riga.Length > 0) righe.Add(riga);
                riga = p;
            }
        }
        if (riga.Length > 0) righe.Add(riga);
        return righe;
    }

    string pgLatFile = "";
    bool pgLatOk = false;
    int pgLatW = 0, pgLatH = 0;

    void PannelloPesce(TItem voce, float yTop)
    {
        float PW = 320f;
        float px0 = MX + MW + 6f;
        if (px0 + PW > 1280f) px0 = MX - 6f - PW;
        if (px0 < 0f) px0 = 0f;

        string file = voce.Img;
        if (file != pgLatFile)
        {
            pgLatFile = file;
            pgLatOk = false;
            pgLatW = 0; pgLatH = 0;
            try { pgLatOk = (file != null && file.Length > 0 && File.Exists(file)); } catch { }
            if (pgLatOk) MisuraPng(file, out pgLatW, out pgLatH);
        }

        float iw = PW - 10f;
        float ih = iw * 175f / 440f;

        // il testo, spezzato in righe
        string testo = (voce.Desc == null) ? "" : voce.Desc;
        string r1 = testo, rE = "", rA = "";
        int tagE = testo.IndexOf("Esche:");
        if (tagE < 0) tagE = testo.IndexOf("Baits:");
        if (tagE > 0) { r1 = testo.Substring(0, tagE).Trim(); rE = testo.Substring(tagE).Trim(); }
        int tagA = rE.IndexOf("Amo:");
        if (tagA < 0) tagA = rE.IndexOf("Hook:");
        if (tagA > 0) { rA = rE.Substring(tagA).Trim(); rE = rE.Substring(0, tagA).Trim(); }

        float tw = PW - 16f;
        List<string> le1 = ACapo(r1, 0.21f, tw);
        List<string> leE = ACapo(rE, 0.21f, tw);
        List<string> leA = ACapo(rA, 0.21f, tw);

        int nr = le1.Count + leE.Count + leA.Count;
        if (leE.Count > 0) nr += 0;
        float th = nr * 15f + 8f;
        float H = ih + 6f + th;

        DrawRect(px0, yTop, PW, ih + 6f, 52, 56, 62, 235);
        DrawRect(px0, yTop + ih + 6f, PW, th, 30, 32, 38, 235);

        if (pgLatOk)
        {
            try
            {
                float dw = iw, dh2 = ih, dx = px0 + 5f, dy = yTop + 3f;
                if (pgLatW > 0 && pgLatH > 0)
                {
                    float sx = iw / (float)pgLatW;
                    float sy = ih / (float)pgLatH;
                    float sc = (sx < sy) ? sx : sy;
                    dw = pgLatW * sc; dh2 = pgLatH * sc;
                    dx = px0 + 5f + (iw - dw) * 0.5f;
                    dy = yTop + 3f + (ih - dh2) * 0.5f;
                }
                CustomSprite sp = new CustomSprite(file, new SizeF(dw, dh2), new PointF(dx, dy));
                sp.Draw();
            }
            catch { pgLatOk = false; }
        }

        float ty = yTop + ih + 10f;
        int k;
        // prima riga: peso bianco, prezzo verde
        for (k = 0; k < le1.Count; k++)
        {
            string t2 = le1[k];
            int d = t2.IndexOf('$');
            float cx = px0 + 8f;
            if (d > 0)
            {
                string a = t2.Substring(0, d).Trim();
                string b = t2.Substring(d).Trim();
                DrawText(a, cx, ty, 0.21f, Color.FromArgb(255, 235, 238, 245));
                cx += TextWidth(a, 0.21f) + 10f;
                DrawText(b, cx, ty, 0.21f, Color.FromArgb(255, 130, 225, 180));
            }
            else if (d == 0)
                DrawText(t2, cx, ty, 0.21f, Color.FromArgb(255, 130, 225, 180));
            else
                DrawText(t2, cx, ty, 0.21f, Color.FromArgb(255, 235, 238, 245));
            ty += 15f;
        }
        // esche: la parola in rosa pesca, il resto bianco
        for (k = 0; k < leE.Count; k++)
        {
            string t2 = leE[k];
            float cx = px0 + 8f;
            if (k == 0)
            {
                int dp = t2.IndexOf(':');
                if (dp > 0)
                {
                    string et = t2.Substring(0, dp + 1);
                    DrawText(et, cx, ty, 0.21f, Color.FromArgb(255, 250, 170, 150));
                    cx += TextWidth(et, 0.21f) + 5f;
                    t2 = t2.Substring(dp + 1).Trim();
                }
            }
            DrawText(t2, cx, ty, 0.21f, Color.FromArgb(255, 225, 228, 235));
            ty += 15f;
        }
        // amo: la parola azzurrina
        for (k = 0; k < leA.Count; k++)
        {
            string t2 = leA[k];
            float cx = px0 + 8f;
            if (k == 0)
            {
                int dp = t2.IndexOf(':');
                if (dp > 0)
                {
                    string at = t2.Substring(0, dp + 1);
                    DrawText(at, cx, ty, 0.21f, Color.FromArgb(255, 130, 200, 245));
                    cx += TextWidth(at, 0.21f) + 5f;
                    t2 = t2.Substring(dp + 1).Trim();
                }
            }
            DrawText(t2, cx, ty, 0.21f, Color.FromArgb(255, 225, 228, 235));
            ty += 15f;
        }
    }

    string pgSchedaFile = "";
    bool pgSchedaOk = false;
    int pgSchedaW = 0, pgSchedaH = 0;

    // legge larghezza e altezza dall'intestazione IHDR di un PNG,
    // cosi' l'immagine si disegna nelle sue proporzioni e non stirata
    // accorcia il testo finche' non entra nella larghezza data
    string Entra(string t, float size, float maxW)
    {
        if (t == null || t.Length == 0) return "";
        if (TextWidth(t, size) <= maxW) return t;
        int n = t.Length;
        while (n > 4 && TextWidth(t.Substring(0, n) + "...", size) > maxW) n--;
        return t.Substring(0, n) + "...";
    }

    static bool MisuraPng(string file, out int w, out int h)
    {
        w = 0; h = 0;
        try
        {
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            try
            {
                byte[] b = new byte[24];
                if (fs.Read(b, 0, 24) < 24) return false;
                if (b[0] != 0x89 || b[1] != 0x50 || b[2] != 0x4E || b[3] != 0x47) return false;
                w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
            }
            finally { fs.Close(); }
            return (w > 0 && h > 0);
        }
        catch { return false; }
    }

    float PannelloImgFile(TItem voce, float y)
    {
        string file = voce.Img;
        if (file != pgSchedaFile)
        {
            pgSchedaFile = file;
            pgSchedaOk = false;
            pgSchedaW = 0; pgSchedaH = 0;
            try { pgSchedaOk = File.Exists(file); } catch { }
            if (pgSchedaOk) MisuraPng(file, out pgSchedaW, out pgSchedaH);
        }
        // DUE CASI DIVERSI.
        // Il BANNER di un torneo e' un'immagine piena, larga e bassa: va
        // da bordo a bordo, senza cornice, se no si vede il grigio attorno.
        // Il disegno di un PESCE o di un ATTREZZO e' un PNG col fondo
        // trasparente: quello lo sfondo grigio ce lo deve avere, se no
        // resta appeso sopra il gioco.
        // misura FISSA: riquadro immagine + striscia da 2 righe, sempre
        // uguali, cosi' la lista sotto non balla mai
        float iw = MW - 10f;
        float ih = iw * 175f / 440f;
        float H = ih + 6f;
        // UN BANNER RIEMPIE TUTTO, un pesce no.
        // Il banner di un torneo e' un'immagine piena, larga e bassa:
        // va da bordo a bordo, senza cornice e senza margini, se no si
        // vede il grigio attorno. Il disegno di un pesce o di un
        // attrezzo invece ha il fondo trasparente, e il grigio dietro
        // ce lo deve avere: si riconoscono dalla forma, un banner e'
        // largo piu' del doppio di quanto e' alto.
        // NON BASTA LA FORMA.
        // Le canne sono immagini lunghe e basse quanto un banner, e col
        // solo rapporto finivano disegnate a tutto campo, senza il fondo
        // scuro dietro: restavano appese sopra il gioco. Un banner e' un
        // banner per DOVE STA, non per quanto e' larga: zone e tornei.
        bool inCartellaBanner = false;
        try
        {
            string bs = (file == null) ? "" : file.ToLower();
            inCartellaBanner = bs.Contains("\\zone\\") || bs.Contains("\\tornei\\")
                            || bs.Contains("/zone/") || bs.Contains("/tornei/");
        }
        catch { }
        bool eBanner = (pgSchedaOk && pgSchedaW > 0 && pgSchedaH > 0
                        && inCartellaBanner
                        && (float)pgSchedaW / (float)pgSchedaH >= 2.2f);
        if (eBanner)
        {
            H = MW * (float)pgSchedaH / (float)pgSchedaW;
            try
            {
                CustomSprite sb = new CustomSprite(file, new SizeF(MW, H),
                                                   new PointF(MX, y));
                sb.Draw();
            }
            catch { pgSchedaOk = false; }
        }
        else
        {
            DrawRect(MX, y, MW, H, 52, 56, 62, 235);
            if (pgSchedaOk)
            {
                try
                {
                    // dentro il riquadro, ma nelle proporzioni vere del PNG
                    float dw = iw, dh2 = ih, dx = MX + 5f, dy = y + 3f;
                    if (pgSchedaW > 0 && pgSchedaH > 0)
                    {
                        float sx = iw / (float)pgSchedaW;
                        float sy = ih / (float)pgSchedaH;
                        float sc = (sx < sy) ? sx : sy;
                        dw = pgSchedaW * sc;
                        dh2 = pgSchedaH * sc;
                        dx = MX + 5f + (iw - dw) * 0.5f;
                        dy = y + 3f + (ih - dh2) * 0.5f;
                    }
                    CustomSprite sp = new CustomSprite(file, new SizeF(dw, dh2),
                                                       new PointF(dx, dy));
                    sp.Draw();
                }
                catch { pgSchedaOk = false; }
            }
        }

        // la scheda: riga 1 = livello, peso, amo e prezzo tutti insieme;
        // sotto ci vanno le esche, che vanno a capo se non ci stanno.
        string dsc = (voce.Desc == null) ? "" : voce.Desc;
        string riga1 = dsc, rEsche = "", rAmo = "";
        {
            int tagE = dsc.IndexOf("Esche:");
            if (tagE < 0) tagE = dsc.IndexOf("Baits:");
            if (tagE > 0)
            {
                riga1 = dsc.Substring(0, tagE).Trim();
                rEsche = dsc.Substring(tagE).Trim();
            }
            int tagA = rEsche.IndexOf("Amo:");
            if (tagA < 0) tagA = rEsche.IndexOf("Hook:");
            if (tagA > 0)
            {
                rAmo = rEsche.Substring(tagA).Trim();
                rEsche = rEsche.Substring(0, tagA).Trim();
            }
        }

        // l'amo esce dalla sua riga e va in cima, prima del prezzo.
        // Quello che segue l'amo (per esempio l'avviso dei denti) resta
        // testo e finisce in fondo alle esche.
        string amoVal = "", coda = "";
        if (rAmo.Length > 0)
        {
            int dp3 = rAmo.IndexOf(':');
            amoVal = (dp3 > 0) ? rAmo.Substring(dp3 + 1).Trim() : rAmo;
            int pun = amoVal.IndexOf('.');
            if (pun > 0)
            {
                coda = amoVal.Substring(pun + 1).Trim();
                amoVal = amoVal.Substring(0, pun).Trim();
            }
        }
        // "leader" da solo e' un avviso, non testo: va in prima riga, giallo
        bool vuoleLeader = false;
        if (coda.ToLower() == "leader") { vuoleLeader = true; coda = ""; }
        if (coda.Length > 0)
            rEsche = (rEsche.Length > 0) ? (rEsche + "  " + coda) : coda;

        // le esche: quante righe servono davvero (al massimo tre)
        string escEt = "", escVal = "";
        List<string> escRighe = new List<string>();
        if (rEsche.Length > 0)
        {
            int dp = rEsche.IndexOf(':');
            escEt = (dp > 0) ? rEsche.Substring(0, dp + 1) : "";
            escVal = (dp > 0) ? rEsche.Substring(dp + 1).Trim() : rEsche;
            float primaW = MW - 16f - ((escEt.Length > 0) ? TextWidth(escEt, 0.21f) + 5f : 0f);
            escRighe = ACapo(escVal, 0.21f, primaW);
            while (escRighe.Count > 3) escRighe.RemoveAt(escRighe.Count - 1);
        }

        int nRighe = 1 + escRighe.Count;
        float dh = nRighe * 15f + 6f;
        DrawRect(MX, y + H, MW, dh, 30, 32, 38, 235);
        if (dsc.Length > 0)
        {
            // riga 1: Liv. giallo, peso bianco, amo azzurrino, prezzo verde
            float px = MX + 8f, py = y + H + 2f;
            string liv = "", peso = "", prezzo = "";
            // "SEI QUI" SI STACCA E DIVENTA ROSA.
            // E' un segno, non un dato: non deve confondersi coi numeri.
            string segnaQui = "";
            {
                int sq = riga1.IndexOf("sei qui");
                if (sq < 0) sq = riga1.IndexOf("you are here");
                if (sq >= 0)
                {
                    segnaQui = riga1.Substring(sq).Trim();
                    riga1 = riga1.Substring(0, sq).Trim();
                }
            }

            // IL LIVELLO SI PESCA DOVUNQUE SIA.
            // Prima si guardava solo l'inizio: scritto in fondo finiva
            // dentro il prezzo e usciva tutto verde.
            string t2 = riga1;
            {
                string[] pz = t2.Split(' ');
                int qL;
                for (qL = 0; qL < pz.Length; qL++)
                {
                    string w4 = pz[qL].Trim();
                    if (w4.StartsWith("Liv.") || w4.StartsWith("Lv."))
                    {
                        liv = w4;
                        pz[qL] = "";
                        t2 = string.Join(" ", pz).Trim();
                        while (t2.IndexOf("  ") >= 0) t2 = t2.Replace("  ", " ");
                        break;
                    }
                }
            }
            int d = t2.IndexOf('$');
            if (d >= 0) { peso = t2.Substring(0, d).Trim(); prezzo = t2.Substring(d).Trim(); }
            else peso = t2;
            if (liv.Length > 0)
            {
                DrawText(liv, px, py, 0.21f, Color.FromArgb(255, 245, 205, 80));
                px += TextWidth(liv, 0.21f) + 10f;
            }
            if (peso.Length > 0)
            {
                DrawText(peso, px, py, 0.21f, Color.FromArgb(255, 235, 238, 245));
                px += TextWidth(peso, 0.21f) + 10f;
            }
            if (amoVal.Length > 0)
            {
                DrawText(amoVal, px, py, 0.21f, Color.FromArgb(255, 130, 200, 245));
                px += TextWidth(amoVal, 0.21f) + 10f;
            }
            if (vuoleLeader)
            {
                DrawText("leader", px, py, 0.21f, Color.FromArgb(255, 245, 205, 80));
                px += TextWidth("leader", 0.21f) + 10f;
            }
            if (prezzo.Length > 0)
            {
                DrawText(Entra(prezzo, 0.21f, MX + MW - 8f - px), px, py, 0.21f,
                         Color.FromArgb(255, 130, 225, 180));
                px += TextWidth(prezzo, 0.21f) + 10f;
            }
            // "sei qui": rosa, staccato dal resto
            if (segnaQui.Length > 0)
                DrawText(segnaQui, px + 6f, py, 0.21f,
                         Color.FromArgb(255, 250, 175, 205));

            // sotto: le esche, con l'etichetta solo sulla prima riga
            int q2;
            for (q2 = 0; q2 < escRighe.Count; q2++)
            {
                float ex = MX + 8f;
                float ey = y + H + 17f + q2 * 15f;
                if (q2 == 0 && escEt.Length > 0)
                {
                    DrawText(escEt, ex, ey, 0.21f, Color.FromArgb(255, 250, 170, 150));
                    ex += TextWidth(escEt, 0.21f) + 5f;
                }
                DrawText(escRighe[q2], ex, ey, 0.21f, Color.FromArgb(255, 225, 228, 235));
            }
        }
        return H + dh;
    }

    float PannelloArma(TItem it, float y)
    {
        string dato = (it.Data != null) ? it.Data.Trim() : "";
        if (dato.Length == 0) return 0f;
        if (dato != paSchedaId)
        {
            paSchedaId = dato;
            paSchedaImg = false;
            paSchedaFile = "";
            int wh;
            if (int.TryParse(dato, out wh))
            {
                paSchedaFile = ((WeaponHash)wh).ToString().ToLower();
                try { paSchedaImg = File.Exists(Path.Combine(DATA_DIR, "armi\\" + paSchedaFile + ".png")); }
                catch { }
            }
        }
        if (!paSchedaImg || paSchedaFile.Length == 0) return 0f;

        float iw = MW - 10f;
        float ih = iw * 310f / 780f;
        float H = ih + 6f;
        DrawRect(MX, y, MW, H, 52, 56, 62, 235);
        try
        {
            CustomSprite sp = new CustomSprite(
                Path.Combine(DATA_DIR, "armi\\" + paSchedaFile + ".png"),
                new SizeF(iw, ih), new PointF(MX + 5f, y + 3f));
            sp.Draw();
        }
        catch { paSchedaImg = false; return 0f; }
        return H;
    }

    void DrawStatusPanel()
    {
        if (open) return;   // a menu aperto sparisce, riappare alla chiusura

        bool bodyOn = (tBody != null && tBody.On);
        if (!bodyOn) return;

        Ped p = Game.Player.Character;
        if (p == null || !p.Exists()) return;

        // agganciata sotto l'header, larga quanto il menu
        float px = MX, pw = MW;
        float lH = 12f;

        bool showHead = (tTopBar == null || tTopBar.On);
        float y = MY + (showHead ? HEAD_H : 0f);

        if (bodyOn)
        {
            DrawBar(px, pw, y, lH, L("Hunger", "Fame"), hunger, 255, 190, 150);     // pesca pastello
            y = y + lH;
            DrawBar(px, pw, y, lH, L("Thirst", "Sete"), thirst, 155, 215, 240);     // azzurro pastello
            y = y + lH;
        }

    }

    void DrawMenu()
    {
        TMenu m = menus[cur];
        int n = m.Items.Count;

        bool showHead = (tTopBar == null || tTopBar.On);
        float y = MY + (showHead ? HEAD_H : 0f);

        // l'immagine del veicolo o dell'arma selezionata: sotto l'header,
        // larga quanto il menu; la lista scorre sotto di lei
        if (m.Sel >= 0 && m.Sel < n)
        {
            TItem selIt = m.Items[m.Sel];
            if (selIt.Id == 210) y += PannelloVeicolo(selIt, y);
            else if (selIt.Id == 505) y += PannelloArma(selIt, y);
            else
            {
                bool menuImg = (selIt.Img != null && selIt.Img.Length > 0);
                if (!menuImg)
                {
                    int qi;
                    for (qi = 0; qi < n; qi++)
                        if (m.Items[qi].Img != null && m.Items[qi].Img.Length > 0) { menuImg = true; break; }
                }
                // il pesce sta SOPRA la lista come tutto il resto: ora che il
                // menu e' largo 300 il testo ci sta, e il banner non balla piu'.
                // Il pannello laterale resta scritto ma non si usa piu'.
                if (m.IconRows) { }
                else if (menuImg) y += PannelloImgFile(selIt, y);
            }
        }

        // L'ARMATURA SULLO SCHERMO: quando il cursore ci arriva, ogni
        // casella prende un fondo scuro e quella scelta si accende.
        // I rettangoli li passa la mod, sono gli stessi con cui disegna.
        if (m.Rig != null && m.Rig.Count > 0 && m.RigSel >= 0)
        {
            int qr;
            for (qr = 0; qr < m.Rig.Count; qr++)
            {
                float rx = PanNum(PanCampo(m.Rig[qr], 0));
                float ry2 = PanNum(PanCampo(m.Rig[qr], 1));
                float rw = PanNum(PanCampo(m.Rig[qr], 2));
                float rh = PanNum(PanCampo(m.Rig[qr], 3));
                if (rw <= 0f || rh <= 0f) continue;
                // il margine lo decide la mod, casella per casella:
                // il primo e' quello di lato, il secondo sopra e sotto.
                // Se il secondo non c'e' vale il primo.
                string mg = PanCampo(m.Rig[qr], 5);
                string mv = PanCampo(m.Rig[qr], 6);
                float pd = (mg.Length > 0) ? PanNum(mg) : 3f;
                float pv = (mv.Length > 0) ? PanNum(mv) : pd;
                // solo quella scelta si vede: giallo fosforescente
                // trasparente. Le altre non hanno nessun fondo.
                if (qr == m.RigSel)
                    DrawRect(rx - pd, ry2 - pv, rw + pd * 2f, rh + pv * 2f,
                             225, 255, 40, 70);
            }
            // e in fondo, sotto la colonna, cosa fa il tasto
            float cy = 648f;
            // la fascia larga quanto la scritta, non quanto tutta la colonna
            string tS = "(X) SMONTA";
            float wS = TextWidth(tS, 0.19f);
            float xS = 1172f - wS * 0.5f;
            DrawRect(xS - 8f, cy, wS + 16f, 20f, 0, 0, 0, 200);
            DrawText(tS, xS, cy + 3f, 0.19f,
                     Color.FromArgb(255, 110, 175, 255));
        }

        // Il riquadro fisso a SINISTRA: stessa roba di quello a destra,
        // solo dall'altra parte della finestra. Non si scorre e non si
        // sceglie: e' un pannello che guardi.
        if (m.PannelloSx != null && m.PannelloSx.Count > 0)
        {
            float qx0 = MX - MW - 8f;
            if (qx0 < 0f) qx0 = 0f;
            float qy = y;

            string s0 = PanCampo(m.PannelloSx[0], 0);
            if (s0.StartsWith("- ")) s0 = s0.Substring(2);
            DrawRect(qx0, qy, MW, HEAD_H, 0, 0, 0, 210);
            float ws = TextWidth(s0, 0.24f);
            DrawText(s0, qx0 + (MW - ws) * 0.5f, qy + 2f, 0.24f,
                     Color.FromArgb(255, 235, 238, 245));
            qy += HEAD_H;

            // cambiata la voce in mezzo, il riquadro riparte da capo
            string chOra = SxChiave(m);
            if (m.SxVista != chOra) { m.SxVista = chOra; m.PanTopSx = 0; }

            int qs, viste = 0;
            int primoS = (m.PanTopSx < 1) ? 1 : m.PanTopSx;
            for (qs = primoS; qs < m.PannelloSx.Count && viste < MAX_VIS; qs++)
            {
                if (!SxSiVede(m, qs)) continue;
                string rs = PanCampo(m.PannelloSx[qs], 0);
                string isx = PanCampo(m.PannelloSx[qs], 1);
                string dsx = PanCampo(m.PannelloSx[qs], 2);
                bool tits = rs.StartsWith("- ");
                if (tits) rs = rs.Substring(2);
                viste++;

                bool sels = (m.PanSelSx == qs);
                float SH = (isx.Length > 0) ? 30f : ITEM_H;
                if (sels) DrawRect(qx0, qy, MW, SH, 245, 246, 250, 235);
                else DrawRect(qx0, qy, MW, SH, 0, 0, 0, tits ? 195 : 150);

                Color cs = sels ? Color.FromArgb(255, 20, 22, 26)
                                : (tits ? Color.FromArgb(255, 130, 200, 245)
                                        : Color.FromArgb(255, 235, 238, 245));

                if (isx.Length > 0)
                {
                    try
                    {
                        if (File.Exists(isx))
                        {
                            int iw, ih;
                            float bw = 40f, bh = SH - 4f;
                            float dw = bw, dh = bh;
                            if (MisuraPng(isx, out iw, out ih) && iw > 0 && ih > 0)
                            {
                                float sx1 = bw / (float)iw, sy1 = bh / (float)ih;
                                float sc1 = (sx1 < sy1) ? sx1 : sy1;
                                dw = iw * sc1; dh = ih * sc1;
                            }
                            CustomSprite sps = new CustomSprite(isx, new SizeF(dw, dh),
                                new PointF(qx0 + 4f + (bw - dw) * 0.5f,
                                           qy + 2f + (bh - dh) * 0.5f));
                            sps.Draw();
                        }
                    }
                    catch { }
                    string sts = PanCampo(m.PannelloSx[qs], 7);
                    float wss = 0f;
                    if (sts.Length > 0)
                    {
                        wss = TextWidth(sts, 0.19f) + 10f;
                        Color css = sels ? Color.FromArgb(255, 70, 74, 82)
                                         : ColoreScritto(PanCampo(m.PannelloSx[qs], 8),
                                                         Color.FromArgb(255, 150, 155, 165));
                        DrawTextRight(sts, qx0 + MW - 8f, qy + 2f, 0.19f, css);
                    }
                    DrawText(Entra(rs, 0.22f, MW - 58f - wss), qx0 + 50f, qy + 2f, 0.22f, cs);
                    string qs2 = PanCampo(m.PannelloSx[qs], 4);
                    float dx1 = qx0 + 50f;
                    float dmax1 = qx0 + MW - 8f;
                    if (qs2.Length > 0)
                    {
                        DrawText(qs2, dx1, qy + 15f, 0.20f, ColoreValore(qs2, sels));
                        dx1 += TextWidth(qs2, 0.20f) + 9f;
                    }
                    if (dsx.Length > 0)
                    {
                        string[] pz = dsx.Split(new string[] { "   " },
                                                StringSplitOptions.RemoveEmptyEntries);
                        int qz1;
                        for (qz1 = 0; qz1 < pz.Length; qz1++)
                        {
                            string p1 = pz[qz1].Trim();
                            if (p1.Length == 0) continue;
                            float w1 = TextWidth(p1, 0.20f);
                            if (dx1 + w1 > dmax1) break;
                            DrawText(p1, dx1, qy + 15f, 0.20f, ColoreValore(p1, sels));
                            dx1 += w1 + 9f;
                        }
                    }
                }
                else if (tits)
                {
                    CaptionRiquadro(qx0, qy, m.PannelloSx[qs], rs, cs);
                }
                else
                {
                    DrawText(Entra(rs, 0.22f, MW - 18f), qx0 + 9f, qy + 3f, 0.22f, cs);
                }
                qy += SH;
            }

            // il piede del riquadro
            if (m.PanSxPie.Length > 0)
            {
                string rf = PanCampo(m.PanSxPie, 0);
                if (rf.StartsWith("- ")) rf = rf.Substring(2);
                DrawRect(qx0, qy, MW, ITEM_H, 0, 0, 0, 195);
                CaptionRiquadro(qx0, qy, m.PanSxPie, rf,
                                Color.FromArgb(255, 190, 195, 205));
                qy += ITEM_H;
            }
        }

        // Il riquadro fisso a destra: stessa larghezza e stessi colori della
        // lista di sinistra, e si scorre come lei. La riga scelta si accende
        // di bianco, come nel menu.
        if (m.Pannello != null && m.Pannello.Count > 0)
        {
            float px0 = MX + MW + 8f;
            if (px0 + MW > 1280f) px0 = 1280f - MW;
            float py = y;

            // riga 0: il titolo, sempre visibile
            string t0 = PanCampo(m.Pannello[0], 0);
            if (t0.StartsWith("- ")) t0 = t0.Substring(2);
            DrawRect(px0, py, MW, HEAD_H, 0, 0, 0, 210);
            float wt = TextWidth(t0, 0.24f);
            DrawText(t0, px0 + (MW - wt) * 0.5f, py + 2f, 0.24f,
                     Color.FromArgb(255, 235, 238, 245));
            py += HEAD_H;

            string chOraD = SxChiave(m);
            if (m.DxVista != chOraD) { m.DxVista = chOraD; m.PanTop = 0; }

            int qp, mostrate = 0;
            int primo = (m.PanTop < 1) ? 1 : m.PanTop;
            for (qp = primo; qp < m.Pannello.Count && mostrate < MAX_VIS; qp++)
            {
                if (!DxSiVede(m, qp)) continue;
                string rp = PanCampo(m.Pannello[qp], 0);
                string ip = PanCampo(m.Pannello[qp], 1);
                string dp = PanCampo(m.Pannello[qp], 2);
                bool selp = (m.PanSel == qp);
                bool tit = rp.StartsWith("- ");
                if (tit) rp = rp.Substring(2);
                mostrate++;

                float RH = (ip.Length > 0) ? 30f : ITEM_H;
                if (selp) DrawRect(px0, py, MW, RH, 245, 246, 250, 235);
                else DrawRect(px0, py, MW, RH, 0, 0, 0, tit ? 195 : 150);

                Color cTesto = selp ? Color.FromArgb(255, 20, 22, 26)
                                    : (tit ? Color.FromArgb(255, 130, 200, 245)
                                           : Color.FromArgb(255, 235, 238, 245));
                Color cDett = selp ? Color.FromArgb(255, 70, 74, 82)
                                   : Color.FromArgb(255, 150, 200, 170);

                if (ip.Length > 0)
                {
                    try
                    {
                        if (File.Exists(ip))
                        {
                            int iw, ih;
                            float bw = 40f, bh = RH - 4f;
                            float dw = bw, dh = bh;
                            if (MisuraPng(ip, out iw, out ih) && iw > 0 && ih > 0)
                            {
                                float sx = bw / (float)iw, sy = bh / (float)ih;
                                float sc = (sx < sy) ? sx : sy;
                                dw = iw * sc; dh = ih * sc;
                            }
                            CustomSprite sp = new CustomSprite(ip, new SizeF(dw, dh),
                                new PointF(px0 + 4f + (bw - dw) * 0.5f,
                                           py + 2f + (bh - dh) * 0.5f));
                            sp.Draw();
                        }
                    }
                    catch { }
                    // LO STATO IN FONDO ALLA RIGA: "Armato" o "Disarmato",
                    // piccolo, col suo colore. Il nome si stringe di quanto
                    // serve per non finirci sotto.
                    string stp = PanCampo(m.Pannello[qp], 7);
                    float wsp = 0f;
                    if (stp.Length > 0)
                    {
                        wsp = TextWidth(stp, 0.19f) + 10f;
                        Color csp = selp ? Color.FromArgb(255, 70, 74, 82)
                                         : ColoreScritto(PanCampo(m.Pannello[qp], 8),
                                                         Color.FromArgb(255, 150, 155, 165));
                        DrawTextRight(stp, px0 + MW - 8f, py + 2f, 0.19f, csp);
                    }
                    DrawText(Entra(rp, 0.22f, MW - 58f - wsp), px0 + 50f, py + 2f, 0.22f, cTesto);
                    // sotto: la quantita' e poi i dati, ognuno col suo colore
                    string qp2 = PanCampo(m.Pannello[qp], 4);
                    float dx = px0 + 50f;
                    float dmax = px0 + MW - 8f;
                    if (qp2.Length > 0)
                    {
                        DrawText(qp2, dx, py + 15f, 0.20f, ColoreValore(qp2, selp));
                        dx += TextWidth(qp2, 0.20f) + 9f;
                    }
                    if (dp.Length > 0)
                    {
                        string[] pezzi = dp.Split(new string[] { "   " },
                                                  StringSplitOptions.RemoveEmptyEntries);
                        int qz;
                        for (qz = 0; qz < pezzi.Length; qz++)
                        {
                            string pz2 = pezzi[qz].Trim();
                            if (pz2.Length == 0) continue;
                            float w2 = TextWidth(pz2, 0.20f);
                            if (dx + w2 > dmax) break;
                            DrawText(pz2, dx, py + 15f, 0.20f, ColoreValore(pz2, selp));
                            dx += w2 + 9f;
                        }
                    }
                }
                else if (tit)
                {
                    CaptionRiquadro(px0, py, m.Pannello[qp], rp, cTesto);
                }
                else
                {
                    DrawText(Entra(rp, 0.22f, MW - 18f), px0 + 9f, py + 3f, 0.22f, cTesto);
                }
                py += RH;
            }

            if (m.PanPie.Length > 0)
            {
                string rf2 = PanCampo(m.PanPie, 0);
                if (rf2.StartsWith("- ")) rf2 = rf2.Substring(2);
                DrawRect(px0, py, MW, ITEM_H, 0, 0, 0, 195);
                CaptionRiquadro(px0, py, m.PanPie, rf2,
                                Color.FromArgb(255, 190, 195, 205));
                py += ITEM_H;
            }
        }

        // quando c'e' il riquadro a destra, la lista di sinistra prende il
        // suo titolo centrato, cosi' le due finestre sono pari
        if (((m.Pannello != null && m.Pannello.Count > 0)
             || (m.PannelloSx != null && m.PannelloSx.Count > 0))
            && m.Titolo != null && m.Titolo.Length > 0)
        {
            DrawRect(MX, y, MW, HEAD_H, 0, 0, 0, 210);
            float wt2 = TextWidth(m.Titolo, 0.24f);
            DrawText(m.Titolo, MX + (MW - wt2) * 0.5f, y + 2f, 0.24f,
                     Color.FromArgb(255, 235, 238, 245));
            y += HEAD_H;
        }

        // l'insegna: immagine larga quanto il menu, sopra a tutto
        if (m.Insegna != null && m.Insegna.Length > 0)
        {
            try
            {
                if (File.Exists(m.Insegna))
                {
                    int iw, ih;
                    float hh = MW * 191f / 630f;
                    if (MisuraPng(m.Insegna, out iw, out ih) && iw > 0 && ih > 0)
                        hh = MW * (float)ih / (float)iw;
                    CustomSprite ins = new CustomSprite(m.Insegna, new SizeF(MW, hh),
                        new PointF(MX, y));
                    ins.Draw();
                    y += hh;
                }
            }
            catch { }
        }

        // IL BLOCCO DI DESCRIZIONE.
        // Sotto l'insegna, un riquadro scuro con dentro il testo che va a
        // capo da solo. I soldi verdi e il livello giallo come dappertutto,
        // i titoletti che cominciano con "- " azzurrini.
        if (m.Blocco != null && m.Blocco.Count > 0)
        {
            List<string> righe = new List<string>();
            List<bool> tit = new List<bool>();
            int qb;
            for (qb = 0; qb < m.Blocco.Count; qb++)
            {
                string rb = m.Blocco[qb].Trim();
                if (rb.Length == 0) { righe.Add(""); tit.Add(false); continue; }
                bool t3 = rb.StartsWith("- ");
                if (t3)
                {
                    rb = rb.Substring(2).Trim();
                    if (righe.Count > 0) { righe.Add(""); tit.Add(false); }
                }
                // le righe coi soldi o col livello si disegnano parola per
                // parola, e parola per parola gli spazi crescono: a quelle
                // si lascia piu' margine a destra, se no escono dal riquadro
                bool spez = (rb.IndexOf('$') >= 0 || rb.IndexOf("Liv.") >= 0
                             || rb.IndexOf("Lv.") >= 0);
                List<string> sp3 = ACapo(rb, 0.21f, MW - (spez ? 46f : 30f));
                int qc;
                for (qc = 0; qc < sp3.Count; qc++) { righe.Add(sp3[qc]); tit.Add(t3); }
            }
            float hb = righe.Count * 15f + 8f;
            DrawRect(MX, y, MW, hb, 30, 32, 38, 235);
            int qd;
            for (qd = 0; qd < righe.Count; qd++)
            {
                float by = y + 4f + qd * 15f;
                if (tit[qd])
                {
                    DrawText(righe[qd], MX + 14f, by, 0.21f,
                             Color.FromArgb(255, 130, 200, 245));
                    continue;
                }
                // se non ci sono soldi ne' livelli la riga si scrive
                // intera: parola per parola gli spazi crescono e il testo
                // esce dal menu.
                if (righe[qd].IndexOf('$') < 0 && righe[qd].IndexOf("Liv.") < 0
                    && righe[qd].IndexOf("Lv.") < 0)
                {
                    DrawText(righe[qd], MX + 14f, by, 0.21f,
                             Color.FromArgb(255, 225, 228, 235));
                    continue;
                }
                // parola per parola, cosi' i soldi e il livello si vedono
                string[] par = righe[qd].Split(' ');
                float bx = MX + 14f;
                int qe;
                for (qe = 0; qe < par.Length; qe++)
                {
                    string w3 = par[qe];
                    if (w3.Length == 0) { bx += TextWidth(" ", 0.21f); continue; }
                    Color cw = Color.FromArgb(255, 225, 228, 235);
                    if (w3.StartsWith("$"))
                        cw = Color.FromArgb(255, 130, 225, 180);
                    else if (w3.StartsWith("Liv.") || w3.StartsWith("Lv."))
                        cw = Color.FromArgb(255, 245, 205, 80);
                    DrawText(w3, bx, by, 0.21f, cw);
                    bx += TextWidth(w3 + " ", 0.21f);
                }
            }
            y += hb;
        }

        // la fascia fissa: sta sopra la lista e non scorre con lei
        if (m.Nota != null && m.Nota.Length > 0)
        {
            DrawRect(MX, y, MW, 16f, 24, 26, 30, 235);
            DrawText(m.Nota, MX + 9f, y + 1f, 0.20f, Color.FromArgb(255, 130, 200, 245));
            y += 16f;
        }

        if (n == 0)
        {
            DrawRect(MX, y, MW, ITEM_H, 0, 0, 0, 150);
            DrawText(L("(empty)", "(vuoto)"), MX + 9f, y + 3f, 0.24f, Color.FromArgb(255, 170, 170, 180));
            y = y + ITEM_H;
        }

        // INTESTAZIONE APPICCICATA.
        // Se la sezione a cui appartiene la prima riga visibile e' scorsa
        // via, la si riscrive in cima: scorrendo devi sempre sapere se
        // stai guardando la roba di casa o quella che ti porti.
        if (m.Top > 0)
        {
            int hh;
            for (hh = m.Top - 1; hh >= 0; hh--)
            {
                if (m.Items[hh].Kind != TItem.HEADER) continue;
                string hs = Txt(m.Items[hh]).Trim();
                while (hs.StartsWith("-")) hs = hs.Substring(1).Trim();
                while (hs.EndsWith("-")) hs = hs.Substring(0, hs.Length - 1).Trim();
                if (hs.Length > 0)
                {
                    DrawRect(MX, y, MW, ITEM_H, 0, 0, 0, 215);
                    DrawText(hs, MX + 9f, y + 3f, 0.22f,
                             Color.FromArgb(255, m.Items[hh].Cr,
                                            m.Items[hh].Cg, m.Items[hh].Cb));
                    y = y + ITEM_H;
                }
                break;
            }
        }

        int shown = 0;
        int i;
        for (i = m.Top; i < n && shown < MAX_VIS; i++)
        {
            TItem it = m.Items[i];
            bool sel = (i == m.Sel);

            if (it.Kind == TItem.HEADER)
            {
                // solo testo maiuscolo colorato: niente barretta a sinistra
                // e niente trattini attorno alla parola
                string ht = Txt(it).Trim();
                while (ht.StartsWith("-")) ht = ht.Substring(1).Trim();
                while (ht.EndsWith("-")) ht = ht.Substring(0, ht.Length - 1).Trim();
                DrawRect(MX, y, MW, ITEM_H, 0, 0, 0, 195);
                DrawText(ht, MX + 9f, y + 3f, 0.22f, Color.FromArgb(255, it.Cr, it.Cg, it.Cb));
                y = y + ITEM_H;
                shown++;
                continue;
            }

            // riga alta con l'immagine dentro, a sinistra
            // L'ALTEZZA LA DECIDE LA RIGA, NON IL MENU.
            // In una lista mista - categorie sopra, roba con l'icona sotto -
            // una voce senza immagine deve restare bassa come in un menu
            // normale, se no dieci categorie si mangiano lo schermo.
            bool rigaIcona = m.IconRows && it.Img != null && it.Img.Length > 0;
            bool rigaSotto = rigaIcona && it.Sotto != null && it.Sotto.Length > 0;
            float RH = rigaIcona ? (rigaSotto ? 33f : 30f) : ITEM_H;

            // RIGA-INSEGNA: l'immagine larga quanto il menu, e sotto il
            // nome con la sua riga piccola. Serve per i banner: schiacciarli
            // dentro un'iconcina da trenta pixel non ha nessun senso.
            bool rigaIns = m.Insegne && it.Img != null && it.Img.Length > 0;
            float insH = 0f;
            if (rigaIns)
            {
                int iwI, ihI;
                insH = MW * 191f / 630f;
                if (MisuraPng(it.Img, out iwI, out ihI) && iwI > 0 && ihI > 0)
                    insH = MW * (float)ihI / (float)iwI;
                RH = insH + (it.Sotto != null && it.Sotto.Length > 0 ? 32f : 20f);
            }

            // LA RIGA SCELTA: prima era un blocco bianco pieno, e per
            // leggerci sopra tutti i testi diventavano neri - il giallo del
            // livello, il verde dei prezzi, il rosso, tutto perso.
            // Adesso e' un velo chiaro sopra il fondo scuro: i colori
            // restano quelli che sono, e si vede lo stesso qual e' la riga.
            // il nero pieno era troppo cupo: un grigio scurissimo,
            // ma non nero, e le immagini si staccano meglio
            DrawRect(MX, y, MW, RH, 30, 32, 38, 240);
            if (sel) DrawRect(MX, y, MW, RH, 255, 255, 255, 60);

            // il colore del testo non dipende piu' dalla riga scelta:
            // il velo chiaro lascia leggere tutto senza cambiare i colori
            Color fg;
            if (it.Tinted)
            {
                fg = it.FondoPieno ? Color.FromArgb(255, 255, 255, 255)
                                   : Color.FromArgb(255, it.Cr, it.Cg, it.Cb);
            }
            else
            {
                fg = Color.FromArgb(255, 235, 235, 240);
            }

            if (it.Tinted && !(sel && rigaIcona))
            {
                if (it.FondoPieno)
                    DrawRect(MX, y, MW, RH, it.Cr, it.Cg, it.Cb, sel ? 150 : 90);
                else
                    DrawRect(MX, y, 3f, RH, it.Cr, it.Cg, it.Cb, sel ? 255 : 220);
            }

            float tx = MX + 9f;
            if (rigaIns)
            {
                try
                {
                    if (File.Exists(it.Img))
                    {
                        CustomSprite ib = new CustomSprite(it.Img, new SizeF(MW, insH),
                            new PointF(MX, y));
                        ib.Draw();
                    }
                }
                catch { }
                float ty = y + insH + 1f;
                DrawText(Txt(it), MX + 9f, ty, 0.24f, fg);
                if (it.Desc != null && it.Desc.Length > 0)
                    DrawTextRight(it.Desc, MX + MW - 9f, ty, 0.22f,
                        Color.FromArgb(255, 245, 205, 80));
                if (it.Sotto != null && it.Sotto.Length > 0)
                {
                    // testo chiaro, e i soldi verdi come dappertutto
                    Color cb = sel ? Color.FromArgb(255, 45, 45, 52)
                                   : Color.FromArgb(255, 225, 227, 235);
                    string s3 = it.Sotto, p3 = "";
                    int d3 = s3.IndexOf('$');
                    if (d3 > 0) { p3 = s3.Substring(d3); s3 = s3.Substring(0, d3); }
                    else if (d3 == 0) { p3 = s3; s3 = ""; }
                    DrawText(s3, MX + 9f, ty + 15f, 0.20f, cb);
                    if (p3.Length > 0)
                        DrawText(p3, MX + 9f + TextWidth(s3, 0.20f), ty + 15f, 0.20f,
                            Color.FromArgb(255, 130, 225, 180));
                }
                y = y + RH;
                shown++;
                continue;
            }
            if (rigaIcona)
            {
                // l'immagine nella sua misura vera, alta quanto la riga
                if (it.Img != null && it.Img.Length > 0)
                {
                    try
                    {
                        if (File.Exists(it.Img))
                        {
                            int iwp, ihp;
                            float lato = RH - 4f;
                            float dw = lato, dh3 = lato;
                            if (MisuraPng(it.Img, out iwp, out ihp) && iwp > 0 && ihp > 0)
                            {
                                float sc = lato / (float)(iwp > ihp ? iwp : ihp);
                                dw = iwp * sc; dh3 = ihp * sc;
                            }
                            CustomSprite ic = new CustomSprite(it.Img, new SizeF(dw, dh3),
                                new PointF(MX + 6f + (lato - dw) * 0.5f, y + 2f + (lato - dh3) * 0.5f));
                            ic.Draw();
                        }
                    }
                    catch { }
                }
                tx = MX + 6f + RH;

                // la seconda immagine, subito accanto alla scatolina
                if (it.Img2 != null && it.Img2.Length > 0)
                {
                    try
                    {
                        if (File.Exists(it.Img2))
                        {
                            int iw2, ih2;
                            float lato2 = RH - 4f;
                            float dw2 = lato2, dh2 = lato2;
                            if (MisuraPng(it.Img2, out iw2, out ih2) && iw2 > 0 && ih2 > 0)
                            {
                                float sc2 = lato2 / (float)(iw2 > ih2 ? iw2 : ih2);
                                dw2 = iw2 * sc2; dh2 = ih2 * sc2;
                            }
                            CustomSprite ic2 = new CustomSprite(it.Img2, new SizeF(dw2, dh2),
                                new PointF(tx + (lato2 - dw2) * 0.5f, y + 2f + (lato2 - dh2) * 0.5f));
                            ic2.Draw();
                            tx += RH - 2f;
                        }
                    }
                    catch { }
                }
            }
            string etichetta = Txt(it);
            // menu centrato: la voce sta in mezzo alla riga
            if (m.Centrato && !rigaIcona && !rigaIns)
            {
                float wc = TextWidth(etichetta, 0.24f);
                float txc = MX + (MW - wc) * 0.5f;
                if (txc > tx) tx = txc;
            }
            if (rigaIcona)
            {
                // l'etichetta non deve finire sotto livello e prezzo
                float spazio = MX + MW - 9f - tx;
                if (it.Desc != null && it.Desc.Length > 0)
                    spazio -= TextWidth(it.Desc, 0.22f) + 24f;
                etichetta = Entra(etichetta, 0.24f, spazio);
            }
            if (rigaIcona && etichetta.IndexOf('#') > 0)
            {
                // la misura dell'amo azzurrina, come nel quaderno dei pesci
                int ia = etichetta.IndexOf('#');
                string pa = etichetta.Substring(0, ia);
                string pb = etichetta.Substring(ia);
                DrawText(pa, tx, y + 8f, 0.24f, fg);
                DrawText(pb, tx + TextWidth(pa, 0.24f), y + 8f, 0.24f,
                         Color.FromArgb(255, 130, 200, 245));
            }
            else
            {
                // "sei qui" attaccato al nome, rosa: e' un segno, non un dato
                float ey2 = y + (rigaIcona ? (rigaSotto ? 4f : 8f) : 3f);
                int sq2 = etichetta.IndexOf("sei qui");
                if (sq2 < 0) sq2 = etichetta.IndexOf("you are here");
                if (sq2 > 0)
                {
                    string e1 = etichetta.Substring(0, sq2);
                    string e2 = etichetta.Substring(sq2);
                    DrawText(e1, tx, ey2, 0.24f, fg);
                    DrawText(e2, tx + TextWidth(e1, 0.24f), ey2, 0.24f,
                             Color.FromArgb(255, 250, 175, 205));
                }
                else DrawText(etichetta, tx, ey2, 0.24f, fg);
            }

            // la riga piccola sotto il nome: i dati del pezzo, spenti,
            // cosi' il lato destro resta libero per lo stato
            if (rigaSotto)
            {
                // LA RIGA PICCOLA: testo chiaro e soldi verdi.
                // Prima era tutta gialla e non si capiva niente: il giallo
                // e' del livello, i dollari sono verdi come dappertutto.
                Color cs2 = Color.FromArgb(255, 225, 227, 235);
                if (it.SottoTinta)
                    cs2 = Color.FromArgb(255, it.Sr, it.Sg, it.Sb);
                string st2 = it.Sotto, sp2 = "";
                int ds2 = st2.IndexOf('$');
                if (ds2 > 0) { sp2 = st2.Substring(ds2); st2 = st2.Substring(0, ds2); }
                else if (ds2 == 0) { sp2 = st2; st2 = ""; }
                DrawText(st2, tx, y + 18f, 0.19f, cs2);
                if (sp2.Length > 0)
                    DrawText(sp2, tx + TextWidth(st2, 0.19f), y + 18f, 0.19f,
                        Color.FromArgb(255, 130, 225, 180));
            }
            if (rigaIcona && it.Desc != null && it.Desc.Length > 0)
            {
                // "Liv.7   $315": il livello giallo come sempre, il prezzo verde.
                // Sulla riga selezionata il fondo e' bianco, quindi testo scuro.
                string dl = it.Desc, dliv = "", dpr = "";
                int dd = dl.IndexOf('$');
                if (dd > 0) { dliv = dl.Substring(0, dd).Trim(); dpr = dl.Substring(dd).Trim(); }
                else if (dd == 0) dpr = dl;
                else dliv = dl;
                Color cLiv = Color.FromArgb(255, 245, 205, 80);
                if (it.DescTinta)
                    cLiv = Color.FromArgb(255, it.Dr, it.Dg, it.Db);
                Color cPr = Color.FromArgb(255, 130, 225, 180);
                // A destra puo' esserci la freccia del sottomenu o il
                // valore di un interruttore: si lascia il loro spazio piu'
                // dieci pixel, se no il testo ci finisce sotto.
                float riservaDx = 0f;
                if (it.Kind == TItem.SUB)
                    riservaDx = TextWidth(">", 0.24f) + 10f;
                else if (it.Kind == TItem.TOGGLE)
                    riservaDx = TextWidth(it.On ? "[ ON ]" : "[ OFF ]", 0.24f) + 10f;
                else if (it.Kind == TItem.LIST && it.Opts != null && it.Opts.Length > 0)
                    riservaDx = TextWidth("< " + it.Opts[it.Sel] + " >", 0.24f) + 10f;
                else if (it.Kind == TItem.NUMBER)
                    riservaDx = TextWidth("< " + it.Val + " >", 0.24f) + 10f;
                float rx = MX + MW - 9f - riservaDx;
                if (dpr.Length > 0)
                {
                    // GLI XP NON SONO SOLDI.
                    // Dal dollaro in poi era tutto verde e i punti
                    // finivano verdi anche loro: si staccano e vanno
                    // nell'azzurro che hanno dappertutto.
                    string dxp = "";
                    int ip = dpr.IndexOf('+');
                    if (ip > 0) { dxp = dpr.Substring(ip); dpr = dpr.Substring(0, ip).TrimEnd(); }
                    if (dxp.Length > 0)
                    {
                        DrawTextRight(dxp, rx, y + (rigaSotto ? 4f : 8f), 0.22f,
                                      Color.FromArgb(255, 130, 200, 245));
                        rx -= TextWidth(dxp, 0.22f) + 10f;
                    }
                    if (dpr.Length > 0)
                    {
                        DrawTextRight(dpr, rx, y + (rigaSotto ? 4f : 8f), 0.22f, cPr);
                        rx -= TextWidth(dpr, 0.22f) + 10f;
                    }
                }
                if (dliv.Length > 0)
                    DrawTextRight(dliv, rx, y + (rigaSotto ? 4f : 8f), 0.22f, cLiv);
            }

            string right = "";
            if (it.Kind == TItem.SUB) right = ">";
            else if (it.Kind == TItem.TOGGLE) right = it.On ? "[ ON ]" : "[ OFF ]";
            else if (it.Kind == TItem.LIST && it.Opts != null && it.Opts.Length > 0) right = "< " + it.Opts[it.Sel] + " >";
            else if (it.Kind == TItem.NUMBER) right = "< " + it.Val + " >";

            if (right.Length > 0)
            {
                Color vc = fg;
                if (it.SignedValue && it.Opts != null && it.Opts.Length > 0)
                {
                    string ov = it.Opts[it.Sel];
                    if (ov.StartsWith("-")) vc = sel ? Color.FromArgb(255, 165, 25, 25) : Color.FromArgb(255, 255, 110, 110);
                    else if (ov.StartsWith("+")) vc = sel ? Color.FromArgb(255, 20, 120, 45) : Color.FromArgb(255, 120, 230, 130);
                    else vc = Color.FromArgb(255, 245, 245, 245);
                }
                DrawTextRight(right, MX + MW - 9f, y + (m.IconRows ? 8f : 3f), 0.23f, vc);
            }

            y = y + RH;
            shown++;
        }

        // footer
        DrawRect(MX, y, MW, FOOT_H, 0, 0, 0, 185);
        string pos = n > 0 ? ((m.Sel + 1) + "/" + n) : "0/0";
        DrawText(TitleOf(m).ToUpper() + "   " + pos, MX + 9f, y + 2f, 0.19f, Color.FromArgb(255, 200, 200, 210));
        DrawTextRight(L("F7 / RB+RIGHT", "F7 / RB+DESTRA"), MX + MW - 9f, y + 2f, 0.18f, Color.FromArgb(255, 170, 170, 185));
        y += FOOT_H;

        // L'ARMATURA DISEGNATA, sotto la finestra: la canna dritta a
        // destra e i pezzi incolonnati dal basso in su, come nell'HUD.
        if (m.Armatura != null && m.Armatura.Count > 0)
        {
            y += 8f;
            DrawRect(MX, y, MW, HEAD_H, 0, 0, 0, 210);
            float wa = TextWidth("ARMATURA", 0.24f);
            DrawText("ARMATURA", MX + (MW - wa) * 0.5f, y + 2f, 0.24f,
                     Color.FromArgb(255, 235, 238, 245));
            y += HEAD_H;

            // LE MISURE SONO QUELLE DELL'HUD, NON RIFATTE A OCCHIO.
            // Nell'HUD il blocco sta fra x 1128 e 1288 e fra y 365 e 640:
            // 160 larghezza, 275 altezza. Qui dentro ci sta uguale, si
            // sposta e basta. (X,Y) dell'HUD -> (ox + X-1128, oy + Y-365).
            float ARM_H = 287f;
            DrawRect(MX, y, MW, ARM_H, 0, 0, 0, 170);
            float ox = MX + (MW - 160f) * 0.5f;
            float oy = y + 6f;

            // la canna: girata di 90 gradi, 270 x 108, a 1180,365
            string ic = PanCampo(m.Armatura[0], 0);
            if (ic.Length > 0) SpriteGirataT(ic, ox + 52f, oy, 270f, 108f, 90f);

            // la colonna: si parte da y 590 e si sale di 54 per volta.
            // Il mulinello e' 128 x 50 e sta otto pixel piu' a sinistra,
            // gli altri 112 x 44.
            int qa;
            for (qa = 1; qa < m.Armatura.Count && qa <= 4; qa++)
            {
                float ry = oy + (590f - 365f) - (qa - 1) * 54f;
                string ia = PanCampo(m.Armatura[qa], 0);
                string da = PanCampo(m.Armatura[qa], 1);
                if (ia.Length > 0)
                {
                    if (qa == 1) SpriteT(ia, ox, ry - 3f, 128f, 50f);
                    else SpriteT(ia, ox + 8f, ry, 112f, 44f);
                }
                if (da.Length > 0)
                    DrawText(da, ox + 17f, ry + 15f, 0.19f,
                             Color.FromArgb(255, 245, 245, 250));
            }
            y += ARM_H;
        }

        // IL QUARTO RIQUADRO: sotto la finestra, largo uguale. Ci sta
        // quello che e' montato adesso. Non si sceglie: si guarda.
        if (m.PannelloGiu != null && m.PannelloGiu.Count > 0)
        {
            y += 8f;
            string g0 = PanCampo(m.PannelloGiu[0], 0);
            if (g0.StartsWith("- ")) g0 = g0.Substring(2);
            DrawRect(MX, y, MW, HEAD_H, 0, 0, 0, 210);
            float wg = TextWidth(g0, 0.24f);
            DrawText(g0, MX + (MW - wg) * 0.5f, y + 2f, 0.24f,
                     Color.FromArgb(255, 235, 238, 245));
            y += HEAD_H;

            int qg;
            for (qg = 1; qg < m.PannelloGiu.Count; qg++)
            {
                string rg2 = PanCampo(m.PannelloGiu[qg], 0);
                string ig2 = PanCampo(m.PannelloGiu[qg], 1);
                string dg2 = PanCampo(m.PannelloGiu[qg], 2);
                bool titg = rg2.StartsWith("- ");
                if (titg) rg2 = rg2.Substring(2);

                float GH = (ig2.Length > 0) ? 30f : ITEM_H;
                DrawRect(MX, y, MW, GH, 0, 0, 0, titg ? 195 : 150);

                Color cg = titg ? Color.FromArgb(255, 130, 200, 245)
                                : Color.FromArgb(255, 235, 238, 245);

                if (ig2.Length > 0)
                {
                    try
                    {
                        if (File.Exists(ig2))
                        {
                            int iw3, ih3;
                            float bw3 = 40f, bh3 = GH - 4f;
                            float dw3 = bw3, dh3 = bh3;
                            if (MisuraPng(ig2, out iw3, out ih3) && iw3 > 0 && ih3 > 0)
                            {
                                float sx3 = bw3 / (float)iw3, sy3 = bh3 / (float)ih3;
                                float sc3 = (sx3 < sy3) ? sx3 : sy3;
                                dw3 = iw3 * sc3; dh3 = ih3 * sc3;
                            }
                            CustomSprite spg = new CustomSprite(ig2, new SizeF(dw3, dh3),
                                new PointF(MX + 4f + (bw3 - dw3) * 0.5f,
                                           y + 2f + (bh3 - dh3) * 0.5f));
                            spg.Draw();
                        }
                    }
                    catch { }
                    DrawText(Entra(rg2, 0.22f, MW - 58f), MX + 50f, y + 2f, 0.22f, cg);
                    string qg2 = PanCampo(m.PannelloGiu[qg], 4);
                    float dxg = MX + 50f;
                    float dmaxg = MX + MW - 8f;
                    if (qg2.Length > 0)
                    {
                        DrawText(qg2, dxg, y + 15f, 0.20f, ColoreValore(qg2, false));
                        dxg += TextWidth(qg2, 0.20f) + 9f;
                    }
                    if (dg2.Length > 0)
                    {
                        string[] pzg = dg2.Split(new string[] { "   " },
                                                 StringSplitOptions.RemoveEmptyEntries);
                        int qzg;
                        for (qzg = 0; qzg < pzg.Length; qzg++)
                        {
                            string p3 = pzg[qzg].Trim();
                            if (p3.Length == 0) continue;
                            float w3 = TextWidth(p3, 0.20f);
                            if (dxg + w3 > dmaxg) break;
                            DrawText(p3, dxg, y + 15f, 0.20f, ColoreValore(p3, false));
                            dxg += w3 + 9f;
                        }
                    }
                }
                else if (titg)
                {
                    Color ct3 = PanColore(m.PannelloGiu[qg], cg);
                    string ru3 = Entra(rg2.ToUpper(), 0.185f, MW - 18f);
                    float wu3 = TextWidth(ru3, 0.185f);
                    DrawText(ru3, MX + (MW - wu3) * 0.5f, y + 5f, 0.185f, ct3);
                }
                else
                {
                    DrawText(Entra(rg2, 0.22f, MW - 18f), MX + 9f, y + 3f, 0.22f, cg);
                }
                y += GH;
            }
        }
    }

    // Rettangolo con gli angoli smussati. Il gioco sa disegnare solo
    // rettangoli, quindi la curva si fa a gradini: quattro righe sottili
    // che rientrano sopra e sotto. A questa dimensione l'occhio legge un
    // angolo tondo. (Un PNG darebbe la curva vera ma finirebbe sopra a
    // tutto: le texture di ScriptHookV si disegnano dopo i rettangoli.)
    void DrawRoundRect(float px, float py, float pw, float ph, int r, int g, int b, int a)
    {
        DrawRoundRect(px, py, pw, ph, r, g, b, a, 1f);
    }

    // 'k' allarga o stringe lo smusso: 1 = angolo discreto, 3 = quasi ovale
    void DrawRoundRect(float px, float py, float pw, float ph, int r, int g, int b, int a, float k)
    {
        float[] inset = new float[] { 5f * k, 3f * k, 2f * k, 1f * k };
        int n = inset.Length;

        // corpo centrale, pieno
        DrawRect(px, py + n, pw, ph - n * 2f, r, g, b, a);

        int i;
        for (i = 0; i < n; i++)
        {
            float d = inset[i];
            DrawRect(px + d, py + i, pw - d * 2f, 1f, r, g, b, a);                    // sopra
            DrawRect(px + d, py + ph - 1f - i, pw - d * 2f, 1f, r, g, b, a);          // sotto
        }
    }

    void SpriteT(string f, float x, float y, float w, float h)
    {
        if (f == null || f.Length == 0) return;
        try
        {
            if (!File.Exists(f)) return;
            float dw = w, dh = h, dx = x, dy = y;
            int iw, ih;
            if (MisuraPng(f, out iw, out ih) && iw > 0 && ih > 0)
            {
                float sx = w / (float)iw, sy = h / (float)ih;
                float sc = (sx < sy) ? sx : sy;
                dw = iw * sc; dh = ih * sc;
                dx = x + (w - dw) * 0.5f;
                dy = y + (h - dh) * 0.5f;
            }
            CustomSprite sp = new CustomSprite(f, new SizeF(dw, dh), new PointF(dx, dy));
            sp.Draw();
        }
        catch { }
    }

    // un'immagine girata: ruotando resta fermo il centro, quindi e' li'
    // che va messa. 'w' e' la misura lungo il lato lungo.
    void SpriteGirataT(string f, float x, float y, float w, float h, float gradi)
    {
        if (f == null || f.Length == 0) return;
        try
        {
            if (!File.Exists(f)) return;
            float dw = w, dh = h;
            int iw, ih;
            if (MisuraPng(f, out iw, out ih) && iw > 0 && ih > 0)
            {
                float sx = w / (float)iw, sy = h / (float)ih;
                float sc = (sx < sy) ? sx : sy;
                dw = iw * sc; dh = ih * sc;
            }
            float cx = x + h * 0.5f;
            float cy = y + w * 0.5f;
            CustomSprite sp = new CustomSprite(f, new SizeF(dw, dh),
                                               new PointF(cx - dw * 0.5f, cy - dh * 0.5f));
            sp.Rotation = gradi;
            sp.Draw();
        }
        catch { }
    }

    void DrawRect(float px, float py, float pw, float ph, int r, int g, int b, int a)
    {
        float ccx = (px + pw * 0.5f) / 1280f;
        float ccy = (py + ph * 0.5f) / 720f;
        Function.Call(Hash.DRAW_RECT, ccx, ccy, pw / 1280f, ph / 720f, r, g, b, a);
    }

    // Arco a segmenti: non esiste una primitiva per gli archi, quindi si
    // disegnano tanti quadratini lungo la circonferenza. 'a0' e 'a1' sono
    // i gradi (0 = ore 12, si gira in senso orario), 'pct' e' 0..100.
    void DrawArc(float cx, float cy, float rad, float a0, float a1,
                 float pct, float th, Color colOn, Color colOff)
    {
        int n = 48;
        int i;
        for (i = 0; i <= n; i++)
        {
            float t = (float)i / (float)n;
            float deg = a0 + (a1 - a0) * t;
            float rd = (deg - 90f) * 0.0174533f;

            float px = cx + (float)Math.Cos(rd) * rad;
            float py = cy + (float)Math.Sin(rd) * rad;

            Color c = (t * 100f <= pct) ? colOn : colOff;
            DrawRect(px - th * 0.5f, py - th * 0.5f, th, th, c.R, c.G, c.B, c.A);
        }
    }

    float Clamp01(float x)
    {
        if (x < 0f) return 0f;
        if (x > 1f) return 1f;
        return x;
    }

    // Lancetta: un segmento dal raggio 'rIn' al raggio 'rOut', disegnato
    // con quadratini perche' non c'e' una primitiva per le linee.
    void DrawNeedle(float cx, float cy, float rIn, float rOut,
                    float deg, float th, Color col)
    {
        float rd = (deg - 90f) * 0.0174533f;
        float dx = (float)Math.Cos(rd);
        float dy = (float)Math.Sin(rd);

        int n = 12;
        int i;
        for (i = 0; i <= n; i++)
        {
            float r = rIn + (rOut - rIn) * ((float)i / (float)n);
            float px = cx + dx * r;
            float py = cy + dy * r;
            DrawRect(px - th * 0.5f, py - th * 0.5f, th, th, col.R, col.G, col.B, col.A);
        }
    }

    // larghezza del testo in pixel sulla griglia 1280x720
    float TextWidth(string txt, float scale)
    {
        TextElement el = new TextElement(txt, new PointF(0f, 0f), scale);
        el.Font = GTA.UI.Font.ChaletLondon;
        return el.Width;
    }

    void DrawText(string txt, float x, float y, float scale, Color col)
    {
        TextElement el = new TextElement(txt, new PointF(x, y), scale);
        el.Color = col;
        el.Font = GTA.UI.Font.ChaletLondon;
        el.Outline = false;
        el.Draw();
    }

    void DrawTextCenter(string txt, float x, float y, float scale, Color col)
    {
        TextElement el = new TextElement(txt, new PointF(x, y), scale);
        el.Color = col;
        el.Font = GTA.UI.Font.ChaletLondon;
        el.Alignment = Alignment.Center;
        el.Outline = false;
        el.Draw();
    }

    void DrawTextCenterOutline(string txt, float x, float y, float scale, Color col)
    {
        TextElement el = new TextElement(txt, new PointF(x, y), scale);
        el.Color = col;
        el.Font = GTA.UI.Font.ChaletLondon;
        el.Alignment = Alignment.Center;
        el.Outline = true;
        el.Draw();
    }

    void DrawTextOutline(string txt, float x, float y, float scale, Color col)
    {
        TextElement el = new TextElement(txt, new PointF(x, y), scale);
        el.Color = col;
        el.Font = GTA.UI.Font.ChaletLondon;
        el.Alignment = Alignment.Right;
        el.Outline = true;
        el.Draw();
    }

    void DrawTextRight(string txt, float x, float y, float scale, Color col)
    {
        TextElement el = new TextElement(txt, new PointF(x, y), scale);
        el.Color = col;
        el.Font = GTA.UI.Font.ChaletLondon;
        el.Alignment = Alignment.Right;
        el.Outline = false;
        el.Draw();
    }

}
