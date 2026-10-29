# Lapterv — Windows 10/11 telepítős próbaverzió (0.2.0)

Ez a **GitHubra közvetlenül feltölthető forráscsomag**, amelyből a **GitHub Actions saját maga fordítja le** a Windows 64 bites telepítő `.exe` állományt. **Nem szabad csak a ZIP-et feltölteni** a repositoryba: előtte ki kell csomagolni, és a fájlokat/mappákat feltölteni.

## 1. Feltöltés a már meglévő GitHub repositoryba

A projekt előre el van rendezve az egyetlen `GITHUBRA_FELTOLTENDO` mappában.

1. Nyisd meg a [saját Lapterv-Windows repositorydat](https://github.com/kissjaniapp/Lapterv-Windows).
2. A **Code** lapon válaszd az **Add file → Upload files** menüt (üres repositorynál az **uploading an existing file** hivatkozást).
3. A számítógépen nyisd meg a **GITHUBRA_FELTOLTENDO** mappát, nyomj **Ctrl+A**-t, majd a kijelölt fájlokat **és mappákat egyszerre** húzd a GitHub feltöltőterületére. **Egyetlen fájlt sem kell áthelyezni vagy egyesével feltölteni.**
4. Ellenőrizd, hogy `.github/workflows/lapterv-windows.yml` formában szerepel a workflow a repository gyökerében.
5. Kattints a **Commit changes** gombra a **main** ágon.
6. A **GitHub Actions** fülön a fordítás automatikusan elindul. A zöld pipás futás **Artifacts** részéből töltheted le a Windows-telepítőt.

**Ne a ZIP-fájlt töltsd fel:** a kibontott `GITHUBRA_FELTOLTENDO` mappa **tartalma** kerüljön a repository gyökerébe (ne egy újabb `GITHUBRA_FELTOLTENDO` almappába).

## 2. Mi épül?

- `Lapterv_Telepito_0.2.0_x64.exe`: saját, rendszergazdai jogosultság nélkül telepíthető Windows 10/11 (64-bit) próbaváltozat; Start menü és asztali parancsikon.
- `Lapterv_Portable_0.2.0.zip`: hordozható próba telepítés nélkül.
- `SHA256.txt`: a telepítő ellenőrző összege.

A felhasználói felület offline futó HTML alkalmazás, és az Edge vagy Chrome böngészőmotorja nyitja meg alkalmazásablakban (nem teljes Electron program). A használathoz internet nem szükséges. Az EXE digitálisan nincs aláírva, ezért a Windows biztonsági figyelmeztetést adhat. Csak akkor futtasd, ha a fájlban megbízol.

## 3. Későbbi módosítások

Az alkalmazás felülete az `app/index.htm` fájlban található. Ha ezt kicseréled/frissíted, és mented a GitHubon, a `main` ágra történő új mentés **ismét lefuttatja a fordítást**. A telepítő `desktop-go/Build-Windows.cmd` alapján készül, a GitHubon Windows alatt.

## 4. A szabászgépi program exportjának állapota

- A Lapterv már készít gépi **előkészítő** ZIP/JSON adatokat (vágási adatok ellenőrzésére).
- A `machine/xmix.cjs` modul a kapott `.xMix` minta alapján XML-manifestet tud összeállítani **létező** `.xPrg` fájlok hivatkozásaival.
- **Valódi, géppel futtatható `.xPrg` export jelenleg még nincs.** A natív `.xPrg` utasításokat az eredeti mintaprogram nélkül nem lehet biztonságosan előállítani. A gépi próba előtt a gép pontos típusa és valós `.xPrg` mintája szükséges.

## 5. Fájlstruktúra

```
.github/workflows/lapterv-windows.yml    ← GitHub automatikus Windows-fordítás
app/index.htm                            ← a Lapterv teljes felülete
desktop-go/                              ← Windows indító és saját telepítő forrásai
machine/xmix.cjs                         ← xMix-kezelő modul (nem xPrg export)
tests/                                   ← automatizált ellenőrzések
fixtures/eredeti_pelda.xMix              ← eredeti tesztminta
```

A GitHubon a tesztlépésnek **6 sikeres ellenőrzést** kell jeleznie, mielőtt elindul a fordítás. A Windows telepítő valódi felhasználói próbája még hátravan.
