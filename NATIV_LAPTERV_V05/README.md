# Lapterv 0.5 – natív Windows / választható anyagtakarékos guillotine kiosztás

**Ez valódi Windows Forms alkalmazás, nem böngésző vagy XML-alapú kezelőfelület.**
A korábbi **hagyományos sávozás** változatlan választási lehetőség, mellette az **Anyagtakarékos – teljes átvágások** mód is szerepel.

## Egyetlen mappa feltöltése a GitHubra

1. Csomagold ki ezt a ZIP-et. Az egyetlen gyökérmappa neve `NATIV_LAPTERV_V05`.
2. Nyisd meg a saját [kissjaniapp/Lapterv-Windows](https://github.com/kissjaniapp/Lapterv-Windows) repositorydat, majd **Code → Add file → Upload files**.
3. Húzd be **egyszerre a teljes `NATIV_LAPTERV_V05` mappát**. Ne a ZIP-et! Az almappákat a GitHubnak meg kell tartania.
4. **Commit changes** a `main` ágon. A GitHubon beállított `.github/workflows/lapterv-native.yml` automatikusan elkészíti a Windows 64 bites programot és a saját telepítőt. Az **Actions → Lapterv NATIV Windows 0.5** futásból töltheted le az artifact ZIP-et.
5. A sikeres artifactban `Lapterv.exe`, `Lapterv_Telepito_0.5.0_x64.exe`, `Lapterv_Nativ_Portable_0.5.0.zip` és `SHA256.txt` lesz.

A régi `NATIV_LAPTERV_V04` mappa maradhat a repositoryban; az új workflow kizárólag a V05 mappából épít.

## Az új vágási mód pontos működése

- **1. Hagyományos sávozás:** megtartja az előző első-találat (first fit) keresést és a régi kiosztásokat.
- **2. Anyagtakarékos – teljes átvágások:** az alkatrészeket először sávszélesség szerint csoportosítja; az azonos szélességű alkatrészek hosszait többféle best-fit / worst-fit rendezéssel különálló csíkokba rendezi. A csíkokat ezután különböző szélességeik szerint többféle sorrendben egész táblákra helyezi (összesen 12 variáció). Az eredményt összeveti a hagyományossal is, ezért nem fog több teljes táblát felhasználni.
- A keresett cél először **kevesebb teljes tábla**, azonos táblaszám mellett **egyben maradó, nagyobb újrahasználható maradék**. A kiosztás nem bizonyított matematikai optimum, hanem többváltozatos heurisztika.
- A fűrész **teljes Y irányú sávokat vág a teljes tábla élétől a másikig**; csak a sávozás befejezése után kap vissza a kezelőtől egy leválasztott csíkot, amelyet **teljes keresztmetszetben (X)** darabol. **Nincsenek részvágások, zárt belső kivágások vagy szabadon induló vágások.**
- Figyelembe veszi az állítható **szélezést (alapból 10–10 mm)**, a fűrészrést, az alkatrész pontos méretét és darabszámát. Szálirány- és élzárásvédelem miatt az alkatrészeket **nem forgatja el automatikusan**.
- Mindkét terv **ugyanazon alkatrészekből egyszerre elkészül**, a 02 Szabásterv fülön a két terv összehasonlítható és a kezelő választ. A kihozatal és a legnagyobb egybefüggő maradék is látszik.
- A **Maradéklista (CSV)** gombbal táblánként és sávenként kimenthetők a megőrizhető maradékok. Az „újrahasználható” becsült alsó méret 100 × 100 mm; az egyes maradékok tényleges használhatóságát a kezelő dönti el.
- A projekt JSON-fájlban a kiválasztott kiosztási mód is megmarad.

## Biztonság – kísérleti xPrg/xMix

Az `.xPrg` és `.xMix` létrehozása továbbra is **kísérleti**, nem igazoltuk a tényleges gépi mozgást, az XPrg művelettípusok teljes jelentését, az origót, a maradékkezelést vagy a vezérlő által elvárt műveleti sorrendet. **A fájl NEM tekinthető gépre kész programnak.** A kezelőfelület két szakaszban mutatja a vágásokat, de az XPrg vezérlő saját értelmezése nem bizonyított.

Először csak a gyártó *offline szimulátorában* vizsgáld. A maradék anyagra történő fizikai próbavágás is gépi és személyi kockázatot jelent addig, amíg a vágási sorrend és a vezérlés nincs ellenőrizve. Többrétegű vágás nem kerül a `.xPrg` fájlba (csak csíkpárosítási javaslat).

## Mit tartalmaz a natív verzió?

Bútorparaméterek (1000 × 910 × 600 mm), alapértelmezett Feya szín, alkatrészlista, alapgépi beállítások, két választható szabásterv, rajzos táblanézet, gépi sorrend ellenőrzése, maradéklista CSV, JSON-projekt, kísérleti xPrg/xMix. A korábbi HTML-alkalmazás teljes címke/Excel/nesting funkciókészlete **még nincs átültetve** ebbe a natív próbaverzióba.

## Helyi build (Windows 10/11, .NET SDK 8, Go 1.23)

```powershell
cd NATIV_LAPTERV_V05
dotnet run --project tests/Lapterv.SmokeTests.csproj -c Release
dotnet publish NativeApp/LaptervNative.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o dist
New-Item -ItemType Directory -Force installer/assets | Out-Null
Copy-Item dist/Lapterv.exe installer/assets/Lapterv.exe
go build -C installer -trimpath -ldflags="-s -w -H=windowsgui" -o assets/Lapterv_Eltavolitas.exe ./uninstall
go build -C installer -trimpath -ldflags="-s -w -H=windowsgui" -o ../dist/Lapterv_Telepito_0.5.0_x64.exe .
```

*A forrásfájlok statikus ellenőrzése ebben a környezetben megtörtént, de itt nincs .NET Windows fordító; a tényleges WinForms fordítást és futtatást a GitHub Actions Windows runner végzi.*
