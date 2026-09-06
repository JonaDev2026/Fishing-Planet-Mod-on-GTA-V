# Mod Pesca per GTA V Enhanced

Una mod di pesca per Grand Theft Auto V Enhanced costruita sui dati veri di
**Fishing Planet**: 239 specie con i loro pesi, le loro esche, i loro ami, i
loro orari e la loro rarità; 137 canne, 134 mulinelli, 233 lenze, 381 fra ami,
leader, rig e piombi, 527 esche artificiali, 205 esche naturali. Non è una
pesca "premi un tasto e prendi un pesce": conta dove sei, che ora è, che
temperatura ha l'acqua, cosa hai montato e come lo usi.

35 acque fra Los Santos e Blaine County, licenze giornaliere, negozio, cassetta
e portacanne, 51 tornei con premi in denaro, progressione a livelli e un diario
con 239 specie da riempire.

## Il video

[![La mod in gioco](https://img.youtube.com/vi/fE_OSdsgQKs/hqdefault.jpg)](https://www.youtube.com/watch?v=fE_OSdsgQKs)

https://www.youtube.com/watch?v=fE_OSdsgQKs

Questo file è il manuale: spiega come si gioca e, soprattutto, **perché un
pesce abbocca o no**. Tutte le regole descritte qui sono quelle scritte nel
codice; dove un numero è nostro e non del wiki, è detto.

---

## Indice

1. [Cosa serve e installazione](#1-cosa-serve-e-installazione)
2. [I comandi](#2-i-comandi)
3. [La giornata di pesca](#3-la-giornata-di-pesca)
4. [L'attrezzatura](#4-lattrezzatura)
5. [Sull'acqua, passo per passo](#5-sullacqua-passo-per-passo)
6. [Perché abbocca: le regole](#6-perché-abbocca-le-regole)
7. [La taglia del pesce](#7-la-taglia-del-pesce)
8. [La robaccia](#8-la-robaccia)
9. [L'HUD](#9-lhud)
10. [Esperienza, livelli, diario e soldi](#10-esperienza-livelli-diario-e-soldi)
11. [I tornei](#11-i-tornei)
12. [Le impostazioni](#12-le-impostazioni)
13. [I dati: cosa è vero e cosa è nostro](#13-i-dati-cosa-è-vero-e-cosa-è-nostro)
14. [Note](#14-note)

---

## 1. Cosa serve e installazione

### Le dipendenze

| | versione con cui la mod è stata sviluppata e provata |
|---|---|
| **Grand Theft Auto V Enhanced** | build 1.0.1158.13 (v3889) |
| [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) | build del 15 luglio 2026, v3889.0/1158.13 |
| [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet/releases) | 3.9.0.6 Enhanced (API 3.9.0) |

Sono tutti e tre obbligatori. ScriptHookV va aggiornato a ogni patch del
gioco, altrimenti non parte niente; ScriptHookVDotNet compila `Pesca.cs`
all'avvio, quindi non serve nessun compilatore né altro programma. La mod è
scritta in C# vecchio stile (niente interpolazione di stringhe, niente
lambda) apposta per il compilatore che ScriptHookVDotNet porta con sé.

Sulla stessa installazione girano anche altri script (`Trainer.cs`, i lavori
in `scripts\Lavori`): la mod non dipende da loro.

### Installazione

Copia la cartella `scripts` dentro la cartella del gioco:

```
...\Grand Theft Auto V Enhanced\scripts\Attivita\Pesca\
```

| | |
|---|---|
| `Pesca.cs` | tutta la mod, un file solo |
| `config.ini` | ogni impostazione, commentata riga per riga; si rilegge a gioco acceso |
| `*.txt` | i dati: pesci, attrezzatura, zone, tornei, temperature, traduzioni, guida |
| `img\` | le immagini: pesci, attrezzatura, banner, HUD, grafici dell'attività |
| `suoni\` | i suoni della mod (wav) |
| `gen_attivita.py` | lo script (Python 3 + Pillow) che rigenera i grafici dell'attività: serve solo se cambi i dati dei pesci o delle temperature |

Al primo avvio la mod scrive da sola il salvataggio (`stato.txt`) nella sua
cartella. Per ricaricare gli script senza riavviare il gioco: **INS**
(ScriptHookVDotNet). Se qualcosa non compila, l'errore con la riga sta in
`ScriptHookVDotNet.log` nella cartella del gioco.

Attenzione: ScriptHookVDotNet compila **tutti** i `.cs` dentro `scripts`,
sottocartelle comprese. Un file di backup con estensione `.cs` lasciato lì
dentro viene caricato anche lui: i backup vanno tenuti fuori da `scripts` o
rinominati `.cs.old`.

La lingua si sceglie dal menu (IMPOSTAZIONI → Lingua) ed è salvata in
`config.ini` (`lingua=1` italiano, `0` inglese). I nomi dell'attrezzatura
sono quelli inglesi del catalogo; i pesci hanno la traduzione in
`pesci_it.txt`.

---

## 2. I comandi

La mod è pensata per il pad; la tastiera fa le stesse cose. In basso al centro
dello schermo c'è sempre la **barra dei suggerimenti**, che mostra solo i tasti
utili in quel momento e cambia da sola fra icone del pad e tasti della
tastiera.

### Menu

| Pad | Tastiera | |
|---|---|---|
| RB + SINISTRA | F7 (`menu_tasto` in config.ini) | apre e chiude il menu |
| LB / RB | TAB / Q | cambia scheda (ZONE, EQUIPAGGIAMENTO, NEGOZIO, DIARIO, TORNEI, IMPOSTAZIONI) |
| levetta / croce | frecce | ci si muove |
| A | INVIO | seleziona |
| B | ESC | indietro |

### Con la canna in mano (prima del lancio)

| Pad | Tastiera | |
|---|---|---|
| LB (tieni premuto) | TAB | la **ruota degli attrezzi** |
| RT (tieni premuto e molla) | clic (tieni e molla) | carica e **lancia** |
| croce ◄ ► | ◄ ► | frizione del mulinello |
| croce ▲ ▼ | ▲ ▼ | profondità dell'esca sotto il galleggiante |
| RB | Q | cambia esca |
| X | SPAZIO | ripone la canna |
| levetta sinistra | WASD | cammini, con la canna in mano |

### Con la lenza in acqua

| Pad | Tastiera | |
|---|---|---|
| RT | clic | recupera la lenza (e combatte il pesce) |
| A | INVIO | **aggancia il pesce** quando abbocca |
| croce ◄ ► | ◄ ► | frizione |
| LB | TAB | ritira la lenza e apre la ruota |
| levetta destra | mouse | muove la canna (strappata) |

### Quando il pesce è in mano

| Pad | Tastiera | |
|---|---|---|
| A | INVIO | lo tieni: va nella nassa |
| B | ESC | lo ributti in acqua |

### La ruota degli attrezzi

Mentre peschi, **LB** non apre più la ruota delle armi di GTA: apre la ruota
degli attrezzi. Tieni premuto LB, con la levetta destra scegli lo spicchio,
con ◄ ► della croce scorri i pezzi di quella categoria che hai in borsa,
lasci LB e **tutte** le scelte fatte si montano insieme. Dodici spicchi:
canna, mulinello, lenza, leader, piombo, pesci del lago, zaino, nassa, amo,
galleggiante, esca, cucchiaino. La prima voce di ogni spicchio è **Vuoto**:
smonta quel pezzo. Zaino, nassa e pesci del lago aprono la pagina
corrispondente del menu.

Se lasci LB con la canna montata e non hai la canna in mano, la prendi in
mano; se smonti la canna, la riponi.

---

## 3. La giornata di pesca

### Le zone

Dal menu, **Zone di pesca** elenca le 35 acque: il livello richiesto, quante
specie ci vivono, il prezzo della licenza. Ogni zona della mappa corrisponde a
un'acqua vera di Fishing Planet e ne ha i pesci: l'Alamo Sea è un lago
americano di pesci gatto e bass, il fiume Zancudo è un torrente di trote, il
mare di Paleto è il fiordo norvegese dei merluzzi e degli squali, Vespucci e
Chumash sono le coste della Florida. Selezionando una zona la mod mette il
segnaposto sulla mappa e, se la zona ha un prezzo, ti dice quanto costa.

### La licenza

Finché non compri la licenza sei "a casa" anche se stai sulla riva: il menu ti
lascia aperti il **negozio** completo a prezzo normale e l'**inventario**, così
compri e prepari la borsa con calma.

Arrivato sul posto scegli **Inizia a pescare**: la mod riconosce la zona e ti
propone le licenze di quell'acqua. Una licenza vale per tutta l'acqua, non per
il singolo tratto di riva.

| | |
|---|---|
| **Basic** | niente pesca di notte, niente pesca dalla barca, i trofei vanno rilasciati |
| **Advanced** | nessuna restrizione; costa il doppio |

Quattro tagli: 1 giorno, 3 giorni (2,85 volte un giorno), una settimana (6,3
volte), un mese (24 volte). I rapporti fra i tagli e il raddoppio
Basic → Advanced sono quelli di Fishing Planet; i prezzi di partenza sono
nostri. Scaduta la giornata, se sei ancora lì, puoi **prolungare** di un
giorno a prezzo pieno.

Per partire servono almeno una canna, un mulinello con la sua lenza e una
nassa. Il resto è affar tuo: quante esche portare lo decidi tu.

### L'orologio

Comprata la licenza l'orologio di GTA va alle **05:00** e rallenta: un minuto
di gioco ogni 5 secondi veri, quindi una giornata di 24 ore dura circa **due
ore d'orologio**. L'ora del giorno conta per i pesci (vedi le regole), quindi
la giornata ha un suo ritmo: alba, mattina, pomeriggio, tramonto, notte. Alla
scadenza l'orologio torna normale, la nassa si svuota, il pesce si vende e
l'inventario di casa si riapre. Se la licenza è ancora valida, restare in riva
fa passare le ore come nella realtà.

### Il negozio sul posto

Con la licenza attiva l'inventario di casa si **chiude** per tutta la
giornata: quello che hai dimenticato a casa te lo sei dimenticato. Sull'acqua
c'è un banchetto: vende solo materiale di consumo (esche, lenze, qualche amo e
terminale), solo fino al tuo livello, poche voci, e **al doppio** del prezzo di
casa. Niente canne, mulinelli, nasse o cassette. Chi si organizza prima
risparmia.

---

## 4. L'attrezzatura

### Casa, borsa, portacanne, cassetta

Tutto quello che compri sta a casa. Prima di uscire carichi la borsa: le canne
nel **portacanne** (canne, mulinelli e lenze), il resto nella **cassetta** (ami,
terminali, galleggianti, esche). Quanto ci sta lo decidono i due contenitori
che possiedi; il portacanne più capiente del gioco tiene 7 canne.

L'inventario di casa ha quattro riquadri: al centro le categorie, a sinistra
quello che hai in casa, a destra quello che ti porti dietro, e sull'HUD
l'armatura montata. **A** sposta un pezzo da casa alla borsa e viceversa, **X**
lo monta (o lo vende, se sei a casa), **Y** lo getta.

### Montare non è gratis

- La **lenza** è una bobina coi suoi metri. Armandola, il mulinello ne taglia
  quanti ne tiene; quello che avanza resta in cassetta come bobina separata.
  Smontandola torna una bobina con i metri che le sono rimasti.
- **Ami**, **galleggianti** ed **esche** escono dalla cassetta: dieci ami meno
  quello montato fanno nove. L'esca si consuma a ogni abboccata.
- Il **cucchiaino** e il galleggiante non convivono: a spinning l'artificiale
  è l'amo. Il **leader** invece ci sta sempre, e coi predatori serve.
- Il **piombo** e il galleggiante da soli non pescano: in punta ci vuole un
  amo, una testina, un rig o un artificiale.

### Quanto reggi

Il pesce che riesci a tirare su è il **più debole dei tre pezzi**: la canna
regge fino a tot chili, il mulinello frena fino a tot, la lenza si spezza a
tot. Vince il numero più basso, ed è scritto sull'HUD accanto alla canna. Un
pesce più pesante di così non ti viene proprio attaccato.

La **nassa** ha due limiti suoi: il pesce singolo più grosso che ci sta e
quanti chili in tutto prima di essere piena. Con la nassa piena il pesce lo
puoi solo ributtare.

### La frizione

Il mulinello ha **12 posizioni** di frizione. Con la frizione tutta chiusa la
lenza non cede e la tensione sale alla svelta; più la apri, più il pesce si
porta via filo e meno tira sulla lenza. Sull'HUD è l'anello attorno al
mulinello, con la percentuale sotto.

---

## 5. Sull'acqua, passo per passo

1. **Canna in mano.** Dal menu, dalla ruota o col pezzo montato, prendi la
   canna. Puoi camminare lungo la riva con la canna in mano, scegliere la
   frizione e la profondità dell'esca sotto il galleggiante (da 13 cm a
   2,5 m, a passi di 13 cm).
2. **Lancio.** Tieni premuto RT: la barra si carica; molla e l'esca parte. La
   levetta destra muove la canna. Se l'esca finisce sul prato o sull'asfalto
   la lenza rientra da sola.
3. **Attesa.** Il galleggiante sta a galla (o il cucchiaino affonda, se sei a
   spinning). Se il terminale è più lungo del fondo, il galleggiante si
   **corica** sull'acqua: è il segno che l'esca tocca terra, e il quadrante
   sull'HUD te lo mostra. Puoi recuperare piano per portare l'esca verso di
   te; a galleggiante, tirare forte mentre il pesce assaggia lo spaventa e
   se ne va.
4. **Assaggi.** Negli ultimi secondi prima dell'abboccata il galleggiante
   balla e il pad vibra. Prima di quello l'acqua sta ferma davvero: la pesca
   è quiete, e quando il pesce arriva, arriva.
5. **Abboccata.** Il galleggiante sparisce, il pad vibra forte e nella barra
   compare **A – Aggancia il pesce**. Hai un secondo e mezzo: se non
   agganci, "Se n'è andato".
6. **La lotta.** Tieni RT per recuperare. La barra segnata sopra la frizione è
   la **tensione** della lenza: blu, verde, giallo, rosso. Il pesce tira a
   strappi e ti porta il filo da una parte all'altra, tanto più largo quanto è
   pesante rispetto a quello che reggi; man mano che si stanca il raggio si
   stringe. Se la tensione arriva a fondo scala, la lenza si **spezza**: perdi
   tre metri di lenza, l'amo, il galleggiante e l'esca. Se avevi un
   **leader**, si spezza lui e la lenza madre resta intera. Gli ultimi metri
   sono i più duri: sotto i tre metri il pesce vede la riva e si impunta.
7. **I denti.** Un predatore da 1,2 kg in su, agganciato senza cavetto, la
   lenza te la trancia e se ne va. I cuccioli si prendono anche col filo
   nudo.
8. **A riva.** Quando i metri sono finiti il pesce è tuo: compare la scheda
   con nome, foto, taglia (comune, trofeo, esemplare unico), peso, valore e
   punti. **A** lo tieni, **B** lo ributti. Con la licenza Basic i trofei
   vanno ributtati.

---

## 6. Perché abbocca: le regole

Questa è la parte che fa la differenza fra pescare e premere un tasto. Ogni
volta che lanci succedono, in ordine, tre cose: parte un'**attesa**, alla
scadenza si **sorteggia** un pesce fra quelli del posto, e quel pesce
**guarda l'esca**.

### 6.1 La temperatura dell'acqua

GTA non ha una temperatura: la calcola la mod, ed è mostrata sull'HUD
("Acqua 14°"). Dipende da tre cose:

- l'**ora del gioco**: l'aria fa 13° alle 4 del mattino e 27° alle 16, con una
  curva morbida in mezzo;
- il **meteo**: sole pieno, pioggia, neve spostano di qualche grado;
- la **quota**: sopra i 50 metri l'aria scende di 0,6° ogni 100 metri.

L'acqua è più stabile dell'aria: 16° più il 45% dello scarto dell'aria dai 20°.
Questi numeri sono nostri, non del wiki.

Ogni specie ha il suo **intervallo di temperatura** (`temperature_pesci.txt`:
minimo, massimo, ottimo). Trote e salmerini stanno bene fra 4 e 16°, carpe e
pesci gatto fra 15 e 28°, i pesci dell'Amazzonia fra 24 e 32°, i merluzzi del
mare del Nord fra 3 e 12°. Sono valori indicativi, presi dalla biologia
generale: il wiki di Fishing Planet non li ha.

Alla sua temperatura ottima un pesce **vale pieno**; ai bordi del suo
intervallo vale il 40%; oltre 4° fuori dall'intervallo **non esce**. Quindi al
lago, d'inverno, escono trote e lucci; d'estate carpe e pesci gatto. Il
merluzzo di Paleto non lo prendi con l'acqua a 25°.

### 6.2 L'attesa

Quanto aspetti dopo il lancio dipende da **quanto è viva l'acqua adesso**: si
guarda, fra i pesci del posto, quello che sta meglio con la temperatura di
adesso. Se ce n'è uno alla sua ottima, l'acqua è viva; se tutti stanno ai
bordi del loro intervallo, è lenta; se nessuno è nel suo intervallo, è quasi
ferma.

| l'acqua è | attesa (circa) |
|---|---|
| viva (un pesce alla sua temperatura ottima) | 60–66 secondi |
| così così (i pesci ai bordi del loro intervallo) | 2 minuti e mezzo |
| ferma (nessuno nel suo intervallo) | fino a 6–7 minuti |

La distanza del lancio non conta. L'esca non conta. La base di 60 secondi è in
`config.ini` (`attesa_base`).

### 6.3 Il sorteggio

Scaduta l'attesa si guarda ogni specie del posto. Alcune vengono **scartate**:

- pesa più di quanto regge la tua attrezzatura;
- non vive in questa zona;
- il tuo amo è troppo lontano dalla sua misura (vedi sotto);
- la tecnica è impossibile (il cucchiaino con una canna che non lo lancia);
- è più di 4° fuori dal suo intervallo di temperatura.

A quelle rimaste si dà un **peso** nel sorteggio, che è il prodotto di:

| fattore | come conta |
|---|---|
| **rarità** (dal wiki, 1–5) | comunissimo 100, comune 55, normale 28, raro 12, rarissimo 5 |
| **misura dell'amo** (dal wiki) | dentro il range del pesce 1; una misura fuori 0,55; due fuori 0,25; tre fuori 0,08; oltre non esce |
| **ora del giorno** (abitudine della specie) | chi mangia di notte: notte 1, alba/tramonto 0,45, pieno giorno 0,12. Chi mangia all'alba e al tramonto: 1 nelle sue ore, 0,35 di giorno, 0,30 di notte. Chi mangia di giorno: 1, 0,45, 0,10. Notte = 21–5, alba/tramonto = 5–8 e 18–21 |
| **tecnica** | il predatore (specie che sul wiki ha gli artificiali) a esca naturale abbocca a 0,25 e quasi solo piccolo; il cucchiaino con una canna da lancio vale 1, con una match o telescopica 0,15 |
| **canna per la famiglia** | ogni famiglia ha le sue canne (carpa → canne da carpa, feeder, spod, fondo; lucci, bass e persici → spinning e casting; trote e salmoni → spinning, casting, match; mare → canne da mare e casting; abramidi e panfish → match, feeder, telescopica; pesci gatto → fondo, carpa, mare; storioni → fondo, carpa). Con la canna sbagliata 0,60 se è una match, fondo o telescopica, 0,35 altrimenti |
| **amo per la famiglia** | amo specialista di un'altra famiglia 0,55; amo generico 0,80; amo della sua famiglia 1 |
| **temperatura** | 1 all'ottima, 0,4 ai bordi dell'intervallo |
| **punto caldo** | se l'esca sta sopra il punto di quella specie, × 6 |

Si estrae in proporzione ai pesi. Se nessuna specie resta in gioco (amo, ora,
acqua sbagliati), non succede niente: la lenza resta in acqua e si riprova ogni
6–14 secondi, senza avvisi. Se non abbocca mai, la domanda è sempre la stessa:
che ora è, che temperatura ha l'acqua, che amo hai.

### 6.4 L'esca

L'esca **non scarta** nessuno dal sorteggio: decide dopo. Il pesce estratto
guarda cosa c'è sull'amo:

- se è una delle **sue** (la lista "Preferred baits" o "Preferred lures" della
  sua pagina sul wiki) → **abbocca sempre**;
- se **non** è la sua → abbocca **una volta su tre** (ha fame); le altre due se
  ne va, e tu riaspetti.

Quindi il bluegill col pane lo prendi, ma con i vermi lo prendi tre volte di
più. Vale anche per gli artificiali: un cucchiaino che quel pesce non insegue
lo prende una volta su tre.

Senza niente all'amo non abbocca nessuno.

### 6.5 I punti caldi

Dentro ogni acqua i pesci non stanno sparsi: ci sono punti (una buca, una
sponda, un canneto) dove una specie sta di casa. Sopra il punto di quella
specie il pesce pesa sei volte tanto nel sorteggio; sui punti profondi la
specie non cambia, ma il sorteggio della taglia si sposta verso l'alto. I
punti sono nostri (`punti_caldi.txt`) e non sono segnati: si scoprono.

### 6.6 I pesci che passano

Mentre sei in riva, ogni tanto passa un pesce sott'acqua, da solo o in un
gruppetto di due o tre, ognuno con la sua sagoma. Sono le specie di
quell'acqua: ti dicono cosa c'è, non cosa abboccherà.

---

## 7. La taglia del pesce

Sorteggiata la specie, si tira il **peso**, fra un minimo e un tetto:

- il minimo è il 60% del peso "comune" del wiki;
- il tetto lo decide **l'amo**, come in Fishing Planet. Ogni pesce ha il suo
  range di ami: con l'amo alla misura **piccola** del range escono solo i
  **comuni**; dalla **metà** del range in su anche i **trofei**; con la misura
  **grande** anche gli **esemplari unici**. Il luccio va da #1/0 a #5/0: con
  #1/0 e #2/0 comuni, con #3/0 e #4/0 trofei, con #5/0 unici. L'amo
  dell'artificiale conta allo stesso modo. Fuori dal range si conta la misura
  del range più vicina;
- il tetto non passa mai quello che regge la tua attrezzatura.

Il dado è **truccato verso il basso**: l'amo apre il tetto, non ci porta. Con
la tecnica sbagliata (predatore a esca naturale) escono quasi solo i piccoli;
sul punto profondo il tiro si sposta verso l'alto. L'unico può superare del 20%
il peso del wiki, perché in gioco succede.

Dopo un **colpo grosso** c'è una pausa, per specie, in minuti veri e salvata:
preso un esemplare unico, quella specie non dà altri unici per 20 minuti; preso
un trofeo, niente trofei per 5 minuti. L'unico di luccio non blocca l'unico di
carpa.

---

## 8. La robaccia

Siamo a Los Santos: ogni tanto all'amo si attacca una scarpa, un sacchetto, un
cono, un copertone, una pianta acquatica (`robaccia.txt`). Valgono due soldi,
non danno punti e nella nassa non ci vanno.

La robaccia esce **solo con l'esca sbagliata**: quando il pesce estratto trova
un'esca che non è la sua e se ne va, una volta su quattro al suo posto si
aggancia la robaccia. Con l'esca giusta non esce mai. Senza niente all'amo
esce una volta su tre.

Quando succede: "Qualcosa ha preso: recupera". La roba segue la lenza
sott'acqua, a lenza ritirata la vedi penzolare dalla canna un momento e ti
viene detto cosa hai tirato su.

---

## 9. L'HUD

Tutto in basso a destra, minimo di testo:

- la **canna** con i chili che regge (il più debole dei tre pezzi);
- la **colonna dell'armatura**: lenza, piombo, leader, galleggiante (con la
  portata: leggera, media, pesante), amo, con un riquadro dietro ai pezzi
  montati;
- l'**esca** nel cerchio, col nome, quante ne hai e la misura dell'amo;
- la **nassa**;
- il **quadrante dell'acqua**: mare, lago o fiume col loro colore e il loro
  fondale (sabbia e coralli, fango con alghe e sassi, ciottoli di fiume); il
  galleggiante in scala col fondo vero, coricato quando l'esca tocca terra;
  sotto, "Esca x m" (la profondità impostata), "Fondo x m" (quando la lenza è
  in acqua) e "Acqua x°";
- la **frizione**: l'anello a 12 tacche col mulinello dentro, la percentuale,
  i chili del pesce e i metri di lenza fuori;
- la **barra della tensione**, a tacche, che si colora da blu a rosso;
- la **barra dei suggerimenti** in basso al centro, con i tasti utili adesso.

---

## 10. Esperienza, livelli, diario e soldi

### I punti

Ogni pesce tenuto dà esperienza:

| | |
|---|---|
| base | 20 + 15 per chilo |
| come l'hai preso | esca preferita × 1,5, amo giusto × 1,3, tutti e due × 2 |
| quante volte l'hai già preso | prima volta × 8, dalla 2ª alla 5ª × 3, dalla 6ª alla 20ª × 1,5, oltre × 1,3 |
| taglia | trofeo × 2, esemplare unico × 3 |

Nessun fattore domina: un pesce nuovo preso male vale come uno noto preso
benissimo. Si sale pescando specie diverse, non ripetendo la stessa.

### I livelli

Il livello sblocca l'attrezzatura e le zone, coi livelli veri del wiki. Quanto
ci metti a salire è nostro: circa **tre uscite per livello**, dall'inizio alla
fine, senza muri (`livelli.txt`). La curva vera di Fishing Planet, 110 livelli e
109 milioni di punti per l'ultimo, resta nei riferimenti: là per il livello 80
servono più di duemila uscite.

### Il diario

Quello che prendi finisce nel **diario**: per ogni specie il record di peso,
dove l'hai preso, con che esca e con che amo. 239 specie da riempire. Il
senso della mod sta qui più che nella barra dei livelli: sapere che il Gudgeon
lo prendi a Zancudo all'alba coi vermi rossi, e che nel canyon c'è una trota
che nell'Alamo non trovi.

### I soldi

I prezzi del wiki sono in crediti; qui sono divisi per 10. Il freno vero non
è il prezzo dell'attrezzatura, è la licenza. A fine giornata il pesce della
nassa si vende a un prezzo al chilo, quello del wiki; la robaccia vale quello
che vale. L'attrezzatura si rivende a casa a una percentuale del prezzo.

---

## 11. I tornei

51 tornei, uno per specie, presi dalle pagine dei singoli tornei del wiki:
durata, livello minimo, quota d'iscrizione, premi, regola di punteggio,
attrezzatura ammessa, ora d'inizio e meteo sono i loro. L'unica cosa nostra è
la **zona**: il wiki li tiene su laghi che noi non abbiamo, quindi ogni torneo
sta nella nostra acqua dove quel pesce vive. Bronzo, argento e oro con i loro
premi in denaro, più un extra per trofeo ed esemplare unico.

---

## 12. Le impostazioni

`scripts\Attivita\Pesca\config.ini` si rilegge **a ogni fotogramma**: quasi
tutto si cambia a gioco acceso, senza riavviare. Ogni voce ha il suo commento.
Le più utili per la pesca:

| voce | cosa fa |
|---|---|
| `attesa_base`, `attesa_caso` | l'attesa dopo il lancio (ms) e il pezzo a caso |
| `esca_sbagliata_abbocca` | su 100, quante volte abbocca con l'esca non sua (33) |
| `robaccia_prob_esca_sbagliata`, `robaccia_prob_senza_esca` | la robaccia |
| `temp_pesi`, `temp_bordo`, `temp_fuori` | la temperatura nel sorteggio |
| `amo_taglia` | l'amo che decide la taglia (1 acceso) |
| `unico_pausa_min`, `trofeo_pausa_min` | la pausa dopo il colpo grosso |
| `denti_kg` | da che peso il predatore senza leader trancia la lenza |
| `unico_extra` | quanto l'unico può superare il peso del wiki (%) |
| `friz_posizioni` | le posizioni della frizione |
| `pesci_scena_*` | i pesci che passano: ogni quanto, quanti, dove |
| `canna_disegnata` | 1 canna disegnata che si piega, 0 modello di GTA |
| `consigli_*`, `colonna_*`, `esca_*`, `nassa_*`, `friz_*`, `barra_*` | posizioni e misure dell'HUD |

---

## 13. I dati: cosa è vero e cosa è nostro

**Dal wiki di Fishing Planet**: il catalogo intero (pesci con pesi comune,
trofeo e unico, prezzi, esche preferite, artificiali, misura dell'amo,
famiglia, orari e rarità; canne, mulinelli, lenze, ami, terminali, esche
naturali con la loro classe di peso, artificiali, galleggianti, cassette,
portacanne, nasse, con livelli e prezzi), le acque e i loro pesci, i tornei, i
rapporti fra i tagli delle licenze.

**Nostri**, e scritti come tali in testa al file che li contiene:

- la temperatura dell'acqua e gli intervalli per specie (`temperature_pesci.txt`);
- i pesi del sorteggio (rarità, amo, ora, tecnica, canne per famiglia);
- l'attesa e i suoi numeri;
- il "una su tre" dell'esca sbagliata e la robaccia;
- l'amo che decide la taglia (la regola è di Fishing Planet, i gradini sono nostri) e le pause;
- i punti caldi;
- la curva dei livelli, la formula dei punti, i prezzi delle licenze, il cambio crediti → dollari;
- le zone dove stanno i tornei.

In `regole.txt` c'è il promemoria di cosa abbiamo deciso e perché. I nomi
italiani di pesci, esche e colori stanno in file di traduzione separati.

---

## 14. Note

Progetto personale, senza scopo di lucro, non affiliato né a Rockstar Games né
a Fishing Planet. *Fishing Planet* è un marchio di Fishing Planet LLC; i dati e
le immagini del catalogo vengono dal loro wiki e restano dei rispettivi
proprietari. *Grand Theft Auto V* è un marchio di Rockstar Games.
