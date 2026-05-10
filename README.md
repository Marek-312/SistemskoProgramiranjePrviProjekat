# Palindrome Server

Web server konzolna aplikacija napisana u C# koja broji palindrome u tekstualnim fajlovima.

## Pokretanje

```bash
dotnet run
```

Server se pokreće na `http://localhost:5050/`

## Korišćenje

U browser upiši:http://localhost:5050/naziv_fajla.txt
Server pretražuje `Files/` folder i sve podfoldere.

## Arhitektura sistema  
### Komponente

- **WebServer** — prima GET zahteve preko HttpListener-a
- **RequestQueue** — deljeni red čekanja između prijema i obrade
- **Cache** — keš sa ograničenjem veličine (FIFO strategija)
- **Logger** — thread-safe logovanje u konzolu i fajl

## Mehanizmi sinhronizacije

| Mehanizam | Gde se koristi |
|---|---|
| `lock` | Zaštita RequestQueue i Cache |
| `Monitor.Wait` | Blokiranje niti dok čekaju na zahtev/rezultat |
| `Monitor.Pulse` | Buđenje worker niti kada stigne zahtev |
| `Monitor.PulseAll` | Buđenje svih niti pri gašenju servera |
| `ThreadPool` | Upravljanje worker nitima |

## Keš

- Strategija: **FIFO** (ograničenje veličine, default 10 unosa)
- Cache stampede zaštita — isti resurs se računa samo jednom
- Thread-safe pristup kroz `lock` i `Monitor`

## Primer odgovora
✅ Fajl 'tekst.txt' sadrzi 3 palindroma.
❌ Fajl 'tekst.txt' ne sadrzi palindrome.
❌ Greska: Fajl 'nepostoji.txt' nije pronadjen!

## Struktura projekta
PalindromeServer/
├── Files/           ← txt fajlovi za pretragu
├── Cache.cs         ← keš sa FIFO strategijom
├── Logger.cs        ← thread-safe logovanje
├── RequestQueue.cs  ← red čekanja zahteva
├── WebServer.cs     ← glavni server
└── Program.cs       ← ulazna tačka