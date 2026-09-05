# Mod Pesca per GTA V Enhanced

Una mod di pesca per Grand Theft Auto V Enhanced, costruita sui dati veri di
**Fishing Planet**: 239 specie con i loro pesi, le loro esche, i loro orari e
la loro rarità, 137 canne, 134 mulinelli, 233 lenze, 381 fra ami, leader, rig
e piombi, 527 esche artificiali, 205 esche naturali. Non è una pesca
"premi un tasto e prendi un pesce": conta cosa hai montato, come lo usi e
dove sei.

35 acque di Los Santos e Blaine County, 51 tornei con premi in denaro, un
sistema di licenze giornaliere, negozio, cassetta, portacanne e progressione
a livelli.

---

## Cosa serve

- **Grand Theft Auto V Enhanced**
- [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/)
- [ScriptHookVDotNet 3.9.0](https://github.com/scripthookvdotnet/scripthookvdotnet/releases)

## Installazione

Copia la cartella `scripts` dentro la cartella del gioco:

```
...\Grand Theft Auto V Enhanced\scripts\
```

Dentro ci trovi:

| | |
|---|---|
| `TrainerPesca.cs` | il menu — si apre con **F7**, o **RB + DESTRA** sul pad |
| `Attivita\Pesca\` | la mod: dati, immagini, suoni e impostazioni |

Al primo avvio la mod si scrive da sola le pagine del menu e il salvataggio.

---

## Come si gioca

### La giornata

Si comincia dal menu: **Zone di pesca** ti fa vedere tutte le acque, con il
livello richiesto, quante specie ci vivono e il prezzo della licenza. Premendo
una zona ti mette il segnaposto sulla mappa.

Arrivato sul posto compri la **licenza** — uno o tre giorni — e la giornata
parte alle 5 del mattino. Un minuto di gioco vale 5 secondi veri, quindi una
giornata di 24 ore dura circa **due ore d'orologio**. Quando finisce si torna
a casa: la nassa si svuota e il pesce si vende.

### L'attrezzatura

**Inventario di casa** è fatto di quattro riquadri:

- al **centro** le categorie, le stesse del negozio
- a **sinistra** quello che hai in casa di quella categoria
- a **destra** quello che ti sei portato dietro
- in **basso a destra**, sull'HUD, l'armatura montata sulla canna

Ci si muove con destra e sinistra fra i riquadri, e ogni tasto fa una cosa
sola:

| | |
|---|---|
| **A** | sposta il pezzo da casa alla borsa e viceversa |
| **X** | monta il pezzo sulla canna, o lo vende se sei a casa |
| **Y** | getta via (due pressioni, che non torna indietro) |

Premendo ancora destra il cursore va **sull'armatura**, sui pezzi disegnati
nell'HUD: lì **X** smonta.

Quello che ti porti dietro è limitato dal **portacanne** (canne, mulinelli e
lenze) e dalla **cassetta** (ami, esche, galleggianti). Il portacanne più
capiente del gioco tiene 7 canne: oltre non si va.

### Montare conta

Montare non è gratis, e questa è la parte che cambia tutto:

- la **lenza** è una bobina coi suoi metri. Armandola, il mulinello ne taglia
  quanti ne tiene: quei metri escono dalla bobina, e quello che avanza resta
  in cassetta come bobina separata. Smontandola torna indietro una bobina coi
  metri che le sono rimasti — non si riattacca a quella di prima.
- **amo** e **galleggiante** escono dalla cassetta: dieci ami meno quello
  montato fanno nove.
- il **cucchiaino** e il galleggiante non convivono: a spinning l'artificiale
  *è* l'amo. Il leader invece ci sta, e col luccio serve davvero.

### Quando si rompe

Se la lenza si spezza perdi tutto quello che stava sotto la rottura: tre metri
di lenza, l'amo, il galleggiante, l'esca. Se avevi un **leader**, si spezza
lui e la lenza madre resta intera: è esattamente perché lo si monta.

E i denti contano. Un predatore sopra il chilo e due, agganciato senza
cavetto, la lenza te la trancia e se ne va. I cuccioli di luccio no, quelli si
prendono anche col filo nudo.

### In acqua

Il grilletto carica il lancio e lo molla; la levetta muove la canna, non il
mulinello. Sull'HUD, sopra la barra, c'è il quadrante dell'acqua: il pelo in
alto, il fondo in basso, e in mezzo quello che stai offrendo.

- a **galleggiante** vedi il galleggiante a galla e l'amo appeso sotto
- a **fondo** solo l'amo con l'esca, che scende
- a **spinning** il cucchiaino, che affonda di testa e risale di testa quando
  recuperi: il tira-e-molla dello stop and go si vede tutto lì

Quando morde, l'esca sussulta. Non c'è nessuna scritta "FERRA!": lo devi
vedere tu.

La canna si piega davvero mentre il pesce tira, e il pesce grosso ti porta la
lenza da una parte all'altra — quanto raggio fa lo decide il suo peso contro
quello che regge la tua attrezzatura, e si stringe man mano che si stanca.

---

## Le regole vere

Con le *regole vere* accese — sono di serie — quale pesce abbocca non è a
caso. Contano:

- l'**esca**: ogni specie ha la sua lista, presa dalla pagina del pesce sul
  wiki. Se quello che offri non è fra le sue, quel pesce non c'è.
- la **misura dell'amo**: fuori misura le probabilità crollano
- l'**ora del giorno**: chi mangia all'alba, chi di notte, chi in pieno giorno
- la **rarità**: dal comunissimo al rarissimo
- il **punto**: sopra una buca l'esca pesca meglio, e i pesci escono più grossi
- l'**equilibrio** dell'attrezzatura: un pesce più pesante di quello che
  reggi non ti viene proprio attaccato

Quello che prendi finisce nel **diario**, con il record di peso, dove l'hai
preso, con che esca e con che amo.

---

## Le impostazioni

`scripts\Attivita\Pesca\config.ini` si rilegge **a ogni fotogramma**: quasi
tutto si può ritoccare a gioco acceso, senza riavviare niente. Ci sono dentro
le posizioni dell'HUD, la grandezza del galleggiante, la velocità con cui
affonda il cucchiaino, quanto si piega la canna, quanti metri perdi quando
strappi, la percentuale di vendita e molto altro. Ogni voce ha il suo commento
sopra.

---

## I dati

Pesi, prezzi, livelli, esche, ami, orari e rarità vengono dal **wiki di
Fishing Planet**. Non è roba inventata a tavolino: dove un valore è stato
deciso da noi — la curva dei livelli, i prezzi delle licenze, il valore degli
XP — è scritto nell'intestazione del file che lo contiene, e in `regole.txt`
c'è il quadro completo di cosa è nostro e cosa no.

I nomi italiani dei pesci, delle esche e dei colori stanno in file di
traduzione separati, così si correggono senza toccare i dati.

---

## Note

Progetto personale, senza scopo di lucro, non affiliato né a Rockstar Games né
a Fishing Planet. *Fishing Planet* è un marchio di Fishing Planet LLC; i dati e
le immagini del catalogo vengono dal loro wiki e restano dei rispettivi
proprietari. *Grand Theft Auto V* è un marchio di Rockstar Games.

La mod è in italiano.
