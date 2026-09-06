# Fishing Mod 2026 per GTA V Enhanced

*[English version → README.en.md](README.en.md)*

Una mod di pesca per Grand Theft Auto V Enhanced costruita su dati veri:
239 specie con i loro pesi, le loro esche, i loro ami, i loro orari e la loro
rarità; 137 canne, 134 mulinelli, 233 lenze, 381 fra ami, testine, rig, leader
e piombi, 25 galleggianti, 527 esche artificiali, 205 esche naturali, 31 nasse
e fili, 18 cassette e borse, 9 portacanne. Non è una pesca "premi un tasto e
prendi un pesce": conta dove sei, che ora è, che temperatura ha l'acqua, cosa
hai montato e come lo usi.

35 acque fra Los Santos e Blaine County, licenze giornaliere, negozio, casa e
zaino, cassette e portacanne, 51 tornei con premi in denaro, progressione a
110 livelli, un diario con 239 specie da riempire, menu a schermo intero in
stile Rockstar, italiano e inglese, unità metriche o imperiali.

## Il video

[![La mod in gioco](https://img.youtube.com/vi/fE_OSdsgQKs/hqdefault.jpg)](https://www.youtube.com/watch?v=fE_OSdsgQKs)

https://www.youtube.com/watch?v=fE_OSdsgQKs

Questo file è il manuale: spiega come si installa, come si gioca e,
soprattutto, **perché un pesce abbocca o no**. Tutte le regole descritte qui
sono quelle scritte nel codice (`Pesca.cs`); dove un numero è nostro e non del
catalogo di riferimento, è detto. La stessa guida, in forma più breve, si
legge anche in gioco: menu → IMPOSTAZIONI → Guida.

---

## Indice

1. [Cosa serve e installazione](#1-cosa-serve-e-installazione)
2. [I comandi](#2-i-comandi)
3. [Il menu, scheda per scheda](#3-il-menu-scheda-per-scheda)
4. [La giornata di pesca](#4-la-giornata-di-pesca)
5. [L'attrezzatura](#5-lattrezzatura)
6. [Sull'acqua, passo per passo](#6-sullacqua-passo-per-passo)
7. [Perché abbocca: le regole](#7-perché-abbocca-le-regole)
8. [La taglia del pesce](#8-la-taglia-del-pesce)
9. [La robaccia](#9-la-robaccia)
10. [L'HUD](#10-lhud)
11. [Esperienza, livelli, diario e soldi](#11-esperienza-livelli-diario-e-soldi)
12. [I tornei](#12-i-tornei)
13. [Le impostazioni e config.ini](#13-le-impostazioni-e-configini)
14. [Il salvataggio](#14-il-salvataggio)
15. [I dati: cosa è vero e cosa è nostro](#15-i-dati-cosa-è-vero-e-cosa-è-nostro)
16. [Problemi comuni](#16-problemi-comuni)
17. [Note](#17-note)

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

La mod non dipende da altri script e non ne richiede. Se nella stessa
cartella `scripts` girano altre mod, non si disturbano, a patto che non usino
gli stessi tasti (vedi [I comandi](#2-i-comandi)).

### Installazione

1. Scarica il repository (Code → Download ZIP) e scompattalo.
2. Copia la cartella `scripts` dentro la cartella del gioco. Alla fine deve
   esistere:

```
...\Grand Theft Auto V Enhanced\scripts\Attivita\Pesca\Pesca.cs
```

3. Avvia il gioco. Al primo avvio compare in basso "Modulo pesca 1.0 pronto".

La mod legge i suoi file (dati, config, salvataggio) da un percorso fisso,
scritto in testa a `Pesca.cs` (`MY_DIR`, riga 20):
`C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced\scripts\Attivita\Pesca`.
Se il gioco è installato altrove (Steam, Epic, un altro disco) cambia quella
riga col tuo percorso, altrimenti la mod parte ma non trova i dati.

Cosa c'è nella cartella `scripts\Attivita\Pesca`:

| | |
|---|---|
| `Pesca.cs` | tutta la mod, un file solo |
| `config.ini` | ogni impostazione, commentata riga per riga; si rilegge a gioco acceso |
| `*.txt` | i dati: pesci, attrezzatura, zone, licenze, tornei, temperature, orari, rarità, punti caldi, traduzioni, guida, suggerimenti (l'elenco completo è nel [capitolo 15](#15-i-dati-cosa-è-vero-e-cosa-è-nostro)) |
| `img\` | le immagini: pesci, attrezzatura, banner delle zone e dei tornei, HUD, ruota, grafici dell'attività |
| `suoni\` | i suoni della mod (wav): lancio, apertura e chiusura del menu |
| `gen_attivita.py` | lo script (Python 3 + Pillow) che rigenera i grafici dell'attività: serve solo se cambi i dati dei pesci o delle temperature |

Al primo avvio la mod scrive da sola il salvataggio (`stato.txt`) nella sua
cartella. Per ricaricare gli script senza riavviare il gioco: **INS**
(ScriptHookVDotNet). Se qualcosa non compila, l'errore con la riga sta in
`ScriptHookVDotNet.log` nella cartella del gioco.

Attenzione: ScriptHookVDotNet compila **tutti** i `.cs` dentro `scripts`,
sottocartelle comprese. Un file di backup con estensione `.cs` lasciato lì
dentro viene caricato anche lui: i backup vanno tenuti fuori da `scripts` o
rinominati `.cs.old`.

### Lingua e unità

La lingua si sceglie dal menu (IMPOSTAZIONI → Lingua) ed è salvata in
`config.ini` (`lingua=0` inglese, `1` italiano; il repository parte in
inglese). Cambia tutto: menu, HUD, messaggi, guida, suggerimenti, nomi dei
pesci (`pesci_it.txt`), delle zone (`zone_en.txt`), delle esche
(`esche_it.txt`) e dei colori (`colori_it.txt`). I nomi dell'attrezzatura
sono quelli inglesi del catalogo in tutte e due le lingue.

Le unità di misura (IMPOSTAZIONI → Unità, `unita=0` metriche, `1`
imperiali) convertono ovunque chili in libbre, grammi in once, metri in
piedi, centimetri in pollici e gradi Celsius in Fahrenheit. I file dei dati
restano in metrico.

---

## 2. I comandi

La mod è pensata per il pad; la tastiera fa le stesse cose. In basso al
centro dello schermo c'è sempre la **barra dei tasti**, che mostra solo i tasti
utili in quel momento e cambia da sola fra icone del pad e tasti della
tastiera. Subito sopra compaiono i **messaggi** della mod (tutti lì: la mod
non usa le notifiche di GTA sopra il radar).

### Menu

| Pad | Tastiera | |
|---|---|---|
| RB + SINISTRA della croce | F7 (`menu_tasto` in config.ini) | apre e chiude il menu |
| LB / RB | TAB / Q | cambia scheda |
| croce ▲ ▼ | ▲ ▼ | scorre la lista |
| croce ◄ ► | ◄ ► | passa da una colonna all'altra |
| A | INVIO | seleziona / conferma |
| X | SPAZIO | vende (in EQUIPAGGIAMENTO) |
| Y | Y | getta (in EQUIPAGGIAMENTO) |
| B | ESC | indietro / chiude |

Dentro al menu il mondo è fermo: il tempo di gioco non scorre. Col menu
aperto l'audio di GTA va in muto dal mixer di Windows e torna alla chiusura.

### Con la canna in mano (prima del lancio)

| Pad | Tastiera | |
|---|---|---|
| LB (tieni premuto) | TAB | la **ruota degli attrezzi** |
| RT (tieni premuto e molla) | clic sinistro (tieni e molla) | carica e **lancia** |
| croce ◄ ► | ◄ ► | frizione del mulinello |
| croce ▲ ▼ | ▲ / ALT | profondità dell'esca sotto il galleggiante |
| RB | Q | cambia esca (scorre quelle nello zaino) |
| X | SPAZIO | ripone la canna |
| levetta sinistra | WASD | cammini, con la canna in mano |

### Con la lenza in acqua

| Pad | Tastiera | |
|---|---|---|
| RT | clic sinistro | recupera la lenza (e combatte il pesce) |
| A | INVIO | **aggancia il pesce** quando abbocca |
| croce ◄ ► | ◄ ► | frizione |
| LB | TAB | ritira la lenza e apre la ruota |
| levetta sinistra ◄ ► | A / D | sposta la canna: la lenza spazza a destra e a sinistra e si porta dietro il pesce |
| levetta sinistra indietro | S | la **strappata**: un colpo di canna verso di te, non recupera filo (`strappata=0` la spegne) |

### Quando il pesce (o la robaccia) è in mano

| Pad | Tastiera | |
|---|---|---|
| A | INVIO | lo tieni: va nella nassa |
| B | ESC | lo ributti in acqua (la robaccia: la getti via) |

### La ruota degli attrezzi

Mentre peschi, **LB** non apre più la ruota delle armi di GTA: apre la ruota
degli attrezzi. Tieni premuto LB, con la levetta destra scegli lo spicchio,
con ◄ ► della croce scorri i pezzi di quella categoria che hai nello zaino,
lasci LB e **tutte** le scelte fatte si montano insieme. Dodici spicchi:
canna, mulinello, lenza, leader, piombo, pesci del lago, zaino, nassa, amo,
galleggiante, esca, cucchiaino. La prima voce di ogni spicchio è **Vuoto**:
smonta quel pezzo.

Tre spicchi non montano niente ma aprono il menu: **zaino** apre
EQUIPAGGIAMENTO, **nassa** apre EQUIPAGGIAMENTO sulla nassa, **pesci del
lago** apre il DIARIO sul posto dove sei.

Se lasci LB con la canna montata e non hai la canna in mano, la prendi in
mano; se smonti la canna, la riponi.

---

## 3. Il menu, scheda per scheda

Il menu è a schermo intero, con sei schede in alto: **ZONE, EQUIPAGGIAMENTO,
NEGOZIO, DIARIO, TORNEI, IMPOSTAZIONI**. Si apre da qualsiasi posto, anche
mentre peschi. In alto a destra ci sono i soldi, il livello e i punti
esperienza.

### ZONE

A sinistra l'elenco delle 35 acque, con il livello richiesto. Selezionandone
una, a destra c'è la sua scheda: il banner, il nome, l'ora, il meteo con la
temperatura dell'aria e dell'acqua, il grafico dell'attività dei pesci ora
per ora col meteo di adesso, e l'elenco dei pesci che ci vivono. Per ogni
pesce tre caselle, **comune, trofeo, esemplare unico**, che si segnano quando
hai preso quella taglia **in quel posto**. Passando con ► sulla colonna dei pesci e
scorrendo con ▲ ▼ si apre la scheda completa di ognuno: foto, pesi delle tre
taglie, valore, famiglia, esche e artificiali preferiti, misura dell'amo,
quando mangia, temperatura.

In fondo alla scheda della zona ci sono le licenze: **un giorno** e, se
esiste, **tre giorni**, col loro prezzo. A compra la licenza. La riga
**Raggiungi il posto** mette il segnaposto sulla mappa. Quando sei sul posto
con la licenza in tasca compare **Inizia a pescare**; mentre peschi compare **Smetti di pescare**, che
chiude la giornata in anticipo (vedi [La giornata](#4-la-giornata-di-pesca)).

### EQUIPAGGIAMENTO

A sinistra le otto categorie che si spostano: canne, mulinelli, lenze, ami e
terminali, esche, esche artificiali, galleggianti, nasse e fili. Sotto, due
riquadri fissi: il **portacanne** e la **cassetta** che possiedi, con quanto
posto danno (non si spostano: fanno posto ovunque stiano).

A destra tre colonne: **CASA | lista | ZAINO**. In mezzo la lista dei pezzi
della categoria; a sinistra quelli che hai a casa, a destra quelli che hai
nello zaino (con la capienza: "Canne 1/3", "Oggetti 4/10"…). ◄ ► passa da
casa a zaino, **A** sposta il pezzo selezionato dall'altra parte, **X** lo
vende (a metà prezzo, chiede conferma), **Y** lo getta (chiede conferma).
La casa ha spazio illimitato; lo zaino no.

Le **lenze** hanno una riga in più: le bobine tagliate, con i metri che
restano, si vedono sotto la lenza intera e si spostano anche loro con A.

Sotto **nasse e fili** c'è la nassa, una sola alla volta, e mentre peschi
sotto di lei vedi **cosa c'è nella rete**: i pesci presi oggi, coi chili.

Mentre peschi, la casa è chiusa: quello che hai nello zaino è quello che ti
sei portato, non puoi spostare né vendere. Puoi comprare (vedi NEGOZIO).

### NEGOZIO

Tre colonne: la categoria, il tipo (per le canne spinning, casting, da
fondo, feeder…; per le lenze monofilo, fluorocarbon, trecciato, da mare; per i
terminali ami, testine, rig, leader, piombi; e così via) e gli articoli, con
foto, dati, **prezzo** in verde e **livello** richiesto in giallo (rosso se
non ci arrivi). Un **triangolino verde** in basso a sinistra segna gli
articoli che possiedi già, a casa, nello zaino o montati (per tutte le
categorie tranne le esche naturali, che si consumano). A compra: da casa il
pezzo va in casa, mentre peschi va direttamente nello zaino.

Mentre peschi il negozio resta aperto ma **costa il triplo**: è il chiosco
sul posto, comodo e caro. Chi si organizza prima risparmia. Non si vende
mentre si pesca.

### DIARIO

A sinistra le 35 acque; a destra, per quella selezionata, i pesci che hai
preso **lì**: quante volte, il peso record, l'esca e l'amo con cui l'hai
fatto, e la taglia del record (comune, trofeo, unico). È il quaderno della
mod: sapere che il Gudgeon lo prendi a Zancudo all'alba coi vermi rossi, e
che nel canyon c'è una trota che nell'Alamo non trovi.

### TORNEI

A sinistra i 51 tornei, a destra la scheda del torneo selezionato: banner,
pesce, zona, durata, ora d'inizio e meteo, i premi (bronzo, argento, oro, con
i chili da raggiungere e l'extra per trofei e unici), le regole, il tuo record.
In fondo la riga **Iscriviti** col prezzo del biglietto, oppure in rosso il
motivo per cui non puoi (livello, soldi, licenza in tasca…). Sul posto la
riga diventa **Inizia il torneo**; durante il torneo **Ritirati**. Tutto nel
[capitolo 12](#12-i-tornei).

### IMPOSTAZIONI

| voce | cosa fa |
|---|---|
| **Grandezza galleggiante** | quanto grande si vede il galleggiante sull'acqua (salvato nel salvataggio) |
| **Lingua** | italiano / inglese, subito |
| **Unità** | metriche / imperiali, subito |
| **Azzera il diario** | cancella catture, record ed esplorazione; premi due volte |
| **Ricomincia da zero** | cancella tutto: livello, attrezzatura, diario, record dei tornei; premi due volte. I soldi restano: sono quelli di GTA |
| **Guida** | la guida completa in gioco, a capitoli |

---

## 4. La giornata di pesca

### Le zone

Ci sono 35 acque fra Los Santos e Blaine County (`aree_livello.txt`). Ogni
zona della mappa corrisponde a un'acqua vera del simulatore di riferimento e
ne ha i pesci: l'Alamo Sea è un lago americano di pesci gatto e bass, il fiume
Zancudo è un torrente di trote, il mare di Paleto è il fiordo norvegese dei
merluzzi e degli squali, Vespucci e Chumash sono le coste della Florida
(`pesci_aree.txt`).

Ogni acqua ha un **livello**: sotto quel livello non ti fanno entrare. Le acque
grandi (Alamo Sea, Zancudo, Cassidy, Land Act, Paleto) hanno più tratti di
riva, e tratti diversi hanno livelli e pesci diversi. Le ultime otto zone
chiedono i livelli 83, 94 e 100.

### La licenza

La licenza si compra dalla scheda ZONE, da qualsiasi posto: resta in tasca
finché non sei sul posto e premi **Inizia a pescare**. Vale per tutta
l'**acqua**, non per il singolo tratto: paghi "Alamo Sea" e peschi su tutte
le sue rive. Due tagli: **un giorno** e **tre giorni** (2,85 volte un giorno,
il rapporto del simulatore di riferimento). Non ci sono altre restrizioni:
niente licenze "base" o "avanzate", si pesca a qualsiasi ora e si tiene
qualsiasi taglia (con un'eccezione: ai laghetti del golf escono solo i
comuni, `aree_livello.txt`, ultima colonna).

Il prezzo è **nostro** (`licenze.txt`): un giorno costa il 25% di quello che
vale una nassa piena del pesce che paga di più in quel tratto, con la miglior
nassa comprabile a quel livello. Un'acqua con più tratti ha un prezzo per
ogni livello di tratto: paghi la riga più alta che non supera il tuo livello,
cioè paghi per quello che puoi pescare. La licenza è il vero freno del gioco,
non l'attrezzatura.

Per partire servono almeno una **canna**, un **mulinello con la lenza** e
una **nassa** nello zaino. Il resto è affar tuo.

Con una licenza in tasca o in corso non ti puoi iscrivere a un torneo, e col
biglietto di un torneo in tasca non compri la licenza: una cosa alla volta.

### L'orologio

Premuto **Inizia a pescare**, l'orologio di GTA va alle **05:00** e rallenta:
un minuto di gioco ogni 5 secondi veri, quindi una giornata di 24 ore dura
circa **due ore d'orologio**. L'ora del giorno conta per i pesci (vedi le
regole), quindi la giornata ha un suo ritmo: alba, mattina, pomeriggio,
tramonto, notte. Dietro di te compare il **campo**: cassetta, zaino e una
canna di riserva a terra (`campo.txt`).

A fine giornata: l'orologio torna normale, la **nassa si svuota e il pesce si
vende**, tutto quello che avevi montato torna nello zaino, la casa si riapre.
Con la licenza da tre giorni la giornata riparte dalle 5 del mattino
successivo. **Smetti di pescare** (nella scheda ZONE) fa la stessa cosa in
anticipo, e la licenza finisce lì anche se era da tre giorni.

Se chiudi il gioco a metà giornata, tutto è salvato: al prossimo avvio riparti
dalla stessa ora, con la stessa nassa e la stessa licenza.

---

## 5. L'attrezzatura

### Casa, zaino, cassetta e portacanne

Tutto quello che compri sta a casa. Prima di uscire carichi lo zaino dalla
scheda EQUIPAGGIAMENTO. Con la giornata in corso lo zaino è quello che ti sei
portato: quello che hai dimenticato a casa te lo sei dimenticato.

Lo **zaino** ce l'hai da sempre: una canna, un mulinello, due lenze e dieci
**oggetti** (ami e terminali, galleggianti, artificiali, esche). Gli oggetti si
contano **per tipo**, non per pezzo: dieci ami uguali occupano un posto, un
secondo cucchiaino uguale a uno che hai già non prende posto.

Il **portacanne** e la **cassetta** sono fissi: appena li possiedi fanno
posto, che stiano a casa o dietro, e non occupano spazio. Il portacanne
aggiunge canne, mulinelli e lenze (dal HobbyGear da due canne al Rodster XL
da sette canne e sedici lenze, `rodcase.txt`); la cassetta aggiunge oggetti e
lenze (`cassette.txt`). Gli
spazi si sommano a quelli dello zaino. Se ne possiedi più d'uno conta il più
capiente.

La **nassa** va a parte, una sola alla volta.

### Le canne

Ogni canna (`canne.txt`) ha una lunghezza, un **peso di lancio** (i grammi
dell'esca che lancia bene), i chili di lenza che regge e una potenza. Nove
tipi: spinning, casting, da fondo, feeder, match, da carpa, telescopiche, da
mare, spod.

Ogni famiglia di pesci ha le sue canne: la carpa vuole canne da carpa,
feeder, spod o da fondo; lucci, bass e persici spinning e casting; trote e
salmoni spinning, casting o match; il mare canne da mare e casting; abramidi
e pesci piccoli match, feeder o telescopica; i pesci gatto fondo, carpa o
mare; gli storioni fondo e carpa. Con la canna sbagliata il pesce esce meno
(vedi il sorteggio).

### I mulinelli e la frizione

Il mulinello (`mulinelli.txt`) ha una **frizione** in chili, un rapporto di
recupero e una **capacità**: quanti metri di filo ci stanno, secondo il
diametro. Più sottile è la lenza, più metri entrano.

La frizione ha **12 posizioni** (`friz_posizioni`). Tutta chiusa, la lenza non
cede e la tensione sale alla svelta; più la apri, più il pesce si porta via
filo e meno tira sulla lenza. Sull'HUD è l'anello attorno al mulinello.

### Le lenze

Quattro tipi (`lenze.txt`): monofilo, fluorocarbon, trecciato e lenze da mare.
Ogni lenza ha un diametro in millimetri, un **carico** in chili e i metri
della bobina.

La lenza è una **bobina** coi suoi metri. Armandola, il mulinello ne taglia
quanti ne tiene; quello che avanza resta come bobina separata coi metri
rimasti. Smontandola torna una bobina. Le bobine tagliate le vedi in
EQUIPAGGIAMENTO sotto Lenze, con i loro metri, e si spostano fra casa e zaino
come il resto.

Quando la lenza si spezza perdi tre metri (`lenza_persa`), l'amo o
l'artificiale, il piombo, il galleggiante e l'esca. Se avevi un **leader** si
spezza lui, e la lenza madre, il piombo e il galleggiante restano.

### Gli ami e i terminali

Gli ami (`terminali.txt`) vanno dal **#16** (il più piccolo) al **#1**, poi
dal **#1/0** al **#18/0** (il più grande). Ogni pesce ha sul catalogo il suo
intervallo di misure. Nella stessa categoria ci sono le **testine piombate**
(amo e piombo in un pezzo solo, per le esche morbide), i **rig** già
montati, i **leader** (il pezzo di filo prima dell'amo: quello in titanio
serve coi pesci che hanno i denti) e i **piombi**.

Il piombo e il galleggiante da soli non pescano: in punta ci vuole un amo,
una testina, un rig o un artificiale.

Un pesce **coi denti** (la colonna `denti` di `pesci.txt`: lucci, barracuda,
squali…) da **1,2 kg** in su (`denti_kg`), agganciato senza leader, dopo
qualche secondo di lotta (`denti_secondi`, 3,5) la lenza te la trancia e se
ne va. I piccoli si prendono anche col filo nudo.

### I galleggianti

Il galleggiante (`galleggianti.txt`) ha una misura e una **portata**: quanto
piombo regge (leggera, media, pesante). Sull'HUD lo vedi in scala col fondo
vero: se il terminale è più lungo del fondo, il galleggiante si **corica**
sull'acqua. È il segno che l'esca tocca terra.

La profondità dell'esca sotto il galleggiante si regola con la croce, da
**13 cm a 2,5 metri** a passi di 13 cm. Il cucchiaino e il galleggiante non
convivono: a spinning l'artificiale è l'amo.

### Le esche

Cinque famiglie di esche naturali (`esche_negozio.txt`): comuni (pane,
formaggio, mais…), vermi e insetti, fresche, boilies e pellet, da mare. Ogni
esca ha una quantità per confezione, una classe di **peso** (leggero, medio,
pesante: è un'indicazione del catalogo, la mod non la usa nel lancio) e le
misure d'amo con cui si usa.
L'esca **si consuma** a ogni abboccata. Con RB (Q) cambi esca fra quelle che
hai nello zaino senza aprire il menu.

Ogni pesce ha le sue **esche preferite**, quelle della sua pagina sul
catalogo (colonne di `pesci.txt`; `esche_pesci.txt` è la prova, in chiaro):
le vedi nella scheda del pesce, dentro ZONE. Un pesce che sul catalogo non
ha esche preferite abbocca a qualsiasi esca naturale.

### Le esche artificiali

Sei tipi (`artificiali.txt`): cucchiaini, rotanti, minnow e popper, jig da
bass, siliconici, da mare. Ogni artificiale ha un peso, una lunghezza e la
**misura del suo amo**, che conta per la taglia come un amo normale.

I **predatori** (le specie che sul catalogo hanno gli artificiali) con
un'esca naturale abboccano poco e quasi solo piccoli: per loro ci vuole
l'artificiale. L'artificiale vale pieno con una canna da lancio (spinning,
casting, mare); con qualsiasi altra (match, telescopica, feeder, fondo,
carpa, spod) quasi niente.

### Le nasse

La nassa (`nasse.txt`) ha due limiti suoi: il **pesce singolo** più grosso che
ci sta e i **chili in tutto**. Con la nassa piena, o col pesce troppo grosso
per lei, il pesce lo puoi solo ributtare: la scheda del pesce te lo dice. I
fili portapesce sono nasse piccole ed economiche.

### Quanto reggi

Il pesce che riesci a tirare su è il **più debole dei tre pezzi**: la canna
regge fino a tot chili, il mulinello frena fino a tot, la lenza si spezza a
tot. Vince il numero più basso, ed è scritto sull'HUD accanto alla canna. Un
pesce più pesante di così **non ti viene proprio attaccato**.

---

## 6. Sull'acqua, passo per passo

1. **Canna in mano.** Dalla ruota (o col pezzo montato) prendi la canna. Puoi
   camminare lungo la riva con la canna in mano, scegliere la frizione e la
   profondità dell'esca sotto il galleggiante.
2. **Lancio.** Tieni premuto RT: la barra si carica; molla e l'esca parte. La
   barra è **curva**: i primi colpetti fanno pochi metri, gli ultimi valgono
   tanto (`lancio_curva`). Quanto lontano arrivi al massimo lo decidono la
   canna (lunghezza e peso di lancio), il **peso in punta** (dentro il peso di
   lancio vale pieno; troppo leggero fa pochi metri, troppo pesante ne fa
   ancora meno) e la lenza (sottile scorre, grossa frena). Il peso in punta
   è quello di artificiale, testina e piombo: l'esca naturale non pesa. Se l'esca finisce sul prato o
   sull'asfalto la lenza rientra da sola.
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
   compare **A – Aggancia il pesce**. Hai **un secondo e mezzo**: se non
   agganci, "Se n'è andato".
6. **La lotta.** Tieni RT per recuperare. La barra sopra la frizione è la
   **tensione** della lenza: blu, verde, giallo, rosso. Il pesce fa le sue
   corse: si porta via filo, cambia direzione e carica la lenza, tanto più
   quanto è pesante rispetto a quello che reggi; man mano che si stanca il
   raggio si stringe. Se la tensione resta a fondo scala per più di un attimo
   (`lenza_rottura_ms`, 0,7 s) la lenza si **spezza**. Gli ultimi metri sono
   i più duri: sotto i tre metri il pesce vede la riva e si impunta.
7. **I denti.** Un pesce coi denti da 1,2 kg in su, agganciato senza
   leader, dopo qualche secondo la lenza te la trancia e se ne va.
8. **A riva.** Quando i metri sono finiti il pesce è tuo: compare la scheda
   con nome, foto, taglia (comune, trofeo, esemplare unico), peso, valore e
   punti. **A** lo tieni, **B** lo ributti. Se la nassa non lo può prendere,
   la scheda dice perché e resta solo B.

---

## 7. Perché abbocca: le regole

Questa è la parte che fa la differenza fra pescare e premere un tasto. Ogni
volta che lanci succedono, in ordine, tre cose: parte un'**attesa**, alla
scadenza si **sorteggia** un pesce fra quelli del posto, e quel pesce
**guarda l'esca**.

### 7.1 La temperatura dell'acqua

GTA non ha una temperatura: la calcola la mod, ed è mostrata sull'HUD.
Dipende da tre cose:

- l'**ora del gioco**: l'aria fa 13° alle 4 del mattino e 27° alle 16, con una
  curva morbida in mezzo;
- il **meteo**: sole pieno, pioggia, neve spostano di qualche grado;
- la **quota**: sopra i 50 metri l'aria scende di 0,6° ogni 100 metri.

L'acqua è più stabile dell'aria: 16° più il 45% dello scarto dell'aria dai 20°.
Questi numeri sono nostri, non del catalogo.

Ogni specie ha il suo **intervallo di temperatura** (`temperature_pesci.txt`:
minimo, massimo, ottimo). Trote e salmerini stanno bene fra 4 e 16°, carpe e
pesci gatto fra 15 e 28°, i pesci dell'Amazzonia fra 24 e 32°, i merluzzi del
mare del Nord fra 3 e 12°. Sono valori indicativi, presi dalla biologia
generale: il catalogo di riferimento non li ha.

Alla sua temperatura ottima un pesce **vale pieno**; ai bordi del suo
intervallo vale il 40% (`temp_bordo`); oltre 4° fuori dall'intervallo
(`temp_fuori`) **non esce**. Quindi al lago, d'inverno, escono trote e lucci;
d'estate carpe e pesci gatto. Il merluzzo di Paleto non lo prendi con l'acqua
a 25°.

Nella scheda della zona, in ZONE, c'è il grafico dell'attività dei pesci di
quel posto ora per ora, col meteo di adesso, e la riga dell'ora in cui sei.
Lo stesso grafico sta sull'HUD in alto a sinistra mentre peschi.

### 7.2 L'attesa

Quanto aspetti dopo il lancio dipende da **quanto è viva l'acqua adesso**: si
guarda, fra i pesci del posto, quello che sta meglio con la temperatura di
adesso, e l'attesa base (`attesa_base`, 60 secondi) si divide per quel valore.

| l'acqua è | attesa (circa) |
|---|---|
| viva (un pesce alla sua temperatura ottima) | 60–66 secondi |
| così così (i pesci ai bordi del loro intervallo) | 2 minuti e mezzo |
| ferma (nessuno nel suo intervallo) | fino a 6–7 minuti |

La distanza del lancio non conta. L'esca non conta. Recuperare piano
mentre aspetti accorcia un po' l'attesa.

### 7.3 Il sorteggio

Scaduta l'attesa si guarda ogni specie del posto. Alcune vengono **scartate**:

- pesa più di quanto regge la tua attrezzatura;
- non vive in questa zona;
- il tuo amo è troppo lontano dalla sua misura (vedi sotto);
- la tecnica è impossibile (il cucchiaino con una canna che non lo lancia);
- è più di 4° fuori dal suo intervallo di temperatura.

A quelle rimaste si dà un **peso** nel sorteggio, che è il prodotto di:

| fattore | come conta |
|---|---|
| **rarità** (dal catalogo, colonna di `pesci.txt`) | comunissimo 100, comune 55, normale 28, raro 12, rarissimo 5 |
| **misura dell'amo** (dal catalogo) | dentro l'intervallo del pesce 1; una misura fuori 0,55; due fuori 0,25; tre fuori 0,08; oltre non esce |
| **ora del giorno** (abitudine della specie, colonna di `pesci.txt`) | chi mangia di notte: notte 1, alba/tramonto 0,45, pieno giorno 0,12. Chi mangia all'alba e al tramonto: 1 nelle sue ore, 0,35 di giorno, 0,30 di notte. Chi mangia di giorno: 1, 0,45, 0,10. Notte = 21–5, alba/tramonto = 5–8 e 18–21. Chi mangia sempre: 0,75 a ogni ora |
| **tecnica** | il predatore a esca naturale abbocca a 0,25 e quasi solo piccolo; l'artificiale con una canna da lancio (spinning, casting, mare) vale 1, con qualsiasi altra canna 0,15 |
| **canna per la famiglia** | con la canna sbagliata 0,60 se è una match, fondo o telescopica, 0,35 altrimenti |
| **amo per la famiglia** | amo specialista di un'altra famiglia 0,55; amo generico 0,80; amo della sua famiglia 1 |
| **temperatura** | 1 all'ottima, 0,4 ai bordi dell'intervallo |
| **punto caldo** | se l'esca sta sopra il punto di quella specie, × 6 |

Si estrae in proporzione ai pesi. Se nessuna specie resta in gioco (amo, ora,
acqua sbagliati), non succede niente: la lenza resta in acqua e si riprova,
senza avvisi. Se non abbocca mai, la domanda è sempre la stessa: che ora è,
che temperatura ha l'acqua, che amo hai.

### 7.4 L'esca

L'esca **non scarta** nessuno dal sorteggio: decide dopo. Il pesce estratto
guarda cosa c'è sull'amo:

- se è una delle **sue** (la lista della sua pagina sul catalogo) →
  **abbocca sempre**;
- se **non** è la sua → abbocca **una volta su tre** (`esca_sbagliata_abbocca`,
  33 su 100); le altre due se ne va, e tu riaspetti.

Quindi il bluegill col pane lo prendi, ma con i vermi lo prendi tre volte di
più. Vale anche per gli artificiali: un cucchiaino che quel pesce non insegue
lo prende una volta su tre.

Senza niente all'amo non abbocca nessuno.

### 7.5 I punti caldi

Dentro ogni acqua i pesci non stanno sparsi: ci sono punti (una buca, una
sponda, un canneto, un punto al largo) dove una specie sta di casa. Sopra il
punto di quella specie il pesce pesa sei volte tanto nel sorteggio; sui punti
profondi la specie non cambia, ma il sorteggio della taglia si sposta verso
l'alto. I punti sono nostri (`punti_caldi.txt`) e non sono segnati: si
scoprono. Nelle acque grandi e in mare ci sono punti a cento e duecento metri
dalla riva: il centro del lago non è vuoto.

### 7.6 I pesci che passano

Mentre sei in riva, ogni tanto passa un pesce sott'acqua, da solo o in un
gruppetto fino a tre, ognuno con la sua sagoma e la sua nuotata. Sono le
specie di quell'acqua: ti dicono cosa c'è, non cosa abboccherà
(`pesci_scena_*` in config.ini).

### 7.7 Il pescatore di passaggio

Ogni tanto (da 1 a 5 minuti veri) sulla riva, fra 30 e 80 metri da te,
arriva un altro pescatore: sta lì cinque minuti, ogni tanto tira su un
pesce dell'acqua e lo mostra un attimo, poi se ne va. È scena, non
concorrenza: non ti toglie pesci. Si spegne con `pnj_pescatore=0`.

---

## 8. La taglia del pesce

Sorteggiata la specie, si tira il **peso**, fra un minimo e un tetto:

- il minimo è il 60% del peso "comune" del catalogo;
- il tetto lo decide **l'amo** (`amo_taglia`), come nel simulatore di
  riferimento. Ogni pesce ha il suo intervallo di ami: con l'amo alla misura
  **piccola** dell'intervallo escono solo i **comuni**; dalla **metà** in su
  anche i **trofei**; con la misura **grande** anche gli **esemplari unici**.
  Il luccio va da #1/0 a #8/0: da #1/0 a #4/0 comuni, da #5/0 a #7/0
  trofei, con #8/0 unici. L'amo dell'artificiale conta allo stesso modo.
  Fuori dall'intervallo si conta la misura più vicina. Il tetto dei trofei
  è il peso trofeo più il 10% (`trofeo_extra`);
- il tetto non passa mai quello che regge la tua attrezzatura;
- ai laghetti del golf il tetto è il peso comune: lì escono solo i comuni.

Il dado è **truccato verso il basso**: l'amo apre il tetto, non ci porta. Con
la tecnica sbagliata (predatore a esca naturale) escono quasi solo i piccoli;
sul punto profondo il tiro si sposta verso l'alto. L'unico può superare del
20% (`unico_extra`) il peso del catalogo.

Dopo un **colpo grosso** c'è una pausa, per specie, in minuti veri e salvata:
preso un esemplare unico, quella specie non dà altri unici per 20 minuti
(`unico_pausa_min`) né trofei per 5; preso un trofeo, niente trofei per 5
minuti (`trofeo_pausa_min`). L'unico di luccio non blocca l'unico di carpa.

Per le 23 specie che sul catalogo hanno un peso solo, trofeo e unico sono
nostri: 1,9 e 2,9 volte il peso comune (`pesci.txt`, in testa al file).

---

## 9. La robaccia

Siamo a Los Santos: ogni tanto all'amo si attacca una vecchia scarpa, un
sacchetto, un cono stradale, una pianta acquatica, una bottiglia, una lattina,
un copertone, uno sportello d'auto, un barattolo di vernice, una valigetta
(`robaccia.txt`). Valgono da zero a cinque dollari, non danno punti e nella
nassa non ci vanno.

La robaccia esce **solo con l'esca sbagliata**: quando il pesce estratto trova
un'esca che non è la sua e se ne va, una volta su quattro
(`robaccia_prob_esca_sbagliata`, 25 su 100) al suo posto si aggancia la
robaccia. Con l'esca giusta non esce mai. Senza niente all'amo esce una volta
su tre (`robaccia_prob_senza_esca`, 35 su 100).

Quando succede: "Qualcosa ha preso: recupera". La roba segue la lenza
sott'acqua e, tirata su, penzola dalla canna come un pesce; compare la scheda
**RIFIUTI** con quello che hai pescato, il peso e i due soldi che vale.
**A** o **B**: la getti via e incassi.

---

## 10. L'HUD

Mentre peschi, in **alto a sinistra**:

- l'**ora** del gioco e, sotto, quanto manca alla licenza;
- il **livello** e i punti esperienza;
- il **suggerimento**: ogni 5 minuti veri (`sugg_ogni_min`) compare per 30
  secondi (`sugg_dura_sec`) un consiglio sul gioco, a caso e senza ripetersi
  finché non sono usciti tutti (`suggerimenti_it.txt`, `suggerimenti_en.txt`);
- il **posto** dove sei, con la barra dell'**esplorazione** (quante specie
  del posto hai già preso);
- il meteo con la **temperatura dell'aria** e quella dell'**acqua**;
- il grafico dell'**attività** dei pesci del posto ora per ora, con il puntino
  dell'ora di adesso.

In **alto a destra**:

- l'**esca** nel cerchio, col nome, quante ne hai e la misura dell'amo;
- la **nassa**, e sotto i chili che ci sono dentro.

In **basso a destra**, minimo di testo:

- la **canna** con i chili che regge (il più debole dei tre pezzi);
- la **colonna dell'armatura**: lenza, piombo, leader, galleggiante (con la
  portata: leggera, media, pesante), amo, con un riquadro dietro ai pezzi
  montati;
- il **quadrante dell'acqua**: mare, lago o fiume col loro colore e il loro
  fondale (sabbia e coralli, fango con alghe e sassi, ciottoli di fiume); il
  galleggiante in scala col fondo vero, coricato quando l'esca tocca terra;
  sotto, "Esca x m" (la profondità impostata), "Fondo x m" (quando la lenza è
  in acqua) e "Acqua x°";
- la **frizione**: l'anello a 12 tacche col mulinello dentro, la percentuale,
  i chili del pesce e i metri di lenza fuori;
- la **barra della tensione**, a tacche, che si colora da blu a rosso.

In **basso al centro**: la barra dei tasti utili adesso e, sopra, i messaggi
della mod. Quando hai la canna riposta, la barra ricorda i due tasti che
servono: LB per l'armatura, RB + SINISTRA (F7) per il menu.

Tutte le posizioni e le misure si cambiano in `config.ini`.

---

## 11. Esperienza, livelli, diario e soldi

### I punti

Ogni pesce **portato a riva** dà esperienza e finisce nel diario, anche se
poi lo ributti; in nassa, e quindi in vendita, va solo quello che tieni:

| | |
|---|---|
| base | 20 + 15 per chilo |
| quante volte hai già preso quella specie | prima volta × 8, dalla 2ª alla 5ª × 3, dalla 6ª alla 20ª × 1,5, oltre × 1,3 |
| taglia | trofeo × 2, esemplare unico × 3 |

I tre fattori si moltiplicano. Nessuno domina: un pesce nuovo preso piccolo
vale come uno noto preso grosso. Si sale pescando **specie diverse**, non
ripetendo la stessa. L'esca e l'amo non danno punti in più: decidono se e
quanto grosso abbocca, non quanto vale.

### I livelli

Il livello sblocca l'attrezzatura e le zone, coi livelli veri del catalogo.
Quanto ci metti a salire è nostro (`livelli.txt`): circa **tre uscite per
livello**, dall'inizio alla fine, senza muri. I livelli sono **110**: al 100
hai aperto tutte le 35 acque, da lì in poi si sbloccano solo gli ultimi
terminali del negozio.

### Il diario

Quello che prendi finisce nel **diario** (scheda DIARIO): per ogni zona i
pesci che hai preso lì e quante volte, e per ogni specie il record di peso,
l'esca e l'amo con cui l'hai fatto. Nella scheda della zona le tre caselle
comune, trofeo e unico si segnano quando le hai prese **in quel posto**; la
barra dell'esplorazione dice quante specie del posto hai già scoperto.
239 specie da riempire.

### I soldi

I prezzi dell'attrezzatura sono quelli del catalogo **divisi per 10**
(`CAMBIO` nel codice). Il pesce si vende **a fine giornata**, a prezzo pieno
al chilo, quello del catalogo per la sua taglia; i soldi arrivano sul conto
del personaggio. L'attrezzatura si rivende a **metà prezzo** (`vendi_percento`,
50) da casa o dallo zaino, con X in EQUIPAGGIAMENTO, ma non mentre peschi.
Il chiosco sul posto costa il triplo del negozio di casa.

---

## 12. I tornei

51 tornei, presi dalle schede dei singoli tornei del catalogo (`tornei.txt`),
quasi tutti su una specie sola (alcune specie ne hanno più d'uno, in zone
diverse; due tornei contano tutti i pesci del lago): durata, livello minimo, quota d'iscrizione, premi, regola di
punteggio, attrezzatura consigliata, ora d'inizio e meteo sono i loro.
L'unica cosa nostra è la **zona**: il catalogo li tiene su laghi che noi non
abbiamo, quindi ogni torneo sta nella nostra acqua dove quel pesce vive.

Come funziona:

1. Dalla scheda TORNEI, da qualsiasi posto, compri l'**iscrizione**: il
   biglietto resta in tasca. Serve il livello minimo del torneo e non devi
   avere una licenza in tasca o in corso.
2. Sul posto premi **Inizia il torneo**: parte una giornata a sé, con l'ora e
   il cielo del torneo, e i minuti scorrono (30, 45, 60… minuti veri secondo
   il torneo).
3. Contano solo i pesci di **quella specie** messi in nassa (tutti, nei due
   tornei "tutto il lago"): i chili si sommano. **Bronzo, argento e oro** sono le soglie di chili da mettere
   insieme, ognuna col suo premio in dollari; se hai preso almeno un trofeo
   c'è un extra, e un altro se hai preso almeno un esemplare unico. Sotto il
   bronzo non si vince niente.
4. Allo scadere la giornata si chiude: il pesce si vende, il meteo torna
   libero, il premio arriva e il tuo risultato migliore resta come **record**
   del torneo (`tornei_record.txt`). Se premi **Ritirati** la giornata si
   chiude subito, senza premio e senza record.

L'attrezzatura scritta nelle regole del torneo è quella del catalogo, a
titolo di consiglio: la mod non la controlla.

---

## 13. Le impostazioni e config.ini

`scripts\Attivita\Pesca\config.ini` si rilegge **a gioco acceso**: quasi tutto
si cambia senza riavviare. Ogni voce ha il suo commento. Le più utili:

| voce | cosa fa |
|---|---|
| `lingua`, `unita` | 0 inglese / metrico, 1 italiano / imperiale (si cambiano anche dal menu) |
| `menu_tasto` | il tasto della tastiera che apre il menu (F7) |
| `attesa_base`, `attesa_caso` | l'attesa dopo il lancio (ms) e il pezzo a caso |
| `esca_sbagliata_abbocca` | su 100, quante volte abbocca con l'esca non sua (33) |
| `robaccia_prob_esca_sbagliata`, `robaccia_prob_senza_esca` | la robaccia (25, 35) |
| `temp_pesi`, `temp_bordo`, `temp_fuori` | la temperatura nel sorteggio |
| `amo_taglia` | l'amo che decide la taglia (1 acceso) |
| `unico_pausa_min`, `trofeo_pausa_min` | la pausa dopo il colpo grosso (20, 5) |
| `unico_extra` | quanto l'unico può superare il peso del catalogo (%) |
| `trofeo_extra` | quanto il trofeo può superare il peso del catalogo (%) |
| `denti_kg`, `denti_secondi` | da che peso il pesce coi denti senza leader trancia la lenza (1,2) e dopo quanti secondi (3,5) |
| `lenza_persa`, `lenza_rottura_ms` | metri persi alla rottura (3) e quanto deve restare nel rosso la tensione prima che si spezzi (700) |
| `strappata`, `strappo_metri` | la strappata con la levetta indietro (1 accesa) e i metri che recupera (0) |
| `lancio_curva`, `lancio_minimo` | la curva della barra di lancio e i metri minimi |
| `friz_posizioni` | le posizioni della frizione (12) |
| `vendi_percento` | a quanto si rivende l'attrezzatura (50) |
| `pesci_scena_*` | i pesci che passano: ogni quanto, quanti, dove |
| `pnj_*` | il pescatore di passaggio: ogni quanto, a che distanza, per quanto |
| `sugg_*` | i suggerimenti: posizione, ogni quanti minuti, per quanti secondi |
| `canna_disegnata` | 1 canna disegnata che si piega, 0 modello di GTA |
| `menu_*` | misure e caratteri del menu |
| `orario_*`, `liv_*`, `posto_*`, `esplora_y`, `temp_*`, `attivita_*` | l'HUD in alto a sinistra |
| `consigli_*`, `colonna_*`, `esca_*`, `nassa_*`, `friz_*`, `barra_*`, `messaggio_*` | l'HUD in basso e in alto a destra |

Alcune voci (`denti_kg`, `denti_secondi`, `lenza_persa`, `vendi_percento`,
`messaggio_*`) non stanno nel file: la mod usa il valore scritto qui sopra;
per cambiarle basta aggiungere la riga `voce=valore` in `config.ini`.

---

## 14. Il salvataggio

Tutto sta in `scripts\Attivita\Pesca\stato.txt`, scritto dalla mod a ogni
minuto di gioco e a ogni azione importante: livello e punti, casa e zaino,
l'armatura montata, le bobine tagliate, il diario (catture, record, taglie
prese per zona), la licenza in corso con l'ora della giornata e la nassa, il
biglietto o il torneo in corso, le pause dopo i colpi grossi, la grandezza
del galleggiante. I record dei tornei stanno in `tornei_record.txt`.

Quindi puoi chiudere il gioco a metà giornata e riprendere da dove eri. Per
ricominciare: IMPOSTAZIONI → **Azzera il diario** (solo le catture) o
**Ricomincia da zero** (tutto). Cancellare `stato.txt` e `tornei_record.txt`
a gioco spento fa lo stesso.

I soldi non sono nel salvataggio: sono quelli del personaggio di GTA.

---

## 15. I dati: cosa è vero e cosa è nostro

**Dal catalogo di riferimento**: il catalogo intero (pesci con pesi comune,
trofeo e unico, prezzi, esche preferite, artificiali, misura dell'amo,
famiglia, orari e rarità; canne, mulinelli, lenze, ami, terminali, esche
naturali con la loro classe di peso, artificiali, galleggianti, cassette,
nasse, portacanne, con livelli e prezzi), le acque e i loro pesci, i tornei,
il rapporto fra licenza da un giorno e da tre.

**Nostri**, e scritti come tali in testa al file che li contiene:

- la temperatura dell'acqua e gli intervalli per specie (`temperature_pesci.txt`);
- i pesi del sorteggio (rarità, amo, ora, tecnica, canne e ami per famiglia);
- l'attesa e i suoi numeri;
- il "una su tre" dell'esca sbagliata e la robaccia;
- i gradini dell'amo per la taglia (la regola è del simulatore di riferimento, i gradini sono nostri) e le pause;
- i punti caldi (`punti_caldi.txt`);
- la curva dei livelli (`livelli.txt`), la formula dei punti, i prezzi delle licenze (`licenze.txt`), il cambio crediti → dollari;
- le zone dei tornei;
- trofeo e unico delle 23 specie che sul catalogo hanno un peso solo.

In `regole.txt` c'è il promemoria di cosa abbiamo deciso e perché.

I file dei dati, tutti testo con il separatore `|` e l'intestazione in testa:

| file | contenuto |
|---|---|
| `pesci.txt` | le 239 specie: pesi, prezzi, denti, amo, famiglia, esche, artificiali, zone, quando, rarità, predatore |
| `pesci_it.txt`, `esche_it.txt`, `colori_it.txt`, `zone_en.txt` | le traduzioni |
| `pesci_aree.txt`, `aree_livello.txt` | i pesci di ogni zona e il livello di ogni zona |
| `temperature_pesci.txt` | a che temperatura mangia ogni specie |
| `esche_pesci.txt`, `orari_pesci.txt`, `rarita_pesci.txt` | la prova in chiaro di esche, orari e rarità (le stesse colonne di `pesci.txt`; la mod legge `pesci.txt`) |
| `pesci_modello.txt` | che modello di GTA si vede per ogni specie |
| `canne.txt`, `mulinelli.txt`, `lenze.txt`, `terminali.txt`, `galleggianti.txt`, `artificiali.txt`, `esche.txt`, `esche_negozio.txt`, `nasse.txt`, `cassette.txt`, `rodcase.txt` | l'attrezzatura in vendita (`portacanne.txt` è un vecchio elenco nostro, non più letto) |
| `licenze.txt`, `negozi_zona.txt` | prezzi delle licenze, nomi dei chioschi |
| `tornei.txt` | i 51 tornei |
| `punti_caldi.txt`, `acque.txt`, `accessi.txt`, `zone_marcate.txt`, `campo.txt` | punti caldi, la mappa delle acque, gli accessi, il campo |
| `robaccia.txt` | i rifiuti |
| `guida_it.txt`, `guida_en.txt`, `suggerimenti_it.txt`, `suggerimenti_en.txt` | la guida in gioco e i suggerimenti |
| `regole.txt` | il promemoria delle decisioni |

---

## 16. Problemi comuni

**"Modulo pesca pronto" non compare.** ScriptHookV non è aggiornato alla
versione del gioco, o ScriptHookVDotNet non è quello Enhanced. Guarda
`ScriptHookVDotNet.log`.

**Errore di compilazione dopo una modifica.** La riga è nel log. Ricorda che il
compilatore è quello di ScriptHookVDotNet: niente `$"..."`, niente lambda.

**Il menu non si apre con F7.** Un'altra mod usa lo stesso tasto: cambia
`menu_tasto` in config.ini.

**Non abbocca mai.** Nell'ordine: che ora è, che temperatura ha l'acqua (il
grafico dell'attività sull'HUD dice se il posto è vivo), che amo hai (la
scheda del pesce dice la misura), che esca hai. Un posto con l'acqua ferma
aspetta fino a sette minuti.

**Il pesce non si attacca mai / la lenza si spezza subito.** Guarda i chili
accanto alla canna sull'HUD: è il più debole dei tre pezzi. Apri la frizione
durante le corse del pesce.

**Il gioco è muto in cuffia dopo aver usato il menu.** Col menu aperto la mod
mette GTA in muto dal mixer di Windows e lo riattiva alla chiusura e a ogni
avvio. Se resta muto (per esempio dopo un crash col menu aperto), togli il
muto a GTA nel mixer del volume di Windows.

**Voglio ripartire da zero.** IMPOSTAZIONI → Ricomincia da zero, oppure
cancella `stato.txt` a gioco spento.

---

## 17. Note

Progetto personale, senza scopo di lucro, non affiliato a Rockstar Games.
*Grand Theft Auto V* è un marchio di Rockstar Games. I dati dell'attrezzatura
e dei pesci vengono da un catalogo di riferimento pubblico e restano dei
rispettivi proprietari.
