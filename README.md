# Lapterv — Windows 10/11 telepítős próbaverzió (0.2.0)

Ez a **GitHubra közvetlenül feltölthető forráscsomag**, amelyből a **GitHub Actions saját maga fordítja le** a Windows 64 bites telepítő `.exe` állományt. **Nem szabad csak a ZIP-et feltölteni** a repositoryba: előtte ki kell csomagolni, és a fájlokat/mappákat feltölteni.

## 1. Feltöltés (GitHub weboldalon, programtelepítés nélkül)

1. Nyisd meg: https://github.com/new — jelentkezz be a GitHub-fiókoddal.
2. Repository name: `Lapterv-Windows`. Választhatsz `Private` beállítást; a példaprojektet nem szükséges nyilvánossá tenni. Az induláskor ne kérj automatikusan `README`, `.gitignore` vagy licenc fájlt.
3. Kattints a `Create repository` gombra.
4. A számítógépeden **csomagold ki** a `Lapterv_GitHub_Feltoltheto_0.2.0.zip` ZIP-et. Nyisd meg a kibontott mappát, majd jelöld ki az **összes fájlt és almappát**, beleértve a `.github` mappát is.
5. A GitHubon kattints az `uploading an existing file` hivatkozásra (vagy `Add file` → `Upload files`). **Húzd rá a kibontott tartalmat**, majd `Commit changes` a `main` ágra. Fontos: a `.github/workflows/lapterv-windows.yml` útvonal a repository **gyökerében** legyen, és ne egy `Lapterv_...` nevű plusz almappában.
6. Nyisd meg az `Actions` fület, majd a **Lapterv - Windows telepito automatikus epites** munkafolyamatot. A `main` ágra történő feltöltés után automatikusan indul. Ha a GitHub jóváhagyást kér az Actions használatához, engedélyezd; később a `Run workflow` gombbal is futtathatod.
7. A sikeres, **zöld pipás** futást megnyitva az `Artifacts` résznél töltsd le a `Lapterv-Windows-Telepito-0.2.0` nevű ZIP-et. Kicsomagolás után benne lesz a **`Lapterv_Telepito_0.2.0_x64.exe`** és a hordozható változat.

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
