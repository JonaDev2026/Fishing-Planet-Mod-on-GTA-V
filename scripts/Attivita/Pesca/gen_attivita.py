# I GRAFICI DELL'ATTIVITA' DELLA GIORNATA, un PNG per posto e per meteo,
# come le collinette del catalogo ma coi NOSTRI numeri (le stesse regole del
# codice: temperatura dell'acqua ora per ora, intervalli dei pesci,
# ore in cui mangiano). Si lancia da scripts\Attivita\Pesca:
#   python gen_attivita.py
# e scrive img\attivita\<n>_<meteo>.png (n = riga del posto in pesci_aree.txt).
import math, os, sys
from PIL import Image, ImageDraw

BASE = os.path.dirname(os.path.abspath(sys.argv[0]))
D = os.path.join(BASE, 'scripts', 'Attivita', 'Pesca') if os.path.isdir(os.path.join(BASE, 'scripts')) else BASE
OUT = os.path.join(D, 'img', 'attivita')
os.makedirs(OUT, exist_ok=True)

def righe(f):
    for l in open(os.path.join(D, f), encoding='utf-8'):
        l = l.strip()
        if l and not l.startswith('#'): yield l.split('|')

temp = {}
for c in righe('temperature_pesci.txt'):
    temp[c[0].strip()] = (float(c[1]), float(c[2]), float(c[3]))
quando = {}
for c in righe('orari_pesci.txt'):
    quando[c[0].strip()] = c[1].strip() if len(c) > 1 else 'sempre'
aree = [(c[0].strip(), [p.strip() for p in c[1].split(';') if p.strip()]) for c in righe('pesci_aree.txt')]

# i meteo di GTA raggruppati come sul catalogo: quanto spostano l'aria
METEO = {'sole': 4.0, 'sereno': 2.0, 'nuvole': -1.0, 'pioggia': -4.0, 'neve': -12.0}
BORDO, FUORI = 0.4, 4.0

def val_temp(sp, t):
    if sp not in temp: return 1.0
    a, b, o = temp[sp]
    if a <= t <= b:
        meta = (o - a) if t < o else (b - o)
        if meta < 0.5: meta = 0.5
        d = min(1.0, abs(t - o) / meta)
        return 1.0 - (1.0 - BORDO) * d
    oltre = (a - t) if t < a else (t - b)
    if oltre >= FUORI: return 0.0
    return BORDO * (1.0 - oltre / FUORI)

def val_ora(q, hh):
    notte = hh >= 21 or hh < 5
    piena = 8 <= hh < 18
    mezza = not notte and not piena
    if q == 'notte': return 1.0 if notte else (0.45 if mezza else 0.12)
    if q == 'alba_tramonto': return 1.0 if mezza else (0.35 if piena else 0.30)
    if q == 'giorno': return 1.0 if piena else (0.45 if mezza else 0.10)
    return 0.75

def curva(pesci, off):
    v = []
    for h in range(24):
        aria = 20 + 7 * (-math.cos((h + 0.5 - 4) / 24 * 2 * math.pi)) + off
        acqua = 16 + (aria - 20) * 0.45
        # la media dei pesci del posto (il "meglio messo" faceva una riga piatta)
        tot = sum(val_temp(sp, acqua) * val_ora(quando.get(sp, 'sempre'), h) for sp in pesci)
        v.append(tot / len(pesci) if pesci else 0.0)
    top = max(v) or 1.0
    v = [x / top for x in v]
    l = [v[(h + 22) % 24] * 0.15 + v[(h + 23) % 24] * 0.2 + v[h] * 0.3
         + v[(h + 1) % 24] * 0.2 + v[(h + 2) % 24] * 0.15 for h in range(24)]
    return l

def valore(c, ora):
    ora -= 0.5
    while ora < 0: ora += 24
    while ora >= 24: ora -= 24
    h0 = int(ora); u = ora - h0
    w = (1 - math.cos(u * math.pi)) * 0.5
    return c[h0 % 24] * (1 - w) + c[(h0 + 1) % 24] * w

SS = 3                       # si disegna 3 volte piu' grande e si rimpicciolisce: bordi lisci
W, H, DA = 640 * SS, 104 * SS, 5.0     # la riga base sta sul bordo in basso
BASSO = (40, 80, 130)        # blu dell'acqua, come il quadrante
ALTO = (235, 195, 85)        # giallo, dove i pesci mangiano
n = 0
for i, (nome, pesci) in enumerate(aree):
    for m, off in METEO.items():
        c = curva(pesci, off)
        im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        px = im.load()
        ys = []
        for x in range(W):
            v = valore(c, DA + x / W * 24)
            ys.append(H - 1 - v * (H - 3))
        for x in range(W):
            y0 = ys[x]
            for y in range(int(y0), H):
                f = (H - 1 - y) / (H - 1)          # 0 in basso, 1 in alto
                r = int(BASSO[0] + (ALTO[0] - BASSO[0]) * f)
                g = int(BASSO[1] + (ALTO[1] - BASSO[1]) * f)
                b = int(BASSO[2] + (ALTO[2] - BASSO[2]) * f)
                a = int(150 - 60 * (1 - f))
                px[x, y] = (r, g, b, a)
        dr = ImageDraw.Draw(im)
        pts = [(x, ys[x]) for x in range(W)]
        dr.line(pts, fill=(255, 255, 255, 230), width=2 * SS)
        im = im.resize((W // SS, H // SS), Image.LANCZOS)
        im.save(os.path.join(OUT, '%d_%s.png' % (i, m)))
        n += 1
print('scritti', n, 'grafici in', OUT)
